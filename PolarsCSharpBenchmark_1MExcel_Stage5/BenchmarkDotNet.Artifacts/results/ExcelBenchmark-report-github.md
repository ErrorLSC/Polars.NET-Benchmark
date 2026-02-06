```

BenchmarkDotNet v0.15.8, Linux Fedora Linux 43 (KDE Plasma Desktop Edition)
Intel Core i7-10700 CPU 2.90GHz (Max: 0.80GHz), 1 CPU, 16 logical and 8 physical cores
.NET SDK 10.0.102
  [Host]     : .NET 10.0.2 (10.0.2, 10.0.226.5608), X64 RyuJIT x86-64-v3
  Job-FGEKWY : .NET 10.0.2 (10.0.2, 10.0.226.5608), X64 RyuJIT x86-64-v3

IterationCount=3  LaunchCount=1  WarmupCount=1  

```
| Method           | Mean    | Error    | StdDev   | Ratio | Gen0        | Gen1       | Gen2       | Allocated     | Alloc Ratio |
|----------------- |--------:|---------:|---------:|------:|------------:|-----------:|-----------:|--------------:|------------:|
| MiniExcel_Read   | 4.737 s | 0.2408 s | 0.0132 s |  1.00 | 398000.0000 |  1000.0000 |          - | 3257760.18 KB |       1.000 |
| EPPlus_Read      | 6.404 s | 0.0888 s | 0.0049 s |  1.35 | 228000.0000 | 57000.0000 | 13000.0000 | 2759137.59 KB |       0.847 |
| Polars_ReadExcel | 2.996 s | 0.1858 s | 0.0102 s |  0.63 |           - |          - |          - |       2.42 KB |       0.000 |
