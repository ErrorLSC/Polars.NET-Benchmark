import matplotlib.pyplot as plt
import seaborn as sns
import numpy as np

# --- 1. 风格设置 (与图2保持一致) ---
sns.set_theme(style="white", context="talk")
plt.rcParams['font.family'] = 'sans-serif'
plt.rcParams['text.color'] = '#333333'

# --- 2. 数据准备 ---
methods = ['Deedle', 'Polars.FSharp']
colors = ['#BDC3C7', '#E05206'] # 灰 vs 橙

# --- 3. 绘图布局 ---
fig, (ax1, ax2) = plt.subplots(1, 2, figsize=(12, 6))
# 调整间距
plt.subplots_adjust(wspace=0.3, top=0.85, bottom=0.15)

# =======================
# 子图 1: 时间对比 (Time)
# =======================
times = [1824.86, 41.89] # ms
bars1 = ax1.bar(methods, times, color=colors, width=0.6, edgecolor='none')

# 设置轴
ax1.set_title('Execution Time\n(Lower is Better)', fontsize=14, fontweight='bold', pad=15)
ax1.set_ylabel('Time (ms)', fontweight='bold', fontsize=12)
ax1.set_ylim(0, 2400) # 给上方标注留足空间
sns.despine(ax=ax1, left=True) # 去掉左边框，只留网格
ax1.yaxis.grid(True, linestyle='--', alpha=0.3)
ax1.tick_params(axis='y', length=0)

# 数值标签
for i, rect in enumerate(bars1):
    height = rect.get_height()
    fw = 'bold' if i == 1 else 'normal'
    ax1.text(rect.get_x() + rect.get_width()/2., height + 40,
             f'{height:.1f} ms',
             ha='center', va='bottom', fontsize=12, fontweight=fw, color='#333333')

# 标注: Speedup (复刻图2的气泡)
speedup = times[0] / times[1]
# ax1.text(1, 800, 
#          f'⚡ {speedup:.0f}x Faster!', 
#          ha='center', fontsize=12, color='#E05206', fontweight='bold',
#          bbox=dict(boxstyle="round,pad=0.4", fc="white", ec="#E05206", alpha=0.9, linewidth=1.5))
# # 画个箭头指向 Polars
# ax1.annotate('', xy=(1, 100), xytext=(1, 780),
#              arrowprops=dict(arrowstyle='->', color='#E05206', lw=1.5))


# =======================
# 子图 2: 内存对比 (Memory)
# =======================
memories = [4.24, 0.00000135] # GB
bars2 = ax2.bar(methods, memories, color=colors, width=0.6, edgecolor='none')

# 关键: Log Scale
ax2.set_yscale('log')
ax2.set_title('GC Pressure\n(Lower is Better)', fontsize=14, fontweight='bold', pad=15)
ax2.set_ylabel('Managed Memory (GB) - Log Scale', fontweight='bold', fontsize=12)
ax2.set_ylim(0.0000001, 100) # 范围要涵盖 KB 到 GB

sns.despine(ax=ax2, left=True)
ax2.yaxis.grid(True, linestyle='--', alpha=0.3)
ax2.tick_params(axis='y', length=0)

# 数值标签
for i, rect in enumerate(bars2):
    height = rect.get_height()
    # 智能单位
    if height < 0.00001:
        txt = "1.35 KB"
        v_offset = height * 2.5 # Log Scale下稍微往上一点
    else:
        txt = f"{height:.2f} GB"
        v_offset = height * 1.3
        
    fw = 'bold' if i == 1 else 'normal'
    ax2.text(rect.get_x() + rect.get_width()/2., v_offset,
             txt,
             ha='center', va='bottom', fontsize=12, fontweight=fw, color='#333333')

# 标注: Memory Saving
mem_diff = memories[0] / memories[1]
# 这个气泡放在两柱之间上方
# ax2.text(0.5, 10, 
#          f'{mem_diff:,.0f}x Less Memory', 
#          ha='center', fontsize=12, color='#E05206', fontweight='bold',
#          bbox=dict(boxstyle="round,pad=0.4", fc="white", ec="#E05206", alpha=0.9, linewidth=1.5))

# --- 4. 全局标题 ---
plt.suptitle('Polars.FSharp vs Deedle', 
             fontsize=18, fontweight='bold', y=0.98, color='#333333')

plt.tight_layout()
plt.show()