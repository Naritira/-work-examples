from steam_integration import get_steam_games
from game_database import create_database, add_game_if_missing
import sqlite3

def update_database_from_steam():
    steam_id = input("Enter your Steam ID: ")
    user_games = get_steam_games(steam_id)

    conn = sqlite3.connect("games.db")
    create_database(conn)

    print("\nUpdating database with your Steam games...")
    for game in user_games:
        success = add_game_if_missing(game['name'], conn)
        if success:
            print(f"✅ Added: {game['name']}")
        else:
            print(f"ℹ️ Already exists or not found: {game['name']}")

    cursor = conn.cursor()
    cursor.execute("SELECT COUNT(*) FROM games")
    total = cursor.fetchone()[0]
    print(f"\nTotal games in database: {total}")

    conn.close()

if __name__ == "__main__":
    update_database_from_steam()