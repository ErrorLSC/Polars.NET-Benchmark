import polars as pl
import time
from datetime import datetime

# 指向我们刚刚用 C# 下载并生成的那个 Delta 目录
delta_path = "/home/qinglei/Projects/PolarsDotnetBenchmark/PolarsDeltaBenchmark/data/ny_taxi_2025_01.delta"

def full_scan():
    # 1. 全量扫描
    pl.scan_delta(delta_path).collect()

def predicate_pushdown():
    # 2. 谓词下推
    pl.scan_delta(delta_path).filter(
        (pl.col("tpep_pickup_datetime") >= datetime(2025, 1, 15)) &
        (pl.col("tpep_pickup_datetime") < datetime(2025, 1, 16))
    ).collect()

def groupby_aggregation():
    # 3. 列裁剪 + 聚合
    pl.scan_delta(delta_path).group_by("passenger_count").agg(
        pl.col("total_amount").sum().alias("total_revenue"),
        pl.col("total_amount").mean().alias("avg_fare")
    ).collect()

# 微型 Benchmark 运行器
def run_bench(name, func, warmup=5, iters=20):
    # 预热 (Warmup): 填满 OS 缓存，让 CPU 睿频起来
    for _ in range(warmup):
        func()
    
    # 正式计时
    start_time = time.perf_counter()
    for _ in range(iters):
        func()
    end_time = time.perf_counter()
    
    mean_ms = ((end_time - start_time) / iters) * 1000
    print(f"| {name:<18} | {mean_ms:>7.2f} ms |")

if __name__ == "__main__":
    print(f"| {'Method':<18} | {'Mean':>10} |")
    print("|--------------------|------------|")
    run_bench("FullScan", full_scan)
    run_bench("PredicatePushdown", predicate_pushdown)
    run_bench("GroupByAggregation", groupby_aggregation)