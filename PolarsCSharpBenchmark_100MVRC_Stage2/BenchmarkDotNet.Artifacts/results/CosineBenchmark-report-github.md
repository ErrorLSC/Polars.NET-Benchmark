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
| TensorPrimitives_CosSim |    37.33 ms |  0.450 ms | 0.025 ms |  1.00 |    0.00 |          - |           - |          NA |
| Polars_CosSim           |          NA |        NA |       NA |     ? |       ? |         NA |          NA |           ? |
| MathNet_CosSim          | 1,377.88 ms | 25.608 ms | 1.404 ms | 36.91 |    0.04 | 70000.0000 | 592000112 B |          NA |
| Linq_CosSim             |   150.96 ms | 37.151 ms | 2.036 ms |  4.04 |    0.05 |          - |       104 B |          NA |

Benchmarks with issues:
  CosineBenchmark.Polars_CosSim: Job-FGEKWY(IterationCount=3, LaunchCount=1, WarmupCount=1)
