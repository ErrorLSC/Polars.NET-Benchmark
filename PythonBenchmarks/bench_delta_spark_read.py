import time
from pyspark.sql import SparkSession
import pyspark.sql.functions as F
from delta import configure_spark_with_delta_pip
import os

delta_path = "/home/qinglei/Projects/PolarsDotnetBenchmark/PolarsDeltaBenchmark/data/ny_taxi_2025_01.delta"
os.environ["JAVA_HOME"] = "/usr/lib/jvm/java-21-openjdk"
print("Initializing Spark Session (Cold Start)...")
builder = SparkSession.builder.appName("DeltaReadBenchmark") \
    .config("spark.sql.extensions", "io.delta.sql.DeltaSparkSessionExtension") \
    .config("spark.sql.catalog.spark_catalog", "org.apache.spark.sql.delta.catalog.DeltaCatalog") \
    .config("spark.sql.shuffle.partitions", "4") \
    .config("spark.ui.enabled", "false")

cold_start_begin = time.perf_counter()
# 这里是关键：自动下载并配置 Delta Lake 的 JAR 包
spark = configure_spark_with_delta_pip(builder).getOrCreate()
spark.sparkContext.setLogLevel("ERROR")
cold_start_end = time.perf_counter()
cold_start_ms = (cold_start_end - cold_start_begin) * 1000
print(f"Spark Cold Start: {cold_start_ms:.2f} ms\n")

def full_scan():
    # 1. 全量扫描
    df = spark.read.format("delta").load(delta_path)
    df.write.format("noop").mode("overwrite").save()

def predicate_pushdown():
    # 2. 谓词下推
    df = spark.read.format("delta").load(delta_path)
    df.filter(
        (F.col("tpep_pickup_datetime") >= "2025-01-15 00:00:00") &
        (F.col("tpep_pickup_datetime") < "2025-01-16 00:00:00")
    ).write.format("noop").mode("overwrite").save()

def groupby_aggregation():
    # 3. 列裁剪 + 聚合
    df = spark.read.format("delta").load(delta_path)
    df.groupBy("passenger_count").agg(
        F.sum("total_amount").alias("total_revenue"),
        F.mean("total_amount").alias("avg_fare")
    ).write.format("noop").mode("overwrite").save()

# 微型 Benchmark 运行器
def run_bench(name, func, warmup=2, iters=10):
    # Spark 的预热 (Warmup) 非常重要
    for _ in range(warmup):
        func()
    
    # 正式计时
    start_time = time.perf_counter()
    for _ in range(iters):
        func()
    end_time = time.perf_counter()
    
    mean_ms = ((end_time - start_time) / iters) * 1000
    print(f"| {name:<18} | {mean_ms:>14.2f} ms |")

if __name__ == "__main__":
    print(f"| {'Method':<18} | {'Mean (PySpark)':>17} |")
    print("|--------------------|-------------------|")
    run_bench("FullScan", full_scan)
    run_bench("PredicatePushdown", predicate_pushdown)
    run_bench("GroupByAggregation", groupby_aggregation)