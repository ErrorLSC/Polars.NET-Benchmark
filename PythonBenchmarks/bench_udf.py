import pandas as pd
import polars as pl
import time

ROWS = 1_000_000

print(f"Generating {ROWS} Rows String Data")
data = [f"ID_{i}_OK" for i in range(ROWS)]

# -------------------------------------------------
# Pandas UDF
# -------------------------------------------------
print("\n[Pandas] Loading")
pdf = pd.DataFrame({"log": data})

print("[Pandas] starts apply (String UDF)")
start = time.perf_counter()

res_pandas = pdf["log"].apply(lambda x: int(x.split('_')[1]))

duration = time.perf_counter() - start
print(f"Pandas Time: {duration:.4f} s")

# -------------------------------------------------
# PyPolars UDF
# -------------------------------------------------
print("\n[PyPolars] Loading")
pl_df = pl.DataFrame({"log": data})

print("[PyPolars] starts map_elements (String UDF)")
start = time.perf_counter()

res_polars = pl_df.select(
    pl.col("log").map_elements(lambda x: int(x.split('_')[1]), return_dtype=pl.Int64)
)

duration = time.perf_counter() - start
print(f"PyPolars Time: {duration:.4f} s")