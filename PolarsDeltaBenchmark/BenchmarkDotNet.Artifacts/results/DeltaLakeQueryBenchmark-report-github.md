```

BenchmarkDotNet v0.15.8, Linux Fedora Linux 43 (KDE Plasma Desktop Edition)
Intel Core i7-10700 CPU 2.90GHz (Max: 0.80GHz), 1 CPU, 16 logical and 8 physical cores
.NET SDK 10.0.102
  [Host]     : .NET 10.0.2 (10.0.2, 10.0.226.5608), X64 RyuJIT x86-64-v3
  Job-NUBXJZ : .NET 10.0.2 (10.0.2, 10.0.226.5608), X64 RyuJIT x86-64-v3

IterationCount=20  WarmupCount=5  

```
| Method             | Mean     | Error    | StdDev   | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------- |---------:|---------:|---------:|------:|--------:|----------:|------------:|
| FullScan           | 91.37 ms | 3.245 ms | 3.472 ms |  1.00 |    0.05 |     163 B |        1.00 |
| PredicatePushdown  | 11.86 ms | 0.186 ms | 0.214 ms |  0.13 |    0.01 |     802 B |        4.92 |
| GroupByAggregation | 24.25 ms | 0.734 ms | 0.845 ms |  0.27 |    0.01 |    1133 B |        6.95 |
