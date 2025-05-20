import tkinter as tk
import numpy as np
from tkinter import ttk, messagebox
import sqlite3
import matplotlib.pyplot as plt
from matplotlib.backends.backend_tkagg import FigureCanvasTkAgg
from hybrid_model import hybrid_recommendations
from steam_integration import get_steam_games

class RecommenderApp:
    def __init__(self, root):
        self.root = root
        self.root.title("Game Recommendation System")
        self.root.geometry("1000x700")
        self.root.configure(bg='white')

        # Стилізація
        self.style = ttk.Style()
        self.style.theme_use('clam')
        self.configure_styles()

        self.create_widgets()
        self.setup_database()

    def configure_styles(self):
        self.style.configure('.', background='white', foreground='black')
        self.style.configure('TFrame', background='white')
        self.style.configure('TLabel', background='white', foreground='black', font=('Segoe UI', 10))
        self.style.configure('TButton', background='#f0f0f0', foreground='black',
                             borderwidth=1, focusthickness=3, focuscolor='#d9d9d9')
        self.style.map('TButton', background=[('active', '#e6e6e6')], foreground=[('active', 'black')])
        self.style.configure('TNotebook', background='white', borderwidth=0)
        self.style.configure('TNotebook.Tab', background='#e6e6e6', padding=[10, 5])
        self.style.map('TNotebook.Tab', background=[('selected', 'white')],
                       foreground=[('selected', 'black')])

    def setup_database(self):
        self.conn = sqlite3.connect('games.db')

    def create_widgets(self):
        # Створення вкладок
        self.notebook = ttk.Notebook(self.root)
        self.notebook.pack(fill=tk.BOTH, expand=True)

        # Вкладка рекомендацій
        self.recommendations_tab = ttk.Frame(self.notebook)
        self.notebook.add(self.recommendations_tab, text='Recommendations')
        self.create_recommendations_tab()

        # Вкладка аналізу
        self.analysis_tab = ttk.Frame(self.notebook)
        self.notebook.add(self.analysis_tab, text='Account Analysis')
        self.create_analysis_tab()

        # Статус бар
        self.status_var = tk.StringVar()
        status_bar = ttk.Label(self.root, textvariable=self.status_var,
                               relief=tk.SUNKEN, anchor=tk.W, font=('Segoe UI', 9))
        status_bar.pack(fill=tk.X, pady=(0, 5))

    def create_recommendations_tab(self):
        # Вміст вкладки рекомендацій
        main_frame = ttk.Frame(self.recommendations_tab, padding="15")
        main_frame.pack(fill=tk.BOTH, expand=True)

        title_label = ttk.Label(main_frame, text="Game Recommendation Engine",
                                font=('Segoe UI', 16, 'bold'))
        title_label.pack(pady=10)

        input_frame = ttk.Frame(main_frame)
        input_frame.pack(fill=tk.X, pady=10)

        ttk.Label(input_frame, text="Steam ID:").pack(side=tk.LEFT)
        self.steam_id_entry = ttk.Entry(input_frame, width=30)
        self.steam_id_entry.pack(side=tk.LEFT, padx=10)
        self.steam_id_entry.insert(0, "76561198447060708")

        ttk.Button(input_frame, text="Get Recommendations",
                   command=self.generate_recommendations).pack(side=tk.LEFT)

        self.results_frame = ttk.Frame(main_frame)
        self.results_frame.pack(fill=tk.BOTH, expand=True)

    def create_analysis_tab(self):
        # Вміст вкладки аналізу
        analysis_frame = ttk.Frame(self.analysis_tab, padding="15")
        analysis_frame.pack(fill=tk.BOTH, expand=True)

        ttk.Label(analysis_frame, text="Account Analysis",
                  font=('Segoe UI', 16, 'bold')).pack(pady=10)

        # Фрейм для графіків
        self.chart_frame = ttk.Frame(analysis_frame)
        self.chart_frame.pack(fill=tk.BOTH, expand=True, pady=10)

        # Інформаційний фрейм
        info_frame = ttk.Frame(analysis_frame)
        info_frame.pack(fill=tk.X, pady=5)

        self.total_games_var = tk.StringVar(value="Total games: -")
        self.top_genre_var = tk.StringVar(value="Top genre: -")
        self.avg_rating_var = tk.StringVar(value="Average rating: -")

        ttk.Label(info_frame, textvariable=self.total_games_var).pack(side=tk.LEFT, padx=10)
        ttk.Label(info_frame, textvariable=self.top_genre_var).pack(side=tk.LEFT, padx=10)
        ttk.Label(info_frame, textvariable=self.avg_rating_var).pack(side=tk.LEFT, padx=10)

    def generate_recommendations(self):
        steam_id = self.steam_id_entry.get()
        if not steam_id:
            messagebox.showerror("Error", "Please enter Steam ID")
            return

        self.status_var.set("Fetching your games from Steam...")
        self.root.update()

        try:
            user_games = get_steam_games(steam_id)

            self.status_var.set("Generating recommendations...")
            self.root.update()

            recommendations = hybrid_recommendations(user_games, self.conn)
            self.show_results(recommendations)

            # Оновлення аналітики
            self.update_analysis(user_games)

            self.status_var.set(f"Success! Found {len(recommendations)} recommendations")

        except Exception as e:
            messagebox.showerror("Error", f"Failed to generate recommendations: {str(e)}")
            self.status_var.set("Error occurred")

    def update_analysis(self, user_games):
        # Очищення попередніх графіків
        for widget in self.chart_frame.winfo_children():
            widget.destroy()

        if not user_games:
            ttk.Label(self.chart_frame, text="No game data available").pack()
            return

        # Отримання даних з бази даних
        cursor = self.conn.cursor()

        # Аналіз жанрів та рейтингів
        genre_counts = {}
        total_rating = 0
        rated_games = 0
        user_game_names = [game['name'] for game in user_games if 'name' in game]

        # Формування SQL-запиту для аналізу
        placeholders = ','.join(['?'] * len(user_game_names))
        query = f"""
        SELECT genres, rating 
        FROM games 
        WHERE name IN ({placeholders}) 
        AND genres IS NOT NULL
        """

        cursor.execute(query, user_game_names)
        db_games = cursor.fetchall()

        for genres, rating in db_games:
            # Підрахунок жанрів
            if genres:
                for genre in genres.split(', '):
                    genre_counts[genre] = genre_counts.get(genre, 0) + 1

            # Підрахунок рейтингів
            if rating:
                total_rating += float(rating)
                rated_games += 1

        # Оновлення текстової інформації
        self.total_games_var.set(f"Total games: {len(user_games)}")

        if genre_counts:
            top_genre = max(genre_counts.items(), key=lambda x: x[1])[0]
            self.top_genre_var.set(f"Top genre: {top_genre}")
        else:
            self.top_genre_var.set("Top genre: N/A")

        if rated_games > 0:
            avg_rating = round(total_rating / rated_games, 2)
            self.avg_rating_var.set(f"Average rating: {avg_rating}")
        else:
            self.avg_rating_var.set("Average rating: N/A")

        # Створення візуалізацій
        self.create_genre_chart(genre_counts)
        self.create_rating_chart(db_games)

    def create_genre_chart(self, genre_counts):
        if not genre_counts:
            ttk.Label(self.chart_frame, text="No genre data available").pack()
            return

        fig_genre = plt.figure(figsize=(6, 4), facecolor='white')
        ax_genre = fig_genre.add_subplot(111)

        genres = list(genre_counts.keys())
        counts = list(genre_counts.values())

        # Сортування та обмеження кількості жанрів
        sorted_indices = np.argsort(counts)[::-1]
        genres = [genres[i] for i in sorted_indices]
        counts = [counts[i] for i in sorted_indices]

        max_genres = 8
        if len(genres) > max_genres:
            other_count = sum(counts[max_genres:])
            genres = genres[:max_genres] + ['Other']
            counts = counts[:max_genres] + [other_count]

        # Кругова діаграма жанрів
        colors = plt.cm.tab20c(np.linspace(0, 1, len(genres)))
        ax_genre.pie(counts, labels=genres, autopct='%1.1f%%',
                     startangle=90, colors=colors, shadow=True)
        ax_genre.set_title('Genre Distribution in Your Library', pad=20)

        canvas_genre = FigureCanvasTkAgg(fig_genre, master=self.chart_frame)
        canvas_genre.draw()
        canvas_genre.get_tk_widget().pack(fill=tk.BOTH, expand=True, padx=10, pady=5)

    def create_rating_chart(self, db_games):
        if not db_games:
            return

        ratings = [float(game[1]) for game in db_games if game[1]]
        if not ratings:
            return

        fig_rating = plt.figure(figsize=(6, 3), facecolor='white')
        ax_rating = fig_rating.add_subplot(111)

        # Гістограма рейтингів
        ax_rating.hist(ratings, bins=10, color='skyblue', edgecolor='black')
        ax_rating.set_title('Rating Distribution')
        ax_rating.set_xlabel('Rating')
        ax_rating.set_ylabel('Number of Games')
        ax_rating.grid(True, linestyle='--', alpha=0.7)

        canvas_rating = FigureCanvasTkAgg(fig_rating, master=self.chart_frame)
        canvas_rating.draw()
        canvas_rating.get_tk_widget().pack(fill=tk.BOTH, expand=True, padx=10, pady=5)

    def show_results(self, recommendations):
        # Очищення попередніх результатів
        for widget in self.results_frame.winfo_children():
            widget.destroy()

        if not recommendations:
            no_results_label = ttk.Label(self.results_frame,
                                         text="No recommendations found",
                                         foreground='black')
            no_results_label.pack()
            return

        # Створення кастомного стилю
        self.style.configure("Game.Treeview",
                             background="white",
                             foreground="black",
                             fieldbackground="white",
                             font=('Segoe UI', 10),
                             rowheight=25)

        self.style.map("Game.Treeview",
                       background=[('selected', '#0078d7')],
                       foreground=[('selected', 'white')])

        # Створення Treeview
        tree = ttk.Treeview(self.results_frame,
                            columns=('num', 'game'),
                            show='headings',
                            style="Game.Treeview",
                            selectmode='browse')

        # Налаштування стовпців
        tree.heading('num', text='#', anchor='center')
        tree.heading('game', text='Game Title', anchor='w')
        tree.column('num', width=40, anchor='center')
        tree.column('game', width=740, anchor='w')

        # Додавання прокрутки
        scrollbar = ttk.Scrollbar(self.results_frame,
                                  orient=tk.VERTICAL,
                                  command=tree.yview)
        tree.configure(yscroll=scrollbar.set)
        scrollbar.pack(side=tk.RIGHT, fill=tk.Y)
        tree.pack(fill=tk.BOTH, expand=True, padx=5, pady=5)

        # Додавання даних
        for i, game in enumerate(recommendations, 1):
            tree.insert('', tk.END, values=(i, game), tags=('game',))

        # Кнопки
        btn_frame = ttk.Frame(self.results_frame)
        btn_frame.pack(pady=10)

        ttk.Button(btn_frame,
                   text="Save to File",
                   command=lambda: self.save_to_file(recommendations)).pack(side=tk.LEFT, padx=5)

        ttk.Button(btn_frame,
                   text="Copy to Clipboard",
                   command=lambda: self.copy_to_clipboard(recommendations)).pack(side=tk.LEFT)

    def save_to_file(self, recommendations):
        with open("game_recommendations.txt", "w", encoding='utf-8') as f:
            f.write("Your Game Recommendations:\n")
            f.write("=" * 30 + "\n")
            for i, game in enumerate(recommendations, 1):
                f.write(f"{i}. {game}\n")
        messagebox.showinfo("Success", "Recommendations saved to game_recommendations.txt")

    def copy_to_clipboard(self, recommendations):
        text = "Recommended Games:\n" + "\n".join(f"{i}. {game}" for i, game in enumerate(recommendations, 1))
        self.root.clipboard_clear()
        self.root.clipboard_append(text)
        messagebox.showinfo("Success", "Copied to clipboard!")

    def on_close(self):
        self.conn.close()
        self.root.destroy()


if __name__ == "__main__":
    root = tk.Tk()
    app = RecommenderApp(root)
    root.protocol("WM_DELETE_WINDOW", app.on_close)
    root.mainloop()