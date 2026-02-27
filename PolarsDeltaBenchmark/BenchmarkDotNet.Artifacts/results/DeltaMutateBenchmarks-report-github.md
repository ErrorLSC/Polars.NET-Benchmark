```

BenchmarkDotNet v0.15.8, Linux Fedora Linux 43 (KDE Plasma Desktop Edition)
Intel Core i7-10700 CPU 2.90GHz (Max: 0.80GHz), 1 CPU, 16 logical and 8 physical cores
.NET SDK 10.0.102
  [Host]     : .NET 10.0.2 (10.0.2, 10.0.226.5608), X64 RyuJIT x86-64-v3
  Job-QKDGBD : .NET 10.0.2 (10.0.2, 10.0.226.5608), X64 RyuJIT x86-64-v3

InvocationCount=1  IterationCount=10  UnrollFactor=1  
WarmupCount=3  

```
| Method              | Mean      | Error     | StdDev    | Allocated |
|-------------------- |----------:|----------:|----------:|----------:|
| DeleteBench         |  43.04 ms |  8.818 ms |  5.833 ms |     840 B |
| OptimizeZOrderBench | 215.92 ms | 68.606 ms | 45.379 ms |     104 B |
| MergeBench          | 451.05 ms | 10.597 ms |  7.010 ms |    2456 B |
| OverwriteBench      | 314.03 ms | 14.727 ms |  9.741 ms |    1056 B |
