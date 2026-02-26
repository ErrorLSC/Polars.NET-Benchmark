import matplotlib.pyplot as plt
import seaborn as sns
import numpy as np

# --- 1. 设置高级感风格 ---
sns.set_theme(style="white", context="talk") # "white" 比 "whitegrid" 更干净
plt.rcParams['font.family'] = 'sans-serif'
plt.rcParams['text.color'] = '#333333'
plt.rcParams['axes.labelcolor'] = '#333333'
plt.rcParams['xtick.color'] = '#333333'
plt.rcParams['ytick.color'] = '#333333'

# --- 2. 数据准备 ---
methods = ['Polars.NET', 'DuckDB', 'PLINQ', 'MDA']
# 倒序排列，让 Polars.NET 在最上面
methods.reverse() 

times = [1.688, 2.298, 38.723, 171.973]
times.reverse()

memory = [0.0024, 0.0058, 15.32, 498] # GB
memory.reverse()

# 颜色定义
color_polars = '#E05206'  # Polars 经典橙色
color_gray = '#E0E0E0'    # 极浅的灰色，作为背景板
color_memory = '#2C3E50'  # 深蓝灰色，代表内存，稳重

colors = [color_polars if 'Polars' in m else color_gray for m in methods]

# --- 3. 绘图开始 ---
fig, ax1 = plt.subplots(figsize=(12, 6))

# A. 绘制时间 (Bar Chart)
y_pos = np.arange(len(methods))
height = 0.5

# 绘制条形
bars = ax1.barh(y_pos + 0.15, times, height=height, color=colors, edgecolor='none')

# 设置 X 轴 (Time) - Log Scale
ax1.set_xscale('log')
ax1.set_xlabel('Execution Time (seconds) - Log Scale', fontsize=12, fontweight='bold', labelpad=10)
# 关键：右边留足空间，最大值 172，我们设到 2000
ax1.set_xlim(0.8, 2000) 

# 去掉多余的边框
sns.despine(left=True, bottom=False, right=True, top=True)
ax1.tick_params(axis='y', length=0) # 隐藏 Y 轴刻度线

# Y 轴标签
ax1.set_yticks(y_pos + 0.15)
ax1.set_yticklabels(methods, fontsize=13, fontweight='bold')

# B. 绘制内存 (Scatter Plot) - 双轴
ax2 = ax1.twiny() # 创建共享 Y 轴的第二个 X 轴

# 错位绘制：内存点画在条形图下方 (y_pos - 0.2)
ax2.plot(memory, y_pos - 0.2, 'o', color=color_memory, markersize=10, 
         markeredgecolor='white', markeredgewidth=1.5, label='Managed Memory Usage')

# 设置 X 轴 (Memory)
ax2.set_xscale('log')
ax2.set_xlabel('Memory Usage (GB)', color=color_memory, fontsize=12, fontweight='bold', labelpad=10)
ax2.tick_params(axis='x', colors=color_memory)
sns.despine(ax=ax2, top=False, right=True, left=True, bottom=True) # 稍微保留顶部轴线

# 关键：内存轴范围也要对齐，最大 498，设到 2000 保证点不贴边
ax2.set_xlim(0.001, 2000)

# --- 4. 关键：添加精美的数值标签 ---

# 时间标签 (在条形图末尾)
for i, v in enumerate(times):
    # 根据颜色判断是否加粗 Polars 的数据
    fw = 'bold' if 'Polars' in methods[i] else 'normal'
    fc = color_polars if 'Polars' in methods[i] else '#666666'
    
    ax1.text(v * 1.15, y_pos[i] + 0.15, f"{v:.2f} s", 
             va='center', fontsize=11, fontweight=fw, color=fc)

# 内存标签 (在圆点右侧)
for i, m in enumerate(memory):
    # 格式化文本
    txt = f"{m:.4f} GB" if m < 1 else f"{m:.0f} GB"
    if m < 0.01: txt = f"{m*1000:.1f} MB" # 极小内存用 MB 显示更直观
    
    ax2.text(m * 1.25, y_pos[i] - 0.2, txt, 
             va='center', fontsize=10, color=color_memory)

# 标题
plt.title('Performance Benchmark: CSV Parsing & GroupBy (100M Rows)', 
          y=1.2, fontsize=16, fontweight='bold', color='#333333')

# 调整布局，防止文字被切
plt.subplots_adjust(top=0.8, bottom=0.15, left=0.15, right=0.9)

plt.show()