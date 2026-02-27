```

BenchmarkDotNet v0.15.8, Linux Fedora Linux 43 (KDE Plasma Desktop Edition)
Intel Core i7-10700 CPU 2.90GHz (Max: 0.80GHz), 1 CPU, 16 logical and 8 physical cores
.NET SDK 10.0.102
  [Host]     : .NET 10.0.2 (10.0.2, 10.0.226.5608), X64 RyuJIT x86-64-v3 DEBUG
  DefaultJob : .NET 10.0.2 (10.0.2, 10.0.226.5608), X64 RyuJIT x86-64-v3


```
| Method                 | Mean        | Error     | StdDev    | Ratio  | RatioSD | Gen0        | Gen1        | Gen2      | Allocated    | Alloc Ratio   |
|----------------------- |------------:|----------:|----------:|-------:|--------:|------------:|------------:|----------:|-------------:|--------------:|
| Deedle_Decimal_Rolling | 1,717.47 ms | 34.301 ms | 45.791 ms | 163.97 |    4.32 | 491000.0000 | 124000.0000 | 7000.0000 | 4240150560 B | 23,044,296.52 |
| Polars_Rolling         |    38.63 ms |  0.305 ms |  0.238 ms |   3.69 |    0.02 |           - |           - |         - |        604 B |          3.28 |
| FSharp_Decimal_Native  |    10.47 ms |  0.037 ms |  0.033 ms |   1.00 |    0.00 |           - |           - |         - |        184 B |          1.00 |
