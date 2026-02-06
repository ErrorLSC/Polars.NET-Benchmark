import polars as pl
import numpy as np
import time
import pandas as pd

ROWS = 10_000_000

print(f"Generate {ROWS} Rows Data (NumPy)")

ids = np.random.randint(0, 100, ROWS)
vals = np.random.rand(ROWS)

print("Build DataFrame")
df = pl.DataFrame({
    "id": ids,
    "val": vals
})

print(f"DataFrame Shape: {df.shape}")

# ==========================================
# Test PyPolars GroupBy (Lazy)
# ==========================================
print("\n[PyPolars] Starts GroupBy Sum")
start_time = time.perf_counter()

res = (
    df.lazy()
    .group_by("id")
    .agg(pl.col("val").sum().alias("total"))
    .collect() 
)

end_time = time.perf_counter()
print(f"PyPolars Time: {(end_time - start_time) * 1000:.2f} ms")

print(f"Result Rows: {res.height}")

# ==========================================
# 2. Pandas GroupBy (Eager)
# ==========================================
print("\n[Pandas] Converting to Pandas DataFrame...")
df_pd = df.to_pandas() 

print("[Pandas] Starts GroupBy Sum")
start_time = time.perf_counter()

res_pd = df_pd.groupby("id")["val"].sum()

end_time = time.perf_counter()
pandas_time = (end_time - start_time) * 1000
print(f"Pandas Time  : {pandas_time:.2f} ms")