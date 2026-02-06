```

BenchmarkDotNet v0.15.8, Linux Fedora Linux 43 (KDE Plasma Desktop Edition)
Intel Core i7-10700 CPU 2.90GHz (Max: 0.80GHz), 1 CPU, 16 logical and 8 physical cores
.NET SDK 10.0.102
  [Host]     : .NET 10.0.2 (10.0.2, 10.0.226.5608), X64 RyuJIT x86-64-v3
  Job-FGEKWY : .NET 10.0.2 (10.0.2, 10.0.226.5608), X64 RyuJIT x86-64-v3

IterationCount=3  LaunchCount=1  WarmupCount=1  

```
| Method          | Mean    | Error   | StdDev  | Ratio | RatioSD | Gen0         | Gen1        | Gen2       | Allocated      | Alloc Ratio  |
|---------------- |--------:|--------:|--------:|------:|--------:|-------------:|------------:|-----------:|---------------:|-------------:|
| Polars_Mixed    | 14.74 s | 2.509 s | 0.138 s |  1.00 |    0.01 |            - |           - |          - |        2.74 KB |         1.00 |
| MiniExcel_Mixed | 28.26 s | 0.974 s | 0.053 s |  1.92 |    0.02 | 1950000.0000 |  26000.0000 |          - | 15935125.02 KB | 5,811,099.72 |
| EPPlus_Mixed    | 34.84 s | 2.042 s | 0.112 s |  2.36 |    0.02 |  822000.0000 | 186000.0000 | 16000.0000 | 10328914.66 KB | 3,766,669.73 |
