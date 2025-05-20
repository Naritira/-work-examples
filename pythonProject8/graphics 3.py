# Імпорт бібліотек
import pandas as pd
import numpy as np
import matplotlib.pyplot as plt
import seaborn as sns
from sklearn.preprocessing import MinMaxScaler, StandardScaler
from sklearn.feature_extraction.text import TfidfVectorizer


# ----------------- Ілюстрація 1: Нормалізація часу гри та рейтингів -----------------
def plot_normalization_example():
    # Симуляція даних
    data = {
        'Час гри (хв)': [1200, 45, 300, 600, 30],
        'Рейтинг (1-5)': [4.5, 2.3, 3.8, 4.1, 1.5]
    }
    df = pd.DataFrame(data)

    # Нормалізація Min-Max та Z-score
    scaler_minmax = MinMaxScaler()
    scaler_zscore = StandardScaler()

    df['Час гри (MinMax)'] = scaler_minmax.fit_transform(df[['Час гри (хв)']])
    df['Рейтинг (MinMax)'] = scaler_minmax.fit_transform(df[['Рейтинг (1-5)']])
    df['Час гри (Z-score)'] = scaler_zscore.fit_transform(df[['Час гри (хв)']])
    df['Рейтинг (Z-score)'] = scaler_zscore.fit_transform(df[['Рейтинг (1-5)']])

    # Візуалізація
    fig, axes = plt.subplots(2, 2, figsize=(12, 8))
    df[['Час гри (хв)', 'Рейтинг (1-5)']].plot(kind='bar', ax=axes[0, 0], title='До нормалізації')
    df[['Час гри (MinMax)', 'Рейтинг (MinMax)']].plot(kind='bar', ax=axes[0, 1], title='Min-Max')
    df[['Час гри (Z-score)', 'Рейтинг (Z-score)']].plot(kind='bar', ax=axes[1, 0], title='Z-score')
    fig.delaxes(axes[1, 1])
    plt.suptitle('Нормалізація даних: Час гри vs Рейтинг')
    plt.tight_layout()
    plt.show()


# ----------------- Ілюстрація 2: TF-IDF для жанрів -----------------
def plot_tfidf_heatmap():
    # Приклад даних: ігри та їхні жанри
    games = [
        "RPG, Пригоди, Відкритий світ",
        "Стратегія, Економіка, Історичний",
        "Екшн, Шутер, Мультиплеєр"
    ]

    # Розрахунок TF-IDF
    vectorizer = TfidfVectorizer()
    tfidf_matrix = vectorizer.fit_transform(games)
    features = vectorizer.get_feature_names_out()

    # Теплова карта
    plt.figure(figsize=(10, 4))
    sns.heatmap(tfidf_matrix.toarray(),
                annot=True,
                xticklabels=features,
                yticklabels=["Гра 1", "Гра 2", "Гра 3"],
                cmap="YlGnBu")
    plt.title('TF-IDF ваги для ігрових жанрів')
    plt.xlabel('Жанри')
    plt.ylabel('Ігри')
    plt.show()


# ----------------- Ілюстрація 3: Матриця ознак -----------------
def plot_feature_matrix():
    # Приклад матриці ознак
    data = {
        'Жанр: RPG': [1, 0, 1],
        'Жанр: Стратегія': [0, 1, 0],
        'Час гри (норм.)': [0.9, 0.2, 0.5],
        'Рейтинг (норм.)': [0.8, 0.4, 0.6]
    }
    df = pd.DataFrame(data, index=['Гра A', 'Гра B', 'Гра C'])

    # Візуалізація
    plt.figure(figsize=(8, 4))
    sns.heatmap(df, annot=True, cmap="Blues", cbar=False)
    plt.title('Матриця ознак для рекомендаційної системи')
    plt.show()


# ----------------- Ілюстрація 4: Згасання ваг -----------------
def plot_decay_weights():
    # Симуляція експоненційного згасання
    time = np.linspace(0, 30, 100)  # Дні
    weights = np.exp(-0.2 * time)  # λ = 0.2

    # Графік
    plt.figure(figsize=(10, 5))
    plt.plot(time, weights, color='#e34a33', linewidth=2)
    plt.title('Експоненційне згасання ваг ігрових сесій')
    plt.xlabel('Дні з моменту останньої гри')
    plt.ylabel('Вага')
    plt.grid(alpha=0.3)
    plt.annotate('Ваги старих ігор\nстрімко зменшуються',
                 xy=(10, 0.1),
                 xytext=(15, 0.5),
                 arrowprops=dict(facecolor='black', shrink=0.05))
    plt.show()


# ----------------- Запуск усіх функцій -----------------
if __name__ == "__main__":
    plot_normalization_example()
    plot_tfidf_heatmap()
    plot_feature_matrix()
    plot_decay_weights()