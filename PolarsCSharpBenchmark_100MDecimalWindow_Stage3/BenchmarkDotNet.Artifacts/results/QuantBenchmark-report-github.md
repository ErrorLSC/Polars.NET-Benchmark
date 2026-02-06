```

BenchmarkDotNet v0.15.8, Linux Fedora Linux 43 (KDE Plasma Desktop Edition)
Intel Core i7-10700 CPU 2.90GHz (Max: 0.80GHz), 1 CPU, 16 logical and 8 physical cores
.NET SDK 10.0.102
  [Host]     : .NET 10.0.2 (10.0.2, 10.0.226.5608), X64 RyuJIT x86-64-v3
  Job-FGEKWY : .NET 10.0.2 (10.0.2, 10.0.226.5608), X64 RyuJIT x86-64-v3

IterationCount=3  LaunchCount=1  WarmupCount=1  

```
| Method             | Mean     | Error     | StdDev   | Ratio | RatioSD | Gen0        | Gen1       | Gen2      | Allocated     | Alloc Ratio |
|------------------- |---------:|----------:|---------:|------:|--------:|------------:|-----------:|----------:|--------------:|------------:|
| Polars_Rolling_MA  | 16.739 s | 16.8846 s | 0.9255 s |  5.00 |    0.24 |           - |          - |         - |       2.73 KB |       0.000 |
| DuckDB_Rolling_MA  |  5.677 s |  0.2891 s | 0.0158 s |  1.70 |    0.01 |           - |          - |         - |       1.16 KB |       0.000 |
| Linq_Rolling_100M  | 28.110 s |  5.1386 s | 0.2817 s |  8.39 |    0.10 |  84000.0000 | 83000.0000 | 6000.0000 | 1288413.37 KB |       0.164 |
| PLinq_Rolling_100M |  3.349 s |  0.5944 s | 0.0326 s |  1.00 |    0.01 | 176000.0000 | 97000.0000 | 1000.0000 | 7869481.27 KB |       1.000 |
