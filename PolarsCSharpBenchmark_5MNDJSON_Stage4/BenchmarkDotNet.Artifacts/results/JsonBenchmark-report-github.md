```

BenchmarkDotNet v0.15.8, Linux Fedora Linux 43 (KDE Plasma Desktop Edition)
Intel Core i7-10700 CPU 2.90GHz (Max: 0.80GHz), 1 CPU, 16 logical and 8 physical cores
.NET SDK 10.0.102
  [Host]     : .NET 10.0.2 (10.0.2, 10.0.226.5608), X64 RyuJIT x86-64-v3
  Job-FGEKWY : .NET 10.0.2 (10.0.2, 10.0.226.5608), X64 RyuJIT x86-64-v3

IterationCount=3  LaunchCount=1  WarmupCount=1  

```
| Method                 | Mean        | Error      | StdDev    | Ratio | RatioSD | Gen0         | Gen1        | Allocated      | Alloc Ratio   |
|----------------------- |------------:|-----------:|----------:|------:|--------:|-------------:|------------:|---------------:|--------------:|
| DuckDB_ScanNdjson      |    764.1 ms |   316.3 ms |  17.34 ms |  1.00 |    0.03 |            - |           - |        2.73 KB |          1.00 |
| Polars_ScanNdjson      |  2,279.5 ms |   885.9 ms |  48.56 ms |  2.98 |    0.08 |            - |           - |         3.6 KB |          1.32 |
| SystemTextJson_Process |  7,267.9 ms |   457.3 ms |  25.07 ms |  9.52 |    0.19 |  674000.0000 |   2000.0000 |  5510787.13 KB |  2,021,148.29 |
| Newtonsoft_Dynamic     | 25,970.3 ms | 5,027.7 ms | 275.58 ms | 34.00 |    0.74 | 5237000.0000 | 132000.0000 | 42776424.27 KB | 15,688,774.52 |
