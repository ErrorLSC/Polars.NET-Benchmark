import matplotlib.pyplot as plt
import seaborn as sns
import pandas as pd
import numpy as np

# --- 全局风格 ---
sns.set_theme(style="white", context="talk")
plt.rcParams['font.family'] = 'sans-serif'
plt.rcParams['text.color'] = '#333333'

# ==========================================
# 图 1: Excel Parsing (单项战)
# ==========================================
def plot_excel():
    methods = ['Polars.NET', 'MiniExcel', 'EPPlus']
    times = [14.74, 28.26, 34.84] # seconds
    colors = ['#E05206', '#BDC3C7', '#BDC3C7'] # 主角橙 vs 配角灰
    
    fig, ax = plt.subplots(figsize=(10, 5))
    
    # 倒序画，让 Polars 在最上面
    y_pos = np.arange(len(methods))
    ax.barh(y_pos, times, color=colors, height=0.6)
    
    ax.set_title('Excel Parsing (1M Rows, 20 Cols)', fontsize=16, fontweight='bold', pad=20)
    ax.set_xlabel('Time (seconds) - Lower is Better', fontsize=12, fontweight='bold')
    
    # 轴设置
    ax.set_yticks(y_pos)
    ax.set_yticklabels(methods, fontsize=13, fontweight='bold')
    sns.despine(left=True, bottom=False)
    ax.tick_params(axis='y', length=0)
    
    # 数值标签
    for i, v in enumerate(times):
        fw = 'bold' if i == 0 else 'normal'
        col = '#E05206' if i == 0 else '#333333'
        ax.text(v + 1, i, f"{v:.2f} s", va='center', fontsize=12, fontweight=fw, color=col)
        
    plt.tight_layout()
    plt.show()

# ==========================================
# 图 2: 降维打击总览 (Speedup Summary)
# ==========================================
def plot_summary():
    # 数据：场景，基准工具，Polars倍数
    data = [
        {'Task': 'CSV Parsing\n(vs MS.Data.Analysis)', 'Speedup': 102},
        {'Task': 'Rolling Window\n(vs Deedle)',   'Speedup': 44},
        {'Task': 'Join (20M)\n(vs LINQ)',         'Speedup': 24},
        {'Task': 'Excel Read\n(vs EPPlus)',       'Speedup': 2.4},
    ]
    df = pd.DataFrame(data)
    
    fig, ax = plt.subplots(figsize=(12, 6))
    
    # 颜色映射：倍数越高颜色越深，或者统一用品牌橙
    # 这里用统一橙色，干净有力
    bars = ax.barh(df['Task'], df['Speedup'], color='#E05206', height=0.5)
    
    ax.set_title('Polars.NET vs Traditional .NET Ecosystem\n(Speedup Factor)', 
                 fontsize=18, fontweight='bold', pad=30, color='#333333')
    ax.set_xlabel('Speedup (x Times Faster)', fontsize=12, fontweight='bold')
    
    # Log Scale: 因为 100x 和 2x 差距太大，用对数坐标能让 Excel 的 2.4x 也被看见
    ax.set_xscale('log')
    # 手动设置范围，保证 1x 左边有空隙，100x 右边有空隙
    ax.set_xlim(1, 150)
    
    # 格式化 X 轴刻度 (1x, 10x, 100x)
    from matplotlib.ticker import ScalarFormatter
    ax.xaxis.set_major_formatter(ScalarFormatter())
    
    sns.despine(left=True, bottom=False)
    ax.tick_params(axis='y', length=0)
    ax.set_yticklabels(df['Task'], fontsize=13, fontweight='bold')

    # 数值标签
    for i, v in enumerate(df['Speedup']):
        # 在条形图末尾添加文字
        label = f"{v:.1f}x"
        if v > 100: label = "~100x" # 特效
        
        ax.text(v * 1.1, i, label, va='center', fontsize=14, fontweight='bold', color='#E05206')

    plt.tight_layout()
    plt.show()

# 运行绘图
plot_excel()
plot_summary()