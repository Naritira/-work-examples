using Godot;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Stopwatch = System.Diagnostics.Stopwatch;

public partial class LevelGenerator : Node2D
{
	// ============================================================
	// NODES
	// ============================================================

	[ExportGroup("Nodes")]
	[Export] public TileMapLayer GroundLayer;
	[Export] public TileMapLayer WallLayer;
	[Export] public TileMapLayer PropBottomLayer;
	[Export] public TileMapLayer PropDecorLayer;
	[Export] public TileMapLayer PropTopLayer;
	[Export] public Node2D Player;

	[ExportGroup("Agent Nodes")]
	[Export] public Node2D Agent1;
	[Export] public Node2D Agent2;
	[Export] public Node2D Agent3;
	[Export] public Node2D Agent4;
	[Export] public Node2D Agent5;
	[Export] public Node2D Agent6;
	[Export] public Node2D Agent7;
	[Export] public Node2D Agent8;

	[ExportGroup("Agents")]
	[Export] public int AgentCount = 3;
	[Export] public int AgentSpawnMinDistance = 3;

	// ============================================================
	// MAP
	// ============================================================

	[ExportGroup("Map")]
	[Export] public int Width = 80;
	[Export] public int Height = 80;
	[Export] public int Seed = 0;

	public int CurrentSeed { get; private set; }

	// ============================================================
	// BSP
	// ============================================================

	[ExportGroup("BSP")]
	[Export] public int MinRoomSize = 8;
	[Export] public int MaxDepth = 4;
	[Export] public int RoomPadding = 2;

	[Export(PropertyHint.Range, "0,1,0.05")]
	public float RoomSizeVariation = 0.45f;

	// ============================================================
	// ROOM ROLES
	// ============================================================

	[ExportGroup("Room Roles")]
	[Export] public float SmallRoomChance = 0.20f;
	[Export] public float LargeRoomChance = 0.18f;
	[Export] public float HubRoomChance = 0.08f;
	[Export] public float DeadEndRoomChance = 0.20f;

	// ============================================================
	// ROOM SHAPES
	// ============================================================

	[ExportGroup("Room Shapes")]
	[Export] public float RectangleChance = 0.22f;
	[Export] public float RoundedChance = 0.14f;
	[Export] public float OvalChance = 0.12f;
	[Export] public float OctagonalChance = 0.10f;
	[Export] public float LShapeChance = 0.14f;
	[Export] public float TShapeChance = 0.14f;
	[Export] public float CrossChance = 0.14f;

	// ============================================================
	// CORRIDORS
	// ============================================================

	[ExportGroup("Corridors")]
	[Export] public int SecondaryCorridorWidth = 2;
	[Export] public int PrimaryCorridorWidth = 3;
	[Export] public int PrimaryConnectionDepth = 1;

	[Export(PropertyHint.Range, "0,1,0.05")]
	public float LCorridorChance = 0.45f;

	[Export(PropertyHint.Range, "0,1,0.05")]
	public float ZCorridorChance = 0.40f;

	[Export] public int CorridorWander = 4;

	// ============================================================
	// ENTRANCES
	// ============================================================

	[ExportGroup("Entrances")]
	[Export] public int EntranceDepth = 1;
	[Export] public int EntranceShoulder = 1;
	[Export] public int EntranceBlendRadius = 1;

	// ============================================================
	// LOOPS
	// ============================================================

	[ExportGroup("Loops")]
	[Export] public int MinLoopCount = 2;
	[Export] public int MaxLoopCount = 4;
	[Export] public int MinLoopDistance = 10;
	[Export] public int MaxLoopDistance = 35;

	// ============================================================
	// FLOOR
	// ============================================================

	[ExportGroup("Floor")]
	[Export] public int FloorTerrainSet = 0;
	[Export] public int FloorTerrain = 0;
	[Export] public int Floor1Terrain = 1;
	[Export] public int Floor2Terrain = 2;

	[Export] public float FloorWeight = 3f;
	[Export] public float Floor1Weight = 1f;
	[Export] public float Floor2Weight = 1f;

	// ============================================================
	// WALL
	// ============================================================

	[ExportGroup("Wall")]
	[Export] public int WallTerrainSet = 0;
	[Export] public int WallTerrain = 0;
	[Export] public int WallThickness = 3;
	[Export] public int WallRepairPasses = 2;

	// ============================================================
	// TREES 1x2
	// ============================================================

	[ExportGroup("Trees")]
	[Export] public bool GenerateTrees = true;

	[Export(PropertyHint.Range, "0,0.2,0.005")]
	public float TreeDensity = 0.02f;

	[Export] public int MaxTreesPerRoom = 4;
	[Export] public int TreeSpacing = 2;
	[Export] public int TreeEntranceClearance = 2;

	// Указывается НИЖНИЙ тайл.
	// Верхний автоматически находится на одну клетку выше в Atlas.

	[ExportGroup("Tree 1")]
	[Export] public int Tree1SourceId = 0;
	[Export] public Vector2I Tree1BottomAtlas = Vector2I.Zero;
	[Export] public float Tree1Weight = 1f;

	[Export(PropertyHint.Flags, "Floor,Floor 1,Floor 2")]
	public int Tree1AllowedFloors = 7;

	[ExportGroup("Tree 2")]
	[Export] public int Tree2SourceId = 0;
	[Export] public Vector2I Tree2BottomAtlas = Vector2I.Zero;
	[Export] public float Tree2Weight = 1f;

	[Export(PropertyHint.Flags, "Floor,Floor 1,Floor 2")]
	public int Tree2AllowedFloors = 7;

	// ============================================================
	// ITEMS GENERAL
	// ============================================================

	[ExportGroup("Items")]
	[Export] public bool GenerateItems = true;

	[Export(PropertyHint.Range, "0,0.3,0.005")]
	public float ItemDensity = 0.035f;

	[Export] public int MaxItemsPerRoom = 6;
	[Export] public int ItemSpacing = 1;
	[Export] public int ItemEntranceClearance = 1;

	// ============================================================
	// ITEM 1
	// ============================================================

	[ExportGroup("Item 1")]
	[Export] public bool Item1Enabled = false;
	[Export] public int Item1SourceId = 0;
	[Export] public Vector2I Item1Atlas = Vector2I.Zero;
	[Export] public Vector2I Item1Size = new(1, 1);
	[Export] public float Item1Weight = 1f;
	[Export] public bool Item1HasPhysics = false;

	[Export(PropertyHint.Flags, "Floor,Floor 1,Floor 2")]
	public int Item1AllowedFloors = 7;

	// ============================================================
	// ITEM 2
	// ============================================================

	[ExportGroup("Item 2")]
	[Export] public bool Item2Enabled = false;
	[Export] public int Item2SourceId = 0;
	[Export] public Vector2I Item2Atlas = Vector2I.Zero;
	[Export] public Vector2I Item2Size = new(1, 1);
	[Export] public float Item2Weight = 1f;
	[Export] public bool Item2HasPhysics = false;

	[Export(PropertyHint.Flags, "Floor,Floor 1,Floor 2")]
	public int Item2AllowedFloors = 7;

	// ============================================================
	// ITEM 3
	// ============================================================

	[ExportGroup("Item 3")]
	[Export] public bool Item3Enabled = false;
	[Export] public int Item3SourceId = 0;
	[Export] public Vector2I Item3Atlas = Vector2I.Zero;
	[Export] public Vector2I Item3Size = new(1, 1);
	[Export] public float Item3Weight = 1f;
	[Export] public bool Item3HasPhysics = false;

	[Export(PropertyHint.Flags, "Floor,Floor 1,Floor 2")]
	public int Item3AllowedFloors = 7;

	// ============================================================
	// ITEM 4
	// ============================================================

	[ExportGroup("Item 4")]
	[Export] public bool Item4Enabled = false;
	[Export] public int Item4SourceId = 0;
	[Export] public Vector2I Item4Atlas = Vector2I.Zero;
	[Export] public Vector2I Item4Size = new(1, 1);
	[Export] public float Item4Weight = 1f;
	[Export] public bool Item4HasPhysics = false;

	[Export(PropertyHint.Flags, "Floor,Floor 1,Floor 2")]
	public int Item4AllowedFloors = 7;

	// ============================================================
	// ITEM 5
	// ============================================================

	[ExportGroup("Item 5")]
	[Export] public bool Item5Enabled = false;
	[Export] public int Item5SourceId = 0;
	[Export] public Vector2I Item5Atlas = Vector2I.Zero;
	[Export] public Vector2I Item5Size = new(1, 1);
	[Export] public float Item5Weight = 1f;
	[Export] public bool Item5HasPhysics = false;

	[Export(PropertyHint.Flags, "Floor,Floor 1,Floor 2")]
	public int Item5AllowedFloors = 7;

	// ============================================================
	// ITEM 6
	// ============================================================

	[ExportGroup("Item 6")]
	[Export] public bool Item6Enabled = false;
	[Export] public int Item6SourceId = 0;
	[Export] public Vector2I Item6Atlas = Vector2I.Zero;
	[Export] public Vector2I Item6Size = new(1, 1);
	[Export] public float Item6Weight = 1f;
	[Export] public bool Item6HasPhysics = false;

	[Export(PropertyHint.Flags, "Floor,Floor 1,Floor 2")]
	public int Item6AllowedFloors = 7;

	// ============================================================
	// ITEM 7
	// ============================================================

	[ExportGroup("Item 7")]
	[Export] public bool Item7Enabled = false;
	[Export] public int Item7SourceId = 0;
	[Export] public Vector2I Item7Atlas = Vector2I.Zero;
	[Export] public Vector2I Item7Size = new(1, 1);
	[Export] public float Item7Weight = 1f;
	[Export] public bool Item7HasPhysics = false;

	[Export(PropertyHint.Flags, "Floor,Floor 1,Floor 2")]
	public int Item7AllowedFloors = 7;

	// ============================================================
	// ITEM 8
	// ============================================================

	[ExportGroup("Item 8")]
	[Export] public bool Item8Enabled = false;
	[Export] public int Item8SourceId = 0;
	[Export] public Vector2I Item8Atlas = Vector2I.Zero;
	[Export] public Vector2I Item8Size = new(1, 1);
	[Export] public float Item8Weight = 1f;
	[Export] public bool Item8HasPhysics = false;

	[Export(PropertyHint.Flags, "Floor,Floor 1,Floor 2")]
	public int Item8AllowedFloors = 7;

	// ============================================================
	// REPAIR / METRICS
	// ============================================================

	[ExportGroup("Repair")]
	[Export] public int TinyGapRepairPasses = 1;
	[Export] public bool RepairRenderGaps = true;
	[Export] public int RenderGapSearchRadius = 5;

	[ExportGroup("Metrics")]
	[Export] public bool PrintMetrics = true;
	[Export] public bool SaveMetricsCsv = true;
	[Export] public string MetricsFileName = "generation_metrics.csv";
	[Export] public bool PrintAgentMetrics = true;
	[Export] public bool SaveAgentMetricsCsv = true;
	[Export] public string AgentMetricsFileName = "agent_metrics.csv";
	[Export] public string NavigationSummaryFileName = "navigation_experiment_summary.csv";

	public enum NavigationAlgorithmMode
	{
		Dijkstra,
		AStar,
		AdaptiveAStar
	}

	[ExportGroup("Navigation Algorithm")]
	[Export] public NavigationAlgorithmMode NavigationMode = NavigationAlgorithmMode.AdaptiveAStar;
	[Export] public float RepathIntervalSeconds = 0.35f;
	[Export] public int AdaptiveAgentAvoidanceRadiusCells = 4;
	[Export] public float AdaptiveAgentCostMultiplier = 3.0f;
	[Export] public string NavigationExperimentLabel = "adaptive-default";

	// Совместимое имя для Agent.cs: адаптивная стоимость включена только в AdaptiveAStar.
	public bool UseAdaptiveNavigationCosts => NavigationMode == NavigationAlgorithmMode.AdaptiveAStar;
	public string NavigationModeName => NavigationMode.ToString();

	[ExportGroup("Experiment Control")]
	[Export(PropertyHint.Range, "1,600,1")] public float ExperimentDurationSeconds = 60f;
	[Export] public float AgentTargetReachDistancePixels = 48f;
	[Export] public bool StopWhenAllAgentsReachTarget = true;
	[Export] public bool FreezePlayerDuringExperiment = true;

	// ============================================================
	// AUTOMATED ALGORITHM BENCHMARK
	// ============================================================

	[ExportGroup("Automated Benchmark")]
	[Export] public bool EnableAutomatedBenchmark = false;
	[Export(PropertyHint.Range, "1,20,1")] public int BenchmarkSeedCount = 5;
	[Export(PropertyHint.Range, "1,200,1")] public int BenchmarkQueriesPerSeed = 30;
	[Export] public int BenchmarkFirstSeed = 42;
	[Export(PropertyHint.Range, "0,16,1")] public int BenchmarkVirtualAgentCount = 4;
	[Export] public string NavigationBenchmarkFileName = "navigation_benchmark.csv";
	[Export] public string NavigationBenchmarkSummaryFileName = "navigation_benchmark_summary.csv";

	// ============================================================
	// INTERNAL
	// ============================================================

	private const int WALL = 0;
	private const int FLOOR = 1;

	private const int FLOOR_BASE = 0;
	private const int FLOOR_1 = 1;
	private const int FLOOR_2 = 2;

	private int[,] map;
	private int[,] roomGrid;
	private int[,] floorTypeGrid;
	private bool[,] occupiedGrid;
	private bool[,] blockedGrid;
	private AStarGrid2D navigationGrid;
	private readonly HashSet<Vector2I> adaptiveWeightedCells = new();
	private readonly Dictionary<Vector2I, float> activeNavigationWeights = new();

	// Итоговые показатели экспериментального прогона.
	private int configuredAgentCount;
	private int recordedAgentCount;
	private int reachedTargetCount;
	private int totalPathRecalculations;
	private int totalFailedPathfinds;
	private long totalSearchNodesExpanded;
	private double totalPathfindingMilliseconds;
	private double totalDistanceTravelledPixels;
	private double totalLastPlannedPathLengthPixels;
	private double totalTimeToTargetSeconds;
	private int timeToTargetSamples;
	private bool experimentSummarySaved;
	private bool experimentRunning;
	private bool experimentFinished;
	private double experimentElapsedSeconds;
	private readonly List<Agent> experimentAgents = new();
	private readonly HashSet<Agent> reachedAgents = new();

	private readonly List<RoomData> rooms = new();
	private readonly List<TreeData> trees = new();
	private readonly List<ItemData> items = new();
	private readonly HashSet<long> connections = new();
	private readonly List<Vector2I> actorSpawnCells = new();

	private Random rand;

	private enum RoomShape
	{
		Rectangle,
		Rounded,
		Oval,
		Octagonal,
		L,
		T,
		Cross
	}

	private enum RoomRole
	{
		Small,
		Normal,
		Large,
		Hub,
		DeadEnd
	}

	private sealed class RoomData
	{
		public Rect2I Rect;
		public RoomRole Role;
		public int Connections;

		public RoomData(Rect2I rect, RoomRole role)
		{
			Rect = rect;
			Role = role;
		}
	}

	private sealed class BSPNode
	{
		public Rect2I Rect;
		public BSPNode Left;
		public BSPNode Right;
		public int RoomIndex = -1;

		public BSPNode(Rect2I rect) => Rect = rect;

		public bool IsLeaf() => Left == null && Right == null;
	}

	private readonly struct RoomExit
	{
		public readonly Vector2I Cell;
		public readonly Vector2I Outward;

		public RoomExit(Vector2I cell, Vector2I outward)
		{
			Cell = cell;
			Outward = outward;
		}
	}

	private readonly struct TreeData
	{
		public readonly Vector2I Bottom;
		public readonly int Variant;

		public Vector2I Top => Bottom + Vector2I.Up;

		public TreeData(Vector2I bottom, int variant)
		{
			Bottom = bottom;
			Variant = variant;
		}
	}

	private readonly struct ItemData
	{
		public readonly Vector2I Origin;
		public readonly int Variant;

		public ItemData(Vector2I origin, int variant)
		{
			Origin = origin;
			Variant = variant;
		}
	}

	private readonly struct ItemConfig
	{
		public readonly bool Enabled;
		public readonly int SourceId;
		public readonly Vector2I Atlas;
		public readonly Vector2I Size;
		public readonly float Weight;
		public readonly bool HasPhysics;
		public readonly int AllowedFloors;

		public ItemConfig(
			bool enabled,
			int sourceId,
			Vector2I atlas,
			Vector2I size,
			float weight,
			bool hasPhysics,
			int allowedFloors)
		{
			Enabled = enabled;
			SourceId = sourceId;
			Atlas = atlas;
			Size = new Vector2I(
				Mathf.Max(1, size.X),
				Mathf.Max(1, size.Y)
			);
			Weight = Mathf.Max(0f, weight);
			HasPhysics = hasPhysics;
			AllowedFloors = allowedFloors;
		}
	}

	private readonly struct NavigationQueueItem
	{
		public readonly Vector2I Cell;
		public readonly float CostSoFar;

		public NavigationQueueItem(Vector2I cell, float costSoFar)
		{
			Cell = cell;
			CostSoFar = costSoFar;
		}
	}

	private readonly struct NavigationBenchmarkQuery
	{
		public readonly Vector2I Start;
		public readonly Vector2I Target;

		public NavigationBenchmarkQuery(Vector2I start, Vector2I target)
		{
			Start = start;
			Target = target;
		}
	}

	private sealed class NavigationBenchmarkResult
	{
		public int Seed;
		public int Trial;
		public string Algorithm = "";
		public Vector2I Start;
		public Vector2I Target;
		public bool PathFound;
		public int PathCellCount;
		public int ExpandedNodes;
		public double ElapsedMilliseconds;
		public float PathCost;
	}

	private struct LevelMetrics
	{
		public int Rooms;
		public int DeadEnds;
		public int Loops;
		public int FloorCells;
		public int CorridorCells;
		public int Trees;
		public int Items;
		public int BlockingItems;
		public int BlockedCells;
		public int WalkableCells;
		public int Components;
		public float AverageRoomArea;
		public float ObstacleDensity;
		public float WalkableRatio;
	}

	// ============================================================
	// READY
	// ============================================================

	public override void _Ready()
	{
		AddToGroup("level_generator");
		NormalizeSettings();
		GenerateLevel();
		BuildNavigationGrid();

		if (EnableAutomatedBenchmark)
		{
			// В режиме автоматического тестирования не запускаем игровых актёров:
			// иначе их движение влияло бы на измерение алгоритмов.
			DisableActorsForBenchmark();
			RunAutomatedNavigationBenchmark();
			DrawLevel();

			LevelMetrics benchmarkMapMetrics = CalculateMetrics();
			if (PrintMetrics)
				PrintLevelMetrics(benchmarkMapMetrics);
			if (SaveMetricsCsv)
				SaveMetrics(benchmarkMapMetrics);

			GD.Print("[BenchmarkMode] Готово. Игровые агенты не запускались. " +
				"Результаты: user://" + NavigationBenchmarkFileName +
				" и user://" + NavigationBenchmarkSummaryFileName);
			return;
		}

		DrawLevel();
		SpawnPlayer();
		SpawnAgents();

		LevelMetrics metrics = CalculateMetrics();

		if (PrintMetrics)
			PrintLevelMetrics(metrics);

		if (SaveMetricsCsv)
			SaveMetrics(metrics);

		StartExperimentTimer();
	}

	private void DisableActorsForBenchmark()
	{
		Node2D[] actors = { Player, Agent1, Agent2, Agent3, Agent4, Agent5, Agent6, Agent7, Agent8 };
		foreach (Node2D actor in actors)
		{
			if (actor == null || !GodotObject.IsInstanceValid(actor))
				continue;

			actor.Visible = false;
			actor.SetProcess(false);
			actor.SetPhysicsProcess(false);
			actor.SetProcessInput(false);
			actor.SetProcessUnhandledInput(false);

			if (actor is CharacterBody2D body)
				body.Velocity = Vector2.Zero;
		}
	}

	private void RunAutomatedNavigationBenchmark()
	{
		int originallyRequestedSeed = Seed;
		int sceneSeedToRestore = CurrentSeed;
		List<NavigationBenchmarkResult> results = new();

		GD.Print($"[BenchmarkStarted] Label={NavigationExperimentLabel} | " +
			$"Seeds={BenchmarkSeedCount} | QueriesPerSeed={BenchmarkQueriesPerSeed} | " +
			$"Algorithms=Dijkstra,AStar,AdaptiveAStar");

		for (int seedIndex = 0; seedIndex < BenchmarkSeedCount; seedIndex++)
		{
			int benchmarkSeed = Math.Max(1, BenchmarkFirstSeed + seedIndex);
			Seed = benchmarkSeed;
			GenerateLevel();
			BuildNavigationGrid();

			if (navigationGrid == null || GroundLayer == null || GroundLayer.TileSet == null)
			{
				GD.PushWarning($"[Benchmark] Seed {benchmarkSeed} пропущен: навигационная сетка не создана.");
				continue;
			}

			List<Vector2I> walkableCells = GetBenchmarkWalkableCells();
			if (walkableCells.Count < 2)
			{
				GD.PushWarning($"[Benchmark] Seed {benchmarkSeed} пропущен: недостаточно проходимых клеток.");
				continue;
			}

			List<NavigationBenchmarkQuery> queries =
				BuildBenchmarkQueries(walkableCells, benchmarkSeed, BenchmarkQueriesPerSeed);
			if (queries.Count == 0)
			{
				GD.PushWarning($"[Benchmark] Seed {benchmarkSeed} пропущен: не удалось подобрать пары точек.");
				continue;
			}

			List<Vector2I> virtualAgentCells =
				BuildBenchmarkVirtualAgentCells(walkableCells, benchmarkSeed);

			// Прогрев всех трёх ветвей до измерений снижает влияние JIT на первый результат.
			NavigationBenchmarkQuery warmup = queries[0];
			activeNavigationWeights.Clear();
			FindGridPath(warmup.Start, warmup.Target, false, out _);
			activeNavigationWeights.Clear();
			FindGridPath(warmup.Start, warmup.Target, true, out _);
			BuildBenchmarkAdaptiveWeights(virtualAgentCells);
			FindGridPath(warmup.Start, warmup.Target, true, out _);
			activeNavigationWeights.Clear();

			for (int queryIndex = 0; queryIndex < queries.Count; queryIndex++)
			{
				NavigationBenchmarkQuery query = queries[queryIndex];
				results.Add(MeasureBenchmarkQuery(
					benchmarkSeed, queryIndex + 1, "Dijkstra",
					query, useHeuristic: false, adaptive: false,
					virtualAgentCells: virtualAgentCells));

				results.Add(MeasureBenchmarkQuery(
					benchmarkSeed, queryIndex + 1, "AStar",
					query, useHeuristic: true, adaptive: false,
					virtualAgentCells: virtualAgentCells));

				results.Add(MeasureBenchmarkQuery(
					benchmarkSeed, queryIndex + 1, "AdaptiveAStar",
					query, useHeuristic: true, adaptive: true,
					virtualAgentCells: virtualAgentCells));
			}

			GD.Print($"[BenchmarkSeedComplete] Seed={benchmarkSeed} | " +
				$"WalkableCells={walkableCells.Count} | Queries={queries.Count}");
		}

		activeNavigationWeights.Clear();

		if (results.Count > 0)
		{
			SaveNavigationBenchmarkResults(results);
			SaveNavigationBenchmarkSummary(results);
			PrintNavigationBenchmarkSummary(results);
		}
		else
		{
			GD.PushWarning("[Benchmark] Нет результатов для сохранения.");
		}

		// Возвращаем карту, которую пользователь изначально выбрал для отображения.
		// Если Seed был 0, сохраняем исходный фактически сгенерированный seed.
		Seed = sceneSeedToRestore;
		GenerateLevel();
		BuildNavigationGrid();
		Seed = originallyRequestedSeed;
	}

	private List<Vector2I> GetBenchmarkWalkableCells()
	{
		List<Vector2I> cells = new();
		if (navigationGrid == null)
			return cells;

		for (int x = 0; x < Width; x++)
		for (int y = 0; y < Height; y++)
		{
			Vector2I cell = new(x, y);
			if (!navigationGrid.IsPointSolid(cell))
				cells.Add(cell);
		}

		return cells;
	}

	private List<NavigationBenchmarkQuery> BuildBenchmarkQueries(
		List<Vector2I> walkableCells,
		int seed,
		int requestedCount)
	{
		List<NavigationBenchmarkQuery> queries = new();
		Random queryRandom = new(seed ^ 0x2D2816FE);
		int minimumDistance = Math.Max(8, (Width + Height) / 8);
		int maxAttempts = Math.Max(200, walkableCells.Count * 3);

		for (int trial = 0; trial < requestedCount; trial++)
		{
			bool found = false;
			for (int attempt = 0; attempt < maxAttempts; attempt++)
			{
				Vector2I start = walkableCells[queryRandom.Next(walkableCells.Count)];
				Vector2I target = walkableCells[queryRandom.Next(walkableCells.Count)];
				if (start == target)
					continue;

				int manhattanDistance =
					Math.Abs(start.X - target.X) + Math.Abs(start.Y - target.Y);
				if (manhattanDistance < minimumDistance)
					continue;

				// Отбрасываем пары без маршрута, чтобы сравнивать алгоритмы на одинаковых задачах.
				activeNavigationWeights.Clear();
				List<Vector2I> validationPath =
					FindGridPath(start, target, false, out _);
				if (validationPath.Count == 0)
					continue;

				queries.Add(new NavigationBenchmarkQuery(start, target));
				found = true;
				break;
			}

			if (!found)
			{
				GD.PushWarning($"[Benchmark] Для seed {seed} подобрано только {queries.Count} " +
					$"из {requestedCount} пар маршрутов.");
				break;
			}
		}

		activeNavigationWeights.Clear();
		return queries;
	}

	private List<Vector2I> BuildBenchmarkVirtualAgentCells(
		List<Vector2I> walkableCells,
		int seed)
	{
		List<Vector2I> result = new();
		if (BenchmarkVirtualAgentCount <= 0 || walkableCells.Count == 0)
			return result;

		Random agentRandom = new(seed ^ 0x51ED270B);
		HashSet<Vector2I> used = new();
		int desiredCount = Math.Min(BenchmarkVirtualAgentCount, walkableCells.Count);
		int attempts = desiredCount * 20;

		while (result.Count < desiredCount && attempts-- > 0)
		{
			Vector2I cell = walkableCells[agentRandom.Next(walkableCells.Count)];
			if (used.Add(cell))
				result.Add(cell);
		}

		return result;
	}

	private NavigationBenchmarkResult MeasureBenchmarkQuery(
		int seed,
		int trial,
		string algorithm,
		NavigationBenchmarkQuery query,
		bool useHeuristic,
		bool adaptive,
		List<Vector2I> virtualAgentCells)
	{
		activeNavigationWeights.Clear();
		Stopwatch stopwatch = Stopwatch.StartNew();

		if (adaptive)
			BuildBenchmarkAdaptiveWeights(virtualAgentCells);

		List<Vector2I> path =
			FindGridPath(query.Start, query.Target, useHeuristic, out int expandedNodes);

		stopwatch.Stop();
		float pathCost = CalculateBenchmarkPathCost(path, activeNavigationWeights);

		NavigationBenchmarkResult result = new()
		{
			Seed = seed,
			Trial = trial,
			Algorithm = algorithm,
			Start = query.Start,
			Target = query.Target,
			PathFound = path.Count > 0,
			PathCellCount = path.Count,
			ExpandedNodes = expandedNodes,
			ElapsedMilliseconds = stopwatch.Elapsed.TotalMilliseconds,
			PathCost = pathCost
		};

		activeNavigationWeights.Clear();
		return result;
	}

	private void BuildBenchmarkAdaptiveWeights(List<Vector2I> virtualAgentCells)
	{
		activeNavigationWeights.Clear();
		if (navigationGrid == null || virtualAgentCells == null ||
			AdaptiveAgentCostMultiplier <= 0f)
			return;

		int radius = Mathf.Max(1, AdaptiveAgentAvoidanceRadiusCells);
		float radiusFloat = radius;

		foreach (Vector2I center in virtualAgentCells)
		{
			for (int x = Mathf.Max(0, center.X - radius);
				x <= Mathf.Min(Width - 1, center.X + radius); x++)
			for (int y = Mathf.Max(0, center.Y - radius);
				y <= Mathf.Min(Height - 1, center.Y + radius); y++)
			{
				Vector2I cell = new(x, y);
				if (navigationGrid.IsPointSolid(cell))
					continue;

				float distance = new Vector2(x - center.X, y - center.Y).Length();
				if (distance > radiusFloat)
					continue;

				float penalty = AdaptiveAgentCostMultiplier *
					(1f - distance / radiusFloat);
				float currentScale = activeNavigationWeights.TryGetValue(cell, out float existing)
					? existing
					: 1f;
				activeNavigationWeights[cell] = currentScale + penalty;
			}
		}
	}

	private static float CalculateBenchmarkPathCost(
		List<Vector2I> path,
		Dictionary<Vector2I, float> weights)
	{
		if (path == null || path.Count < 2)
			return path != null && path.Count == 1 ? 0f : -1f;

		float totalCost = 0f;
		for (int i = 1; i < path.Count; i++)
		{
			float stepCost = weights.TryGetValue(path[i], out float weight)
				? Mathf.Max(1f, weight)
				: 1f;
			totalCost += stepCost;
		}

		return totalCost;
	}

	private void SaveNavigationBenchmarkResults(List<NavigationBenchmarkResult> results)
	{
		string path = "user://" + NavigationBenchmarkFileName;
		bool existed = FileAccess.FileExists(path);
		using FileAccess file = FileAccess.Open(
			path,
			existed ? FileAccess.ModeFlags.ReadWrite : FileAccess.ModeFlags.Write);

		if (file == null)
		{
			GD.PushWarning($"Не удалось сохранить benchmark CSV: {path}");
			return;
		}

		if (!existed)
			file.StoreLine("ExperimentLabel,Seed,Trial,Algorithm,StartX,StartY,TargetX,TargetY,PathFound,PathCellCount,ExpandedNodes,ElapsedMilliseconds,PathCost");
		else
			file.SeekEnd();

		CultureInfo culture = CultureInfo.InvariantCulture;
		string label = EscapeCsv(NavigationExperimentLabel);
		string quotedLabel = "\"" + label + "\"";
		foreach (NavigationBenchmarkResult result in results)
		{
			file.StoreLine(string.Join(",",
				quotedLabel,
				result.Seed.ToString(culture),
				result.Trial.ToString(culture),
				result.Algorithm,
				result.Start.X.ToString(culture),
				result.Start.Y.ToString(culture),
				result.Target.X.ToString(culture),
				result.Target.Y.ToString(culture),
				result.PathFound ? "true" : "false",
				result.PathCellCount.ToString(culture),
				result.ExpandedNodes.ToString(culture),
				result.ElapsedMilliseconds.ToString("F5", culture),
				result.PathCost.ToString("F3", culture)));
		}
	}

	private void SaveNavigationBenchmarkSummary(List<NavigationBenchmarkResult> results)
	{
		string path = "user://" + NavigationBenchmarkSummaryFileName;
		bool existed = FileAccess.FileExists(path);
		using FileAccess file = FileAccess.Open(
			path,
			existed ? FileAccess.ModeFlags.ReadWrite : FileAccess.ModeFlags.Write);

		if (file == null)
		{
			GD.PushWarning($"Не удалось сохранить benchmark summary CSV: {path}");
			return;
		}

		if (!existed)
			file.StoreLine("ExperimentLabel,Scope,Seed,Algorithm,Queries,SuccessfulPaths,SuccessRatePercent,AverageMilliseconds,MedianMilliseconds,AverageExpandedNodes,AveragePathCells,AveragePathCost");
		else
			file.SeekEnd();

		string label = EscapeCsv(NavigationExperimentLabel);
		foreach (var group in results.GroupBy(r => new { r.Seed, r.Algorithm })
			.OrderBy(g => g.Key.Seed).ThenBy(g => g.Key.Algorithm))
		{
			WriteBenchmarkSummaryRow(file, label, "seed", group.Key.Seed, group.Key.Algorithm, group.ToList());
		}

		foreach (var group in results.GroupBy(r => r.Algorithm).OrderBy(g => g.Key))
			WriteBenchmarkSummaryRow(file, label, "all_seeds", 0, group.Key, group.ToList());
	}

	private static void WriteBenchmarkSummaryRow(
		FileAccess file,
		string label,
		string scope,
		int seed,
		string algorithm,
		List<NavigationBenchmarkResult> rows)
	{
		CultureInfo culture = CultureInfo.InvariantCulture;
		string quotedLabel = "\"" + label + "\"";
		int successful = rows.Count(r => r.PathFound);
		double successRate = rows.Count > 0 ? 100.0 * successful / rows.Count : 0.0;
		double averageTime = rows.Count > 0 ? rows.Average(r => r.ElapsedMilliseconds) : 0.0;
		double medianTime = Median(rows.Select(r => r.ElapsedMilliseconds));
		double averageExpanded = rows.Count > 0 ? rows.Average(r => r.ExpandedNodes) : 0.0;
		double averageCells = rows.Count > 0 ? rows.Average(r => r.PathCellCount) : 0.0;
		double averageCost = rows.Count > 0 ? rows.Where(r => r.PathFound).Select(r => (double)r.PathCost).DefaultIfEmpty(0.0).Average() : 0.0;

		file.StoreLine(string.Join(",",
			quotedLabel,
			scope,
			seed.ToString(culture),
			algorithm,
			rows.Count.ToString(culture),
			successful.ToString(culture),
			successRate.ToString("F2", culture),
			averageTime.ToString("F5", culture),
			medianTime.ToString("F5", culture),
			averageExpanded.ToString("F2", culture),
			averageCells.ToString("F2", culture),
			averageCost.ToString("F3", culture)));
	}

	private static double Median(IEnumerable<double> values)
	{
		List<double> sorted = values.OrderBy(v => v).ToList();
		if (sorted.Count == 0)
			return 0.0;
		int middle = sorted.Count / 2;
		return sorted.Count % 2 == 0
			? (sorted[middle - 1] + sorted[middle]) / 2.0
			: sorted[middle];
	}

	private static string EscapeCsv(string value) =>
		(value ?? "").Replace("\"", "\"\"");

	private void PrintNavigationBenchmarkSummary(List<NavigationBenchmarkResult> results)
	{
		GD.Print("[BenchmarkSummary] Агрегированные результаты (время — среднее на один поиск):");
		foreach (var group in results.GroupBy(r => r.Algorithm).OrderBy(g => g.Key))
		{
			double averageMs = group.Average(r => r.ElapsedMilliseconds);
			double averageExpanded = group.Average(r => r.ExpandedNodes);
			double successRate = 100.0 * group.Count(r => r.PathFound) / group.Count();
			GD.Print($"[BenchmarkSummary] {group.Key}: success={successRate:F1}% | " +
				$"avgTime={averageMs:F5} ms | avgExpanded={averageExpanded:F1} | " +
				$"queries={group.Count()}");
		}
	}

	public override void _Process(double delta)
	{
		if (!experimentRunning || experimentFinished)
			return;

		experimentElapsedSeconds += delta;
		if (experimentElapsedSeconds >= Math.Max(1.0, ExperimentDurationSeconds))
			FinishExperiment("time_limit");
	}

	private void StartExperimentTimer()
	{
		if (configuredAgentCount <= 0)
		{
			GD.PushWarning("Эксперимент не запущен: не удалось настроить ни одного агента.");
			return;
		}

		experimentElapsedSeconds = 0.0;
		experimentFinished = false;
		experimentRunning = true;

		// Для сопоставимых запусков игрок по умолчанию остаётся в одной точке.
		if (FreezePlayerDuringExperiment && Player != null && GodotObject.IsInstanceValid(Player))
		{
			if (Player is CharacterBody2D playerBody)
				playerBody.Velocity = Vector2.Zero;
			Player.SetPhysicsProcess(false);
			Player.SetProcessInput(false);
			Player.SetProcessUnhandledInput(false);
		}

		GD.Print($"[ExperimentStarted] Seed={CurrentSeed} | Mode={NavigationModeName} | " +
			$"DurationLimit={ExperimentDurationSeconds:F0}s | Agents={configuredAgentCount} | " +
			$"RepathInterval={RepathIntervalSeconds:F2}s | AdaptiveRadius={AdaptiveAgentAvoidanceRadiusCells} cells | " +
			$"AdaptiveMultiplier={AdaptiveAgentCostMultiplier:F2} | TargetReachDistance={AgentTargetReachDistancePixels:F1}px");
	}

	public void NotifyAgentReachedTarget(Agent agent)
	{
		if (!experimentRunning || experimentFinished || agent == null || !GodotObject.IsInstanceValid(agent))
			return;

		if (!reachedAgents.Add(agent))
			return;

		GD.Print($"[AgentReachedTarget] {agent.Name} | {reachedAgents.Count}/{configuredAgentCount} | " +
			$"Time={agent.TimeToTargetSeconds:F2}s | Distance={agent.FinalDistanceToPlayer:F1}px");

		if (StopWhenAllAgentsReachTarget && configuredAgentCount > 0 && reachedAgents.Count >= configuredAgentCount)
			FinishExperiment("all_agents_reached");
	}

	private void FinishExperiment(string reason)
	{
		if (experimentFinished)
			return;

		experimentFinished = true;
		experimentRunning = false;
		GD.Print($"[ExperimentFinished] Reason={reason} | Seed={CurrentSeed} | " +
			$"Elapsed={experimentElapsedSeconds:F2}s | Reached={reachedAgents.Count}/{configuredAgentCount}");

		foreach (Agent agent in experimentAgents)
		{
			if (agent == null || !GodotObject.IsInstanceValid(agent) || agent.IsQueuedForDeletion())
				continue;

			string outcome = agent.ReachedTarget
				? "reached_target"
				: reason == "time_limit" ? "timeout" : "not_reached";
			agent.FinalizeExperiment(outcome);
		}

		if (experimentFinished && !experimentSummarySaved && configuredAgentCount > 0 && recordedAgentCount >= configuredAgentCount)
			SaveNavigationExperimentSummary();

		// Оставляем окно игры открытым, но полностью замораживаем симуляцию.
		GetTree().Paused = true;
	}

	private void NormalizeSettings()
	{
		Width = Mathf.Max(20, Width);
		Height = Mathf.Max(20, Height);
		AgentCount = Mathf.Max(0, AgentCount);
		AgentSpawnMinDistance = Mathf.Max(0, AgentSpawnMinDistance);

		MinRoomSize = Mathf.Max(6, MinRoomSize);
		MaxDepth = Mathf.Max(1, MaxDepth);
		RoomPadding = Mathf.Max(1, RoomPadding);

		SecondaryCorridorWidth = Mathf.Max(2, SecondaryCorridorWidth);
		PrimaryCorridorWidth = Mathf.Max(SecondaryCorridorWidth, PrimaryCorridorWidth);

		MinLoopCount = Mathf.Max(0, MinLoopCount);
		MaxLoopCount = Mathf.Max(MinLoopCount, MaxLoopCount);

		EntranceDepth = Mathf.Max(0, EntranceDepth);
		EntranceShoulder = Mathf.Max(0, EntranceShoulder);
		EntranceBlendRadius = Mathf.Max(0, EntranceBlendRadius);

		TreeSpacing = Mathf.Max(0, TreeSpacing);
		TreeEntranceClearance = Mathf.Max(0, TreeEntranceClearance);

		ItemSpacing = Mathf.Max(0, ItemSpacing);
		ItemEntranceClearance = Mathf.Max(0, ItemEntranceClearance);

		WallThickness = Mathf.Max(1, WallThickness);
		WallRepairPasses = Mathf.Max(0, WallRepairPasses);
		AdaptiveAgentAvoidanceRadiusCells = Mathf.Max(1, AdaptiveAgentAvoidanceRadiusCells);
		AdaptiveAgentCostMultiplier = Mathf.Max(0f, AdaptiveAgentCostMultiplier);
		BenchmarkSeedCount = Mathf.Clamp(BenchmarkSeedCount, 1, 20);
		BenchmarkQueriesPerSeed = Mathf.Clamp(BenchmarkQueriesPerSeed, 1, 200);
		BenchmarkVirtualAgentCount = Mathf.Clamp(BenchmarkVirtualAgentCount, 0, 16);
		if (string.IsNullOrWhiteSpace(NavigationBenchmarkFileName))
			NavigationBenchmarkFileName = "navigation_benchmark.csv";
		if (string.IsNullOrWhiteSpace(NavigationBenchmarkSummaryFileName))
			NavigationBenchmarkSummaryFileName = "navigation_benchmark_summary.csv";
		RepathIntervalSeconds = Mathf.Clamp(RepathIntervalSeconds, 0.1f, 5.0f);

		if (GroundLayer != null)
			GroundLayer.CollisionEnabled = false;

		if (WallLayer != null)
			WallLayer.CollisionEnabled = true;

		if (PropBottomLayer != null)
			PropBottomLayer.CollisionEnabled = true;

		if (PropDecorLayer != null)
			PropDecorLayer.CollisionEnabled = false;

		if (PropTopLayer != null)
		{
			PropTopLayer.CollisionEnabled = false;

			if (Player != null)
				PropTopLayer.ZIndex =
					Mathf.Max(PropTopLayer.ZIndex, Player.ZIndex + 1);
		}
	}

	// ============================================================
	// GENERATION
	// ============================================================

	private void GenerateLevel()
	{
		InitRandom();

		map = new int[Width, Height];
		roomGrid = new int[Width, Height];
		floorTypeGrid = new int[Width, Height];
		occupiedGrid = new bool[Width, Height];
		blockedGrid = new bool[Width, Height];

		rooms.Clear();
		trees.Clear();
		items.Clear();
		connections.Clear();
		actorSpawnCells.Clear();

		for (int x = 0; x < Width; x++)
		for (int y = 0; y < Height; y++)
		{
			map[x, y] = WALL;
			roomGrid[x, y] = -1;
		}

		BSPNode root = new(
			new Rect2I(1, 1, Width - 2, Height - 2)
		);

		Split(root, 0);
		CreateRooms(root);
		EnsureHub();

		ConnectRooms(root, 0);
		EnsureRoleConnections();
		AssignDeadEnds();
		CreateLoops();

		RepairGeometry();
		AssignFloorTypes();

		if (GenerateTrees)
			GenerateTreesInternal();

		if (GenerateItems)
			GenerateItemsInternal();
	}

	private void InitRandom()
	{
		CurrentSeed = Seed != 0
			? Seed
			: (int)(DateTime.UtcNow.Ticks % int.MaxValue);

		if (CurrentSeed <= 0)
			CurrentSeed = 1;

		rand = new Random(CurrentSeed);
	}

	// ============================================================
	// BSP
	// ============================================================

	private void Split(BSPNode node, int depth)
	{
		if (node == null || depth >= MaxDepth)
			return;

		int w = node.Rect.Size.X;
		int h = node.Rect.Size.Y;

		if (w < MinRoomSize * 2 && h < MinRoomSize * 2)
			return;

		bool horizontal =
			w > h * 1.25f ? false :
			h > w * 1.25f ? true :
			rand.Next(2) == 0;

		if (horizontal)
		{
			int max = h - MinRoomSize;
			if (max <= MinRoomSize) return;

			int split = rand.Next(MinRoomSize, max + 1);

			node.Left = new BSPNode(
				new Rect2I(
					node.Rect.Position.X,
					node.Rect.Position.Y,
					w,
					split
				)
			);

			node.Right = new BSPNode(
				new Rect2I(
					node.Rect.Position.X,
					node.Rect.Position.Y + split,
					w,
					h - split
				)
			);
		}
		else
		{
			int max = w - MinRoomSize;
			if (max <= MinRoomSize) return;

			int split = rand.Next(MinRoomSize, max + 1);

			node.Left = new BSPNode(
				new Rect2I(
					node.Rect.Position.X,
					node.Rect.Position.Y,
					split,
					h
				)
			);

			node.Right = new BSPNode(
				new Rect2I(
					node.Rect.Position.X + split,
					node.Rect.Position.Y,
					w - split,
					h
				)
			);
		}

		Split(node.Left, depth + 1);
		Split(node.Right, depth + 1);
	}

	// ============================================================
	// ROOMS
	// ============================================================

	private void CreateRooms(BSPNode node)
	{
		if (node == null)
			return;

		if (node.IsLeaf())
		{
			CreateRoom(node);
			return;
		}

		CreateRooms(node.Left);
		CreateRooms(node.Right);
	}

	private void CreateRoom(BSPNode node)
	{
		int availableW = node.Rect.Size.X - RoomPadding * 2;
		int availableH = node.Rect.Size.Y - RoomPadding * 2;

		if (availableW < MinRoomSize || availableH < MinRoomSize)
			return;

		RoomRole role = PickRoomRole();
		GetRoomScale(role, out float minScale, out float maxScale);

		int minW = Mathf.Clamp(
			Mathf.RoundToInt(availableW * minScale),
			MinRoomSize,
			availableW
		);

		int minH = Mathf.Clamp(
			Mathf.RoundToInt(availableH * minScale),
			MinRoomSize,
			availableH
		);

		int maxW = Mathf.Clamp(
			Mathf.RoundToInt(availableW * maxScale),
			minW,
			availableW
		);

		int maxH = Mathf.Clamp(
			Mathf.RoundToInt(availableH * maxScale),
			minH,
			availableH
		);

		int w = rand.Next(minW, maxW + 1);
		int h = rand.Next(minH, maxH + 1);

		int minX = node.Rect.Position.X + RoomPadding;
		int minY = node.Rect.Position.Y + RoomPadding;

		int maxX = Mathf.Max(
			minX,
			node.Rect.End.X - RoomPadding - w
		);

		int maxY = Mathf.Max(
			minY,
			node.Rect.End.Y - RoomPadding - h
		);

		Rect2I room = new(
			rand.Next(minX, maxX + 1),
			rand.Next(minY, maxY + 1),
			w,
			h
		);

		node.RoomIndex = rooms.Count;
		rooms.Add(new RoomData(room, role));

		GenerateRoomShape(
			room,
			PickRoomShape(),
			rand.Next(4),
			node.RoomIndex
		);
	}

	private RoomRole PickRoomRole()
	{
		float roll = (float)rand.NextDouble();

		if (roll < HubRoomChance)
			return RoomRole.Hub;

		roll -= HubRoomChance;

		if (roll < LargeRoomChance)
			return RoomRole.Large;

		roll -= LargeRoomChance;

		if (roll < SmallRoomChance)
			return RoomRole.Small;

		return RoomRole.Normal;
	}

	private void GetRoomScale(
		RoomRole role,
		out float min,
		out float max)
	{
		float normal = Mathf.Clamp(
			1f - RoomSizeVariation,
			0.35f,
			0.95f
		);

		switch (role)
		{
			case RoomRole.Small:
				min = Mathf.Max(0.40f, normal - 0.15f);
				max = 0.80f;
				break;

			case RoomRole.Large:
				min = Mathf.Max(0.70f, normal + 0.15f);
				max = 1f;
				break;

			case RoomRole.Hub:
				min = Mathf.Max(0.80f, normal + 0.25f);
				max = 1f;
				break;

			default:
				min = normal;
				max = 1f;
				break;
		}

		min = Mathf.Min(min, max);
	}

	private void EnsureHub()
	{
		if (rooms.Count == 0)
			return;

		if (rooms.Any(r => r.Role == RoomRole.Hub))
			return;

		int best = 0;
		int bestArea = -1;

		for (int i = 0; i < rooms.Count; i++)
		{
			int area = rooms[i].Rect.Size.X * rooms[i].Rect.Size.Y;

			if (area > bestArea)
			{
				bestArea = area;
				best = i;
			}
		}

		rooms[best].Role = RoomRole.Hub;
	}

	// ============================================================
	// ROOM SHAPES
	// ============================================================

	private RoomShape PickRoomShape()
	{
		float[] chances =
		{
			RectangleChance,
			RoundedChance,
			OvalChance,
			OctagonalChance,
			LShapeChance,
			TShapeChance,
			CrossChance
		};

		float total = chances.Sum(v => Mathf.Max(0f, v));

		if (total <= 0f)
			return RoomShape.Rectangle;

		float roll = (float)rand.NextDouble() * total;

		for (int i = 0; i < chances.Length; i++)
		{
			roll -= Mathf.Max(0f, chances[i]);

			if (roll <= 0f)
				return (RoomShape)i;
		}

		return RoomShape.Rectangle;
	}

	private void GenerateRoomShape(
		Rect2I room,
		RoomShape shape,
		int orientation,
		int roomIndex)
	{
		for (int x = room.Position.X; x < room.End.X; x++)
		for (int y = room.Position.Y; y < room.End.Y; y++)
		{
			if (!InsideRoomShape(x, y, room, shape, orientation))
				continue;

			map[x, y] = FLOOR;
			roomGrid[x, y] = roomIndex;
		}
	}

	private bool InsideRoomShape(
		int x,
		int y,
		Rect2I r,
		RoomShape shape,
		int o)
	{
		return shape switch
		{
			RoomShape.Rectangle => true,
			RoomShape.Rounded => InsideRounded(x, y, r),
			RoomShape.Oval => InsideOval(x, y, r),
			RoomShape.Octagonal => InsideOctagon(x, y, r),
			RoomShape.L => InsideL(x, y, r, o),
			RoomShape.T => InsideT(x, y, r, o),
			RoomShape.Cross => InsideCross(x, y, r),
			_ => true
		};
	}

	private bool InsideRounded(int x, int y, Rect2I r)
	{
		int radius = Mathf.Min(
			2,
			Mathf.Min(r.Size.X, r.Size.Y) / 4
		);

		if (radius <= 0)
			return true;

		int l = r.Position.X;
		int t = r.Position.Y;
		int rr = r.End.X - 1;
		int b = r.End.Y - 1;

		if (x >= l + radius && x <= rr - radius)
			return true;

		if (y >= t + radius && y <= b - radius)
			return true;

		return
			InCircle(x, y, l + radius, t + radius, radius) ||
			InCircle(x, y, rr - radius, t + radius, radius) ||
			InCircle(x, y, l + radius, b - radius, radius) ||
			InCircle(x, y, rr - radius, b - radius, radius);
	}

	private bool InsideOval(int x, int y, Rect2I r)
	{
		float cx = r.Position.X + (r.Size.X - 1) / 2f;
		float cy = r.Position.Y + (r.Size.Y - 1) / 2f;

		float rx = Mathf.Max(1f, r.Size.X / 2f);
		float ry = Mathf.Max(1f, r.Size.Y / 2f);

		float dx = (x - cx) / rx;
		float dy = (y - cy) / ry;

		return dx * dx + dy * dy <= 1f;
	}

	private bool InsideOctagon(int x, int y, Rect2I r)
	{
		int cut = Mathf.Min(
			2,
			Mathf.Min(r.Size.X, r.Size.Y) / 4
		);

		if (cut <= 0)
			return true;

		int l = r.Position.X;
		int t = r.Position.Y;
		int rr = r.End.X - 1;
		int b = r.End.Y - 1;

		if (x < l + cut && y < t + cut)
			return (x - l) + (y - t) >= cut;

		if (x > rr - cut && y < t + cut)
			return (rr - x) + (y - t) >= cut;

		if (x < l + cut && y > b - cut)
			return (x - l) + (b - y) >= cut;

		if (x > rr - cut && y > b - cut)
			return (rr - x) + (b - y) >= cut;

		return true;
	}

	private bool InsideL(int x, int y, Rect2I r, int o)
	{
		int w = Mathf.Max(3, r.Size.X / 2);
		int h = Mathf.Max(3, r.Size.Y / 2);

		return o switch
		{
			0 => y < r.Position.Y + h || x < r.Position.X + w,
			1 => y < r.Position.Y + h || x >= r.End.X - w,
			2 => y >= r.End.Y - h || x < r.Position.X + w,
			_ => y >= r.End.Y - h || x >= r.End.X - w
		};
	}

	private bool InsideT(int x, int y, Rect2I r, int o)
	{
		int w = Mathf.Max(3, r.Size.X / 3);
		int h = Mathf.Max(3, r.Size.Y / 3);

		int cx = r.Position.X + r.Size.X / 2;
		int cy = r.Position.Y + r.Size.Y / 2;

		bool vertical = Mathf.Abs(x - cx) <= w / 2;
		bool horizontal = Mathf.Abs(y - cy) <= h / 2;

		return o switch
		{
			0 => y < r.Position.Y + h || vertical,
			1 => y >= r.End.Y - h || vertical,
			2 => x < r.Position.X + w || horizontal,
			_ => x >= r.End.X - w || horizontal
		};
	}

	private bool InsideCross(int x, int y, Rect2I r)
	{
		int cx = r.Position.X + r.Size.X / 2;
		int cy = r.Position.Y + r.Size.Y / 2;

		int w = Mathf.Max(3, r.Size.X / 3);
		int h = Mathf.Max(3, r.Size.Y / 3);

		return
			Mathf.Abs(x - cx) <= w / 2 ||
			Mathf.Abs(y - cy) <= h / 2;
	}

	private static bool InCircle(
		int x,
		int y,
		int cx,
		int cy,
		int radius)
	{
		int dx = x - cx;
		int dy = y - cy;

		return dx * dx + dy * dy <= radius * radius;
	}

	// ============================================================
	// CONNECTIONS
	// ============================================================

	private void ConnectRooms(BSPNode node, int depth)
	{
		if (node == null)
			return;

		ConnectRooms(node.Left, depth + 1);
		ConnectRooms(node.Right, depth + 1);

		if (node.Left == null || node.Right == null)
			return;

		List<int> left = new();
		List<int> right = new();

		CollectRooms(node.Left, left);
		CollectRooms(node.Right, right);

		if (!FindBestRoomPair(left, right, out int a, out int b))
			return;

		int width = depth <= PrimaryConnectionDepth
			? PrimaryCorridorWidth
			: SecondaryCorridorWidth;

		ConnectRoomPair(a, b, width);
	}

	private void CollectRooms(BSPNode node, List<int> result)
	{
		if (node == null)
			return;

		if (node.IsLeaf())
		{
			if (node.RoomIndex >= 0)
				result.Add(node.RoomIndex);

			return;
		}

		CollectRooms(node.Left, result);
		CollectRooms(node.Right, result);
	}

	private bool FindBestRoomPair(
		List<int> aList,
		List<int> bList,
		out int bestA,
		out int bestB)
	{
		bestA = -1;
		bestB = -1;

		float best = float.MaxValue;

		foreach (int a in aList)
		foreach (int b in bList)
		{
			if (HasConnection(a, b))
				continue;

			float score =
				Manhattan(GetRoomCenter(a), GetRoomCenter(b)) +
				rooms[a].Connections * 10 +
				rooms[b].Connections * 10;

			if (score >= best)
				continue;

			best = score;
			bestA = a;
			bestB = b;
		}

		return bestA >= 0 && bestB >= 0;
	}

	private void EnsureRoleConnections()
	{
		for (int a = 0; a < rooms.Count; a++)
		{
			int required = rooms[a].Role switch
			{
				RoomRole.Hub => 3,
				RoomRole.Large => 2,
				_ => 1
			};

			while (rooms[a].Connections < required)
			{
				int b = FindNearestPartner(a);

				if (b < 0)
					break;

				if (!ConnectRoomPair(a, b, SecondaryCorridorWidth))
					break;
			}
		}
	}

	private int FindNearestPartner(int source)
	{
		int best = -1;
		int bestDistance = int.MaxValue;

		for (int i = 0; i < rooms.Count; i++)
		{
			if (i == source || HasConnection(source, i))
				continue;

			int distance =
				Manhattan(
					GetRoomCenter(source),
					GetRoomCenter(i)
				);

			if (distance >= bestDistance)
				continue;

			bestDistance = distance;
			best = i;
		}

		return best;
	}

	private void AssignDeadEnds()
	{
		foreach (RoomData room in rooms)
		{
			if (
				room.Role != RoomRole.Hub &&
				room.Connections == 1 &&
				rand.NextDouble() < DeadEndRoomChance)
			{
				room.Role = RoomRole.DeadEnd;
			}
		}
	}

	private void CreateLoops()
	{
		if (rooms.Count < 3)
			return;

		int target = rand.Next(
			MinLoopCount,
			MaxLoopCount + 1
		);

		while (GetLoopCount() < target)
		{
			List<(int A, int B, int Distance)> candidates = new();

			for (int a = 0; a < rooms.Count; a++)
			{
				if (rooms[a].Role == RoomRole.DeadEnd)
					continue;

				for (int b = a + 1; b < rooms.Count; b++)
				{
					if (
						rooms[b].Role == RoomRole.DeadEnd ||
						HasConnection(a, b))
						continue;

					int distance =
						Manhattan(
							GetRoomCenter(a),
							GetRoomCenter(b)
						);

					if (
						distance >= MinLoopDistance &&
						distance <= MaxLoopDistance)
					{
						candidates.Add((a, b, distance));
					}
				}
			}

			if (candidates.Count == 0)
				break;

			candidates.Sort(
				(a, b) => a.Distance.CompareTo(b.Distance)
			);

			var pair = candidates[
				rand.Next(Mathf.Min(5, candidates.Count))
			];

			if (!ConnectRoomPair(
				pair.A,
				pair.B,
				SecondaryCorridorWidth))
			{
				break;
			}
		}
	}

	private bool ConnectRoomPair(
		int roomA,
		int roomB,
		int width)
	{
		if (roomA == roomB || HasConnection(roomA, roomB))
			return false;

		List<RoomExit> a = GetRoomExits(roomA);
		List<RoomExit> b = GetRoomExits(roomB);

		if (a.Count > 0 && b.Count > 0)
		{
			GetBestExitPair(
				a,
				b,
				out RoomExit exitA,
				out RoomExit exitB
			);

			CreateCorridor(exitA, exitB, width);
		}
		else
		{
			CreateRoute(
				GetRoomCenter(roomA),
				GetRoomCenter(roomB),
				width
			);
		}

		RegisterConnection(roomA, roomB);
		return true;
	}

	private void RegisterConnection(int a, int b)
	{
		if (!connections.Add(ConnectionKey(a, b)))
			return;

		rooms[a].Connections++;
		rooms[b].Connections++;
	}

	private bool HasConnection(int a, int b)
		=> connections.Contains(ConnectionKey(a, b));

	private static long ConnectionKey(int a, int b)
	{
		int min = Mathf.Min(a, b);
		int max = Mathf.Max(a, b);

		return ((long)min << 32) | (uint)max;
	}

	private int GetLoopCount()
		=> Mathf.Max(0, connections.Count - rooms.Count + 1);

	// ============================================================
	// EXITS
	// ============================================================

	private List<RoomExit> GetRoomExits(int roomIndex)
	{
		List<RoomExit> normal = new();
		List<RoomExit> corners = new();

		Rect2I room = rooms[roomIndex].Rect;

		Vector2I[] dirs =
		{
			Vector2I.Right,
			Vector2I.Left,
			Vector2I.Down,
			Vector2I.Up
		};

		for (int x = room.Position.X; x < room.End.X; x++)
		for (int y = room.Position.Y; y < room.End.Y; y++)
		{
			if (roomGrid[x, y] != roomIndex)
				continue;

			Vector2I cell = new(x, y);
			List<Vector2I> outside = new();

			foreach (Vector2I dir in dirs)
			{
				Vector2I next = cell + dir;

				if (
					Inside(next) &&
					roomGrid[next.X, next.Y] != roomIndex)
				{
					outside.Add(dir);
				}
			}

			if (outside.Count == 1)
			{
				normal.Add(
					new RoomExit(cell, outside[0])
				);
			}
			else if (outside.Count > 1)
			{
				foreach (Vector2I dir in outside)
					corners.Add(new RoomExit(cell, dir));
			}
		}

		return normal.Count > 0 ? normal : corners;
	}

	private void GetBestExitPair(
		List<RoomExit> aList,
		List<RoomExit> bList,
		out RoomExit bestA,
		out RoomExit bestB)
	{
		bestA = aList[0];
		bestB = bList[0];

		float best = float.MaxValue;

		foreach (RoomExit a in aList)
		foreach (RoomExit b in bList)
		{
			Vector2I delta = b.Cell - a.Cell;

			float score =
				Manhattan(a.Cell, b.Cell) +
				DirectionPenalty(a.Outward, delta) +
				DirectionPenalty(b.Outward, -delta);

			if (score >= best)
				continue;

			best = score;
			bestA = a;
			bestB = b;
		}
	}

	private static float DirectionPenalty(
		Vector2I dir,
		Vector2I target)
	{
		int dot =
			dir.X * target.X +
			dir.Y * target.Y;

		return dot > 0 ? 0 : dot == 0 ? 8 : 40;
	}

	// ============================================================
	// CORRIDORS
	// ============================================================

	private void CreateCorridor(
		RoomExit a,
		RoomExit b,
		int width)
	{
		Vector2I start = a.Cell + a.Outward;
		Vector2I end = b.Cell + b.Outward;

		if (!Inside(start)) start = a.Cell;
		if (!Inside(end)) end = b.Cell;

		CreateRoute(start, end, width);
		CarveEntrance(a, width);
		CarveEntrance(b, width);
	}

	private void CarveEntrance(RoomExit exit, int width)
	{
		Vector2I normal = exit.Outward;
		Vector2I tangent = new(-normal.Y, normal.X);

		int first = -width / 2;
		int last = first + width - 1;

		for (int depth = -EntranceDepth; depth <= EntranceDepth; depth++)
		for (int offset = first; offset <= last; offset++)
		{
			CarveFloor(
				exit.Cell +
				normal * depth +
				tangent * offset
			);
		}

		for (int side = 1; side <= EntranceShoulder; side++)
		{
			Vector2I left =
				exit.Cell +
				tangent * (first - side);

			Vector2I right =
				exit.Cell +
				tangent * (last + side);

			if (ShouldFillEntrance(left, normal))
				CarveFloor(left);

			if (ShouldFillEntrance(right, normal))
				CarveFloor(right);
		}
	}

	private bool ShouldFillEntrance(
		Vector2I cell,
		Vector2I normal)
	{
		if (!Inside(cell))
			return false;

		if (
			IsFloor(cell) ||
			IsFloor(cell - normal) ||
			IsFloor(cell + normal))
		{
			return true;
		}

		int neighbours = 0;

		for (int dx = -1; dx <= 1; dx++)
		for (int dy = -1; dy <= 1; dy++)
		{
			if (dx == 0 && dy == 0)
				continue;

			if (IsFloor(cell.X + dx, cell.Y + dy))
				neighbours++;
		}

		return neighbours >= 3;
	}

	private void CreateRoute(
		Vector2I a,
		Vector2I b,
		int width)
	{
		if (a.X == b.X || a.Y == b.Y)
		{
			CarveSegment(a, b, width);
			return;
		}

		double roll = rand.NextDouble();

		if (roll < LCorridorChance)
			CreateLRoute(a, b, width);
		else if (roll < LCorridorChance + ZCorridorChance)
			CreateZRoute(a, b, width);
		else
			CreateSRoute(a, b, width);
	}

	private void CreateLRoute(
		Vector2I a,
		Vector2I b,
		int width)
	{
		if (rand.Next(2) == 0)
		{
			CarvePolyline(
				width,
				a,
				new Vector2I(b.X, a.Y),
				b
			);
		}
		else
		{
			CarvePolyline(
				width,
				a,
				new Vector2I(a.X, b.Y),
				b
			);
		}
	}

	private void CreateZRoute(
		Vector2I a,
		Vector2I b,
		int width)
	{
		int dx = b.X - a.X;
		int dy = b.Y - a.Y;

		float t =
			0.35f +
			(float)rand.NextDouble() * 0.30f;

		if (Mathf.Abs(dx) >= Mathf.Abs(dy))
		{
			int x =
				a.X +
				Mathf.RoundToInt(dx * t);

			CarvePolyline(
				width,
				a,
				new Vector2I(x, a.Y),
				new Vector2I(x, b.Y),
				b
			);
		}
		else
		{
			int y =
				a.Y +
				Mathf.RoundToInt(dy * t);

			CarvePolyline(
				width,
				a,
				new Vector2I(a.X, y),
				new Vector2I(b.X, y),
				b
			);
		}
	}

	private void CreateSRoute(
		Vector2I a,
		Vector2I b,
		int width)
	{
		int dx = b.X - a.X;
		int dy = b.Y - a.Y;

		if (Mathf.Abs(dx) >= Mathf.Abs(dy))
		{
			int x1 = a.X + dx / 3;
			int x2 = a.X + dx * 2 / 3;

			int y = Mathf.Clamp(
				(a.Y + b.Y) / 2 +
				rand.Next(
					-CorridorWander,
					CorridorWander + 1
				),
				1,
				Height - 2
			);

			CarvePolyline(
				width,
				a,
				new Vector2I(x1, a.Y),
				new Vector2I(x1, y),
				new Vector2I(x2, y),
				new Vector2I(x2, b.Y),
				b
			);
		}
		else
		{
			int y1 = a.Y + dy / 3;
			int y2 = a.Y + dy * 2 / 3;

			int x = Mathf.Clamp(
				(a.X + b.X) / 2 +
				rand.Next(
					-CorridorWander,
					CorridorWander + 1
				),
				1,
				Width - 2
			);

			CarvePolyline(
				width,
				a,
				new Vector2I(a.X, y1),
				new Vector2I(x, y1),
				new Vector2I(x, y2),
				new Vector2I(b.X, y2),
				b
			);
		}
	}

	private void CarvePolyline(
		int width,
		params Vector2I[] points)
	{
		for (int i = 0; i < points.Length - 1; i++)
		{
			CarveSegment(
				points[i],
				points[i + 1],
				width
			);

			if (i > 0)
				CarveJoint(points[i], width);
		}
	}

	private void CarveSegment(
		Vector2I a,
		Vector2I b,
		int width)
	{
		if (a.Y == b.Y)
		{
			int sy = a.Y - width / 2;

			for (int x = Mathf.Min(a.X, b.X); x <= Mathf.Max(a.X, b.X); x++)
			for (int i = 0; i < width; i++)
				CarveFloor(x, sy + i);

			return;
		}

		if (a.X == b.X)
		{
			int sx = a.X - width / 2;

			for (int y = Mathf.Min(a.Y, b.Y); y <= Mathf.Max(a.Y, b.Y); y++)
			for (int i = 0; i < width; i++)
				CarveFloor(sx + i, y);

			return;
		}

		CreateLRoute(a, b, width);
	}

	private void CarveJoint(Vector2I center, int width)
	{
		int sx = center.X - width / 2;
		int sy = center.Y - width / 2;

		for (int x = 0; x < width; x++)
		for (int y = 0; y < width; y++)
			CarveFloor(sx + x, sy + y);
	}

	private void CarveFloor(Vector2I cell)
		=> CarveFloor(cell.X, cell.Y);

	private void CarveFloor(int x, int y)
	{
		if (IsInsideMap(x, y))
			map[x, y] = FLOOR;
	}

	// ============================================================
	// GEOMETRY REPAIR
	// ============================================================

	private void RepairGeometry()
	{
		EnsureConnectivity();

		for (int pass = 0; pass < TinyGapRepairPasses; pass++)
		{
			List<Vector2I> fill = new();

			for (int x = 1; x < Width - 1; x++)
			for (int y = 1; y < Height - 1; y++)
			{
				if (map[x, y] == FLOOR)
					continue;

				int neighbours = 0;

				for (int dx = -1; dx <= 1; dx++)
				for (int dy = -1; dy <= 1; dy++)
				{
					if (dx == 0 && dy == 0)
						continue;

					if (IsFloor(x + dx, y + dy))
						neighbours++;
				}

				if (neighbours >= 7)
					fill.Add(new Vector2I(x, y));
			}

			foreach (Vector2I cell in fill)
				CarveFloor(cell);
		}

		EnsureConnectivity();
	}

	private void EnsureConnectivity()
	{
		for (int attempt = 0; attempt < 8; attempt++)
		{
			List<List<Vector2I>> components =
				FindComponents(false);

			if (components.Count <= 1)
				return;

			List<Vector2I> main =
				components
					.OrderByDescending(c => c.Count)
					.First();

			Vector2I bestA = main[0];
			Vector2I bestB = components[1][0];

			float best = float.MaxValue;

			foreach (List<Vector2I> other in components)
			{
				if (ReferenceEquals(other, main))
					continue;

				int stepA = Mathf.Max(1, main.Count / 100);
				int stepB = Mathf.Max(1, other.Count / 100);

				for (int a = 0; a < main.Count; a += stepA)
				for (int b = 0; b < other.Count; b += stepB)
				{
					float distance =
						main[a].DistanceSquaredTo(other[b]);

					if (distance >= best)
						continue;

					best = distance;
					bestA = main[a];
					bestB = other[b];
				}
			}

			CreateLRoute(
				bestA,
				bestB,
				SecondaryCorridorWidth
			);
		}
	}

	// ============================================================
	// FLOOR TYPES
	// ============================================================

	private void AssignFloorTypes()
	{
		int[] roomFloorTypes = new int[rooms.Count];

		for (int i = 0; i < rooms.Count; i++)
			roomFloorTypes[i] = PickFloorType();

		for (int x = 0; x < Width; x++)
		for (int y = 0; y < Height; y++)
		{
			if (map[x, y] != FLOOR)
				continue;

			int roomIndex = roomGrid[x, y];

			if (
				roomIndex < 0 ||
				NearCorridor(
					new Vector2I(x, y),
					EntranceBlendRadius))
			{
				floorTypeGrid[x, y] = FLOOR_BASE;
			}
			else
			{
				floorTypeGrid[x, y] =
					roomFloorTypes[roomIndex];
			}
		}
	}

	private int PickFloorType()
	{
		float a = Mathf.Max(0f, FloorWeight);
		float b = Mathf.Max(0f, Floor1Weight);
		float c = Mathf.Max(0f, Floor2Weight);

		float total = Mathf.Max(0.001f, a + b + c);
		float roll = (float)rand.NextDouble() * total;

		if (roll < a) return FLOOR_BASE;
		if (roll < a + b) return FLOOR_1;

		return FLOOR_2;
	}

	// ============================================================
	// TREES 1x2
	// ============================================================

	private void GenerateTreesInternal()
	{
		for (int roomIndex = 0; roomIndex < rooms.Count; roomIndex++)
		{
			List<Vector2I> cells =
				GetRoomFloorCells(roomIndex);

			int target = Mathf.Clamp(
				Mathf.RoundToInt(
					cells.Count * TreeDensity
				),
				0,
				MaxTreesPerRoom
			);

			if (target <= 0)
				continue;

			Shuffle(cells);

			int placed = 0;

			foreach (Vector2I bottom in cells)
			{
				if (placed >= target)
					break;

				int variant =
					PickTreeVariant(
						floorTypeGrid[
							bottom.X,
							bottom.Y
						]
					);

				if (
					variant < 0 ||
					!CanPlaceTree(
						bottom,
						roomIndex,
						variant))
				{
					continue;
				}

				blockedGrid[
					bottom.X,
					bottom.Y
				] = true;

				if (FindComponents(true).Count != 1)
				{
					blockedGrid[
						bottom.X,
						bottom.Y
					] = false;

					continue;
				}

				Vector2I top =
					bottom + Vector2I.Up;

				occupiedGrid[
					bottom.X,
					bottom.Y
				] = true;

				occupiedGrid[
					top.X,
					top.Y
				] = true;

				trees.Add(
					new TreeData(
						bottom,
						variant
					)
				);

				placed++;
			}
		}
	}

	private int PickTreeVariant(int floorType)
	{
		bool allow1 =
			FloorAllowed(
				Tree1AllowedFloors,
				floorType
			);

		bool allow2 =
			FloorAllowed(
				Tree2AllowedFloors,
				floorType
			);

		float a =
			allow1
				? Mathf.Max(0f, Tree1Weight)
				: 0f;

		float b =
			allow2
				? Mathf.Max(0f, Tree2Weight)
				: 0f;

		if (a + b <= 0f)
			return -1;

		return
			(float)rand.NextDouble() *
			(a + b) < a
				? 0
				: 1;
	}

	private bool CanPlaceTree(
		Vector2I bottom,
		int roomIndex,
		int variant)
	{
		Vector2I top =
			bottom + Vector2I.Up;

		if (!Inside(top))
			return false;

		if (
			roomGrid[bottom.X, bottom.Y] != roomIndex ||
			roomGrid[top.X, top.Y] != roomIndex)
		{
			return false;
		}

		if (!IsFloor(bottom) || !IsFloor(top))
			return false;

		if (
			occupiedGrid[bottom.X, bottom.Y] ||
			occupiedGrid[top.X, top.Y])
		{
			return false;
		}

		int allowed =
			variant == 0
				? Tree1AllowedFloors
				: Tree2AllowedFloors;

		if (
			!FloorAllowed(
				allowed,
				floorTypeGrid[
					bottom.X,
					bottom.Y
				]) ||
			!FloorAllowed(
				allowed,
				floorTypeGrid[
					top.X,
					top.Y
				]))
		{
			return false;
		}

		if (
			NearCorridor(
				bottom,
				TreeEntranceClearance) ||
			NearCorridor(
				top,
				TreeEntranceClearance))
		{
			return false;
		}

		return
			!NearOccupied(bottom, TreeSpacing) &&
			!NearOccupied(top, TreeSpacing);
	}

	// ============================================================
	// 8 ITEMS
	// ============================================================

	private ItemConfig GetItemConfig(int index)
	{
		return index switch
		{
			0 => new ItemConfig(
				Item1Enabled,
				Item1SourceId,
				Item1Atlas,
				Item1Size,
				Item1Weight,
				Item1HasPhysics,
				Item1AllowedFloors
			),

			1 => new ItemConfig(
				Item2Enabled,
				Item2SourceId,
				Item2Atlas,
				Item2Size,
				Item2Weight,
				Item2HasPhysics,
				Item2AllowedFloors
			),

			2 => new ItemConfig(
				Item3Enabled,
				Item3SourceId,
				Item3Atlas,
				Item3Size,
				Item3Weight,
				Item3HasPhysics,
				Item3AllowedFloors
			),

			3 => new ItemConfig(
				Item4Enabled,
				Item4SourceId,
				Item4Atlas,
				Item4Size,
				Item4Weight,
				Item4HasPhysics,
				Item4AllowedFloors
			),

			4 => new ItemConfig(
				Item5Enabled,
				Item5SourceId,
				Item5Atlas,
				Item5Size,
				Item5Weight,
				Item5HasPhysics,
				Item5AllowedFloors
			),

			5 => new ItemConfig(
				Item6Enabled,
				Item6SourceId,
				Item6Atlas,
				Item6Size,
				Item6Weight,
				Item6HasPhysics,
				Item6AllowedFloors
			),

			6 => new ItemConfig(
				Item7Enabled,
				Item7SourceId,
				Item7Atlas,
				Item7Size,
				Item7Weight,
				Item7HasPhysics,
				Item7AllowedFloors
			),

			7 => new ItemConfig(
				Item8Enabled,
				Item8SourceId,
				Item8Atlas,
				Item8Size,
				Item8Weight,
				Item8HasPhysics,
				Item8AllowedFloors
			),

			_ => new ItemConfig(
				false,
				0,
				Vector2I.Zero,
				new Vector2I(1, 1),
				0,
				false,
				0
			)
		};
	}

	private void GenerateItemsInternal()
	{
		for (int roomIndex = 0; roomIndex < rooms.Count; roomIndex++)
		{
			List<Vector2I> cells =
				GetRoomFloorCells(roomIndex);

			int target = Mathf.Clamp(
				Mathf.RoundToInt(
					cells.Count * ItemDensity
				),
				0,
				MaxItemsPerRoom
			);

			if (target <= 0)
				continue;

			Shuffle(cells);

			int placed = 0;

			foreach (Vector2I origin in cells)
			{
				if (placed >= target)
					break;

				int variant =
					PickItemVariant(
						floorTypeGrid[
							origin.X,
							origin.Y
						]
					);

				if (variant < 0)
					continue;

				ItemConfig config =
					GetItemConfig(variant);

				if (!CanPlaceItem(
					origin,
					roomIndex,
					config))
				{
					continue;
				}

				if (config.HasPhysics)
				{
					SetFootprintBlocked(
						origin,
						config.Size,
						true
					);

					if (FindComponents(true).Count != 1)
					{
						SetFootprintBlocked(
							origin,
							config.Size,
							false
						);

						continue;
					}
				}

				SetFootprintOccupied(
					origin,
					config.Size,
					true
				);

				items.Add(
					new ItemData(
						origin,
						variant
					)
				);

				placed++;
			}
		}
	}

	private int PickItemVariant(int floorType)
	{
		float total = 0f;

		for (int i = 0; i < 8; i++)
		{
			ItemConfig config = GetItemConfig(i);

			if (
				config.Enabled &&
				FloorAllowed(
					config.AllowedFloors,
					floorType))
			{
				total += config.Weight;
			}
		}

		if (total <= 0f)
			return -1;

		float roll =
			(float)rand.NextDouble() *
			total;

		for (int i = 0; i < 8; i++)
		{
			ItemConfig config =
				GetItemConfig(i);

			if (
				!config.Enabled ||
				!FloorAllowed(
					config.AllowedFloors,
					floorType))
			{
				continue;
			}

			roll -= config.Weight;

			if (roll <= 0f)
				return i;
		}

		return -1;
	}

	private bool CanPlaceItem(
		Vector2I origin,
		int roomIndex,
		ItemConfig config)
	{
		for (int dx = 0; dx < config.Size.X; dx++)
		for (int dy = 0; dy < config.Size.Y; dy++)
		{
			Vector2I cell =
				origin +
				new Vector2I(dx, dy);

			if (
				!Inside(cell) ||
				!IsFloor(cell) ||
				roomGrid[cell.X, cell.Y] != roomIndex ||
				occupiedGrid[cell.X, cell.Y])
			{
				return false;
			}

			if (
				!FloorAllowed(
					config.AllowedFloors,
					floorTypeGrid[
						cell.X,
						cell.Y
					]))
			{
				return false;
			}

			if (
				NearCorridor(
					cell,
					ItemEntranceClearance) ||
				NearOccupied(
					cell,
					ItemSpacing))
			{
				return false;
			}
		}

		return true;
	}

	private void SetFootprintOccupied(
		Vector2I origin,
		Vector2I size,
		bool value)
	{
		for (int x = 0; x < size.X; x++)
		for (int y = 0; y < size.Y; y++)
		{
			occupiedGrid[
				origin.X + x,
				origin.Y + y
			] = value;
		}
	}

	private void SetFootprintBlocked(
		Vector2I origin,
		Vector2I size,
		bool value)
	{
		for (int x = 0; x < size.X; x++)
		for (int y = 0; y < size.Y; y++)
		{
			blockedGrid[
				origin.X + x,
				origin.Y + y
			] = value;
		}
	}

	// ============================================================
	// OBJECT HELPERS
	// ============================================================

	private static bool FloorAllowed(int mask, int floorType)
		=> (mask & (1 << floorType)) != 0;

	private bool NearOccupied(
		Vector2I center,
		int radius)
	{
		for (int dx = -radius; dx <= radius; dx++)
		for (int dy = -radius; dy <= radius; dy++)
		{
			int x = center.X + dx;
			int y = center.Y + dy;

			if (
				IsInsideMap(x, y) &&
				occupiedGrid[x, y])
			{
				return true;
			}
		}

		return false;
	}

	private bool NearCorridor(
		Vector2I center,
		int radius)
	{
		for (int dx = -radius; dx <= radius; dx++)
		for (int dy = -radius; dy <= radius; dy++)
		{
			int x = center.X + dx;
			int y = center.Y + dy;

			if (
				IsInsideMap(x, y) &&
				map[x, y] == FLOOR &&
				roomGrid[x, y] < 0)
			{
				return true;
			}
		}

		return false;
	}

	// ============================================================
	// DRAW
	// ============================================================

	private void DrawLevel()
	{
		GroundLayer?.Clear();
		WallLayer?.Clear();
		PropBottomLayer?.Clear();
		PropDecorLayer?.Clear();
		PropTopLayer?.Clear();

		List<Vector2I> floorCells =
			CollectFloorCells();

		DrawFloor();

		bool[,] wallMask =
			BuildWallMask();

		List<Vector2I> wallCells =
			CollectWallCells(wallMask);

		SetTerrain(
			WallLayer,
			wallCells,
			WallTerrainSet,
			WallTerrain
		);

		if (RepairRenderGaps)
		{
			RepairTerrainGaps(
				floorCells,
				wallCells,
				wallMask
			);
		}

		DrawTrees();
		DrawItems();
	}

	private List<Vector2I> CollectFloorCells()
	{
		List<Vector2I> cells = new();

		for (int x = 0; x < Width; x++)
		for (int y = 0; y < Height; y++)
		{
			if (map[x, y] == FLOOR)
				cells.Add(new Vector2I(x, y));
		}

		return cells;
	}

	private void DrawFloor()
	{
		List<Vector2I> floor = new();
		List<Vector2I> floor1 = new();
		List<Vector2I> floor2 = new();

		for (int x = 0; x < Width; x++)
		for (int y = 0; y < Height; y++)
		{
			if (map[x, y] != FLOOR)
				continue;

			Vector2I cell = new(x, y);

			switch (floorTypeGrid[x, y])
			{
				case FLOOR_1:
					floor1.Add(cell);
					break;

				case FLOOR_2:
					floor2.Add(cell);
					break;

				default:
					floor.Add(cell);
					break;
			}
		}

		SetTerrain(
			GroundLayer,
			floor1,
			FloorTerrainSet,
			Floor1Terrain
		);

		SetTerrain(
			GroundLayer,
			floor2,
			FloorTerrainSet,
			Floor2Terrain
		);

		// Базовый пол последним — коридоры и входы стабильнее.
		SetTerrain(
			GroundLayer,
			floor,
			FloorTerrainSet,
			FloorTerrain
		);
	}

	private void DrawTrees()
	{
		if (
			PropBottomLayer == null ||
			PropTopLayer == null)
		{
			return;
		}

		foreach (TreeData tree in trees)
		{
			int sourceId =
				tree.Variant == 0
					? Tree1SourceId
					: Tree2SourceId;

			Vector2I bottomAtlas =
				tree.Variant == 0
					? Tree1BottomAtlas
					: Tree2BottomAtlas;

			Vector2I topAtlas =
				bottomAtlas + Vector2I.Up;

			// Низ — с физикой.
			PropBottomLayer.SetCell(
				tree.Bottom,
				sourceId,
				bottomAtlas,
				0
			);

			// Верх — без физики, поверх игрока.
			PropTopLayer.SetCell(
				tree.Top,
				sourceId,
				topAtlas,
				0
			);
		}
	}

	private void DrawItems()
	{
		foreach (ItemData item in items)
		{
			ItemConfig config =
				GetItemConfig(item.Variant);

			TileMapLayer layer =
				config.HasPhysics
					? PropBottomLayer
					: PropDecorLayer;

			if (layer == null)
				continue;

			for (int dx = 0; dx < config.Size.X; dx++)
			for (int dy = 0; dy < config.Size.Y; dy++)
			{
				layer.SetCell(
					item.Origin +
						new Vector2I(dx, dy),
					config.SourceId,
					config.Atlas +
						new Vector2I(dx, dy),
					0
				);
			}
		}
	}

	private static void SetTerrain(
		TileMapLayer layer,
		List<Vector2I> cells,
		int terrainSet,
		int terrain)
	{
		if (layer == null || cells.Count == 0)
			return;

		layer.SetCellsTerrainConnect(
			new Godot.Collections.Array<Vector2I>(
				cells.ToArray()
			),
			terrainSet,
			terrain
		);
	}

	// ============================================================
	// WALL MASK
	// ============================================================

	private bool[,] BuildWallMask()
	{
		bool[,] mask =
			new bool[Width, Height];

		for (int x = 0; x < Width; x++)
		for (int y = 0; y < Height; y++)
		{
			if (map[x, y] != FLOOR)
				continue;

			for (int dx = -WallThickness; dx <= WallThickness; dx++)
			for (int dy = -WallThickness; dy <= WallThickness; dy++)
			{
				int nx = x + dx;
				int ny = y + dy;

				if (
					IsInsideMap(nx, ny) &&
					map[nx, ny] != FLOOR)
				{
					mask[nx, ny] = true;
				}
			}
		}

		for (int i = 0; i < WallRepairPasses; i++)
			RepairWallMask(mask);

		return mask;
	}

	private void RepairWallMask(bool[,] mask)
	{
		bool[,] add =
			new bool[Width, Height];

		for (int x = 1; x < Width - 1; x++)
		for (int y = 1; y < Height - 1; y++)
		{
			if (map[x, y] == FLOOR || mask[x, y])
				continue;

			bool l = Solid(x - 1, y, mask);
			bool r = Solid(x + 1, y, mask);
			bool u = Solid(x, y - 1, mask);
			bool d = Solid(x, y + 1, mask);

			bool ul = Solid(x - 1, y - 1, mask);
			bool ur = Solid(x + 1, y - 1, mask);
			bool dl = Solid(x - 1, y + 1, mask);
			bool dr = Solid(x + 1, y + 1, mask);

			int count =
				(l ? 1 : 0) +
				(r ? 1 : 0) +
				(u ? 1 : 0) +
				(d ? 1 : 0) +
				(ul ? 1 : 0) +
				(ur ? 1 : 0) +
				(dl ? 1 : 0) +
				(dr ? 1 : 0);

			add[x, y] =
				(l && r) ||
				(u && d) ||
				(l && u && ul) ||
				(r && u && ur) ||
				(l && d && dl) ||
				(r && d && dr) ||
				count >= 6;
		}

		for (int x = 0; x < Width; x++)
		for (int y = 0; y < Height; y++)
		{
			if (add[x, y])
				mask[x, y] = true;
		}
	}

	private bool Solid(
		int x,
		int y,
		bool[,] mask)
	{
		return
			IsInsideMap(x, y) &&
			(
				map[x, y] == FLOOR ||
				mask[x, y]
			);
	}

	private List<Vector2I> CollectWallCells(
		bool[,] mask)
	{
		List<Vector2I> result = new();

		for (int x = 0; x < Width; x++)
		for (int y = 0; y < Height; y++)
		{
			if (mask[x, y])
				result.Add(new Vector2I(x, y));
		}

		return result;
	}

	// ============================================================
	// RENDER GAP REPAIR
	// ============================================================

	private void RepairTerrainGaps(
		List<Vector2I> floor,
		List<Vector2I> walls,
		bool[,] wallMask)
	{
		List<Vector2I> missingFloor =
			floor
				.Where(c =>
					GroundLayer != null &&
					GroundLayer.GetCellSourceId(c) < 0)
				.ToList();

		List<Vector2I> missingWalls =
			walls
				.Where(c =>
					WallLayer != null &&
					WallLayer.GetCellSourceId(c) < 0)
				.ToList();

		SetTerrain(
			GroundLayer,
			missingFloor,
			FloorTerrainSet,
			FloorTerrain
		);

		SetTerrain(
			WallLayer,
			missingWalls,
			WallTerrainSet,
			WallTerrain
		);

		foreach (Vector2I cell in missingFloor)
		{
			if (
				GroundLayer != null &&
				GroundLayer.GetCellSourceId(cell) < 0)
			{
				CopyNearestRenderedTile(
					GroundLayer,
					cell,
					true,
					wallMask
				);
			}
		}

		foreach (Vector2I cell in missingWalls)
		{
			if (
				WallLayer != null &&
				WallLayer.GetCellSourceId(cell) < 0)
			{
				CopyNearestRenderedTile(
					WallLayer,
					cell,
					false,
					wallMask
				);
			}
		}
	}

	private bool CopyNearestRenderedTile(
		TileMapLayer layer,
		Vector2I target,
		bool floor,
		bool[,] wallMask)
	{
		for (int radius = 1; radius <= RenderGapSearchRadius; radius++)
		{
			Vector2I best = Vector2I.Zero;
			float bestDistance = float.MaxValue;
			bool found = false;

			for (int dx = -radius; dx <= radius; dx++)
			for (int dy = -radius; dy <= radius; dy++)
			{
				if (
					Mathf.Abs(dx) != radius &&
					Mathf.Abs(dy) != radius)
				{
					continue;
				}

				Vector2I cell =
					target +
					new Vector2I(dx, dy);

				if (!Inside(cell))
					continue;

				bool correctType = floor
					? IsFloor(cell)
					: map[cell.X, cell.Y] != FLOOR &&
					  wallMask[cell.X, cell.Y];

				if (
					!correctType ||
					layer.GetCellSourceId(cell) < 0)
				{
					continue;
				}

				float distance =
					target.DistanceSquaredTo(cell);

				if (distance >= bestDistance)
					continue;

				bestDistance = distance;
				best = cell;
				found = true;
			}

			if (!found)
				continue;

			layer.SetCell(
				target,
				layer.GetCellSourceId(best),
				layer.GetCellAtlasCoords(best),
				layer.GetCellAlternativeTile(best)
			);

			return true;
		}

		return false;
	}

	// ============================================================
	// WALKABILITY
	// ============================================================

	private List<List<Vector2I>> FindComponents(
		bool useObstacles)
	{
		List<List<Vector2I>> result = new();
		bool[,] visited = new bool[Width, Height];

		Vector2I[] dirs =
		{
			Vector2I.Right,
			Vector2I.Left,
			Vector2I.Down,
			Vector2I.Up
		};

		for (int x = 0; x < Width; x++)
		for (int y = 0; y < Height; y++)
		{
			if (
				visited[x, y] ||
				map[x, y] != FLOOR ||
				(
					useObstacles &&
					blockedGrid[x, y]
				))
			{
				continue;
			}

			List<Vector2I> component = new();
			Queue<Vector2I> queue = new();

			queue.Enqueue(new Vector2I(x, y));
			visited[x, y] = true;

			while (queue.Count > 0)
			{
				Vector2I current =
					queue.Dequeue();

				component.Add(current);

				foreach (Vector2I dir in dirs)
				{
					Vector2I next =
						current + dir;

					if (
						!Inside(next) ||
						visited[next.X, next.Y] ||
						map[next.X, next.Y] != FLOOR ||
						(
							useObstacles &&
							blockedGrid[
								next.X,
								next.Y
							]
						))
					{
						continue;
					}

					visited[
						next.X,
						next.Y
					] = true;

					queue.Enqueue(next);
				}
			}

			result.Add(component);
		}

		return result;
	}

	// ============================================================
	// METRICS
	// ============================================================

	private LevelMetrics CalculateMetrics()
	{
		LevelMetrics m = new()
		{
			Rooms = rooms.Count,
			Loops = GetLoopCount(),
			Trees = trees.Count,
			Items = items.Count
		};

		foreach (RoomData room in rooms)
		{
			if (room.Role == RoomRole.DeadEnd)
				m.DeadEnds++;
		}

		foreach (ItemData item in items)
		{
			if (GetItemConfig(item.Variant).HasPhysics)
				m.BlockingItems++;
		}

		int roomFloorCells = 0;

		for (int x = 0; x < Width; x++)
		for (int y = 0; y < Height; y++)
		{
			if (map[x, y] != FLOOR)
				continue;

			m.FloorCells++;

			if (roomGrid[x, y] < 0)
				m.CorridorCells++;
			else
				roomFloorCells++;

			if (blockedGrid[x, y])
				m.BlockedCells++;
		}

		m.WalkableCells =
			m.FloorCells -
			m.BlockedCells;

		m.Components =
			FindComponents(true).Count;

		m.AverageRoomArea =
			m.Rooms > 0
				? (float)roomFloorCells /
				  m.Rooms
				: 0f;

		m.ObstacleDensity =
			m.FloorCells > 0
				? (float)m.BlockedCells /
				  m.FloorCells
				: 0f;

		m.WalkableRatio =
			m.FloorCells > 0
				? (float)m.WalkableCells /
				  m.FloorCells
				: 0f;

		return m;
	}

	private void PrintLevelMetrics(LevelMetrics m)
	{
		GD.Print(
			$"Seed={CurrentSeed} | " +
			$"Rooms={m.Rooms} | " +
			$"DeadEnds={m.DeadEnds} | " +
			$"Loops={m.Loops} | " +
			$"Floor={m.FloorCells} | " +
			$"Corridors={m.CorridorCells} | " +
			$"Trees={m.Trees} | " +
			$"Items={m.Items} | " +
			$"BlockingItems={m.BlockingItems} | " +
			$"Blocked={m.BlockedCells} | " +
			$"Walkable={m.WalkableCells} | " +
			$"Components={m.Components} | " +
			$"ObstacleDensity={m.ObstacleDensity:F3}"
		);
	}

	private void SaveMetrics(LevelMetrics m)
	{
		string path =
			"user://" +
			MetricsFileName;

		if (!FileAccess.FileExists(path))
		{
			using FileAccess create =
				FileAccess.Open(
					path,
					FileAccess.ModeFlags.Write
				);

			create.StoreLine(
				"Seed,Rooms,DeadEnds,Loops," +
				"FloorCells,CorridorCells," +
				"Trees,Items,BlockingItems," +
				"BlockedCells,WalkableCells," +
				"Components,AverageRoomArea," +
				"ObstacleDensity,WalkableRatio"
			);
		}

		using FileAccess file =
			FileAccess.Open(
				path,
				FileAccess.ModeFlags.ReadWrite
			);

		file.SeekEnd();

		CultureInfo c =
			CultureInfo.InvariantCulture;

		file.StoreLine(
			string.Join(
				",",
				CurrentSeed,
				m.Rooms,
				m.DeadEnds,
				m.Loops,
				m.FloorCells,
				m.CorridorCells,
				m.Trees,
				m.Items,
				m.BlockingItems,
				m.BlockedCells,
				m.WalkableCells,
				m.Components,
				m.AverageRoomArea.ToString("F2", c),
				m.ObstacleDensity.ToString("F4", c),
				m.WalkableRatio.ToString("F4", c)
			)
		);
	}

	/// <summary>Печатает и сохраняет итоговые метрики поведения агента.</summary>
	public void RecordAgentMetrics(Agent agent, string outcome)
	{
		if (agent == null)
			return;

		recordedAgentCount++;
		if (agent.ReachedTarget)
		{
			reachedTargetCount++;
			if (agent.TimeToTargetSeconds >= 0.0)
			{
				totalTimeToTargetSeconds += agent.TimeToTargetSeconds;
				timeToTargetSamples++;
			}
		}
		totalPathRecalculations += agent.PathRecalculations;
		totalFailedPathfinds += agent.FailedPathfinds;
		totalSearchNodesExpanded += agent.TotalSearchNodesExpanded;
		totalPathfindingMilliseconds += agent.TotalPathfindingMilliseconds;
		totalDistanceTravelledPixels += agent.DistanceTravelled;
		totalLastPlannedPathLengthPixels += agent.LastPlannedPathLengthPixels;

		string navigationMode = NavigationModeName;
		float successRate = recordedAgentCount > 0
			? 100f * reachedTargetCount / recordedAgentCount
			: 0f;

		if (PrintAgentMetrics)
		{
			GD.Print(
				$"[AgentMetrics] Name={agent.Name} | Outcome={outcome} | Seed={CurrentSeed} | " +
				$"Mode={navigationMode} | ReachedTarget={agent.ReachedTarget} | " +
				$"TimeToTarget={agent.TimeToTargetSeconds:F2}s | ActiveTime={agent.ActiveTimeSeconds:F2}s | " +
				$"DistanceTravelled={agent.DistanceTravelled:F1}px | LastPathLength={agent.LastPlannedPathLengthPixels:F1}px | " +
				$"CumulativePlannedPath={agent.CumulativePlannedPathLengthPixels:F1}px | " +
				$"AStarCalls={agent.PathRecalculations} | AdaptiveCalls={agent.AdaptivePathRecalculations} | " +
				$"PlannedNodes={agent.TotalPathNodes} | FailedPaths={agent.FailedPathfinds} | " +
				$"Pathfinding={agent.TotalPathfindingMilliseconds:F2}ms | " +
				$"AvgPathfinding={(agent.PathRecalculations > 0 ? agent.TotalPathfindingMilliseconds / agent.PathRecalculations : 0.0):F3}ms | " +
				$"AvgPlayerDistance={agent.AverageDistanceToPlayer:F1}px | " +
				$"MinPlayerDistance={agent.MinimumDistanceToPlayer:F1}px | " +
				$"FinalPlayerDistance={agent.FinalDistanceToPlayer:F1}px"
			);
			GD.Print(
				$"[ExperimentProgress] Seed={CurrentSeed} | Mode={navigationMode} | " +
				$"Recorded={recordedAgentCount}/{configuredAgentCount} | " +
				$"SuccessRateSoFar={successRate:F1}%"
			);
		}

		if (SaveAgentMetricsCsv)
		{
			string fileName = string.IsNullOrWhiteSpace(AgentMetricsFileName)
				? "agent_metrics.csv"
				: AgentMetricsFileName.Trim();
			string path = "user://" + fileName;

			if (!FileAccess.FileExists(path))
			{
				using FileAccess create = FileAccess.Open(path, FileAccess.ModeFlags.Write);
				if (create == null)
				{
					GD.PushWarning($"Не удалось создать CSV метрик агентов: {path}");
					return;
				}
				create.StoreLine(
					"AgentName,Seed,NavigationMode,ExperimentLabel,Outcome,ReachedTarget,TimeToTargetSeconds," +
					"ActiveTimeSeconds,DistanceTravelledPixels,LastPlannedPathLengthPixels,CumulativePlannedPathLengthPixels," +
					"PathRecalculations,AdaptivePathRecalculations,TotalPlannedPathNodes,FailedPathfinds," +
					"LastSearchNodesExpanded,TotalSearchNodesExpanded,RepathIntervalSeconds,AdaptiveRadiusCells,AdaptiveCostMultiplier," +
					"TotalPathfindingMilliseconds,AveragePathfindingMilliseconds,AverageDistanceToPlayerPixels," +
					"MinimumDistanceToPlayerPixels,FinalDistanceToPlayerPixels"
				);
			}

			using FileAccess file = FileAccess.Open(path, FileAccess.ModeFlags.ReadWrite);
			if (file == null)
			{
				GD.PushWarning($"Не удалось открыть CSV метрик агентов: {path}");
				return;
			}

			file.SeekEnd();
			CultureInfo c = CultureInfo.InvariantCulture;
			string safeName = agent.Name.ToString().Replace("\"", "\"\"");
			string safeOutcome = (outcome ?? "unknown").Replace("\"", "\"\"");
			string safeLabel = (NavigationExperimentLabel ?? "").Replace("\"", "\"\"");
			file.StoreLine(string.Join(",",
				$"\"{safeName}\"",
				CurrentSeed.ToString(c),
				$"\"{navigationMode}\"",
				$"\"{safeLabel}\"",
				$"\"{safeOutcome}\"",
				agent.ReachedTarget.ToString(),
				agent.TimeToTargetSeconds.ToString("F3", c),
				agent.ActiveTimeSeconds.ToString("F3", c),
				agent.DistanceTravelled.ToString("F2", c),
				agent.LastPlannedPathLengthPixels.ToString("F2", c),
				agent.CumulativePlannedPathLengthPixels.ToString("F2", c),
				agent.PathRecalculations.ToString(c),
				agent.AdaptivePathRecalculations.ToString(c),
				agent.TotalPathNodes.ToString(c),
				agent.FailedPathfinds.ToString(c),
				agent.LastSearchNodesExpanded.ToString(c),
				agent.TotalSearchNodesExpanded.ToString(c),
				RepathIntervalSeconds.ToString("F3", c),
				AdaptiveAgentAvoidanceRadiusCells.ToString(c),
				AdaptiveAgentCostMultiplier.ToString("F3", c),
				agent.TotalPathfindingMilliseconds.ToString("F3", c),
				(agent.PathRecalculations > 0 ? agent.TotalPathfindingMilliseconds / agent.PathRecalculations : 0.0).ToString("F3", c),
				agent.AverageDistanceToPlayer.ToString("F2", c),
				agent.MinimumDistanceToPlayer.ToString("F2", c),
				agent.FinalDistanceToPlayer.ToString("F2", c)
			));
		}

		if (experimentFinished && !experimentSummarySaved && configuredAgentCount > 0 && recordedAgentCount >= configuredAgentCount)
			SaveNavigationExperimentSummary();
	}

	private void SaveNavigationExperimentSummary()
	{
		if (experimentSummarySaved)
			return;
		experimentSummarySaved = true;

		string fileName = string.IsNullOrWhiteSpace(NavigationSummaryFileName)
			? "navigation_experiment_summary.csv"
			: NavigationSummaryFileName.Trim();
		string path = "user://" + fileName;
		if (!FileAccess.FileExists(path))
		{
			using FileAccess create = FileAccess.Open(path, FileAccess.ModeFlags.Write);
			if (create == null)
			{
				GD.PushWarning($"Не удалось создать CSV сводки эксперимента: {path}");
				return;
			}
			create.StoreLine(
				"Seed,NavigationMode,ExperimentLabel,AgentsConfigured,AgentsRecorded,ReachedTarget," +
				"SuccessRatePercent,AverageTimeToTargetSeconds,AverageLastPlannedPathLengthPixels," +
				"AverageDistanceTravelledPixels,AveragePathfindingMillisecondsPerAgent," +
				"TotalPathRecalculations,TotalFailedPathfinds,TotalSearchNodesExpanded," +
				"RepathIntervalSeconds,AdaptiveRadiusCells,AdaptiveCostMultiplier,ExperimentDurationSeconds,ElapsedSeconds"
			);
		}

		using FileAccess file = FileAccess.Open(path, FileAccess.ModeFlags.ReadWrite);
		if (file == null)
		{
			GD.PushWarning($"Не удалось открыть CSV сводки эксперимента: {path}");
			return;
		}
		file.SeekEnd();
		CultureInfo c = CultureInfo.InvariantCulture;
		string mode = NavigationModeName;
		string label = (NavigationExperimentLabel ?? "").Replace("\"", "\"\"");
		float successRate = configuredAgentCount > 0 ? 100f * reachedTargetCount / configuredAgentCount : 0f;
		double averageTimeToTarget = timeToTargetSamples > 0 ? totalTimeToTargetSeconds / timeToTargetSamples : -1.0;
		double averageLastPath = recordedAgentCount > 0 ? totalLastPlannedPathLengthPixels / recordedAgentCount : 0.0;
		double averageDistance = recordedAgentCount > 0 ? totalDistanceTravelledPixels / recordedAgentCount : 0.0;
		double averagePathfinding = recordedAgentCount > 0 ? totalPathfindingMilliseconds / recordedAgentCount : 0.0;
		file.StoreLine(string.Join(",",
			CurrentSeed.ToString(c),
			$"\"{mode}\"",
			$"\"{label}\"",
			configuredAgentCount.ToString(c),
			recordedAgentCount.ToString(c),
			reachedTargetCount.ToString(c),
			successRate.ToString("F2", c),
			averageTimeToTarget.ToString("F3", c),
			averageLastPath.ToString("F2", c),
			averageDistance.ToString("F2", c),
			averagePathfinding.ToString("F3", c),
			totalPathRecalculations.ToString(c),
			totalFailedPathfinds.ToString(c),
			totalSearchNodesExpanded.ToString(c),
			RepathIntervalSeconds.ToString("F3", c),
			AdaptiveAgentAvoidanceRadiusCells.ToString(c),
			AdaptiveAgentCostMultiplier.ToString("F3", c),
			ExperimentDurationSeconds.ToString("F2", c),
			experimentElapsedSeconds.ToString("F3", c)
		));
		GD.Print(
			$"[ExperimentSummary] Seed={CurrentSeed} | Mode={mode} | " +
			$"SuccessRate={successRate:F1}% ({reachedTargetCount}/{configuredAgentCount}) | " +
			$"AvgTimeToTarget={averageTimeToTarget:F2}s | AvgLastPath={averageLastPath:F1}px | " +
			$"AvgPathfindingPerAgent={averagePathfinding:F2}ms"
		);
	}

	// ============================================================
	// NAVIGATION FOR AGENTS
	// ============================================================

	private void BuildNavigationGrid()
	{
		if (GroundLayer == null || GroundLayer.TileSet == null || map == null || blockedGrid == null)
		{
			GD.PushWarning("Не удалось построить навигационную сетку: проверь GroundLayer и TileSet.");
			return;
		}

		navigationGrid = new AStarGrid2D();
		navigationGrid.Region = new Rect2I(0, 0, Width, Height);
		navigationGrid.CellSize = new Vector2(
			GroundLayer.TileSet.TileSize.X,
			GroundLayer.TileSet.TileSize.Y
		);
		navigationGrid.DiagonalMode = AStarGrid2D.DiagonalModeEnum.Never;
		navigationGrid.Update();

		for (int x = 0; x < Width; x++)
		for (int y = 0; y < Height; y++)
		{
			bool solid = map[x, y] != FLOOR || blockedGrid[x, y];
			navigationGrid.SetPointSolid(new Vector2I(x, y), solid);
		}
	}

	/// <summary>
	/// Ищет путь по единой клеточной сетке. Dijkstra использует нулевую эвристику,
	/// A* — манхэттенскую эвристику, AdaptiveAStar — A* с динамическими штрафами
	/// клеток рядом с другими активными агентами.
	/// </summary>
	public List<Vector2> FindPathWorld(Vector2 worldStart, Vector2 worldTarget, Agent requester = null)
	{
		List<Vector2> result = new();
		if (navigationGrid == null || GroundLayer == null)
			return result;

		Vector2I startCell = GroundLayer.LocalToMap(GroundLayer.ToLocal(worldStart));
		Vector2I targetCell = GroundLayer.LocalToMap(GroundLayer.ToLocal(worldTarget));

		if (!Inside(startCell) || !Inside(targetCell))
			return result;

		if (navigationGrid.IsPointSolid(startCell) || navigationGrid.IsPointSolid(targetCell))
			return result;

		ApplyAdaptiveNavigationWeights(requester);
		bool useHeuristic = NavigationMode != NavigationAlgorithmMode.Dijkstra;
		List<Vector2I> cellPath = FindGridPath(startCell, targetCell, useHeuristic, out int expandedNodes);
		requester?.RegisterSearchNodesExpanded(expandedNodes);

		foreach (Vector2I cell in cellPath)
			result.Add(GroundLayer.ToGlobal(GroundLayer.MapToLocal(cell)));

		return result;
	}

	private List<Vector2I> FindGridPath(
		Vector2I start,
		Vector2I target,
		bool useHeuristic,
		out int expandedNodes)
	{
		expandedNodes = 0;
		List<Vector2I> empty = new();
		if (start == target)
		{
			empty.Add(start);
			return empty;
		}

		float[,] costSoFar = new float[Width, Height];
		Vector2I[,] previous = new Vector2I[Width, Height];
		bool[,] hasPrevious = new bool[Width, Height];
		for (int x = 0; x < Width; x++)
		for (int y = 0; y < Height; y++)
			costSoFar[x, y] = float.PositiveInfinity;

		PriorityQueue<NavigationQueueItem, float> frontier = new();
		costSoFar[start.X, start.Y] = 0f;
		frontier.Enqueue(new NavigationQueueItem(start, 0f), Heuristic(start, target, useHeuristic));

		Vector2I[] directions =
		{
			new Vector2I(1, 0),
			new Vector2I(-1, 0),
			new Vector2I(0, 1),
			new Vector2I(0, -1)
		};

		bool found = false;
		while (frontier.Count > 0)
		{
			NavigationQueueItem item = frontier.Dequeue();
			Vector2I current = item.Cell;

			if (item.CostSoFar > costSoFar[current.X, current.Y] + 0.0001f)
				continue;

			expandedNodes++;
			if (current == target)
			{
				found = true;
				break;
			}

			foreach (Vector2I direction in directions)
			{
				Vector2I next = current + direction;
				if (!Inside(next) || navigationGrid.IsPointSolid(next))
					continue;

				float weight = activeNavigationWeights.TryGetValue(next, out float activeWeight)
					? Mathf.Max(1f, activeWeight)
					: 1f;
				float newCost = costSoFar[current.X, current.Y] + weight;
				if (newCost + 0.0001f >= costSoFar[next.X, next.Y])
					continue;

				costSoFar[next.X, next.Y] = newCost;
				previous[next.X, next.Y] = current;
				hasPrevious[next.X, next.Y] = true;
				float priority = newCost + Heuristic(next, target, useHeuristic);
				frontier.Enqueue(new NavigationQueueItem(next, newCost), priority);
			}
		}

		if (!found)
			return empty;

		List<Vector2I> reversed = new();
		Vector2I cursor = target;
		reversed.Add(cursor);
		while (cursor != start)
		{
			if (!hasPrevious[cursor.X, cursor.Y])
				return empty;
			cursor = previous[cursor.X, cursor.Y];
			reversed.Add(cursor);
		}
		reversed.Reverse();
		return reversed;
	}

	private static float Heuristic(Vector2I from, Vector2I to, bool useHeuristic)
	{
		if (!useHeuristic)
			return 0f;
		return Mathf.Abs(from.X - to.X) + Mathf.Abs(from.Y - to.Y);
	}

	private void ApplyAdaptiveNavigationWeights(Agent requester)
	{
		if (navigationGrid == null)
			return;

		// Сначала полностью сбрасываем веса предыдущего поиска.
		foreach (Vector2I cell in adaptiveWeightedCells)
			navigationGrid.SetPointWeightScale(cell, 1.0f);
		adaptiveWeightedCells.Clear();
		activeNavigationWeights.Clear();

		if (NavigationMode != NavigationAlgorithmMode.AdaptiveAStar ||
			AdaptiveAgentCostMultiplier <= 0f || GetTree() == null)
			return;

		int radius = Mathf.Max(1, AdaptiveAgentAvoidanceRadiusCells);
		float radiusFloat = radius;
		Dictionary<Vector2I, float> weightScales = new();
		foreach (Node node in GetTree().GetNodesInGroup("agents"))
		{
			if (node is not Agent other || other == requester || !other.Visible ||
				other.IsQueuedForDeletion() || other.ReachedTarget)
				continue;

			Vector2I center = GroundLayer.LocalToMap(GroundLayer.ToLocal(other.GlobalPosition));
			if (!Inside(center))
				continue;

			for (int x = Mathf.Max(0, center.X - radius); x <= Mathf.Min(Width - 1, center.X + radius); x++)
			for (int y = Mathf.Max(0, center.Y - radius); y <= Mathf.Min(Height - 1, center.Y + radius); y++)
			{
				Vector2I cell = new(x, y);
				if (navigationGrid.IsPointSolid(cell))
					continue;

				float distance = new Vector2(x - center.X, y - center.Y).Length();
				if (distance > radiusFloat)
					continue;

				float penalty = AdaptiveAgentCostMultiplier * (1f - distance / radiusFloat);
				float currentScale = weightScales.TryGetValue(cell, out float existing) ? existing : 1f;
				weightScales[cell] = currentScale + penalty;
			}
		}

		foreach (KeyValuePair<Vector2I, float> pair in weightScales)
		{
			navigationGrid.SetPointWeightScale(pair.Key, pair.Value);
			adaptiveWeightedCells.Add(pair.Key);
			activeNavigationWeights[pair.Key] = pair.Value;
		}
	}

	// ============================================================
	// PLAYER
	// ============================================================

	private void SpawnPlayer()
	{
		if (
			Player == null ||
			GroundLayer == null ||
			GroundLayer.TileSet == null)
		{
			return;
		}

		Vector2I? spawn = FindSpawnCell();

		if (spawn == null)
			return;

		Vector2 globalPosition =
			GroundLayer.ToGlobal(
				GroundLayer.MapToLocal(spawn.Value)
			);

		if (Player.GetParent() is Node2D playerParent)
			Player.Position = playerParent.ToLocal(globalPosition);
		else
			Player.GlobalPosition = globalPosition;

		occupiedGrid[spawn.Value.X, spawn.Value.Y] = true;
		actorSpawnCells.Add(spawn.Value);
	}

	private void SpawnAgents()
	{
		experimentAgents.Clear();
		reachedAgents.Clear();
		experimentRunning = false;
		experimentFinished = false;
		experimentElapsedSeconds = 0.0;

		if (GroundLayer == null || GroundLayer.TileSet == null)
		{
			GD.PushWarning(
				"Невозможно разместить агентов: GroundLayer или TileSet не назначен."
			);
			return;
		}

		// Агенты уже находятся в дереве сцены, поэтому мы не создаём PackedScene,
		// а только выбираем их узлы и размещаем их на свободных клетках.
		List<Node2D> agentNodes = new();
		AddAgentIfAssigned(agentNodes, Agent1);
		AddAgentIfAssigned(agentNodes, Agent2);
		AddAgentIfAssigned(agentNodes, Agent3);
		AddAgentIfAssigned(agentNodes, Agent4);
		AddAgentIfAssigned(agentNodes, Agent5);
		AddAgentIfAssigned(agentNodes, Agent6);
		AddAgentIfAssigned(agentNodes, Agent7);
		AddAgentIfAssigned(agentNodes, Agent8);

		if (agentNodes.Count == 0)
		{
			GD.PushWarning(
				"Агенты не назначены. Укажи узлы Agent1, Agent2 и т. д. " +
				"в Inspector у LevelGenerator."
			);
			return;
		}

		if (AgentCount <= 0)
		{
			foreach (Node2D node in agentNodes)
				node.Visible = false;
			return;
		}

		List<Vector2I> candidates = new();

		for (int x = 0; x < Width; x++)
		for (int y = 0; y < Height; y++)
		{
			if (
				map[x, y] != FLOOR ||
				roomGrid[x, y] < 0 ||
				blockedGrid[x, y] ||
				occupiedGrid[x, y])
			{
				continue;
			}

			candidates.Add(new Vector2I(x, y));
		}

		Shuffle(candidates);

		int spawned = 0;
		int requested = Mathf.Min(AgentCount, agentNodes.Count);
		int minDistanceSquared =
			AgentSpawnMinDistance * AgentSpawnMinDistance;

		for (int i = 0; i < agentNodes.Count; i++)
		{
			Node2D agent = agentNodes[i];

			if (i >= requested)
			{
				agent.Visible = false;
				continue;
			}

			if (agent is not Agent)
			{
				GD.PushWarning($"Узел {agent.Name} не имеет скрипта Agent.cs и не будет участвовать в эксперименте.");
				agent.Visible = false;
				continue;
			}

			bool placed = false;

			foreach (Vector2I cell in candidates)
			{
				if (occupiedGrid[cell.X, cell.Y] || blockedGrid[cell.X, cell.Y])
					continue;

				bool tooClose = false;
				foreach (Vector2I reservedCell in actorSpawnCells)
				{
					if (cell.DistanceSquaredTo(reservedCell) < minDistanceSquared)
					{
						tooClose = true;
						break;
					}
				}

				if (tooClose)
					continue;

				Vector2 globalPosition =
					GroundLayer.ToGlobal(GroundLayer.MapToLocal(cell));

				if (agent.GetParent() is Node2D agentParent)
					agent.Position = agentParent.ToLocal(globalPosition);
				else
					agent.GlobalPosition = globalPosition;

				agent.Visible = true;
				Agent agentBrain = (Agent)agent;
				agentBrain.Configure(Player, this);
				experimentAgents.Add(agentBrain);
				occupiedGrid[cell.X, cell.Y] = true;
				actorSpawnCells.Add(cell);
				spawned++;
				placed = true;
				break;
			}

			if (!placed)
				agent.Visible = false;
		}

		configuredAgentCount = spawned;
		GD.Print($"Agents positioned: {spawned}/{requested}");
		GD.Print($"Navigation mode: {NavigationModeName} | " +
			$"Experiment label: {NavigationExperimentLabel} | Repath interval: {RepathIntervalSeconds:F2}s | " +
			$"Adaptive radius: {AdaptiveAgentAvoidanceRadiusCells} cells | Cost multiplier: {AdaptiveAgentCostMultiplier:F2}");

		if (spawned < requested)
		{
			GD.PushWarning(
				$"Размещено только {spawned} из {requested} агентов: " +
				"не хватило свободных клеток с заданным расстоянием."
			);
		}
	}

	private static void AddAgentIfAssigned(List<Node2D> agentNodes, Node2D agent)
	{
		if (agent != null && !agentNodes.Contains(agent))
			agentNodes.Add(agent);
	}

	private Vector2I? FindSpawnCell()
	{
		for (int i = 0; i < rooms.Count; i++)
		{
			if (rooms[i].Role == RoomRole.DeadEnd)
				continue;

			Vector2I center =
				GetRoomCenter(i);

			Vector2I? cell =
				FindFreeRoomCell(
					i,
					center
				);

			if (cell != null)
				return cell;
		}

		for (int i = 0; i < rooms.Count; i++)
		{
			Vector2I? cell =
				FindFreeRoomCell(
					i,
					GetRoomCenter(i)
				);

			if (cell != null)
				return cell;
		}

		return null;
	}

	private Vector2I? FindFreeRoomCell(
		int roomIndex,
		Vector2I center)
	{
		Vector2I? best = null;
		float bestDistance = float.MaxValue;

		foreach (
			Vector2I cell
			in GetRoomFloorCells(
				roomIndex
			))
		{
			if (
				blockedGrid[
					cell.X,
					cell.Y
				] ||
				occupiedGrid[
					cell.X,
					cell.Y
				])
			{
				continue;
			}

			float distance =
				cell.DistanceSquaredTo(
					center
				);

			if (distance >= bestDistance)
				continue;

			bestDistance = distance;
			best = cell;
		}

		return best;
	}

	// ============================================================
	// HELPERS
	// ============================================================

	private List<Vector2I> GetRoomFloorCells(
		int roomIndex)
	{
		List<Vector2I> result = new();

		Rect2I room =
			rooms[roomIndex].Rect;

		for (int x = room.Position.X; x < room.End.X; x++)
		for (int y = room.Position.Y; y < room.End.Y; y++)
		{
			if (
				map[x, y] == FLOOR &&
				roomGrid[x, y] == roomIndex)
			{
				result.Add(
					new Vector2I(x, y)
				);
			}
		}

		return result;
	}

	private Vector2I GetRoomCenter(int roomIndex)
	{
		Rect2I room =
			rooms[roomIndex].Rect;

		Vector2I center =
			new(
				room.Position.X +
					room.Size.X / 2,

				room.Position.Y +
					room.Size.Y / 2
			);

		if (
			Inside(center) &&
			IsFloor(center) &&
			roomGrid[
				center.X,
				center.Y
			] == roomIndex)
		{
			return center;
		}

		Vector2I best = center;
		float bestDistance = float.MaxValue;

		foreach (
			Vector2I cell
			in GetRoomFloorCells(
				roomIndex
			))
		{
			float distance =
				cell.DistanceSquaredTo(
					center
				);

			if (distance >= bestDistance)
				continue;

			bestDistance = distance;
			best = cell;
		}

		return best;
	}

	private void Shuffle<T>(List<T> list)
	{
		for (int i = list.Count - 1; i > 0; i--)
		{
			int j = rand.Next(i + 1);
			(list[i], list[j]) =
				(list[j], list[i]);
		}
	}

	private static int Manhattan(
		Vector2I a,
		Vector2I b)
	{
		return
			Mathf.Abs(a.X - b.X) +
			Mathf.Abs(a.Y - b.Y);
	}

	private bool Inside(Vector2I cell)
		=> IsInsideMap(cell.X, cell.Y);

	private bool IsFloor(Vector2I cell)
		=> IsFloor(cell.X, cell.Y);

	private bool IsFloor(int x, int y)
		=>
			IsInsideMap(x, y) &&
			map[x, y] == FLOOR;

	private bool IsInsideMap(int x, int y)
		=>
			x >= 0 &&
			y >= 0 &&
			x < Width &&
			y < Height;
}
