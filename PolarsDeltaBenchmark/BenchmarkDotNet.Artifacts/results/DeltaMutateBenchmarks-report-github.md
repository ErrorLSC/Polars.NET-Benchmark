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
| DeleteBench         |  47.84 ms |  2.422 ms |  1.441 ms |     840 B |
| OptimizeZOrderBench | 172.01 ms | 21.320 ms | 14.102 ms |     104 B |
| MergeBench          | 467.43 ms | 27.523 ms | 18.205 ms |    3200 B |
| OverwriteBench      | 328.95 ms | 26.844 ms | 17.756 ms |    1088 B |
