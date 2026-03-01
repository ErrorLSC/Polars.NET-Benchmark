#r "Polars.FSharp"
#r "Polars.NET.Native.linux-x64"

open Polars.FSharp

let base_path = "/home/qinglei/Projects/PolarsDotnetBenchmark/PolarsDeltaBenchmark/data/ny_taxi_2025_01.delta"
let source_path = "/home/qinglei/Projects/PolarsDotnetBenchmark/PolarsDeltaBenchmark/data/bench_source.delta"

LazyFrame.ScanDelta(base_path)
    .Limit(100000)
    .Unique(subset=["VendorID"; "tpep_pickup_datetime"], keep="first")
    .WithColumn((pl.col "total_amount" + 5.0).alias "total_amount")
    .SinkDelta(source_path, mode=DeltaSaveMode.Overwrite)

printf "Finished!"