import polars as pl

base_path = "/home/qinglei/Projects/PolarsDotnetBenchmark/PolarsDeltaBenchmark/data/ny_taxi_2025_01.delta"
source_path = "/home/qinglei/Projects/PolarsDotnetBenchmark/PolarsDeltaBenchmark/data/bench_source.delta"

(
    pl.scan_delta(base_path)
    .limit(100000)
    .unique(subset=["VendorID", "tpep_pickup_datetime"], keep="first")
    .with_columns((pl.col("total_amount") + 5.0).alias("total_amount"))
    .sink_delta(source_path, mode="overwrite")
)
print("Finished!")