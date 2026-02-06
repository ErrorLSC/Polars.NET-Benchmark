using BenchmarkDotNet.Attributes;
using Polars.CSharp;
using static Polars.CSharp.Polars;
using System.Text.Json.Serialization;
using Newtonsoft.Json.Linq;
using BenchmarkDotNet.Running;
using DuckDB.NET.Data;

public class Program
{
    public static void Main(string[] args)
    {
        BenchmarkRunner.Run<JsonBenchmark>();
    }
}

[SimpleJob(launchCount: 1, warmupCount: 1, iterationCount: 3)]
[MemoryDiagnoser]
public class JsonBenchmark
{
    private const int Rows = 5_000_000; // 5M rows
    private const string FileName = "logs_large.json";

    // Preset POCO Class for System.Text.Json
    public class LogEntry
    {
        [JsonPropertyName("ts")] public DateTime Timestamp { get; set; }
        [JsonPropertyName("level")] public string Level { get; set; }
        [JsonPropertyName("req")] public RequestInfo Req { get; set; }
    }
    public class RequestInfo
    {
        [JsonPropertyName("url")] public string Url { get; set; }
        [JsonPropertyName("method")] public string Method { get; set; }
    }

    [GlobalSetup]
    public void Setup()
    {
        Console.WriteLine($"[Setup] Generate {Rows} rows NDJSON data");
        using var sw = new StreamWriter(FileName);
        
        var methods = new[] { "GET", "POST", "PUT", "DELETE" };
        var levels = new[] { "INFO", "INFO", "INFO", "WARN", "ERROR" }; 
        var urls = Enumerable.Range(0, 500).Select(i => $"/api/v1/resource/{i}").ToArray();

        var rng = Random.Shared;
        for (int i = 0; i < Rows; i++)
        {
            var log = new
            {
                ts = DateTime.UtcNow,
                level = levels[rng.Next(levels.Length)],
                req = new
                {
                    method = methods[rng.Next(methods.Length)],
                    url = urls[rng.Next(urls.Length)],
                    headers = new[] { "Content-Type: json", "User-Agent: bot" }
                },
                user = new
                {
                    id = i,
                    meta = new { tags = new[] { "a", "b" }, loc = "US" }
                }
            };
            sw.WriteLine(System.Text.Json.JsonSerializer.Serialize(log));
        }
        Console.WriteLine("[Setup] JSON Generation Compeleted");
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        if (File.Exists(FileName)) File.Delete(FileName);
    }
    // ==========================================
    // DuckDB.NET
    // ==========================================
    [Benchmark(Baseline = true)]
    public long DuckDB_ScanNdjson()
    {
        using var connection = new DuckDBConnection("Data Source=:memory:");
        connection.Open();
        
        using var command = connection.CreateCommand();
        
        command.CommandText = $@"
            SELECT COUNT(*) 
            FROM read_json_auto('{FileName}', format='newline_delimited')
            WHERE level = 'ERROR' 
              AND length(req.url) > 0"; 

        var result = command.ExecuteScalar();
        return Convert.ToInt64(result);
    }

    // ==========================================
    // Polars.NET
    // ==========================================
    [Benchmark]
    public long Polars_ScanNdjson()
    {
        var q = LazyFrame.ScanNdjson(FileName)
            .Filter(Col("level") == "ERROR")
        .Filter(Col("req").Struct.Field("url").Str.Len() > 0)
        .Select(Col("level").Count());
        var res = q.Collect();
        return (uint)res["level"][0]!;
    }

    // ==========================================
    // System.Text.Json
    // ==========================================
    [Benchmark]
    public int SystemTextJson_Process()
    {
        int errorCount = 0;
        foreach (var line in File.ReadLines(FileName))
        {
            var entry = System.Text.Json.JsonSerializer.Deserialize<LogEntry>(line);
            
            if (entry.Level == "ERROR")
            {
                if (entry.Req.Url.Length > 0) errorCount++;
            }
        }
        return errorCount;
    }

    // ==========================================
    // Newtonsoft.Json
    // ==========================================
    [Benchmark]
    public int Newtonsoft_Dynamic()
    {
        int errorCount = 0;
        using var sr = new StreamReader(FileName);
        string line;
        while ((line = sr.ReadLine()) != null)
        {
            var json = JObject.Parse(line);
            if (json["level"]?.ToString() == "ERROR")
            {
                if (json["req"]?["url"]?.ToString().Length > 0) errorCount++;
            }
        }
        return errorCount;
    }

 
}