import sqlite3
import requests
import difflib

RAWG_API_KEY = '6033f2aadc2c4528956cb89cb1718d2b'

def create_database(conn):
    cursor = conn.cursor()
    cursor.execute('''
        CREATE TABLE IF NOT EXISTS games (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            name TEXT UNIQUE,
            description TEXT,
            genres TEXT,
            developer TEXT,
            rating REAL
        )
    ''')
    conn.commit()

def fetch_game_from_rawg(game_name):
    url = f"https://api.rawg.io/api/games"
    params = {"search": game_name, "key": RAWG_API_KEY}
    response = requests.get(url, params=params)

    if response.status_code == 200:
        results = response.json().get("results", [])
        if results:
            # Пошук найближчої назви
            all_names = [game.get("name") for game in results]
            closest = difflib.get_close_matches(game_name, all_names, n=1)
            if closest:
                for game in results:
                    if game["name"] == closest[0]:
                        return {
                            "name": game.get("name"),
                            "description": game.get("description_raw", ""),
                            "genres": ", ".join([g["name"] for g in game.get("genres", [])]),
                            "developer": game.get("developers", [{}])[0].get("name", ""),
                            "rating": game.get("rating", 0)
                        }
    return None


def game_exists(name, conn):
    cursor = conn.cursor()
    cursor.execute("SELECT 1 FROM games WHERE name = ?", (name,))
    return cursor.fetchone() is not None

def add_game_if_missing(game_name, conn):
    if game_exists(game_name, conn):
        return False

    data = fetch_game_from_rawg(game_name)
    if data:
        cursor = conn.cursor()
        cursor.execute('''
            INSERT OR IGNORE INTO games (name, description, genres, developer, rating)
            VALUES (?, ?, ?, ?, ?)
        ''', (data['name'], data['description'], data['genres'], data['developer'], data['rating']))
        conn.commit()
        return True
    return False