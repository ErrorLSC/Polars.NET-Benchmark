using BenchmarkDotNet.Attributes;
using Polars.CSharp;
using static Polars.CSharp.Polars;
using MiniExcelLibs;
using OfficeOpenXml;
using BenchmarkDotNet.Running;

public class Program
{
    public static void Main(string[] args)
    {
        BenchmarkRunner.Run<ExcelMixedBenchmark>();
    }
}

[SimpleJob(launchCount: 1, warmupCount: 1, iterationCount: 3)]
[MemoryDiagnoser]
public class ExcelMixedBenchmark
{
    private const string FileName = "mixed_million.xlsx";
    private const int Rows = 1_000_000; 
    
    // 10 Columns Double, 10 Columns DateTime
    private const int DoubleCols = 10;
    private const int DateCols = 10;


    // MiniExcel Strong Type DTO
    public class MixedRow
    {
        // Double Columns
        public double N0 { get; set; } public double N1 { get; set; } public double N2 { get; set; } public double N3 { get; set; } public double N4 { get; set; }
        public double N5 { get; set; } public double N6 { get; set; } public double N7 { get; set; } public double N8 { get; set; } public double N9 { get; set; }
        
        // DateTime Columns
        public DateTime D0 { get; set; } public DateTime D1 { get; set; } public DateTime D2 { get; set; } public DateTime D3 { get; set; } public DateTime D4 { get; set; }
        public DateTime D5 { get; set; } public DateTime D6 { get; set; } public DateTime D7 { get; set; } public DateTime D8 { get; set; } public DateTime D9 { get; set; }
    }

    [GlobalSetup]
    public void Setup()
    {
        Console.WriteLine($"[Setup] Generating {Rows} rows Mixed Excel (numeric + datetime)...");

        var seriesList = new List<Series>();

        // Generate Double Columns
        var rawDoubles = Enumerable.Range(0, Rows).Select(i => i * 0.1).ToArray();
        for (int c = 0; c < DoubleCols; c++)
        {
            seriesList.Add(new Series($"N{c}", rawDoubles));
        }

        // Generate DateTime Columns
        var start = new DateTime(2026, 1, 1);
        var rawDates = new DateTime[Rows];
        Parallel.For(0, Rows, i =>
        {
            rawDates[i] = start.AddSeconds(i);
        });

        for (int c = 0; c < DateCols; c++)
        {
            seriesList.Add(new Series($"D{c}", rawDates));
        }

        // Write Excel
        using var df = new DataFrame(seriesList.ToArray());
        df.WriteExcel(FileName);
        
        Console.WriteLine("[Setup] Mixed Excel Generated");
        ExcelPackage.License.SetNonCommercialPersonal("ErrorLSC"); 
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        if (File.Exists(FileName)) File.Delete(FileName);
    }

    // ==========================================
    // Polars.NET
    // ==========================================
    [Benchmark(Baseline = true)]
    public double Polars_Excel()
    {
        double numSum;

        using var df = DataFrame.ReadExcel(FileName);
        
        var res = df.Select(
            (Col("N0").Sum() + Col("N9").Sum()).Alias("NumSum")
        );
        numSum = (double)res["NumSum"][0]!;

        return numSum;
    }

    // ==========================================
    // MiniExcel
    // ==========================================
    [Benchmark]
    public double MiniExcel_ReadExcel()
    {
        double sum = 0;
        long ticks = 0;

        foreach (var row in MiniExcel.Query<MixedRow>(FileName))
        {
            sum += row.N0 + row.N9;
            ticks += row.D0.Ticks + row.D9.Ticks;
        }
        
        return sum + ticks;
    }

    // ==========================================
    // EPPlus
    // ==========================================
    [Benchmark]
    public double EPPlus_ReadExcel()
    {
        double sum = 0;
        long ticks = 0;

        using var package = new ExcelPackage(new FileInfo(FileName));
        var sheet = package.Workbook.Worksheets[0];
        int rowCount = sheet.Dimension.Rows;
        
        for (int i = 2; i <= rowCount; i++) 
        {
            sum += sheet.Cells[i, 1].GetValue<double>(); 
            ticks += sheet.Cells[i, 11].GetValue<DateTime>().Ticks; 
        }

        return sum + ticks;
    }
}