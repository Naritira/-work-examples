import matplotlib.pyplot as plt
from matplotlib.patches import Circle

fig, ax = plt.subplots(figsize=(10, 10))
ax.set_aspect('equal')
ax.axis('off')

# Параметри кіл: (центр_x, центр_y), радіус, колір, текст
circles = [
    ((0.35, 0.6), 0.22, "skyblue", "Контентна\nфільтрація"),
    ((0.65, 0.6), 0.22, "lightgreen", "KNN"),
    ((0.35, 0.4), 0.22, "gold", "Кластеризація"),
    ((0.65, 0.4), 0.22, "salmon", "Apriori")
]

for (x, y), r, color, label in circles:
    # Малюємо коло
    circle = Circle((x, y), r, fc=color, alpha=0.4, ec="black", lw=1.5)
    ax.add_patch(circle)

    # Додаємо текст у центр
    ax.text(x, y, label,
            ha='center', va='center',
            fontsize=12, fontweight='bold')

# Підписи для перетинів
ax.text(0.5, 0.5, "Гібридні\nрекомендації",
        ha='center', va='center', fontsize=11, color='black')

plt.title("Чотири методи рекомендаційної системи", fontsize=16, pad=20)
plt.xlim(0.1, 0.9)
plt.ylim(0.1, 0.9)
plt.show()