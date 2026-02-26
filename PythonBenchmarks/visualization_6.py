import matplotlib.pyplot as plt
import pandas as pd
import numpy as np
import seaborn as sns
from matplotlib.ticker import ScalarFormatter

# ==========================================
# 🎨 1. 配置中心 (绝对统一)
# ==========================================
# 重新定义颜色，不再混用 gray/dark_gray，避免歧义
THEMES = {
    'light': {
        'suffix': '_light',
        'text_color': '#24292f',       # GitHub Light 默认字体黑
        'axis_color': '#57606a',       # GitHub Light 默认边框灰
        'grid_color': '#d0d7de',       # 浅灰网格
        'bar_bg_color': '#f6f8fa',     # 辅助背景色 (用于 Python 对比图的弱项)
        'box_bg': '#ffffff',           # 标注框背景
    },
    'dark': {
        'suffix': '_dark',
        'text_color': '#c9d1d9',       # GitHub Dark 默认字体白
        'axis_color': '#8b949e',       # GitHub Dark 默认边框灰
        'grid_color': '#30363d',       # 深灰网格
        'bar_bg_color': '#161b22',     # 辅助背景色
        'box_bg': '#0d1117',           # 标注框背景
    }
}

POLARS_ORANGE = '#E05206' # 品牌色，永远不变

# ==========================================
# 📊 2. 绘图逻辑
# ==========================================

def setup_ax_style(ax, theme):
    """统一设置坐标轴样式，防止 Seaborn 默认样式干扰"""
    ax.set_facecolor('none') # 确保轴背景透明
    
    # 去除边框
    sns.despine(ax=ax, left=True, bottom=False)
    
    # 底部轴线颜色
    ax.spines['bottom'].set_color(theme['axis_color'])
    ax.spines['bottom'].set_linewidth(1)
    
    # 刻度颜色
    ax.tick_params(axis='x', colors=theme['axis_color'], labelsize=11)
    ax.tick_params(axis='y', colors=theme['text_color'], length=0, labelsize=12)
    
    # 网格线
    ax.grid(visible=True, axis='x', color=theme['grid_color'], linestyle='--', alpha=0.5)
    ax.set_axisbelow(True) # 网格线在图层下方

def draw_python_battle(ax, theme):
    # 数据
    data = [
        {'Scenario': 'UDF (1M Rows)', 'Tool': 'Polars.NET', 'Time': 177.6},
        {'Scenario': 'UDF (1M Rows)', 'Tool': 'PyPolars',   'Time': 515.1},
        {'Scenario': 'UDF (1M Rows)', 'Tool': 'Pandas',     'Time': 656.6},
        
        {'Scenario': 'GroupBy (10M)', 'Tool': 'Polars.NET', 'Time': 16.75},
        {'Scenario': 'GroupBy (10M)', 'Tool': 'PyPolars',   'Time': 17.70},
        {'Scenario': 'GroupBy (10M)', 'Tool': 'Pandas',     'Time': 145.73},
    ]
    df = pd.DataFrame(data)
    
    # 配色：Polars橙色，对手用不同深度的灰色(根据主题自动适配)
    # Light模式下对手是灰色，Dark模式下对手也是灰色(但稍亮以保证对比度)
    palette = {
        'Polars.NET': POLARS_ORANGE,
        'PyPolars':   theme['axis_color'], # 用轴线颜色作为强灰色
        'Pandas':     theme['grid_color']  # 用网格颜色作为弱灰色
    }
    
    # 绘图
    sns.barplot(
        data=df, x='Scenario', y='Time', hue='Tool',
        hue_order=['Polars.NET', 'PyPolars', 'Pandas'],
        palette=palette, ax=ax, edgecolor='none', width=0.7
    )
    
    # 样式应用
    setup_ax_style(ax, theme)
    ax.set_title('Polars.NET vs Python Ecosystem', fontsize=18, fontweight='bold', pad=20, color=theme['text_color'])
    ax.set_ylabel('Execution Time (ms)', fontsize=12, fontweight='bold', color=theme['text_color'])
    ax.set_xlabel('')
    
    # 数值标签
    for container in ax.containers:
        for bar in container:
            h = bar.get_height()
            if h > 0:
                ax.text(
                    bar.get_x() + bar.get_width()/2, h + 15, f'{h:.0f}',
                    ha='center', va='bottom', fontsize=10, fontweight='bold',
                    color=theme['text_color'] # 强制使用主题文字颜色
                )
    
    # 标注框 (RyuJIT)
    ax.text(0, 800, 'RyuJIT Native Speed', ha='center', fontsize=12, 
            color=POLARS_ORANGE, fontweight='bold',
            bbox=dict(boxstyle="round,pad=0.4", fc=theme['box_bg'], ec=POLARS_ORANGE, alpha=1))
    
    # 图例
    leg = ax.legend(frameon=False, loc='upper center', bbox_to_anchor=(0.5, -0.15), ncol=3)
    for text in leg.get_texts():
        text.set_color(theme['text_color'])

    ax.set_ylim(0, 1000)

def draw_summary(ax, theme):
    # 数据
    data = [
        {'Task': 'CSV Parsing\n(vs MS.Data.Analysis)', 'Speedup': 102},
        {'Task': 'Rolling Window\n(vs Deedle)',   'Speedup': 44},
        {'Task': 'Join (20M)\n(vs LINQ)',         'Speedup': 24},
        {'Task': 'Excel Read\n(vs EPPlus)',       'Speedup': 2.4},
    ]
    df = pd.DataFrame(data)
    
    # 绘图
    y_pos = np.arange(len(df))
    ax.barh(y_pos, df['Speedup'], color=POLARS_ORANGE, height=0.5)
    
    # 样式应用
    setup_ax_style(ax, theme)
    ax.set_yticks(y_pos)
    ax.set_yticklabels(df['Task'], fontsize=12, fontweight='bold', color=theme['text_color'])
    
    ax.set_title('Speedup vs Traditional .NET', fontsize=18, fontweight='bold', pad=20, color=theme['text_color'])
    ax.set_xlabel('Speedup Factor (Log Scale)', fontsize=12, fontweight='bold', color=theme['text_color'])
    
    # Log Scale
    ax.set_xscale('log')
    ax.set_xlim(1, 150)
    ax.xaxis.set_major_formatter(ScalarFormatter())
    
    # 数值标签
    for i, v in enumerate(df['Speedup']):
        label = f"{v:.1f}x"
        if v > 100: label = "~100x"
        # 标签统一使用橙色，显眼
        ax.text(v * 1.1, i, label, va='center', fontsize=12, fontweight='bold', color=POLARS_ORANGE)

# ==========================================
# 🚀 3. 执行生成
# ==========================================
if __name__ == "__main__":
    # 全局字体
    plt.rcParams['font.family'] = 'sans-serif'
    plt.rcParams['font.sans-serif'] = ['Arial', 'Segoe UI', 'DejaVu Sans']

    for name, theme in THEMES.items():
        # 1. 生成 Python 对比图
        fig, ax = plt.subplots(figsize=(10, 6))
        fig.patch.set_alpha(0.0) # 透明背景
        draw_python_battle(ax, theme)
        plt.tight_layout()
        plt.savefig(f"benchmark_python{theme['suffix']}.png", dpi=300, transparent=True)
        plt.close()
        print(f"Generated benchmark_python{theme['suffix']}.png ({name} mode)")

        # 2. 生成 Summary 图
        fig, ax = plt.subplots(figsize=(10, 5))
        fig.patch.set_alpha(0.0)
        draw_summary(ax, theme)
        plt.tight_layout()
        plt.savefig(f"benchmark_summary{theme['suffix']}.png", dpi=300, transparent=True)
        plt.close()
        print(f"Generated benchmark_summary{theme['suffix']}.png ({name} mode)")