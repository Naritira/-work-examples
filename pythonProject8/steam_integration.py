import requests

def get_steam_games(steam_id):
    url = "http://api.steampowered.com/IPlayerService/GetOwnedGames/v0001/"
    params = {
        'key': 'A59B3C0D56502AE3F71303E0CF21FA97',
        'steamid': steam_id,
        'format': 'json',
        'include_appinfo': 1,
        'include_played_free_games': 1
    }

    response = requests.get(url, params=params)
    if response.status_code == 200:
        data = response.json()
        games = data.get("response", {}).get("games", [])
        return [{
            'name': game['name'],
            'playtime_forever': game.get('playtime_forever', 0),  # загальний час у хвилинах
            'playtime_2weeks': game.get('playtime_2weeks', 0)    # час за останні  2 тижні у хвилинах
        } for game in games if 'name' in game]
    return []


