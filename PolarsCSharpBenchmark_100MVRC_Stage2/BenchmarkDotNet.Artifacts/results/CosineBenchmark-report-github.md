```

BenchmarkDotNet v0.15.8, Linux Fedora Linux 43 (KDE Plasma Desktop Edition)
Intel Core i7-10700 CPU 2.90GHz (Max: 0.80GHz), 1 CPU, 16 logical and 8 physical cores
.NET SDK 10.0.102
  [Host]     : .NET 10.0.2 (10.0.2, 10.0.226.5608), X64 RyuJIT x86-64-v3
  Job-FGEKWY : .NET 10.0.2 (10.0.2, 10.0.226.5608), X64 RyuJIT x86-64-v3

IterationCount=3  LaunchCount=1  WarmupCount=1  

```
| Method                  | Mean        | Error     | StdDev   | Ratio | RatioSD | Gen0       | Allocated   | Alloc Ratio |
|------------------------ |------------:|----------:|---------:|------:|--------:|-----------:|------------:|------------:|
| TensorPrimitives_CosSim |    37.31 ms |  0.551 ms | 0.030 ms |  1.00 |    0.00 |          - |           - |          NA |
| Polars_CosSim           |   261.96 ms | 19.628 ms | 1.076 ms |  7.02 |    0.03 |          - |      2024 B |          NA |
| MathNet_CosSim          | 1,381.02 ms | 47.750 ms | 2.617 ms | 37.02 |    0.07 | 70000.0000 | 592000112 B |          NA |
| Linq_CosSim             |   150.00 ms |  2.566 ms | 0.141 ms |  4.02 |    0.00 |          - |       104 B |          NA |
