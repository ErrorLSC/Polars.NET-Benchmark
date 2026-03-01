import os
import shutil
import time
from pyspark.sql import SparkSession
from pyspark.sql.functions import col, sum as _sum
from delta import configure_spark_with_delta_pip
from delta.tables import DeltaTable

os.environ["JAVA_HOME"] = "/usr/lib/jvm/java-21-openjdk"
base_path = "/home/qinglei/Projects/PolarsDotnetBenchmark/PolarsDeltaBenchmark/data/ny_taxi_2025_01.delta"
delete_path = "/home/qinglei/Projects/PolarsDotnetBenchmark/PolarsDeltaBenchmark/data/bench_delete_spark.delta"
optimize_path = "/home/qinglei/Projects/PolarsDotnetBenchmark/PolarsDeltaBenchmark/data/bench_optimize_spark.delta"
merge_path = "/home/qinglei/Projects/PolarsDotnetBenchmark/PolarsDeltaBenchmark/data/bench_merge_spark.delta"
overwrite_path = "/home/qinglei/Projects/PolarsDotnetBenchmark/PolarsDeltaBenchmark/data/bench_overwrite_spark.delta"
source_path = "/home/qinglei/Projects/PolarsDotnetBenchmark/PolarsDeltaBenchmark/data/bench_source.delta"

checksum_csv_path = "/home/qinglei/Projects/PolarsDotnetBenchmark/PolarsDeltaBenchmark/benchmark_checksums_spark.csv"

print("Initializing Spark Session (Cold Start)...")
builder = SparkSession.builder.appName("DeltaBenchmark") \
    .config("spark.sql.extensions", "io.delta.sql.DeltaSparkSessionExtension") \
    .config("spark.sql.catalog.spark_catalog", "org.apache.spark.sql.delta.catalog.DeltaCatalog") \
    .config("spark.sql.shuffle.partitions", "4") \
    .config("spark.ui.enabled", "false") \
    .config("spark.databricks.delta.optimize.maxFileSize", "134217728")

cold_start_begin = time.perf_counter()
spark = configure_spark_with_delta_pip(builder).getOrCreate()
spark.sparkContext.setLogLevel("ERROR")
cold_start_end = time.perf_counter()
cold_start_ms = (cold_start_end - cold_start_begin) * 1000

print(f" [Spark Cold Start]: {cold_start_ms / 1000:.2f} seconds ({cold_start_ms:.2f} ms)")
print("-" * 50)

print("Initializing global source data...")

source_df = spark.read.format("delta").load(source_path)
source_df.cache()
source_df.count() 

def enable_dv(path):
    spark.sql(f"ALTER TABLE delta.`{path}` SET TBLPROPERTIES ('delta.enableDeletionVectors' = true)")

def setup_delete():
    if os.path.exists(delete_path): shutil.rmtree(delete_path)
    shutil.copytree(base_path, delete_path)
    enable_dv(delete_path)

def setup_optimize():
    if os.path.exists(optimize_path): shutil.rmtree(optimize_path)
    df = spark.read.format("delta").load(base_path).limit(500000)
    df.repartition(50).write.format("delta").mode("append").save(optimize_path)
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
        df = spark.read.format("delta").load(path)
        sum_value = df.select(_sum("total_amount")).collect()[0][0]
        
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

def run_bench(name, func, setup_func, target_path, iters=3): 
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
# 核心 Benchmark 操作
# ==========================================

def test_delete():
    dt = DeltaTable.forPath(spark, delete_path)
    dt.delete("total_amount < 0 OR passenger_count = 0")

def test_optimize_zorder():
    dt = DeltaTable.forPath(spark, optimize_path)
    dt.optimize().executeZOrderBy("VendorID", "tpep_pickup_datetime")

def test_merge():
    dt = DeltaTable.forPath(spark, merge_path)
    (
        dt.alias("target").merge(
            source=source_df.alias("source"),
            condition="target.VendorID = source.VendorID AND target.tpep_pickup_datetime = source.tpep_pickup_datetime"
        )
        .whenMatchedDelete(
            condition="source.total_amount < 50.0"
        )
        .whenMatchedUpdateAll(
            condition="(source.total_amount > target.total_amount OR target.total_amount IS NULL) AND source.total_amount >= 10.0"
        )
        .whenNotMatchedInsertAll(
            condition="source.passenger_count > 0 AND source.total_amount > 0.0"
        )
        .whenNotMatchedBySourceDelete(
            condition="target.total_amount <= 0.01"
        )
        .execute()
    )

def test_overwrite():
    (
        spark.read.format("delta").load(base_path)
        .withColumn("total_amount", col("total_amount") * 0.9)
        .write.format("delta")
        .mode("overwrite")
        .save(overwrite_path)
    )

if __name__ == "__main__":
    if os.path.exists(checksum_csv_path):
        os.remove(checksum_csv_path)
        
    print_checksum("BaseData", base_path)

    print(f"| {'Method':<19} | {'Mean (PySpark)':>14} |")
    print("|---------------------|----------------|")
    run_bench("DeleteBench", test_delete, setup_delete, delete_path)
    run_bench("OptimizeZOrderBench", test_optimize_zorder, setup_optimize, optimize_path)
    run_bench("MergeBench", test_merge, setup_merge, merge_path)
    run_bench("OverwriteBench", test_overwrite, setup_overwrite, overwrite_path)