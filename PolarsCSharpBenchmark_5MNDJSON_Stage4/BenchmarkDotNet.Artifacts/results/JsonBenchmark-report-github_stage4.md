```

BenchmarkDotNet v0.15.8, Linux Fedora Linux 43 (KDE Plasma Desktop Edition)
Intel Core i7-10700 CPU 2.90GHz (Max: 0.80GHz), 1 CPU, 16 logical and 8 physical cores
.NET SDK 10.0.102
  [Host]     : .NET 10.0.2 (10.0.2, 10.0.226.5608), X64 RyuJIT x86-64-v3
  Job-FGEKWY : .NET 10.0.2 (10.0.2, 10.0.226.5608), X64 RyuJIT x86-64-v3

IterationCount=3  LaunchCount=1  WarmupCount=1  

```
| Method                 | Mean     | Error    | StdDev   | Ratio | RatioSD | Gen0         | Gen1        | Allocated      | Alloc Ratio |
|----------------------- |---------:|---------:|---------:|------:|--------:|-------------:|------------:|---------------:|------------:|
| SystemTextJson_Process |  7.318 s | 0.6558 s | 0.0359 s |  1.00 |    0.01 |  674000.0000 |   2000.0000 |  5510740.66 KB |       1.000 |
| Newtonsoft_Dynamic     | 25.234 s | 6.7879 s | 0.3721 s |  3.45 |    0.05 | 5237000.0000 | 132000.0000 | 42776350.11 KB |       7.762 |
| Polars_ScanNdjson      |  3.685 s | 2.8580 s | 0.1567 s |  0.50 |    0.02 |            - |           - |        2.25 KB |       0.000 |
