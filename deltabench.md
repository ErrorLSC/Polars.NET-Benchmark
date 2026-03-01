# Benchmark: Polars.NET vs The Delta Ecosystem

- Environment: Single-node local execution.
- Dataset: NY Taxi (2025-01), about 3M+ rows.

|Framework|Full Scan|Predicate Pushdown|GroupBy Aggregation|Note|
|---------|---------|------------------|-------------------|----|
|Polars.NET (C#+Rust)|91.37 ms|11.86 ms|24.25 ms|Zero-cost FFI,ultra-low GC allocation (163B).|
|PyPolars (Python+Rust)|75.94 ms|11.37 ms|19.52 ms|Rust native engine baseline.|
|DuckDB.NET(C#+C++)|230.66 ms|33.59 ms|14.50 ms|Highly optimized for pure aggregation,but slower I/O.|
|PySpark (Python+JVM)|545.78 ms|439.51 ms|274.98 ms|Significant JVM / Py4J overhead|

|Framework|Overwrite|Delete|Merge|Optimize (Z-Order)|Validation & Status|
|---------|---------|------|-----|------------------|------------|
|Polars.NET (C#+Rust)|328.95 ms|47.84 ms|467.43 ms|172.01 ms|Passed. Native Deletion Vectors support.|
|PySpark (Python+JVM)|4087.04 ms|1728.95 ms|5766.09 ms|4178.70 ms|Passed. Baseline.|
|PyPolars (Python+Rust)|2029.04 ms|2386.62 ms|2904.90 ms|911.67 ms|Failed Checksum. Missing DV support in scanner.|

