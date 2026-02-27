```

BenchmarkDotNet v0.15.8, Linux Fedora Linux 43 (KDE Plasma Desktop Edition)
Intel Core i7-10700 CPU 2.90GHz (Max: 0.80GHz), 1 CPU, 16 logical and 8 physical cores
.NET SDK 10.0.102
  [Host]     : .NET 10.0.2 (10.0.2, 10.0.226.5608), X64 RyuJIT x86-64-v3
  DefaultJob : .NET 10.0.2 (10.0.2, 10.0.226.5608), X64 RyuJIT x86-64-v3


```
| Method                 | Mean     | Error   | StdDev  | Gen0       | Gen1      | Gen2      | Allocated |
|----------------------- |---------:|--------:|--------:|-----------:|----------:|----------:|----------:|
| PolarsDotNet_StringUdf | 174.3 ms | 2.08 ms | 1.95 ms | 27333.3333 | 1000.0000 | 1000.0000 | 216.79 MB |
