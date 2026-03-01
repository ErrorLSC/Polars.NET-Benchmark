import pandas as pd
import matplotlib.pyplot as plt
import seaborn as sns
import numpy as np

plt.style.use('dark_background')
sns.set_palette("husl")

# ==========================================
# 1. 准备数据
# ==========================================
read_data = {
    'Framework': ['Polars.NET (C#+Rust)', 'PyPolars (Python+Rust)', 'DuckDB.NET (C#+C++)', 'PySpark (Python+JVM)'] * 3,
    'Operation': ['Full Scan']*4 + ['Predicate Pushdown']*4 + ['GroupBy Aggregation']*4,
    'Time (ms)': [
        91.37, 75.94, 230.66, 545.78,
        11.86, 11.37, 33.59, 439.51,
        24.25, 19.52, 14.50, 274.98
    ]
}
df_read = pd.DataFrame(read_data)

write_data = {
    'Framework': ['Polars.NET (C#+Rust)', 'PySpark (Python+JVM)', 'PyPolars (Python+Rust)'] * 4,
    'Operation': ['Overwrite']*3 + ['Delete']*3 + ['Merge']*3 + ['Optimize (Z-Order)']*3,
    'Time (ms)': [
        328.95, 4087.04, 2029.04,
        47.84, 1728.95, 2386.62,
        467.43, 5766.09, 2904.90,
        172.01, 4178.70, 911.67
    ]
}
df_write = pd.DataFrame(write_data)

# ==========================================
# 2. 绘图函数
# ==========================================
def plot_benchmark(df, title, filename, is_write=False):
    fig, ax = plt.subplots(figsize=(14, 8))
    
    # 调色板：Polars.NET 用亮眼的青绿色，Spark 用橙色，Python 用蓝色，DuckDB 用黄色
    if is_write:
        colors = ['#00e5ff', '#ff9100', '#2979ff']
    else:
        colors = ['#00e5ff', '#2979ff', '#ffea00', '#ff9100']
        
    sns.barplot(data=df, x='Operation', y='Time (ms)', hue='Framework', palette=colors, ax=ax)
    
    # 添加标题和标签
    ax.set_title(title, fontsize=20, fontweight='bold', pad=20, color='white')
    ax.set_ylabel('Execution Time (ms) - Lower is Better', fontsize=14, color='white')
    ax.set_xlabel('', fontsize=14)
    ax.tick_params(axis='x', labelsize=13, colors='white')
    ax.tick_params(axis='y', labelsize=12, colors='white')
    
    # 在柱子上添加数值标签
    for p in ax.patches:
        height = p.get_height()
        if not np.isnan(height):
            ax.annotate(f'{height:.2f}', 
                        (p.get_x() + p.get_width() / 2., height), 
                        ha='center', va='bottom', 
                        fontsize=11, color='white', xytext=(0, 5), 
                        textcoords='offset points')
            
    # 针对 PyPolars 的 Failed Checksum 做特殊标记
    # if is_write:
    #     # 手动找 PyPolars 的柱子加上 ❌ 标记
    #     for i, p in enumerate(ax.patches):
    #         if i >= 8: # 第三组 hue (PyPolars)
    #             height = p.get_height()
    #             ax.annotate('❌ Failed DV', 
    #                         (p.get_x() + p.get_width() / 2., height / 2), 
    #                         ha='center', va='center', 
    #                         fontsize=10, color='#ff1744', fontweight='bold', rotation=90)

    # 调整图例
    plt.legend(title='Framework', fontsize=12, title_fontsize=13, facecolor='#212121', edgecolor='white')
    ax.grid(axis='y', linestyle='--', alpha=0.3)
    
    plt.tight_layout()
    plt.savefig(filename, dpi=300, bbox_inches='tight', facecolor=fig.get_facecolor())
    print(f"Saved {filename}")

# ==========================================
# 3. 生成图表
# ==========================================
plot_benchmark(df_read, 'Read Performance: Delta Lake Querying (Single Node)', 'read_benchmark.png', is_write=False)
plot_benchmark(df_write, 'ACID Operations: Delta Lake Write / Merge / Optimize', 'write_benchmark.png', is_write=True)