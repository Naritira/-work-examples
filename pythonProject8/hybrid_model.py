from sklearn.feature_extraction.text import TfidfVectorizer
from sklearn.metrics.pairwise import cosine_similarity
from sklearn.neighbors import NearestNeighbors
from sklearn.cluster import KMeans
from sklearn.decomposition import TruncatedSVD
from scipy.sparse import csr_matrix
from mlxtend.frequent_patterns import apriori
import pandas as pd
import numpy as np
from collections import defaultdict
import warnings
import re


def hybrid_recommendations(user_games, conn):
    warnings.filterwarnings("ignore", category=UserWarning,
                            message="The parameter 'token_pattern' will not be used since 'tokenizer' is not None'")
    warnings.filterwarnings("ignore", category=DeprecationWarning)

    # Завантаження даних
    df = pd.read_sql_query("SELECT id, name, genres, rating FROM games", conn)
    if df.empty:
        return []

    # Підготовка даних
    df = df.fillna({'genres': 'Unknown', 'rating': 0})

    # Аналіз ігор користувача
    user_game_names = {game['name'] for game in user_games if 'name' in game}
    total_playtime = sum(game.get('playtime_forever', 0) for game in user_games)

    # Визначаємо улюблені жанри на основі часу гри
    genre_weights = defaultdict(float)
    for game in user_games:
        if 'genres' in game and 'playtime_forever' in game:
            playtime = game['playtime_forever']
            for genre in game['genres'].split(', '):
                genre_weights[genre] += playtime

    # Нормалізуємо ваги жанрів
    if genre_weights:
        max_weight = max(genre_weights.values())
        for genre in genre_weights:
            genre_weights[genre] /= max_weight

    # Додаємо ваги жанрів до features
    def enhance_features(row):
        features = row['genres'].replace(',', ' ') + " " + str(row['rating']) + " " + row['name']
        if genre_weights:
            for genre, weight in genre_weights.items():
                if genre in row['genres']:
                    features += " " + (genre + " ") * int(1 + weight * 3)
        return features

    df['features'] = df.apply(enhance_features, axis=1)
    df = df[~df['name'].isin(user_game_names)].copy()
    if df.empty:
        return []

    # Ініціалізація TF-IDF
    tfidf = TfidfVectorizer(stop_words='english', tokenizer=lambda x: x.split())
    features_matrix = tfidf.fit_transform(df['features'])

    # --- Оновлені методи рекомендацій ---
    def get_content_top():
        if not user_games:
            return []
        user_features = []
        for game in user_games:
            feature = f"{game.get('genres', 'Unknown')} {game.get('rating', 0)} {game.get('name', '')}"
            # Додаємо додаткову вагу для ігор з великим часом гри
            playtime_weight = min(3, 1 + game.get('playtime_forever', 0) / 1000)
            user_features.extend([feature] * int(playtime_weight))

        user_matrix = tfidf.transform(user_features)
        sim_scores = cosine_similarity(user_matrix, features_matrix).mean(axis=0)
        return df.iloc[np.argsort(sim_scores)[-100:]]['name'].tolist()

    def get_knn_top():
        if not user_games:
            return []
        knn = NearestNeighbors(n_neighbors=min(10, len(df)), metric='cosine')
        knn.fit(features_matrix)
        knn_candidates = set()
        for game in user_games:
            game_features = f"{game.get('genres', 'Unknown')} {game.get('rating', 0)} {game.get('name', '')}"
            vec = tfidf.transform([game_features])
            _, indices = knn.kneighbors(vec, n_neighbors=int(2 + game.get('playtime_forever', 0) / 500))
            knn_candidates.update(df.iloc[indices[0]]['name'])
        return list(knn_candidates)[:100]

    def get_weighted_hybrid():
        df['norm_rating'] = (df['rating'] - df['rating'].min()) / (df['rating'].max() - df['rating'].min())
        genre_tfidf = TfidfVectorizer(tokenizer=lambda x: x.split(', '))
        genre_matrix = genre_tfidf.fit_transform(df['genres'])

        # Додаємо ваги жанрів на основі часу гри
        genre_weights_array = np.zeros(len(df))
        for i, genres in enumerate(df['genres']):
            for genre in genres.split(', '):
                genre_weights_array[i] += genre_weights.get(genre, 0)

        genre_scores = np.asarray(genre_matrix.mean(axis=1)).flatten()
        rating_scores = df['norm_rating'].values
        hybrid_scores = 0.5 * genre_scores + 0.2 * rating_scores + 0.3 * genre_weights_array
        return df.iloc[np.argsort(hybrid_scores)[-100:]]['name'].tolist()

    def get_cluster_recommendations():
        genre_tfidf = TfidfVectorizer(tokenizer=lambda x: x.split(', '))
        genre_vectors = genre_tfidf.fit_transform(df['genres'])
        kmeans = KMeans(n_clusters=min(10, len(df) - 1))
        clusters = kmeans.fit_predict(genre_vectors)

        # Визначаємо кластери з урахуванням часу гри
        user_clusters = defaultdict(float)
        for game in user_games:
            if game['name'] in df['name'].values:
                idx = df.index[df['name'] == game['name']].tolist()[0]
                user_clusters[clusters[idx]] += game.get('playtime_forever', 0)

        if not user_clusters:
            return []

        # Вибираємо топ-3 кластери за сумарним часом
        top_clusters = sorted(user_clusters.items(), key=lambda x: -x[1])[:3]
        top_cluster_ids = [c[0] for c in top_clusters]
        mask = np.isin(clusters, top_cluster_ids)
        return df[mask].nlargest(100, 'rating')['name'].tolist()

    def get_apriori_recommendations():
        try:
            # Створення бінарної матриці жанрів з урахуванням ваг
            genres_dummies = df['genres'].str.get_dummies(', ')

            # Додаємо ваги на основі часу гри в жанрах
            for genre in genre_weights:
                if genre in genres_dummies.columns:
                    genres_dummies[genre] = genres_dummies[genre] * (1 + genre_weights[genre])

            # Знаходження частіших наборів з урахуванням ваг
            frequent_itemsets = apriori(genres_dummies, min_support=0.1, use_colnames=True)

            # Визначаємо топ-5 жанрів користувача за часом гри
            top_user_genres = [genre for genre, _ in sorted(genre_weights.items(),
                                                            key=lambda x: -x[1])[:5]] if genre_weights else []

            # Пошук рекомендацій на основі асоціативних правил
            recommendations = set()
            for _, itemset in frequent_itemsets.iterrows():
                # Додатковий фільтр - мінімум 50% жанрів мають співпадати з топовими
                matching_genres = sum(1 for genre in itemset['itemsets'] if genre in top_user_genres)
                if matching_genres >= max(1, len(itemset['itemsets']) / 2):
                    recommendations.update(itemset['itemsets'])

            # Вибір ігор, що містять рекомендовані жанри
            if recommendations:
                genre_pattern = '|'.join([re.escape(str(genre)) for genre in recommendations])
                mask = df['genres'].str.contains(genre_pattern)
                # Сортуємо за рейтингом і вагою жанрів
                return (df[mask]
                        .assign(genre_weight=df['genres'].apply(
                    lambda x: sum(genre_weights.get(g, 0) for g in x.split(', '))))
                        .sort_values(['genre_weight', 'rating'], ascending=False)
                        ['name'].tolist()[:100])
            return []
        except Exception as e:
            print(f"Apriori Error: {e}")
            return []

    # --- Виклик методів ---
    methods = {
        "content": get_content_top(),
        "knn": get_knn_top(),
        "hybrid": get_weighted_hybrid(),
        "cluster": get_cluster_recommendations(),
        "apriori": get_apriori_recommendations()
    }

    # --- Комбінування результатів ---
    recommendations = defaultdict(float)
    for method_name, items in methods.items():
        for i, name in enumerate(items):
            weight = 1.5 if method_name in ["cluster", "apriori"] else  1.25 if method_name in ["knn", "hybrid"] else 1.0
            recommendations[name] += (100 - i) * weight

    return [name for name, _ in sorted(recommendations.items(), key=lambda x: -x[1])[:11]]