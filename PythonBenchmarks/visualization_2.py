import matplotlib.pyplot as plt
import seaborn as sns
import pandas as pd
import numpy as np

# --- 1. 风格设置 ---
sns.set_theme(style="dark", context="talk")
plt.rcParams['font.family'] = 'sans-serif'
plt.rcParams['text.color'] = '#333333'

# --- 2. 数据准备 (已修正 Pandas 数据) ---
data = [
    # 场景 1: UDF (1M 行)
    {'Scenario': 'UDF (1M Rows)\n', 'Tool': 'Polars.NET', 'Time': 177.6},
    {'Scenario': 'UDF (1M Rows)\n', 'Tool': 'PyPolars',   'Time': 515.1},
    {'Scenario': 'UDF (1M Rows)\n', 'Tool': 'Pandas',     'Time': 656.6},
    
    # 场景 2: GroupBy (10M 行) - 修正：Pandas 应该是慢的那个
    {'Scenario': 'GroupBy (10M Rows)\n',      'Tool': 'Polars.NET', 'Time': 16.75},
    {'Scenario': 'GroupBy (10M Rows)\n',      'Tool': 'PyPolars',   'Time': 17.70},  # PyPolars (Rust) 应该很快
    {'Scenario': 'GroupBy (10M Rows)\n',      'Tool': 'Pandas',     'Time': 145.73}, # Pandas 慢很多
]
df = pd.DataFrame(data)

# --- 3. 配色方案 ---
palette = {
    'Polars.NET': '#E05206',  # 品牌橙
    'PyPolars':   '#7F8C8D',  # 深灰
    'Pandas':     '#BDC3C7'   # 浅灰
}

# --- 4. 绘图 ---
# 稍微增加高度，给底部留空间
fig, ax = plt.subplots(figsize=(10, 7))

tool_order = ['Polars.NET', 'PyPolars', 'Pandas']

chart = sns.barplot(
    data=df, 
    x='Scenario', 
    y='Time', 
    hue='Tool', 
    hue_order=tool_order,
    palette=palette,
    ax=ax,
    edgecolor='none',
    width=0.7
)

# --- 5. 细节美化 ---

plt.title('Polars.NET vs Python Ecosystem\n(Native C# Speed via RyuJIT)', 
          fontsize=16, fontweight='bold', pad=30, color='#333333')
ax.set_ylabel('Execution Time (ms) - Lower is Better', fontweight='bold', labelpad=10, fontsize=12)
ax.set_xlabel('')

# 去边框
sns.despine(left=True, bottom=False)
ax.tick_params(axis='y', length=0)
ax.yaxis.grid(True, linestyle='--', alpha=0.3, color='#CCCCCC')

# 数值标签
for container in ax.containers:
    is_polars = 'Polars.NET' in str(container.get_label())
    for bar in container:
        height = bar.get_height()
        if height > 0:
            font_weight = 'bold' if is_polars else 'normal'
            ax.text(
                bar.get_x() + bar.get_width() / 2, 
                height + 10,
                f'{height:.1f} ms', 
                ha='center', 
                va='bottom', 
                fontsize=11, 
                fontweight=font_weight,
                color='#333333'
            )

# --- 6. 标注 RyuJIT ---
# 这里的坐标硬编码可能需要微调，但在 656ms 上方加标注
udf_max_height = 656.6
ax.text(0, udf_max_height + 100, 
        'RyuJIT compiles C# UDFs\nto Native Code!', 
        ha='center', fontsize=11, color='#E05206', fontweight='bold',
        bbox=dict(boxstyle="round,pad=0.4", fc="white", ec="#E05206", alpha=0.9, linewidth=1.5))

# --- 7. 关键修复：调整底部边距，防止图例糊掉 ---
ax.legend(title='', frameon=False, loc='upper center', bbox_to_anchor=(0.5, -0.12), ncol=3)
ax.set_ylim(0, 850) # 给上方标注留够空间

# 显式增加底部留白 (0.2 应该足够大了)
plt.subplots_adjust(bottom=0.2, top=0.8)

plt.show()