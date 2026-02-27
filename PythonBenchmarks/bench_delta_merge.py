import polars as pl
from deltalake import DeltaTable
import time
import shutil
import os

base_path = "/home/qinglei/Projects/PolarsDotnetBenchmark/PolarsDeltaBenchmark/data/ny_taxi_2025_01.delta"
delete_path = "/home/qinglei/Projects/PolarsDotnetBenchmark/PolarsDeltaBenchmark/data/bench_delete_py.delta"
optimize_path = "/home/qinglei/Projects/PolarsDotnetBenchmark/PolarsDeltaBenchmark/data/bench_optimize_py.delta"
merge_path = "/home/qinglei/Projects/PolarsDotnetBenchmark/PolarsDeltaBenchmark/data/bench_merge_py.delta"
# 新增 overwrite_path
overwrite_path = "/home/qinglei/Projects/PolarsDotnetBenchmark/PolarsDeltaBenchmark/data/bench_overwrite_py.delta"

print("Initializing global source data...")

# 1. 全局数据准备 (严格复刻 C# 的去重与增量修改)
source_df = (
    pl.scan_delta(base_path)
    .limit(100000)
    .unique(subset=["VendorID", "tpep_pickup_datetime"], keep="first")
    .with_columns((pl.col("total_amount") + 5.0).alias("total_amount"))
    .collect()
)

def enable_dv(path):
    # Python 端开启 Deletion Vectors
    dt = DeltaTable(path)
    dt.alter.set_table_properties({"delta.enableDeletionVectors": "true"})

def setup_delete():
    if os.path.exists(delete_path): shutil.rmtree(delete_path)
    shutil.copytree(base_path, delete_path)
    enable_dv(delete_path)

def setup_optimize():
    if os.path.exists(optimize_path): shutil.rmtree(optimize_path)
    df = pl.scan_delta(base_path).limit(500000).collect()
    # 模拟碎片化写入，切成 50 个 chunk
    for i in range(50):
        chunk = df.slice(i * 10000, 10000)
        chunk.write_delta(optimize_path, mode="append")
    enable_dv(optimize_path)

def setup_merge():
    if os.path.exists(merge_path): shutil.rmtree(merge_path)
    shutil.copytree(base_path, merge_path)
    enable_dv(merge_path)

# 新增 overwrite 的 setup
def setup_overwrite():
    if os.path.exists(overwrite_path): shutil.rmtree(overwrite_path)
    # 同样先复制一份老数据作为底表，测试覆盖老表时的 Tombstone 逻辑
    shutil.copytree(base_path, overwrite_path)
    enable_dv(overwrite_path)

def run_bench(name, func, setup_func, iters=5):
    times = []
    # 跑之前先预热一次
    setup_func()
    func()
    
    for _ in range(iters):
        setup_func() # 每次跑之前重置状态，保证公平
        
        start = time.perf_counter()
        func()
        end = time.perf_counter()
        times.append((end - start) * 1000)
    
    mean_ms = sum(times) / len(times)
    print(f"| {name:<19} | {mean_ms:>8.2f} ms |")

# ==========================================
# 核心 Benchmark 操作
# ==========================================

def test_delete():
    dt = DeltaTable(delete_path)
    dt.delete("total_amount < 0 OR passenger_count = 0")

def test_optimize_zorder():
    dt = DeltaTable(optimize_path)
    dt.optimize.z_order(
        columns=["VendorID", "tpep_pickup_datetime"], 
        target_size=128 * 1024 * 1024
    )

def test_merge():
    merger = source_df.lazy().sink_delta(
        target=merge_path,
        mode="merge",
        delta_merge_options={
            "predicate": "target.VendorID = source.VendorID AND target.tpep_pickup_datetime = source.tpep_pickup_datetime",
            "source_alias": "source",
            "target_alias": "target"
        }
    )
    
    (
        merger
        .when_matched_update_all(
            predicate="source.total_amount > target.total_amount OR target.total_amount IS NULL"
        )
        .when_matched_delete(
            predicate="source.total_amount < 0.0"
        )
        .when_not_matched_insert_all(
            predicate="source.passenger_count > 0 AND source.total_amount > 0.0"
        )
        .when_not_matched_by_source_delete(
            predicate="target.total_amount <= 0.01"
        )
        .execute()
    )

# 新增 overwrite benchmark 逻辑
def test_overwrite():
    # 读取全量基准数据，打 9 折，然后直接覆写
    (
        pl.scan_delta(base_path)
        .with_columns((pl.col("total_amount") * 0.9).alias("total_amount"))
        .sink_delta(target=overwrite_path, mode="overwrite")
    )

if __name__ == "__main__":
    print(f"| {'Method':<19} | {'Mean (Python)':>11} |")
    print("|---------------------|-------------|")
    run_bench("DeleteBench", test_delete, setup_delete)
    run_bench("OptimizeZOrderBench", test_optimize_zorder, setup_optimize)
    run_bench("MergeBench", test_merge, setup_merge)
    run_bench("OverwriteBench", test_overwrite, setup_overwrite)