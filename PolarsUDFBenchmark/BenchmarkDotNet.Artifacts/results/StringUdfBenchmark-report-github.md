```

BenchmarkDotNet v0.15.8, Linux Fedora Linux 43 (KDE Plasma Desktop Edition)
Intel Core i7-10700 CPU 2.90GHz (Max: 0.80GHz), 1 CPU, 16 logical and 8 physical cores
.NET SDK 10.0.102
  [Host]     : .NET 10.0.2 (10.0.2, 10.0.226.5608), X64 RyuJIT x86-64-v3
  DefaultJob : .NET 10.0.2 (10.0.2, 10.0.226.5608), X64 RyuJIT x86-64-v3


```
| Method                 | Mean     | Error   | StdDev  | Gen0       | Gen1      | Gen2      | Allocated |
|----------------------- |---------:|--------:|--------:|-----------:|----------:|----------:|----------:|
| PolarsDotNet_StringUdf | 177.6 ms | 0.92 ms | 0.82 ms | 28000.0000 | 1333.3333 | 1333.3333 | 228.74 MB |
