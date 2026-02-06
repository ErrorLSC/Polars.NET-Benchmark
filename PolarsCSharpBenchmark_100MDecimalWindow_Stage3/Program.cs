using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using DuckDB.NET.Data;
using Polars.CSharp;
using static Polars.CSharp.Polars;


[SimpleJob(launchCount: 1, warmupCount: 1, iterationCount: 3)]
[MemoryDiagnoser]
public class QuantBenchmark
{
    private const int Rows = 10_000_0000; 
    private const int TickerCount = 5000; 

    private string[] _tickers;
    private decimal[] _prices;
    
    // Polars
    private DataFrame _df;

    // DuckDB
    private DuckDBConnection _duckConnection;

    [GlobalSetup]
    public void Setup()
    {
        Console.WriteLine($"[Setup] Minting {Rows} Trades for {TickerCount} Tickers...");
        
        _tickers = new string[Rows];
        _prices = new decimal[Rows];
        var tickerNames = Enumerable.Range(0, TickerCount).Select(i => $"TKR_{i:0000}").ToArray();

        Parallel.For(0, Rows, i =>
        {
            var rng = Random.Shared;
            _tickers[i] = tickerNames[rng.Next(TickerCount)];
            _prices[i] = rng.Next(1000000, 11000000) / 10000m;
        });

        // Polars Ingestion
        Console.WriteLine("[Setup] Polars Loading...");
        _df = new DataFrame(
            new Series("Ticker", _tickers),
            new Series("Price", _prices)
        );
        Console.WriteLine("[Setup] Ready.");

        // --- DuckDB Init ---
        _duckConnection = new DuckDBConnection("Data Source=:memory:");
        _duckConnection.Open();

        using (var cmd = _duckConnection.CreateCommand())
        {
            cmd.CommandText = "CREATE TABLE Stocks (Ticker VARCHAR, Price DECIMAL(18,4))";
            cmd.ExecuteNonQuery();
        }

        using (var appender = _duckConnection.CreateAppender("Stocks"))
        {
            for (int i = 0; i < Rows; i++) 
            {
                var row = appender.CreateRow();
                
                row.AppendValue(_tickers[i]); 
                
                row.AppendValue(_prices[i]);  
                
                row.EndRow();
            }
        }
        Console.WriteLine("[Setup] Ready.");
    
    }

    // ==========================================
    // Polars.NET
    // ==========================================
    [Benchmark]
    public double Polars_Rolling_MA()
    {
        var res = _df.Lazy() 
            .Sort("Ticker")  
            .Select(
                Col("Ticker"),
                Col("Price"),
                Col("Price")
                    .RollingMean(windowSize: "20i", minPeriods: 1)
                    .Over("Ticker") 
                    .Alias("MA20")
            )
            .Select(Col("MA20").Last()) 
            .Collect(useStreaming:true);

        double val = res.GetValue<double>("MA20",0);
        
        return val;
    }

    // ==========================================
    // DuckDB
    // ==========================================
    [Benchmark]
    public double DuckDB_Rolling_MA()
    {
        using var command = _duckConnection.CreateCommand();

        command.CommandText = @"
            SELECT 
                AVG(Price) OVER (
                    PARTITION BY Ticker 
                    ROWS BETWEEN 19 PRECEDING AND CURRENT ROW
                ) as MA20
            FROM Stocks
            ORDER BY Ticker DESC
            LIMIT 1";

        var result = command.ExecuteScalar();
        
        return Convert.ToDouble(result);
    }

    // ==========================================
    // LINQ
    // ==========================================
    [Benchmark]
    public decimal Linq_Rolling_100M()
    {
        return Enumerable.Range(0, Rows)
            .GroupBy(i => _tickers[i])
            .SelectMany(g => 
            {
                var queue = new Queue<decimal>(20);
                decimal sum = 0;
                return g.Select(i => 
                {
                    decimal p = _prices[i];
                    queue.Enqueue(p);
                    sum += p;
                    if (queue.Count > 20) sum -= queue.Dequeue();
                    return sum / queue.Count;
                });
            })
            .Last();
    }

    // ==========================================
    // PLINQ
    // ==========================================
    [Benchmark(Baseline = true)]
    public decimal PLinq_Rolling_100M()
    {
        return Enumerable.Range(0, Rows)
            .AsParallel()
            .WithDegreeOfParallelism(Environment.ProcessorCount)
            .GroupBy(i => _tickers[i])
            .SelectMany(g => 
            {
                var queue = new Queue<decimal>(20);
                decimal sum = 0;

                var results = new List<decimal>(); 
                foreach(var i in g)
                {
                    decimal p = _prices[i];
                    queue.Enqueue(p);
                    sum += p;
                    if (queue.Count > 20) sum -= queue.Dequeue();
                    results.Add(sum / queue.Count);
                }
                return results;
            })
            .Last();
    }
}

public class Program
{
    public static void Main(string[] args)
    {
        BenchmarkRunner.Run<QuantBenchmark>();
    }
}