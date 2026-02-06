using BenchmarkDotNet.Attributes;
using static Polars.CSharp.Polars;
using Polars.CSharp;
using BenchmarkDotNet.Running;

public class Program
{
    public static void Main(string[] args)
    {
        BenchmarkRunner.Run<StringUdfBenchmark>();
        BenchmarkRunner.Run<NativeBenchmark>();
    }
}

[MemoryDiagnoser]
public class StringUdfBenchmark
{
    private const int Rows = 1_000_000;
    private DataFrame _df;

    [GlobalSetup]
    public void Setup()
    {
        Console.WriteLine($"Generate {Rows} Rows String Data...");
        // Generate Data: "ID_0_OK", "ID_1_OK" ...
        var rawStrings = Enumerable.Range(0, Rows)
            .Select(i => $"ID_{i}_OK")
            .ToArray();

        _df = new DataFrame(new Series("log", rawStrings));
    }

    // ==========================================
    // Polars.NET
    // ==========================================
    [Benchmark]
    public void PolarsDotNet_StringUdf()
    {
    
        var udfExpr = Col("log")
            .Map<string, int>(str => 
            {
                var parts = str.Split('_'); 
                return int.Parse(parts[1]);
            }, DataType.Int32)
            .Alias("parsed_id");

        using var res = _df.Select(udfExpr);
        
        if (res.Height != Rows) throw new Exception("Rows mismatch");
    }
}

[MemoryDiagnoser]
public class NativeBenchmark
{
    private const int Rows = 10_000_000;
    private DataFrame _df;

    [GlobalSetup]
    public void Setup()
    {
        Console.WriteLine($"[C#] Generate {Rows} Rows Data");
        
        var rng = new Random(42);
        var ids = new int[Rows];
        var vals = new double[Rows];

        Parallel.For(0, Rows, i =>
        {
            ids[i] = i % 100; 
            vals[i] = i * 0.1;
        });

        _df = new DataFrame(
            new Series("id", ids),
            new Series("val", vals)
        );
    }

    // ==========================================
    // Polars.NET 
    // ==========================================
    [Benchmark]
    public void PolarsDotNet_NativeGroupBy()
    {
 
        using var res = _df.Lazy()
            .GroupBy("id")
            .Agg(
                Col("val").Sum().Alias("total")
            )
            .Collect();

        if (res.Height != 100) throw new Exception("Row Mismatch");
    }
}