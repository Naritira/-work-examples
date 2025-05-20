import matplotlib.pyplot as plt
import numpy as np
from sklearn.cluster import KMeans
from sklearn.datasets import make_blobs
from sklearn.decomposition import PCA

# 1. Ілюстрація "Метод ліктя для K-means"
# Генеруємо синтетичні дані
X, _ = make_blobs(n_samples=300, centers=4, cluster_std=1.0, random_state=42)

# Обчислюємо SSE для різних k
sse = []
for k in range(1, 10):
    kmeans = KMeans(n_clusters=k, random_state=42)
    kmeans.fit(X)
    sse.append(kmeans.inertia_)

# Малюємо графік методу ліктя
plt.figure(figsize=(10, 5))
plt.plot(range(1, 10), sse, marker='o', linestyle='--')
plt.title('Метод ліктя для вибору оптимальної кількості кластерів')
plt.xlabel('Кількість кластерів (k)')
plt.ylabel('SSE (Сума квадратів відстаней)')
plt.axvline(x=4, color='r', linestyle='-', label='Оптимальне k')
plt.legend()
plt.grid(True)
plt.show()

# 2. Ілюстрація "Приклади кластеризації ігор за жанрами"
# Генеруємо синтетичні дані про ігри (жанри у вигляді координат)
genres = {
    'Екшн': [9, 2],
    'RPG': [2, 8],
    'Стратегія': [8, 7],
    'Пригоди': [3, 3],
    'Симулятор': [7, 5]
}

# Створюємо "ігри" з різними жанрами
np.random.seed(42)
games = []
for genre, center in genres.items():
    games.append(np.random.normal(loc=center, scale=1.0, size=(20, 2)))

X_games = np.vstack(games)

# Виконуємо кластеризацію
kmeans = KMeans(n_clusters=5, random_state=42)
clusters = kmeans.fit_predict(X_games)

# Візуалізуємо результати
plt.figure(figsize=(10, 8))
scatter = plt.scatter(X_games[:,0], X_games[:,1], c=clusters, cmap='viridis', s=70)
plt.title('Кластеризація ігор за жанрами')
plt.xlabel('Інтенсивність ігрового процесу')
plt.ylabel('Складність стратегії')
plt.colorbar(scatter, label='Кластери')
plt.grid(True)

# Додаємо мітки жанрів
for genre, center in genres.items():
    plt.text(center[0], center[1], genre, fontsize=12, ha='center', va='center',
             bbox=dict(facecolor='white', alpha=0.8, edgecolor='black'))

plt.show()