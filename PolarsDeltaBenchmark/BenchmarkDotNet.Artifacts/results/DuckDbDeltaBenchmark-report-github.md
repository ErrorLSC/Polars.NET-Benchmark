```

BenchmarkDotNet v0.15.8, Linux Fedora Linux 43 (KDE Plasma Desktop Edition)
Intel Core i7-10700 CPU 2.90GHz (Max: 0.80GHz), 1 CPU, 16 logical and 8 physical cores
.NET SDK 10.0.102
  [Host]     : .NET 10.0.2 (10.0.2, 10.0.226.5608), X64 RyuJIT x86-64-v3
  Job-NUBXJZ : .NET 10.0.2 (10.0.2, 10.0.226.5608), X64 RyuJIT x86-64-v3

IterationCount=20  WarmupCount=5  

```
| Method             | Mean      | Error    | StdDev   | Ratio | Gen0     | Allocated  | Alloc Ratio |
|------------------- |----------:|---------:|---------:|------:|---------:|-----------:|------------:|
| FullScan           | 230.66 ms | 1.212 ms | 1.297 ms |  1.00 | 333.3333 | 3241.01 KB |       1.000 |
| PredicatePushdown  |  33.59 ms | 0.859 ms | 0.954 ms |  0.15 |        - |  126.44 KB |       0.039 |
| GroupByAggregation |  14.50 ms | 0.056 ms | 0.065 ms |  0.06 |        - |    4.86 KB |       0.001 |
