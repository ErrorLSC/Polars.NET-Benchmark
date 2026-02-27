```

BenchmarkDotNet v0.15.8, Linux Fedora Linux 43 (KDE Plasma Desktop Edition)
Intel Core i7-10700 CPU 2.90GHz (Max: 0.80GHz), 1 CPU, 16 logical and 8 physical cores
.NET SDK 10.0.102
  [Host]     : .NET 10.0.2 (10.0.2, 10.0.226.5608), X64 RyuJIT x86-64-v3
  Job-FGEKWY : .NET 10.0.2 (10.0.2, 10.0.226.5608), X64 RyuJIT x86-64-v3

IterationCount=3  LaunchCount=1  WarmupCount=1  

```
| Method      | Mean        | Error       | StdDev   | Ratio | RatioSD | Gen0        | Gen1        | Gen2      | Allocated     | Alloc Ratio  |
|------------ |------------:|------------:|---------:|------:|--------:|------------:|------------:|----------:|--------------:|-------------:|
| Polars_Join |    705.5 ms | 1,262.76 ms | 69.22 ms |  2.52 |    0.21 |           - |           - |         - |       3.45 KB |         2.89 |
| DuckDB_Join |    279.8 ms |     5.57 ms |  0.31 ms |  1.00 |    0.00 |           - |           - |         - |        1.2 KB |         1.00 |
| Linq_Join   |  9,494.8 ms |    16.18 ms |  0.89 ms | 33.93 |    0.03 | 102000.0000 | 101000.0000 | 5000.0000 | 1157335.44 KB |   968,228.34 |
| MDA_Join    | 15,461.7 ms | 1,275.17 ms | 69.90 ms | 55.25 |    0.22 | 312000.0000 |  27000.0000 | 4000.0000 | 5838266.03 KB | 4,884,300.99 |
