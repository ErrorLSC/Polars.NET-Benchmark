```

BenchmarkDotNet v0.15.8, Linux Fedora Linux 43 (KDE Plasma Desktop Edition)
Intel Core i7-10700 CPU 2.90GHz (Max: 0.80GHz), 1 CPU, 16 logical and 8 physical cores
.NET SDK 10.0.102
  [Host]     : .NET 10.0.2 (10.0.2, 10.0.226.5608), X64 RyuJIT x86-64-v3 DEBUG
  Job-MYSLML : .NET 10.0.2 (10.0.2, 10.0.226.5608), X64 RyuJIT x86-64-v3

IterationCount=3  LaunchCount=1  RunStrategy=Monitoring  
WarmupCount=1  

```
| Method                      | Mean        | Error      | StdDev    | Ratio | RatioSD | Gen0        | Gen1        | Gen2      | Allocated    | Alloc Ratio |
|---------------------------- |------------:|-----------:|----------:|------:|--------:|------------:|------------:|----------:|-------------:|------------:|
| Deedle_Decimal_Rolling      | 1,824.86 ms | 882.497 ms | 48.373 ms | 1.000 |    0.03 | 491000.0000 | 124000.0000 | 7000.0000 | 4240150912 B |       1.000 |
| Polars_Rolling              |    41.89 ms |   4.345 ms |  0.238 ms | 0.023 |    0.00 |           - |           - |         - |       1352 B |       0.000 |
| FSharp_Decimal_SimpleEngine |    10.38 ms |   0.327 ms |  0.018 ms | 0.006 |    0.00 |           - |           - |         - |        184 B |       0.000 |
