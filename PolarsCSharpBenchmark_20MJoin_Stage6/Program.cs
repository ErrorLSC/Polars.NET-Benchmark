using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Microsoft.Data.Analysis;
using Polars.CSharp;
using static Polars.CSharp.Polars;
using DuckDB.NET.Data;

public class Program
{
    public static void Main(string[] args)
    {
        BenchmarkRunner.Run<JoinBenchmark>();
    }
}

[SimpleJob(launchCount: 1, warmupCount: 1, iterationCount: 3)]
[MemoryDiagnoser]
public class JoinBenchmark
{
    private const int OrderCount = 20_000_000; // 2000万订单
    private const int UserCount = 2_000_000;   // 200万用户
    
    // C# 原始数据
    private int[] _orderUserIds;
    private double[] _orderAmounts;
    private int[] _userIds;
    private string[] _userRegions;

    // Polars 数据
    private Polars.CSharp.DataFrame _dfOrders;
    private Polars.CSharp.DataFrame _dfUsers;

    // MDA
    private Microsoft.Data.Analysis.DataFrame _mdaOrders;
    private Microsoft.Data.Analysis.DataFrame _mdaUsers;

    // DuckDB
    private DuckDBConnection _duckConnection;

    [GlobalSetup]
    public void Setup()
    {
        Console.WriteLine("[Setup] Generate Join Data (20M Orders, 2M Users)...");
        var rng = Random.Shared;

        // Generate Users
        _userIds = Enumerable.Range(0, UserCount).ToArray();
        string[] regions = ["US", "EU", "CN", "JP", "UK"];
        _userRegions = new string[UserCount];
        Parallel.For(0, UserCount, i => 
        {
            _userRegions[i] = regions[i % regions.Length];
        });

        // Generate Orders
        _orderUserIds = new int[OrderCount];
        _orderAmounts = new double[OrderCount];
        Parallel.For(0, OrderCount, i =>
        {
            var r = Random.Shared;
            _orderUserIds[i] = r.Next(0, UserCount); 
            _orderAmounts[i] = r.NextDouble() * 100;
        });

        // Polars Loading
        _dfUsers = new Polars.CSharp.DataFrame(
            new Series("user_id", _userIds),
            new Series("region", _userRegions)
        );
        
        _dfOrders = new Polars.CSharp.DataFrame(
            new Series("user_id", _orderUserIds),
            new Series("amount", _orderAmounts)
        );
        
        // MDA
        var mdaOrderUserIds = new PrimitiveDataFrameColumn<int>("user_id", _orderUserIds);
        var mdaOrderAmounts = new PrimitiveDataFrameColumn<double>("amount", _orderAmounts);
        _mdaOrders = new Microsoft.Data.Analysis.DataFrame(mdaOrderUserIds, mdaOrderAmounts);

        var mdaUserIds = new PrimitiveDataFrameColumn<int>("user_id", _userIds);
        var mdaUserRegions = new StringDataFrameColumn("region", _userRegions);
        _mdaUsers = new Microsoft.Data.Analysis.DataFrame(mdaUserIds, mdaUserRegions);
        Console.WriteLine("[Setup] Ready.");

        // ==========================================
        // DuckDB Loading
        // ==========================================
        Console.WriteLine("[Setup] DuckDB Loading...");
        _duckConnection = new DuckDBConnection("Data Source=:memory:");
        _duckConnection.Open();

        // Build Table
        using (var cmd = _duckConnection.CreateCommand())
        {
            // Users (Dimension)
            cmd.CommandText = "CREATE TABLE Users (user_id INTEGER, region VARCHAR)";
            cmd.ExecuteNonQuery();
            
            // Orders (Fact)
            cmd.CommandText = "CREATE TABLE Orders (user_id INTEGER, amount DOUBLE)";
            cmd.ExecuteNonQuery();
        }

        // Write Users
        using (var appender = _duckConnection.CreateAppender("Users"))
        {
            for (int i = 0; i < UserCount; i++)
            {
                var row = appender.CreateRow();
                row.AppendValue(_userIds[i]);
                row.AppendValue(_userRegions[i]);
                row.EndRow();
            }
        }

        // Write Orders
        using (var appender = _duckConnection.CreateAppender("Orders"))
        {
            for (int i = 0; i < OrderCount; i++)
            {
                var row = appender.CreateRow();
                row.AppendValue(_orderUserIds[i]);
                row.AppendValue(_orderAmounts[i]);
                row.EndRow();
            }
        }
        Console.WriteLine("[Setup] DuckDB Ready.");
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _duckConnection.Dispose();
        _mdaOrders = null; 
        _mdaUsers = null;
    }

    // ==========================================
    // Polars.NET
    // ==========================================
    [Benchmark]
    public double Polars_Join()
    {
        var res = _dfOrders.Lazy()
            .Join(
                _dfUsers.Lazy(), 
                leftOn: "user_id", 
                rightOn: "user_id", 
                JoinType.Inner
            )
            .GroupBy("region")
            .Agg(Col("amount").Sum())
            .Select(Col("amount").Sum()) 
            .Collect();

        return (double)res[0, 0]!;
    }

    // ==========================================
    // DuckDB (SQL Hash Join)
    // ==========================================
    [Benchmark(Baseline = true)]
    public double DuckDB_Join()
    {
        using var command = _duckConnection.CreateCommand();

        command.CommandText = @"
            SELECT SUM(regional_total)
            FROM (
                SELECT u.region, SUM(o.amount) as regional_total
                FROM Orders o
                INNER JOIN Users u ON o.user_id = u.user_id
                GROUP BY u.region
            ) sub";

        var result = command.ExecuteScalar();
        return Convert.ToDouble(result);
    }
    // ==========================================
    // LINQ Join
    // ==========================================
    [Benchmark]
    public double Linq_Join()
    {
        var query = Enumerable.Range(0, OrderCount) 
            .Join(
                Enumerable.Range(0, UserCount),     
                oIdx => _orderUserIds[oIdx],        
                uIdx => _userIds[uIdx],             
                (oIdx, uIdx) => new 
                { 
                    Region = _userRegions[uIdx], 
                    Amount = _orderAmounts[oIdx] 
                } 
            )
            .GroupBy(x => x.Region)
            .Select(g => g.Sum(x => x.Amount));

        return query.Sum(); 
    }

    // ==========================================
    // Microsoft.Data.Analysis
    // ==========================================
    [Benchmark]
    public double MDA_Join()
    {
        // Merge
        var joined = _mdaOrders.Merge(_mdaUsers, ["user_id"], ["user_id"]);

        // GroupBy
        var grouped = joined.GroupBy("region");
        
        // Sum
        var resultDf = grouped.Sum("amount");

        double total = 0;
        var amountCol = resultDf["amount"] as PrimitiveDataFrameColumn<double>;
        
        for (long i = 0; i < amountCol!.Length; i++)
        {
            total += amountCol[i] ?? 0;
        }
        
        return total;
    }
}