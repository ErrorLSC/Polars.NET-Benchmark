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
| Polars_Join |    386.4 ms |   155.52 ms |  8.52 ms |  1.35 |    0.03 |           - |           - |         - |       3.45 KB |         2.89 |
| DuckDB_Join |    286.9 ms |    85.11 ms |  4.67 ms |  1.00 |    0.02 |           - |           - |         - |        1.2 KB |         1.00 |
| Linq_Join   |  9,254.0 ms | 1,797.34 ms | 98.52 ms | 32.26 |    0.54 | 102000.0000 | 101000.0000 | 5000.0000 | 1157332.91 KB |   968,226.23 |
| MDA_Join    | 15,346.4 ms | 1,534.58 ms | 84.12 ms | 53.49 |    0.79 | 312000.0000 |  27000.0000 | 4000.0000 | 5838266.01 KB | 4,884,300.97 |
