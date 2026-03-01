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
    public static void Main()
    {
        BenchmarkRunner.Run<DeltaLakeQueryBenchmark>();
    }
}

[MemoryDiagnoser]
[WarmupCount(5)]       
[IterationCount(20)]  
public class DeltaLakeQueryBenchmark
{
    private string _deltaPath = "/home/qinglei/Projects/PolarsDotnetBenchmark/PolarsDeltaBenchmark/data/ny_taxi_2025_01.delta";

    [Benchmark(Baseline = true)]
    public void FullScan()
    {
        var lf = LazyFrame.ScanDelta(_deltaPath);
        var df = lf.Collect(useStreaming:true); 
    }


    [Benchmark]
    public void PredicatePushdown()
    {
        var lf = LazyFrame.ScanDelta(_deltaPath);
        var result = lf.Filter(
            Col("tpep_pickup_datetime") >= Lit(new DateTime(2025, 1, 15)) &
            Col("tpep_pickup_datetime") < Lit(new DateTime(2025, 1, 16))
        ).Collect(useStreaming:true);
    }


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
            .Collect(useStreaming:true);
    }
}

[MemoryDiagnoser]
[WarmupCount(5)]      
[IterationCount(20)]   
public class DuckDbDeltaBenchmark : IDisposable
{
    private readonly DuckDBConnection _connection;
    private readonly string _deltaPath = "/home/qinglei/Projects/PolarsDotnetBenchmark/PolarsDeltaBenchmark/data/ny_taxi_2025_01.delta";

    public DuckDbDeltaBenchmark()
    {

        _connection = new DuckDBConnection("DataSource=:memory:");
        _connection.Open();

        using var cmd = _connection.CreateCommand();

        cmd.CommandText = "INSTALL delta; LOAD delta;";
        cmd.ExecuteNonQuery();
    }


    [Benchmark(Baseline = true)]
    public void FullScan()
    {
        using var cmd = _connection.CreateCommand();

        cmd.CommandText = $"SELECT * FROM delta_scan('{_deltaPath}')";
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            
        }
    }

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
[IterationCount(10)]
public class DeltaMutateBenchmarks
{
    private readonly string _baseDataPath = "/home/qinglei/Projects/PolarsDotnetBenchmark/PolarsDeltaBenchmark/data/ny_taxi_2025_01.delta"; 
    
    private readonly string _deletePath = "/home/qinglei/Projects/PolarsDotnetBenchmark/PolarsDeltaBenchmark/data/bench_delete.delta";
    private readonly string _optimizePath = "/home/qinglei/Projects/PolarsDotnetBenchmark/PolarsDeltaBenchmark/data/bench_optimize.delta";
    private readonly string _mergePath = "/home/qinglei/Projects/PolarsDotnetBenchmark/PolarsDeltaBenchmark/data/bench_merge.delta";
    private readonly string _overwritePath = "/home/qinglei/Projects/PolarsDotnetBenchmark/PolarsDeltaBenchmark/data/bench_overwrite.delta";
    private readonly string _checksumCsvPath = "/home/qinglei/Projects/PolarsDotnetBenchmark/PolarsDeltaBenchmark/benchmark_checksums_polars.csv";
    private LazyFrame _mergeSourceLf;

    private readonly HashSet<string> _printedChecksums = [];

    // ==========================================
    // Data Prepare
    // ==========================================
    [GlobalSetup]
    public void GlobalSetup()
    {
        var baseLf = LazyFrame.ScanDelta(_baseDataPath);
        _mergeSourceLf = LazyFrame.ScanDelta("/home/qinglei/Projects/PolarsDotnetBenchmark/PolarsDeltaBenchmark/data/bench_source.delta");

        PrintChecksum("BaseData", _baseDataPath);
    }

    // ==========================================
    // Setup
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

    [IterationSetup(Target = nameof(OverwriteBench))]
    public void SetupOverwrite()
    {
        CopyDirectory(_baseDataPath, _overwritePath);
        Delta.AddFeature(_overwritePath, DeltaTableFeatures.DeletionVectors, allowProtocolIncrease: true);
    }

    // ==========================================
    // Checksum
    // ==========================================
    
    [IterationCleanup(Target = nameof(DeleteBench))]
    public void CleanupDelete() => PrintChecksum("DeleteBench", _deletePath);

    [IterationCleanup(Target = nameof(OptimizeZOrderBench))]
    public void CleanupOptimize() => PrintChecksum("OptimizeZOrderBench", _optimizePath);

    [IterationCleanup(Target = nameof(MergeBench))]
    public void CleanupMerge() => PrintChecksum("MergeBench", _mergePath);

    [IterationCleanup(Target = nameof(OverwriteBench))]
    public void CleanupOverwrite() => PrintChecksum("OverwriteBench", _overwritePath);

    // ==========================================
    // Benchmark
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
        var updateCond = (Delta.Source("total_amount") > Delta.Target("total_amount") | Delta.Target("total_amount").IsNull()) 
                         & Delta.Source("total_amount") >= 10.0;
                         
        var deleteCond = Delta.Source("total_amount") < 50.0; 
        
        var insertCond = Delta.Source("passenger_count") > 0 & Delta.Source("total_amount") > 0.0;
        var srcDeleteCond = Delta.Target("total_amount") <= 0.01;

        _mergeSourceLf.MergeDeltaOrdered(
                path: _mergePath,
                mergeKeys: ["VendorID", "tpep_pickup_datetime"],
                canEvolve: false
            )
            .WhenMatchedDelete(deleteCond)
            
            .WhenMatchedUpdate(updateCond)
            
            .WhenNotMatchedInsert(insertCond)
            
            .WhenNotMatchedBySourceDelete(srcDeleteCond)
            
            .Execute(); 
    }

    [Benchmark]
    public void OverwriteBench()
    {
        LazyFrame.ScanDelta(_baseDataPath)
            .WithColumns((Col("total_amount") * 0.9).Alias("total_amount"))
            .SinkDelta(_overwritePath, mode: DeltaSaveMode.Overwrite);
    }

    // ==========================================
    // Helpers
    // ==========================================

    private void PrintChecksum(string benchName, string path)
    {
        if (!_printedChecksums.Add(benchName)) return;

        bool writeHeader = !File.Exists(_checksumCsvPath);

        try
        {
            using var df = LazyFrame.ScanDelta(path)
                .Select(Col("total_amount").Sum())
                .Collect();

            var sumValue = df["total_amount"].GetValue<double>(0); 

            Console.WriteLine($"\n[CHECKSUM VERIFIED] {benchName,-20} | Total Amount Sum: {sumValue:F4}\n");

            using (var writer = new StreamWriter(_checksumCsvPath, append: true))
            {
                if (writeHeader)
                {
                    writer.WriteLine("BenchmarkName,TotalAmountSum,Status"); 
                }
                writer.WriteLine($"{benchName},{sumValue:F4},SUCCESS");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n[CHECKSUM ERROR] {benchName} | Failed to compute checksum: {ex.Message}\n");
            
            using (var writer = new StreamWriter(_checksumCsvPath, append: true))
            {
                if (writeHeader)
                {
                    writer.WriteLine("BenchmarkName,TotalAmountSum,Status");
                }
                writer.WriteLine($"{benchName},0.0000,ERROR: {ex.Message.Replace(",", ";")}"); 
            }
        }
    }

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