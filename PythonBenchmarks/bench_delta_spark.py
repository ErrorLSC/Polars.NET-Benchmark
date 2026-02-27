import os
import shutil
import time
from pyspark.sql import SparkSession
from pyspark.sql.functions import col
from delta import configure_spark_with_delta_pip
from delta.tables import DeltaTable

os.environ["JAVA_HOME"] = "/usr/lib/jvm/java-21-openjdk"
base_path = "/home/qinglei/Projects/PolarsDotnetBenchmark/PolarsDeltaBenchmark/data/ny_taxi_2025_01.delta"
delete_path = "/home/qinglei/Projects/PolarsDotnetBenchmark/PolarsDeltaBenchmark/data/bench_delete_spark.delta"
optimize_path = "/home/qinglei/Projects/PolarsDotnetBenchmark/PolarsDeltaBenchmark/data/bench_optimize_spark.delta"
merge_path = "/home/qinglei/Projects/PolarsDotnetBenchmark/PolarsDeltaBenchmark/data/bench_merge_spark.delta"
# 新增 overwrite_path
overwrite_path = "/home/qinglei/Projects/PolarsDotnetBenchmark/PolarsDeltaBenchmark/data/bench_overwrite_spark.delta"

print("Initializing Spark Session (Cold Start)...")
# 这里的 builder 会自动配置好 Delta 相关的扩展，屏蔽了复杂的参数
builder = SparkSession.builder.appName("DeltaBenchmark") \
    .config("spark.sql.extensions", "io.delta.sql.DeltaSparkSessionExtension") \
    .config("spark.sql.catalog.spark_catalog", "org.apache.spark.sql.delta.catalog.DeltaCatalog") \
    .config("spark.sql.shuffle.partitions", "4") \
    .config("spark.ui.enabled", "false") \
    .config("spark.databricks.delta.optimize.maxFileSize", "134217728") # 设置 Optimize 目标大小 128MB

cold_start_begin = time.perf_counter()

# 启动 JVM 并加载依赖（这一步最慢，但我们把它隔离在 Benchmark 计时之外）
spark = configure_spark_with_delta_pip(builder).getOrCreate()
spark.sparkContext.setLogLevel("ERROR")

cold_start_end = time.perf_counter()
cold_start_ms = (cold_start_end - cold_start_begin) * 1000

print(f" [Spark Cold Start]: {cold_start_ms / 1000:.2f} seconds ({cold_start_ms:.2f} ms)")
print("-" * 50)

print("Initializing global source data...")

# 1. 全局数据准备
source_df = spark.read.format("delta").load(base_path).limit(100000)
source_df = source_df.dropDuplicates(["VendorID", "tpep_pickup_datetime"]) \
    .withColumn("total_amount", col("total_amount") + 5.0)
# 将 source 数据缓存到内存，防止每次 Merge 时重新计算源数据
source_df.cache()
source_df.count() # 触发一次 Action 完成缓存

def enable_dv(path):
    spark.sql(f"ALTER TABLE delta.`{path}` SET TBLPROPERTIES ('delta.enableDeletionVectors' = true)")

def setup_delete():
    if os.path.exists(delete_path): shutil.rmtree(delete_path)
    shutil.copytree(base_path, delete_path)
    enable_dv(delete_path)

def setup_optimize():
    if os.path.exists(optimize_path): shutil.rmtree(optimize_path)
    df = spark.read.format("delta").load(base_path).limit(500000)
    # 模拟碎片化写入
    df.repartition(50).write.format("delta").mode("append").save(optimize_path)
    enable_dv(optimize_path)

def setup_merge():
    if os.path.exists(merge_path): shutil.rmtree(merge_path)
    shutil.copytree(base_path, merge_path)
    enable_dv(merge_path)

# 新增 overwrite 的 setup
def setup_overwrite():
    if os.path.exists(overwrite_path): shutil.rmtree(overwrite_path)
    shutil.copytree(base_path, overwrite_path)
    enable_dv(overwrite_path)

def run_bench(name, func, setup_func, iters=3): # Spark太慢了，iters 建议设少点
    times = []
    # 预热一次 (JIT Warmup)
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
        .whenMatchedUpdateAll(
            condition="source.total_amount > target.total_amount OR target.total_amount IS NULL"
        )
        .whenMatchedDelete(
            condition="source.total_amount < 0.0"
        )
        .whenNotMatchedInsertAll(
            condition="source.passenger_count > 0 AND source.total_amount > 0.0"
        )
        .whenNotMatchedBySourceDelete(
            condition="target.total_amount <= 0.01"
        )
        .execute()
    )

# 新增 overwrite benchmark 逻辑
def test_overwrite():
    # 读取全量基准数据，打 9 折，然后直接以 overwrite 模式写盘
    (
        spark.read.format("delta").load(base_path)
        .withColumn("total_amount", col("total_amount") * 0.9)
        .write.format("delta")
        .mode("overwrite")
        .save(overwrite_path)
    )

if __name__ == "__main__":
    print(f"| {'Method':<19} | {'Mean (PySpark)':>14} |")
    print("|---------------------|----------------|")
    run_bench("DeleteBench", test_delete, setup_delete)
    run_bench("OptimizeZOrderBench", test_optimize_zorder, setup_optimize)
    run_bench("MergeBench", test_merge, setup_merge)
    run_bench("OverwriteBench", test_overwrite, setup_overwrite)