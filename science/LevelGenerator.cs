using Godot;
using System;
using System.Collections.Generic;
using System.Globalization;

public partial class LevelGenerator : Node2D
{
	[Export] public TileMapLayer TileMap;
	[Export] public Node2D Player;

	[Export] public int Width = 80;
	[Export] public int Height = 80;
	[Export] public int Iteration = 100;

	// ===== Алгоритмы =====
	public enum GenerationType
	{
		RandomWalk,
		CellularAutomata,
		Rooms,
		BSP,
		WFC
	}

	[Export] public GenerationType GenType = GenerationType.RandomWalk;

	// ===== Random Walk 
	[Export] public int WalkerCount = 4;
	[Export] public float FloorDensity = 0.4f;

	// ===== Cellular Automata
	[Export] public float InitialFill = 0.45f;
	[Export] public int SimulationSteps = 4;

	// ===== Rooms 
	[Export] public int RoomCount = 10;
	[Export] public int RoomMinSize = 5;
	[Export] public int RoomMaxSize = 12;
	
	// ===== BSP 
	[Export] public int MinRoomSize = 6;
	[Export] public int MaxDepth = 4;
	
	[ExportGroup("WFC")]
	[Export] public int WFCIterations = 5000;
	[ExportGroup("Post Processing")]

	[Export] public bool UsePostProcessing = false;
	[Export] public bool RemoveRegionsEnabled = true;
	[Export] public bool SmoothEnabled = true;

	[Export] public int MinRegionSize = 30;
	
	[Export] public int SmoothIterations = 2;
	private int[,] map;
	private Random rand = new();

	private const int WALL = 0;
	private const int FLOOR = 1;

	public override void _Ready()
	{
		InitCSV();

		for (int i = 0; i < Iteration; i++)
		{
			Generate();
			Analyze();
		}
		
		ApplyPostProcessing();
		Draw();
		SpawnPlayer();
	}

	// =========================
	// GENERATOR SWITCH
	// =========================
	void Generate()
	{
		switch (GenType)
		{
			case GenerationType.RandomWalk:
				GenerateRandomWalk();
				break;

			case GenerationType.CellularAutomata:
				GenerateCellularAutomata();
				break;

			case GenerationType.Rooms:
				GenerateRooms();
				break;

			case GenerationType.BSP:
				GenerateBSP();
				break;

			case GenerationType.WFC:
			GenerateWFC();
			break;
		}
	}
	
	void ApplyPostProcessing()
	{
		if (!UsePostProcessing)
			return;

		if (RemoveRegionsEnabled)
			RemoveSmallRegions(MinRegionSize);

		if (SmoothEnabled)
			SmoothMap(SmoothIterations);
	}
	// =========================
	// RANDOM WALK
	// =========================
	void GenerateRandomWalk()
	{
		map = new int[Width, Height];

		List<Vector2I> walkers = new();

		for (int i = 0; i < WalkerCount; i++)
			walkers.Add(new Vector2I(Width / 2, Height / 2));

		int target = (int)(Width * Height * FloorDensity);
		int floorCount = 0;

		while (floorCount < target)
		{
			for (int i = 0; i < walkers.Count; i++)
			{
				var w = walkers[i];

				if (map[w.X, w.Y] == WALL)
				{
					map[w.X, w.Y] = FLOOR;
					floorCount++;
				}

				int dir = rand.Next(4);

				switch (dir)
				{
					case 0: w.X++; break;
					case 1: w.X--; break;
					case 2: w.Y++; break;
					case 3: w.Y--; break;
				}

				w.X = Mathf.Clamp(w.X, 1, Width - 2);
				w.Y = Mathf.Clamp(w.Y, 1, Height - 2);

				walkers[i] = w;
			}
		}
	}

	// =========================
	// CELLULAR AUTOMATA
	// =========================
	void GenerateCellularAutomata()
	{
		map = new int[Width, Height];

		for (int x = 0; x < Width; x++)
		for (int y = 0; y < Height; y++)
		{
			// границы = стены
			if (x == 0 || y == 0 || x == Width - 1 || y == Height - 1)
			{
				map[x, y] = WALL;
				continue;
			}

			// 💥 ВАЖНО: меньше стен, больше пола
			map[x, y] = rand.NextDouble() < 0.45 ? WALL : FLOOR;
		}

		for (int i = 0; i < SimulationSteps; i++)
			map = SimulationStep(map);
	}

	int[,] SimulationStep(int[,] oldMap)
	{
		int[,] newMap = new int[Width, Height];

		for (int x = 0; x < Width; x++)
		for (int y = 0; y < Height; y++)
		{
			int walls = CountWallsAround(oldMap, x, y);

			// 💥 мягкое правило (ключ к "красоте")
			if (walls > 4)
				newMap[x, y] = WALL;
			else if (walls < 4)
				newMap[x, y] = FLOOR;
			else
				newMap[x, y] = oldMap[x, y];
		}

		return newMap;
	}
	
	int CountWallsAround(int[,] m, int x, int y)
	{
		int count = 0;

		for (int dx = -1; dx <= 1; dx++)
		for (int dy = -1; dy <= 1; dy++)
		{
			if (dx == 0 && dy == 0) continue;

			int nx = x + dx;
			int ny = y + dy;

			if (nx < 0 || ny < 0 || nx >= Width || ny >= Height)
				count++;
			else if (m[nx, ny] == WALL)
				count++;
		}

		return count;
	}

	// =========================
	// ROOMS
	// =========================
	void GenerateRooms()
	{
		map = new int[Width, Height];

		for (int x = 0; x < Width; x++)
		for (int y = 0; y < Height; y++)
			map[x, y] = WALL;

		List<Rect2I> rooms = new();

		for (int i = 0; i < RoomCount; i++)
		{
			int w = rand.Next(RoomMinSize, RoomMaxSize);
			int h = rand.Next(RoomMinSize, RoomMaxSize);

			int x = rand.Next(1, Width - w - 1);
			int y = rand.Next(1, Height - h - 1);

			Rect2I newRoom = new Rect2I(x, y, w, h);

			bool overlaps = false;

			foreach (var room in rooms)
			{
				if (room.Intersects(newRoom))
				{
					overlaps = true;
					break;
				}
			}

			if (overlaps) continue;

			CreateRoom(newRoom);

			if (rooms.Count > 0)
			{
				var prev = GetCenter(rooms[^1]);
				var curr = GetCenter(newRoom);
				CreateCorridor(prev, curr);
			}

			rooms.Add(newRoom);
		}
	}

	void CreateRoom(Rect2I room)
	{
		for (int x = room.Position.X; x < room.End.X; x++)
		for (int y = room.Position.Y; y < room.End.Y; y++)
			map[x, y] = FLOOR;
	}

	Vector2I GetCenter(Rect2I room)
	{
		return new Vector2I(
			room.Position.X + room.Size.X / 2,
			room.Position.Y + room.Size.Y / 2
		);
	}

	void CreateCorridor(Vector2I a, Vector2I b)
	{
		if (rand.Next(2) == 0)
		{
			CreateHorizontal(a.X, b.X, a.Y);
			CreateVertical(a.Y, b.Y, b.X);
		}
		else
		{
			CreateVertical(a.Y, b.Y, a.X);
			CreateHorizontal(a.X, b.X, b.Y);
		}
	}

	void CreateHorizontal(int x1, int x2, int y)
	{
		for (int x = Mathf.Min(x1, x2); x <= Mathf.Max(x1, x2); x++)
			map[x, y] = FLOOR;
	}

	void CreateVertical(int y1, int y2, int x)
	{
		for (int y = Mathf.Min(y1, y2); y <= Mathf.Max(y1, y2); y++)
			map[x, y] = FLOOR;
	}
	class BSPNode
	{
		public Rect2I Rect;
		public BSPNode Left;
		public BSPNode Right;
		public Rect2I Room;

		public BSPNode(Rect2I rect)
		{
			Rect = rect;
		}
	}
	
	void GenerateBSP()
	{
		map = new int[Width, Height];

		// всё стены
		for (int x = 0; x < Width; x++)
		for (int y = 0; y < Height; y++)
			map[x, y] = WALL;

		BSPNode root = new BSPNode(new Rect2I(1, 1, Width - 2, Height - 2));

		Split(root, 0);

		CreateRoomsBSP(root);
		ConnectRooms(root);
	}
	
	void Split(BSPNode node, int depth)
	{
		if (depth >= MaxDepth)
			return;

		bool splitHorizontally = rand.Next(2) == 0;

		if (node.Rect.Size.X < MinRoomSize * 2 &&
			node.Rect.Size.Y < MinRoomSize * 2)
			return;

		if (node.Rect.Size.X > node.Rect.Size.Y)
			splitHorizontally = false;
		else if (node.Rect.Size.Y > node.Rect.Size.X)
			splitHorizontally = true;

		if (splitHorizontally)
		{
			if (node.Rect.Size.Y < MinRoomSize * 2) return;

			int split = rand.Next(MinRoomSize, node.Rect.Size.Y - MinRoomSize);

			node.Left = new BSPNode(new Rect2I(
				node.Rect.Position,
				new Vector2I(node.Rect.Size.X, split)));

			node.Right = new BSPNode(new Rect2I(
				new Vector2I(node.Rect.Position.X, node.Rect.Position.Y + split),
				new Vector2I(node.Rect.Size.X, node.Rect.Size.Y - split)));
		}
		else
		{
			if (node.Rect.Size.X < MinRoomSize * 2) return;

			int split = rand.Next(MinRoomSize, node.Rect.Size.X - MinRoomSize);

			node.Left = new BSPNode(new Rect2I(
				node.Rect.Position,
				new Vector2I(split, node.Rect.Size.Y)));

			node.Right = new BSPNode(new Rect2I(
				new Vector2I(node.Rect.Position.X + split, node.Rect.Position.Y),
				new Vector2I(node.Rect.Size.X - split, node.Rect.Size.Y)));
		}

		Split(node.Left, depth + 1);
		Split(node.Right, depth + 1);
	}
	
	void CreateRoomsBSP(BSPNode node)
	{
		if (node.Left == null && node.Right == null)
		{
			int maxW = Mathf.Max(MinRoomSize, node.Rect.Size.X - 2);
			int maxH = Mathf.Max(MinRoomSize, node.Rect.Size.Y - 2);

			int w = rand.Next(MinRoomSize, maxW);
			int h = rand.Next(MinRoomSize, maxH);

			int x = rand.Next(node.Rect.Position.X, node.Rect.Position.X + node.Rect.Size.X - w);
			int y = rand.Next(node.Rect.Position.Y, node.Rect.Position.Y + node.Rect.Size.Y - h);

			node.Room = new Rect2I(x, y, w, h);

			CreateRoom(node.Room);
		}
		else
		{
			if (node.Left != null) CreateRoomsBSP(node.Left);
			if (node.Right != null) CreateRoomsBSP(node.Right);
		}
	}
	
	void ConnectRooms(BSPNode node)
	{
		if (node.Left != null && node.Right != null)
		{
			Vector2I p1 = GetRoomCenter(node.Left);
			Vector2I p2 = GetRoomCenter(node.Right);

			CreateCorridor(p1, p2);

			ConnectRooms(node.Left);
			ConnectRooms(node.Right);
		}
	}
	
	Vector2I GetRoomCenter(BSPNode node)
	{
		if (node == null)
			return new Vector2I(Width / 2, Height / 2);

		if (node.Room.Size != Vector2I.Zero)
			return GetCenter(node.Room);

		if (node.Left != null)
			return GetRoomCenter(node.Left);

		if (node.Right != null)
			return GetRoomCenter(node.Right);

		return new Vector2I(Width / 2, Height / 2);
	}
	void GenerateWFC()
{
	map = new int[Width, Height];

	HashSet<int>[,] possible = new HashSet<int>[Width, Height];

	// 🔹 начальные состояния (все возможны)
	for (int x = 0; x < Width; x++)
	for (int y = 0; y < Height; y++)
	{
		possible[x, y] = new HashSet<int> { WALL, FLOOR };
	}

	int maxIterations = Width * Height * 3;

	for (int i = 0; i < maxIterations; i++)
	{
		Vector2I? cell = FindLowestEntropy(possible);
		if (cell == null)
			break;

		int x = cell.Value.X;
		int y = cell.Value.Y;

		// 🔥 если вдруг пусто — фикс
		if (possible[x, y].Count == 0)
		{
			possible[x, y].Add(FLOOR);
		}

		int chosen = GetRandomFromSet(possible[x, y]);

		possible[x, y].Clear();
		possible[x, y].Add(chosen);

		PropagateWFC(possible, x, y);
	}

	// 🔥 финальная сборка (ГАРАНТИЯ)
	int floorCount = 0;

	for (int x = 0; x < Width; x++)
	for (int y = 0; y < Height; y++)
	{
		if (possible[x, y].Count == 0)
		{
			map[x, y] = FLOOR; // 💥 НЕ стена!
		}
		else
		{
			map[x, y] = GetRandomFromSet(possible[x, y]);
		}

		if (map[x, y] == FLOOR)
			floorCount++;
	}

	// 💥 защита от пустой карты
	if (floorCount < 50)
	{
		GD.Print("WFC fallback → regenerating");

		// просто fallback на CA
		GenerateCellularAutomata();
	}
}
	Vector2I? FindLowestEntropy(HashSet<int>[,] possible)
{
	int best = int.MaxValue;
	Vector2I? bestCell = null;

	for (int x = 0; x < Width; x++)
	for (int y = 0; y < Height; y++)
	{
		int count = possible[x, y].Count;

		if (count > 1 && count < best)
		{
			best = count;
			bestCell = new Vector2I(x, y);
		}
	}

	return bestCell;
}
int GetRandomFromSet(HashSet<int> set)
{
	if (set.Contains(FLOOR) && rand.NextDouble() < 0.6)
		return FLOOR;

	if (set.Contains(WALL))
		return WALL;

	return FLOOR;
}
void PropagateWFC(HashSet<int>[,] possible, int startX, int startY)
{
	Queue<Vector2I> queue = new();
	queue.Enqueue(new Vector2I(startX, startY));

	Vector2I[] dirs =
	{
		new(1,0), new(-1,0),
		new(0,1), new(0,-1)
	};

	while (queue.Count > 0)
	{
		var p = queue.Dequeue();

		foreach (var d in dirs)
		{
			int nx = p.X + d.X;
			int ny = p.Y + d.Y;

			if (nx < 0 || ny < 0 || nx >= Width || ny >= Height)
				continue;

			var current = possible[p.X, p.Y];
			var neighbor = possible[nx, ny];

			HashSet<int> newSet = new();

			foreach (var n in neighbor)
			{
				foreach (var c in current)
				{
					if (IsValidWFC(c, n))
					{
						newSet.Add(n);
						break;
					}
				}
			}

			// 💥 КРИТИЧНО: не даём умереть
			if (newSet.Count == 0)
			{
				newSet.Add(FLOOR);
			}

			if (newSet.Count < neighbor.Count)
			{
				possible[nx, ny] = newSet;
				queue.Enqueue(new Vector2I(nx, ny));
			}
		}
	}
}
bool IsValidWFC(int a, int b)
{
	// 💥 делаем карту "играбельной"

	// пол тянется к полу
	if (a == FLOOR && b == FLOOR) return true;

	// стена окружает
	if (a == WALL && b == WALL) return true;

	// границы допустимы
	if (a == FLOOR && b == WALL) return true;
	if (a == WALL && b == FLOOR) return true;

	return false;
}
	int[,] SmoothStep(int[,] oldMap)
	{
		int[,] newMap = new int[Width, Height];

		for (int x = 0; x < Width; x++)
		for (int y = 0; y < Height; y++)
		{
			int walls = CountWallsAround(oldMap, x, y);

			if (walls > 4)
				newMap[x, y] = WALL;
			else
				newMap[x, y] = FLOOR;
		}

		return newMap;
	}
	// =========================
	// DRAW (СТЕНЫ + ПОЛ)
	// =========================
	void Draw()
	{
		if (TileMap == null) return;

		TileMap.Clear();

		var floorCells = new List<Vector2I>();

		for (int x = 0; x < Width; x++)
		for (int y = 0; y < Height; y++)
		{
			if (map[x, y] == FLOOR)
				floorCells.Add(new Vector2I(x, y));
			else
				TileMap.SetCell(new Vector2I(x, y), 0, new Vector2I(1, 1)); // стена
		}

		var godotCells = new Godot.Collections.Array<Vector2I>();
		foreach (var c in floorCells)
			godotCells.Add(c);
GD.Print("FLOORS: ", floorCells.Count);
		TileMap.SetCellsTerrainConnect(godotCells, 0, 0); // пол
	}

	// =========================
	// ANALYSIS (укорочено)
	// =========================
	void Analyze()
	{
		int floorCount = 0;

		for (int x = 0; x < Width; x++)
		for (int y = 0; y < Height; y++)
			if (map[x, y] == FLOOR)
				floorCount++;

		float density = (float)floorCount / (Width * Height);
		int components = CountComponents(out int largest);
		float avgPath = EstimateAveragePathLength(20);

		SaveToCSV(density, components, largest, avgPath);
	}

	int CountComponents(out int largest)
	{
		bool[,] visited = new bool[Width, Height];
		int count = 0;
		largest = 0;

		for (int x = 0; x < Width; x++)
		for (int y = 0; y < Height; y++)
		{
			if (map[x, y] == FLOOR && !visited[x, y])
			{
				int size = FloodFill(x, y, visited);
				count++;
				if (size > largest) largest = size;
			}
		}

		return count;
	}

	int FloodFill(int x, int y, bool[,] visited)
	{
		Queue<Vector2I> q = new();
		q.Enqueue(new Vector2I(x, y));
		visited[x, y] = true;

		int size = 0;

		Vector2I[] dirs = { new(1,0), new(-1,0), new(0,1), new(0,-1) };

		while (q.Count > 0)
		{
			var p = q.Dequeue();
			size++;

			foreach (var d in dirs)
			{
				int nx = p.X + d.X;
				int ny = p.Y + d.Y;

				if (nx >= 0 && ny >= 0 && nx < Width && ny < Height)
				{
					if (map[nx, ny] == FLOOR && !visited[nx, ny])
					{
						visited[nx, ny] = true;
						q.Enqueue(new Vector2I(nx, ny));
					}
				}
			}
		}

		return size;
	}

	float EstimateAveragePathLength(int samples)
	{
		List<Vector2I> floors = new();

		for (int x = 0; x < Width; x++)
		for (int y = 0; y < Height; y++)
			if (map[x, y] == FLOOR)
				floors.Add(new Vector2I(x, y));

		if (floors.Count < 2) return 0;

		float total = 0;
		int valid = 0;

		for (int i = 0; i < samples; i++)
		{
			var a = floors[rand.Next(floors.Count)];
			var b = floors[rand.Next(floors.Count)];

			int dist = BFS(a, b);

			if (dist > 0)
			{
				total += dist;
				valid++;
			}
		}

		return valid > 0 ? total / valid : 0;
	}
	
	int CountDeadEnds()
	{
		int count = 0;

		Vector2I[] dirs =
		{
			new(1,0), new(-1,0),
			new(0,1), new(0,-1)
		};

		for (int x = 1; x < Width - 1; x++)
		for (int y = 1; y < Height - 1; y++)
		{
			if (map[x, y] != FLOOR) continue;

			int neighbors = 0;

			foreach (var d in dirs)
				if (map[x + d.X, y + d.Y] == FLOOR)
					neighbors++;

			if (neighbors == 1)
				count++;
		}

		return count;
	}
	
	float CorridorRatio()
	{
		int corridor = 0;
		int floor = 0;

		Vector2I[] dirs =
		{
			new(1,0), new(-1,0),
			new(0,1), new(0,-1)
		};

		for (int x = 1; x < Width - 1; x++)
		for (int y = 1; y < Height - 1; y++)
		{
			if (map[x, y] != FLOOR) continue;

			floor++;

			int neighbors = 0;

			foreach (var d in dirs)
				if (map[x + d.X, y + d.Y] == FLOOR)
					neighbors++;

			if (neighbors == 2)
				corridor++;
		}

		return floor > 0 ? (float)corridor / floor : 0;
	}
int BFS(Vector2I start, Vector2I end)
{
	Queue<(Vector2I pos, int dist)> q = new();
	HashSet<Vector2I> visited = new();

	q.Enqueue((start, 0));
	visited.Add(start);

	Vector2I[] dirs =
	{
		new(1,0), new(-1,0),
		new(0,1), new(0,-1)
	};

	while (q.Count > 0)
	{
		var (p, d) = q.Dequeue();

		if (p == end)
			return d;

		foreach (var dir in dirs)
		{
			var n = p + dir;

			// 💥 КРИТИЧЕСКАЯ ПРОВЕРКА
			if (n.X < 0 || n.Y < 0 || n.X >= Width || n.Y >= Height)
				continue;

			if (map[n.X, n.Y] == FLOOR && !visited.Contains(n))
			{
				visited.Add(n);
				q.Enqueue((n, d + 1));
			}
		}
	}

	return -1;
}
List<Vector2I> GetRegion(int startX, int startY, bool[,] visited)
{
	List<Vector2I> region = new();
	Queue<Vector2I> queue = new();

	queue.Enqueue(new Vector2I(startX, startY));
	visited[startX, startY] = true;

	Vector2I[] dirs =
	{
		new(1,0), new(-1,0),
		new(0,1), new(0,-1)
	};

	while (queue.Count > 0)
	{
		var p = queue.Dequeue();
		region.Add(p);

		foreach (var d in dirs)
		{
			int nx = p.X + d.X;
			int ny = p.Y + d.Y;

			// 💥 ВАЖНО: проверка границ (как в BFS)
			if (nx < 0 || ny < 0 || nx >= Width || ny >= Height)
				continue;

			if (!visited[nx, ny] && map[nx, ny] == FLOOR)
			{
				visited[nx, ny] = true;
				queue.Enqueue(new Vector2I(nx, ny));
			}
		}
	}

	return region;
}
void RemoveSmallRegions(int minSize = 20)
{
	bool[,] visited = new bool[Width, Height];

	for (int x = 0; x < Width; x++)
	for (int y = 0; y < Height; y++)
	{
		if (map[x, y] == FLOOR && !visited[x, y])
		{
			List<Vector2I> region = GetRegion(x, y, visited);

			if (region.Count < minSize)
			{
				foreach (var p in region)
					map[p.X, p.Y] = WALL;
			}
		}
	}
}
void SmoothMap(int iterations = 2)
{
	for (int i = 0; i < iterations; i++)
		map = SmoothStep(map);
}
	// =========================
	// CSV
	// =========================
	void SaveToCSV(float density, int comp, int largest, float path)
	{
		int deadEnds = CountDeadEnds();
		float corridor = CorridorRatio();

		string pathFile = "user://metrics.csv";

		FileAccess file = FileAccess.FileExists(pathFile)
			? FileAccess.Open(pathFile, FileAccess.ModeFlags.ReadWrite)
			: FileAccess.Open(pathFile, FileAccess.ModeFlags.Write);

		file.SeekEnd();

		file.StoreLine(
			$"{GenType}," +
			$"{density.ToString(CultureInfo.InvariantCulture)}," +
			$"{comp}," +
			$"{largest}," +
			$"{path.ToString(CultureInfo.InvariantCulture)}," +
			$"{deadEnds}," +
			$"{corridor.ToString(CultureInfo.InvariantCulture)}"
		);

		file.Close();
	}

	void InitCSV()
	{
		string path = "user://metrics.csv";

		if (!FileAccess.FileExists(path))
		{
			var file = FileAccess.Open(path, FileAccess.ModeFlags.Write);

			file.Close();
		}
	}

	// =========================
	// SPAWN
	// =========================
	void SpawnPlayer()
	{
		if (Player == null) return;

		for (int x = 0; x < Width; x++)
		for (int y = 0; y < Height; y++)
			if (map[x, y] == FLOOR)
			{
				Vector2 tileSize = TileMap.TileSet.TileSize;
				Player.Position = new Vector2(x * tileSize.X, y * tileSize.Y);
				return;
			}
	}
}
