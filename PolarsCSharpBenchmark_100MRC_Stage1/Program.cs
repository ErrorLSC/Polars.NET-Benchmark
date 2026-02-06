using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Polars.CSharp;
using static Polars.CSharp.Polars;
using System.Globalization;
using BenchmarkDotNet.Engines;

using DuckDB.NET.Data;

public class DataGenerator
{
    public static void Generate1BRC_Optimized(string path, long totalRows = 1_000_000_000)
    {
        var stations = new[] 
        { 
            "Hamburg", "Bulawayo", "Palma de Mallorca", "St. John's", "Cracow", 
            "Bridgetown", "Winnipeg", "Kingston", "Las Palmas de Gran Canaria", "Helsinki",
            "Abha", "Abidjan", "Abéché", "Accra", "Addis Ababa", "Adelaide", "Aden", "Ahvaz", 
            "Albuquerque", "Alexandra", "Alexandria", "Algiers", "Alice Springs", "Almaty"
        };
        
        int chunkSize = 100_000; 
        long chunks = totalRows / chunkSize;

        Console.WriteLine($"Generating {totalRows} Clean Data...");
        var sw = System.Diagnostics.Stopwatch.StartNew();

        // Queue capacity limit
        using var queue = new System.Collections.Concurrent.BlockingCollection<string[]>(10);

        // Write to disk
        var consumer = Task.Run(() => 
        {
            using var writer = new StreamWriter(path, false, System.Text.Encoding.UTF8, 65536);
            foreach (var block in queue.GetConsumingEnumerable())
            {
                foreach (var line in block) writer.WriteLine(line);
            }
        });

        // Product parallel
        Parallel.For(0, chunks, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }, _ =>
        {
            var rng = Random.Shared;
            var block = new string[chunkSize];
            for (int i = 0; i < chunkSize; i++)
            {
                var station = stations[rng.Next(stations.Length)];
                var temp = (rng.NextDouble() * 100 - 50).ToString("F1");
                block[i] = station + ";" + temp;
            }
            queue.Add(block);
        });

        queue.CompleteAdding();
        consumer.Wait();

        sw.Stop();
        Console.WriteLine($"Data Generation Finished, Time Consumed: {sw.Elapsed.TotalSeconds:F2}s");
    }
    public static void Run1BRCBenchmark(string path)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();

        var lf = LazyFrame.ScanCsv(path, separator: ';', hasHeader: false,tryParseDates:false,inferSchemaLength:5,quoteChar:null)
                .Rename(
                    existing: ["column_1", "column_2"], 
                    newNames: ["station", "measure"]
                )
                .GroupBy(Col("station"))
                .Agg(
                    Col("measure").Min().Alias("min"),
                    Col("measure").Mean().Alias("mean"),
                    Col("measure").Max().Alias("max")
                )
                .Sort("station");

        Console.WriteLine("Lazy Plan Built, Collect now");

        using var res = lf.Collect(useStreaming:true);
        
        var colStation = res["station"];
        var colMin = res["min"];
        var colMean = res["mean"];
        var colMax = res["max"];
        
        long rows = res.Height;
        for (int i = 0; i < rows; i++)
        {
            var s = colStation.GetValue<string>(i); 
            var v1 = colMin.GetValue<double>(i);
            var v2 = colMean.GetValue<double>(i);
            var v3 = colMax.GetValue<double>(i);
        }
        sw.Stop();
        Console.WriteLine($"Polars.NET Time Consumed: {sw.Elapsed.TotalSeconds:F2}s");
    }
    // public static void Main()
    // {
    //     Environment.SetEnvironmentVariable("POLARS_MAX_THREADS", Environment.ProcessorCount.ToString());
    //     string path = "weatherstation.csv";
    //     // Generate1BRC_Optimized(path,100000000);
    //     Run1BRCBenchmark(path);
    // }
}

[SimpleJob(RunStrategy.Monitoring, launchCount: 1, warmupCount: 1, iterationCount: 3)]
[MemoryDiagnoser] 
[RankColumn]
public class CsvParsingGroupByBenchmark
{
    private string _path = "/home/qinglei/Projects/PolarsDotnetBenchmark/PolarsCSharpBenchmark_100MRC_Stage1/weatherstation_100M.csv";
    
    [GlobalSetup]
    public void Setup()
    {
        if (!File.Exists(_path))
        {
            throw new FileNotFoundException($"Please generate data first: {_path}");
        }
    }

    // Polars.NET (Lazy + Streaming)
    [Benchmark(Baseline = true)]
    public void Polars_Lazy()
    {
        var lf = LazyFrame.ScanCsv(_path, separator: ';', hasHeader: false,tryParseDates:false,inferSchemaLength:5,quoteChar:null)
                .Rename(
                    existing: ["column_1", "column_2"], 
                    newNames: ["station", "measure"]
                )
                .GroupBy(Col("station"))
                .Agg(
                    Col("measure").Min().Alias("min"),
                    Col("measure").Mean().Alias("mean"),
                    Col("measure").Max().Alias("max")
                )
                .Sort("station");

        using var result = lf.CollectStreaming();
    }
    // DuckDB.NET
    [Benchmark]
    public void DuckDB_SQL()
    {
        using var connection = new DuckDBConnection("Data Source=:memory:");
        connection.Open();
        
        using var command = connection.CreateCommand();
        
        command.CommandText = $@"
            SELECT 
                station, 
                MIN(measure) as min,
                AVG(measure) as mean,
                MAX(measure) as max
            FROM read_csv(
                '{_path}', 
                header=false, 
                delim=';', 
                quote='',
                columns={{'station': 'VARCHAR', 'measure': 'DOUBLE'}},
                auto_detect=true
            ) 
            GROUP BY station
            ORDER BY station";
            
        using var reader = command.ExecuteReader();
        
        // Consume the result
        while(reader.Read()) 
        {
            var station = reader.GetString(0);
            var min = reader.GetDouble(1);
            var mean = reader.GetDouble(2);
            var max = reader.GetDouble(3);
        }
    }

    // Microsoft.Data.Analysis
    [Benchmark]
    public void Microsoft_Data_Analysis()
    {
        // LoadCsv
        var df = Microsoft.Data.Analysis.DataFrame.LoadCsv(_path, separator: ';', header: false);
        
        // 2GroupBy
        var gb = df.GroupBy("Column0");

        var minDf = gb.Min("Column1");
        var meanDf = gb.Mean("Column1");
        var maxDf = gb.Max("Column1");
    }

    // PLINQ
    [Benchmark]
    public void Native_PLINQ()
    {
        var query = File.ReadLines(_path)
            .AsParallel() 
            .Select(line => 
            {
                var span = line.AsSpan();
                int idx = span.IndexOf(';');
                
                var station = span.Slice(0, idx).ToString(); 
                
                double temp = double.Parse(span.Slice(idx + 1), CultureInfo.InvariantCulture);
                
                return new { Station = station, Temp = temp };
            })
            .GroupBy(x => x.Station)
            .Select(g => new 
            { 
                Station = g.Key, 
                Min = g.Min(x => x.Temp),
                Mean = g.Average(x => x.Temp),
                Max = g.Max(x => x.Temp) 
            })
            .ToList(); 
    }

    public static void Main(string[] args)
    {
        var summary = BenchmarkRunner.Run<CsvParsingGroupByBenchmark>();
    }
}

