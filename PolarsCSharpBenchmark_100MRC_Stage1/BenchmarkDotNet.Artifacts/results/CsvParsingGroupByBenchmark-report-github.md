```

BenchmarkDotNet v0.15.8, Linux Fedora Linux 43 (KDE Plasma Desktop Edition)
Intel Core i7-10700 CPU 2.90GHz (Max: 0.80GHz), 1 CPU, 16 logical and 8 physical cores
.NET SDK 10.0.102
  [Host]     : .NET 10.0.2 (10.0.2, 10.0.226.5608), X64 RyuJIT x86-64-v3
  Job-MYSLML : .NET 10.0.2 (10.0.2, 10.0.226.5608), X64 RyuJIT x86-64-v3

IterationCount=3  LaunchCount=1  RunStrategy=Monitoring  
WarmupCount=1  

```
| Method                  | Mean      | Error     | StdDev   | Ratio  | RatioSD | Rank | Gen0          | Gen1          | Gen2       | Allocated       | Alloc Ratio    |
|------------------------ |----------:|----------:|---------:|-------:|--------:|-----:|--------------:|--------------:|-----------:|----------------:|---------------:|
| Polars_Lazy             |   1.688 s |  0.7931 s | 0.0435 s |   1.00 |    0.03 |    1 |             - |             - |          - |         2.41 KB |           1.00 |
| DuckDB_SQL              |   2.298 s |  0.7716 s | 0.0423 s |   1.36 |    0.04 |    2 |             - |             - |          - |         5.86 KB |           2.43 |
| Microsoft_Data_Analysis | 171.973 s | 32.3325 s | 1.7723 s | 101.93 |    2.48 |    4 | 60349000.0000 | 15096000.0000 | 17000.0000 | 497648378.43 KB | 206,145,606.60 |
| Native_PLINQ            |  38.723 s | 19.5104 s | 1.0694 s |  22.95 |    0.76 |    3 |  1559000.0000 |   782000.0000 |  6000.0000 |  15323745.74 KB |   6,347,700.50 |
