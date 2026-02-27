```

BenchmarkDotNet v0.15.8, Linux Fedora Linux 43 (KDE Plasma Desktop Edition)
Intel Core i7-10700 CPU 2.90GHz (Max: 0.80GHz), 1 CPU, 16 logical and 8 physical cores
.NET SDK 10.0.102
  [Host]     : .NET 10.0.2 (10.0.2, 10.0.226.5608), X64 RyuJIT x86-64-v3
  DefaultJob : .NET 10.0.2 (10.0.2, 10.0.226.5608), X64 RyuJIT x86-64-v3


```
| Method             | Mean     | Error    | StdDev   | Median   | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------- |---------:|---------:|---------:|---------:|------:|--------:|----------:|------------:|
| FullScan           | 81.66 ms | 1.630 ms | 2.285 ms | 81.05 ms |  1.00 |    0.04 |     163 B |        1.00 |
| PredicatePushdown  | 21.49 ms | 2.172 ms | 6.403 ms | 21.95 ms |  0.26 |    0.08 |     813 B |        4.99 |
| GroupByAggregation | 25.56 ms | 0.526 ms | 1.518 ms | 24.73 ms |  0.31 |    0.02 |    1069 B |        6.56 |
