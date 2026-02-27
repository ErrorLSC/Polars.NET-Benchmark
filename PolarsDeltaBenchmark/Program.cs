using static Polars.CSharp.Polars;
using Polars.CSharp;
using static Polars.CSharp.Delta;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using DuckDB.NET.Data;

// public class DeltaPrepare
// {
//     public static void Main()
//     {
//         var url = "https://d37ci6vzurychx.cloudfront.net/trip-data/yellow_tripdata_2025-01.parquet";

//         var lf = LazyFrame.ScanParquet(url);  

//         lf.SinkDelta("./data/ny_taxi_2025_01.delta",mkdir:true); 

//         Console.WriteLine("Data perfectly ingested to Delta Lake via Polars.NET!");
//     }

// }

public class RunBenchmark
{    
    public static void Main(string[] args)
    {
        var summary = BenchmarkRunner.Run<DeltaMutateBenchmarks>();
    }
}

// 让 BenchmarkDotNet 帮我们监控内存分配
[MemoryDiagnoser]
[WarmupCount(5)]       // 增加预热，填满 OS Cache
[IterationCount(20)]   // 固定迭代次数
// 如果你用的是 .NET 8/9，还可以加上 AOT 的测试维度
// [SimpleJob(RuntimeMoniker.NativeAot80)] 
public class DeltaLakeQueryBenchmark
{
    private string _deltaPath = "/home/qinglei/Projects/PolarsDotnetBenchmark/PolarsDeltaBenchmark/data/ny_taxi_2025_01.delta";

    // 1. 全量扫描：测试引擎读取 Delta Log 并扫完所有 Parquet 块的裸 I/O 速度
    [Benchmark(Baseline = true)]
    public void FullScan()
    {
        var lf = LazyFrame.ScanDelta(_deltaPath);
        var df = lf.Collect(); 
    }

    // 2. 谓词下推 (Predicate Pushdown)：这是 Delta / Parquet 的灵魂
    // 引擎应该只去读取包含 1 月 15 日数据的行组 (RowGroups)，跳过其他数据
    [Benchmark]
    public void PredicatePushdown()
    {
        var lf = LazyFrame.ScanDelta(_deltaPath);
        var result = lf.Filter(
            Col("tpep_pickup_datetime") >= Lit(new DateTime(2025, 1, 15)) &
            Col("tpep_pickup_datetime") < Lit(new DateTime(2025, 1, 16))
        ).Collect();
    }

    // 3. 列裁剪 + 聚合 (Projection Pushdown + Aggregation)
    // 应该极其省内存，因为只把 passenger_count 和 total_amount 两列反序列化到内存
    [Benchmark]
    public void GroupByAggregation()
    {
        var lf = LazyFrame.ScanDelta(_deltaPath);
        var result = lf
            .GroupBy("passenger_count")
            .Agg(
                Col("total_amount").Sum().Alias("total_revenue"),
                Col("total_amount").Mean().Alias("avg_fare")
            )
            .Collect();
    }
}

[MemoryDiagnoser]
[WarmupCount(5)]       // 增加预热，填满 OS Cache
[IterationCount(20)]   // 固定迭代次数
public class DuckDbDeltaBenchmark : IDisposable
{
    private readonly DuckDBConnection _connection;
    private readonly string _deltaPath = "/home/qinglei/Projects/PolarsDotnetBenchmark/PolarsDeltaBenchmark/data/ny_taxi_2025_01.delta";

    public DuckDbDeltaBenchmark()
    {
        // 打开内存数据库连接
        _connection = new DuckDBConnection("DataSource=:memory:");
        _connection.Open();

        using var cmd = _connection.CreateCommand();
        // DuckDB 需要安装和加载 delta 插件
        cmd.CommandText = "INSTALL delta; LOAD delta;";
        cmd.ExecuteNonQuery();
    }

    // 1. 全量扫描
    [Benchmark(Baseline = true)]
    public void FullScan()
    {
        using var cmd = _connection.CreateCommand();
        // 注意：为了贴近 Polars 的 Collect() (在底层物化但不一定全量拷贝到托管内存)
        // 我们用 COUNT() 或者直接把数据查出来并丢弃，这里我们遍历 DataReader 模拟数据消费
        cmd.CommandText = $"SELECT * FROM delta_scan('{_deltaPath}')";
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            // 仅仅步进，不取出具体字段，最小化 C# 托管内存分配带来的噪音
        }
    }

    // 2. 谓词下推 (Predicate Pushdown)
    [Benchmark]
    public void PredicatePushdown()
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = $@"
            SELECT * FROM delta_scan('{_deltaPath}') 
            WHERE tpep_pickup_datetime >= '2025-01-15' 
              AND tpep_pickup_datetime < '2025-01-16'";
        
        using var reader = cmd.ExecuteReader();
        while (reader.Read()) { }
    }

    // 3. 列裁剪 + 聚合
    [Benchmark]
    public void GroupByAggregation()
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = $@"
            SELECT passenger_count, 
                   SUM(total_amount) as total_revenue, 
                   AVG(total_amount) as avg_fare 
            FROM delta_scan('{_deltaPath}') 
            GROUP BY passenger_count";
        
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            // 聚合结果很小，我们直接读取出来
            var passengerCount = reader.IsDBNull(0) ? 0 : reader.GetInt64(0);
            var totalRevenue = reader.IsDBNull(1) ? 0 : reader.GetDouble(1);
            var avgFare = reader.IsDBNull(2) ? 0 : reader.GetDouble(2);
        }
    }

    public void Dispose()
    {
        _connection?.Dispose();
    }
}

[MemoryDiagnoser]
[WarmupCount(3)]
[IterationCount(10)] // 因为这些是写操作，稍微耗时一些，迭代次数可以设小一点
public class DeltaMutateBenchmarks
{
    private readonly string _baseDataPath = "/home/qinglei/Projects/PolarsDotnetBenchmark/PolarsDeltaBenchmark/data/ny_taxi_2025_01.delta"; 
    
    private readonly string _deletePath = "/home/qinglei/Projects/PolarsDotnetBenchmark/PolarsDeltaBenchmark/data/bench_delete.delta";
    private readonly string _optimizePath = "/home/qinglei/Projects/PolarsDotnetBenchmark/PolarsDeltaBenchmark/data/bench_optimize.delta";
    private readonly string _mergePath = "/home/qinglei/Projects/PolarsDotnetBenchmark/PolarsDeltaBenchmark/data/bench_merge.delta";
    // 新增 Overwrite 的测试路径
    private readonly string _overwritePath = "/home/qinglei/Projects/PolarsDotnetBenchmark/PolarsDeltaBenchmark/data/bench_overwrite.delta";

    private LazyFrame _mergeSourceLf;

    // ==========================================
    // 1. 全局数据准备 (不计入 Benchmark 耗时)
    // ==========================================
    [GlobalSetup]
    public void GlobalSetup()
    {
        var baseLf = LazyFrame.ScanDelta(_baseDataPath);
        _mergeSourceLf = baseLf.Limit(10000)
            .Unique(
                subset: Selector.Cols(["VendorID", "tpep_pickup_datetime"]), 
                keep: UniqueKeepStrategy.First
            )
            .WithColumns((Col("total_amount") + 5.0).Alias("total_amount")) 
            .Collect().Lazy();
    }

    // ==========================================
    // 2. 迭代前的数据重置 (每次跑 Benchmark 前恢复现场)
    // ==========================================
    [IterationSetup(Target = nameof(DeleteBench))]
    public void SetupDelete()
    {
        CopyDirectory(_baseDataPath, _deletePath);
        Delta.AddFeature(_deletePath, DeltaTableFeatures.DeletionVectors, allowProtocolIncrease: true);
    }

    [IterationSetup(Target = nameof(OptimizeZOrderBench))]
    public void SetupOptimize()
    {
        if (Directory.Exists(_optimizePath)) Directory.Delete(_optimizePath, true);
        
        var baseLf = LazyFrame.ScanDelta(_baseDataPath).Limit(500000);
        
        for (int i = 0; i < 50; i++)
        {
            long offset = i * 10000;
            baseLf.Slice(offset, 10000).SinkDelta(_optimizePath, mkdir: true, mode: DeltaSaveMode.Append);
        }
        Delta.AddFeature(_optimizePath, DeltaTableFeatures.DeletionVectors, allowProtocolIncrease: true);
    }

    [IterationSetup(Target = nameof(MergeBench))]
    public void SetupMerge()
    {
        CopyDirectory(_baseDataPath, _mergePath);
        Delta.AddFeature(_mergePath, DeltaTableFeatures.DeletionVectors, allowProtocolIncrease: true);
    }

    // 新增 Overwrite 的 Setup
    [IterationSetup(Target = nameof(OverwriteBench))]
    public void SetupOverwrite()
    {
        // 覆写测试最好的场景是：目标路径已经有一张表了。
        // 这样可以测试 Delta 引擎处理旧文件（将其标记为 removed）以及写入新事务日志的性能
        
        CopyDirectory(_baseDataPath, _overwritePath);
        Delta.AddFeature(_overwritePath, DeltaTableFeatures.DeletionVectors, allowProtocolIncrease: true);
    }

    // ==========================================
    // 3. 核心 Benchmark 战场
    // ==========================================

    [Benchmark]
    public void DeleteBench()
    {
        Delta.Delete(_deletePath, Col("total_amount") < Lit(0) | Col("passenger_count") == Lit(0));
    }

    [Benchmark]
    public void OptimizeZOrderBench()
    {
        Delta.Optimize(
            path: _optimizePath,
            targetSizeMb: 128L,
            partitionFilters: null, 
            zOrderColumns:  ["VendorID", "tpep_pickup_datetime"], 
            cloudOptions: null
        );
    }

    [Benchmark]
    public void MergeBench()
    {
        _mergeSourceLf.MergeDelta(
            path: _mergePath,
            mergeKeys: ["VendorID", "tpep_pickup_datetime"],
            matchedUpdateCond: Source("total_amount") > Target("total_amount") | Target("total_amount").IsNull(),
            matchedDeleteCond: Source("total_amount") < 0.0,
            notMatchedInsertCond: Source("passenger_count") > 0 & Source("total_amount") > 0.0,
            notMatchedBySourceDeleteCond: Target("total_amount") <= 0.01,
            canEvolve: false
        );
    }

    // 新增的 Overwrite Benchmark
    [Benchmark]
    public void OverwriteBench()
    {
        // 测试点：覆写整张表。底层不仅要写新的 Parquet，还要更新 _delta_log，把旧数据全部 Tombstone 掉。
        // 注：如果 C# 端的 mode 采用的是强类型 Enum，请改为类似 mode: DeltaWriteMode.Overwrite
        LazyFrame.ScanDelta(_baseDataPath)
            .WithColumns((Col("total_amount") * 0.9).Alias("total_amount"))
            .SinkDelta(_overwritePath, mode: DeltaSaveMode.Overwrite);
    }

    // --- 辅助方法 ---
    private static void CopyDirectory(string sourceDir, string destinationDir)
    {
        if (Directory.Exists(destinationDir)) Directory.Delete(destinationDir, true);
        Directory.CreateDirectory(destinationDir);
        foreach (var file in Directory.GetFiles(sourceDir))
            File.Copy(file, Path.Combine(destinationDir, Path.GetFileName(file)));
        foreach (var dir in Directory.GetDirectories(sourceDir))
            CopyDirectory(dir, Path.Combine(destinationDir, Path.GetFileName(dir)));
    }
}