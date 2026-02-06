import polars as pl
import time
import os

# 你的数据路径
FILE_PATH = "/home/qinglei/Projects/PolarsDotnetBenchmark/PolarsCSharpBenchmark_100MRC_Stage1/weatherstation.csv"

def run_1brc():
    if not os.path.exists(FILE_PATH):
        print(f"❌ 找不到文件: {FILE_PATH}")
        return

    # Get File Size
    file_size_gb = os.path.getsize(FILE_PATH) / (1024**3)
    print(f"Dataset: {FILE_PATH}")
    print(f"Size: {file_size_gb:.2f} GB")
    print("-" * 40)

    print("🚀 [PyPolars] Starting 1BRC Challenge (Streaming Mode)...")
    start_compute = time.perf_counter()

    # Core Logic
    q = (
        pl.scan_csv(
            FILE_PATH, 
            separator=";",       
            has_header=False,    
            new_columns=["station", "measure"], 
            schema={"station": pl.String, "measure": pl.Float64},
            rechunk = False
        )
        .group_by("station")
        .agg([
            pl.col("measure").min().alias("min"),
            pl.col("measure").mean().alias("mean"),
            pl.col("measure").max().alias("max")
        ])
        .sort("station")
    )

    result = q.collect(streaming=True)

    end_compute = time.perf_counter()
    duration_compute = end_compute - start_compute

    # ==========================================
    # Phase 2: Host Consumption
    # ==========================================
    print("🔄 Starting iteration (materializing Python objects)...")
    
    start_consume = time.perf_counter()
    
    row_count = 0
    for row in result.iter_rows():
        station = row[0] # str
        min_val = row[1] # float
        mean_val = row[2] # float
        max_val = row[3] # float
        row_count += 1
        
    end_consume = time.perf_counter()
    duration_consume = end_consume - start_consume

    # ==========================================
    # Report
    # ==========================================
    print("-" * 50)
    print(f"📊 Result Rows: {result.height}")
    print(f"✅ [Engine] Calculation Time : {duration_compute:.4f} s")
    print(f"🐌 [Interop] Iteration Time   : {duration_consume:.4f} s") 
    print(f"⚡ Total Time                 : {duration_compute + duration_consume:.4f} s")
    print("-" * 50)
    print("Head preview:")
    print(result.head(5))
    

if __name__ == "__main__":
    run_1brc()