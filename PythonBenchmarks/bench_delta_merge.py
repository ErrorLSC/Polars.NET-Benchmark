import polars as pl
from deltalake import DeltaTable
import time
import shutil
import os

base_path = "/home/qinglei/Projects/PolarsDotnetBenchmark/PolarsDeltaBenchmark/data/ny_taxi_2025_01.delta"
delete_path = "/home/qinglei/Projects/PolarsDotnetBenchmark/PolarsDeltaBenchmark/data/bench_delete_py.delta"
optimize_path = "/home/qinglei/Projects/PolarsDotnetBenchmark/PolarsDeltaBenchmark/data/bench_optimize_py.delta"
merge_path = "/home/qinglei/Projects/PolarsDotnetBenchmark/PolarsDeltaBenchmark/data/bench_merge_py.delta"
overwrite_path = "/home/qinglei/Projects/PolarsDotnetBenchmark/PolarsDeltaBenchmark/data/bench_overwrite_py.delta"
source_path = "/home/qinglei/Projects/PolarsDotnetBenchmark/PolarsDeltaBenchmark/data/bench_source.delta"

checksum_csv_path = "/home/qinglei/Projects/PolarsDotnetBenchmark/PolarsDeltaBenchmark/benchmark_checksums_py_polars.csv"

print("Initializing global source data...")

source_df = (
    pl.scan_delta(source_path)
    .collect()
)

def enable_dv(path):
    dt = DeltaTable(path)
    dt.alter.set_table_properties({"delta.enableDeletionVectors": "true"})

def setup_delete():
    if os.path.exists(delete_path): shutil.rmtree(delete_path)
    shutil.copytree(base_path, delete_path)
    enable_dv(delete_path)

def setup_optimize():
    if os.path.exists(optimize_path): shutil.rmtree(optimize_path)
    df = pl.scan_delta(base_path).limit(500000).collect()
    for i in range(50):
        chunk = df.slice(i * 10000, 10000)
        chunk.write_delta(optimize_path, mode="append")
    enable_dv(optimize_path)

def setup_merge():
    if os.path.exists(merge_path): shutil.rmtree(merge_path)
    shutil.copytree(base_path, merge_path)
    enable_dv(merge_path)

def setup_overwrite():
    if os.path.exists(overwrite_path): shutil.rmtree(overwrite_path)
    shutil.copytree(base_path, overwrite_path)
    enable_dv(overwrite_path)

# ==========================================
# Checksum 
# ==========================================
def print_checksum(bench_name, path):
    try:
        sum_value = pl.scan_delta(path).select(pl.col("total_amount").sum()).collect().item()
        
        print(f"\n[CHECKSUM VERIFIED] {bench_name:<20} | Total Amount Sum: {sum_value:.4f}\n")
        
        write_header = not os.path.exists(checksum_csv_path)
        with open(checksum_csv_path, mode="a", encoding="utf-8") as f:
            if write_header:
                f.write("BenchmarkName,TotalAmountSum,Status\n")
            f.write(f"{bench_name},{sum_value:.4f},SUCCESS\n")
            
    except Exception as e:
        error_msg = str(e).replace(",", ";").replace("\n", " ")
        print(f"\n[CHECKSUM ERROR] {bench_name} | Failed to compute checksum: {error_msg}\n")
        
        write_header = not os.path.exists(checksum_csv_path)
        with open(checksum_csv_path, mode="a", encoding="utf-8") as f:
            if write_header:
                f.write("BenchmarkName,TotalAmountSum,Status\n")
            f.write(f"{bench_name},0.0000,ERROR: {error_msg}\n")

def run_bench(name, func, setup_func, target_path, iters=5):
    times = []
    setup_func()
    func()
    
    for _ in range(iters):
        setup_func() 
        
        start = time.perf_counter()
        func()
        end = time.perf_counter()
        times.append((end - start) * 1000)
    
    mean_ms = sum(times) / len(times)
    print(f"| {name:<19} | {mean_ms:>8.2f} ms |")
    
    print_checksum(name, target_path)

# ==========================================
# Benchmark
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
        .when_matched_delete(
            predicate="source.total_amount < 50.0"
        )
        .when_matched_update_all(
            predicate="(source.total_amount > target.total_amount OR target.total_amount IS NULL) AND source.total_amount >= 10.0"
        )
        .when_not_matched_insert_all(
            predicate="source.passenger_count > 0 AND source.total_amount > 0.0"
        )
        .when_not_matched_by_source_delete(
            predicate="target.total_amount <= 0.01"
        )
        .execute()
    )

def test_overwrite():
    (
        pl.scan_delta(base_path)
        .with_columns((pl.col("total_amount") * 0.9).alias("total_amount"))
        .sink_delta(target=overwrite_path, mode="overwrite")
    )

if __name__ == "__main__":
    if os.path.exists(checksum_csv_path):
        os.remove(checksum_csv_path)
        
    print_checksum("BaseData", base_path)
    
    print(f"| {'Method':<19} | {'Mean (Python)':>11} |")
    print("|---------------------|-------------|")
    run_bench("DeleteBench", test_delete, setup_delete, delete_path)
    run_bench("OptimizeZOrderBench", test_optimize_zorder, setup_optimize, optimize_path)
    run_bench("MergeBench", test_merge, setup_merge, merge_path)
    run_bench("OverwriteBench", test_overwrite, setup_overwrite, overwrite_path)