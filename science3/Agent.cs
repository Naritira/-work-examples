using Godot;
using System.Collections.Generic;

/// <summary>
/// Агент постоянно знает текущую позицию игрока и строит к нему путь по всей карте.
/// Для анализа поведения собирает время работы, пройденное расстояние и метрики A*.
/// </summary>
public partial class Agent : CharacterBody2D
{
	[ExportGroup("Movement")]
	[Export] public float Speed = 90f;
	[Export] public float WaypointReachDistance = 8f;
	[Export] public float RepathInterval = 0.35f;
	[Export] public float TargetReachDistance = 48f;

	[ExportGroup("Crowd Avoidance")]
	[Export] public float SeparationRadius = 40f;
	[Export] public float SeparationWeight = 0.45f;

	public Node2D Player { get; private set; }
	public LevelGenerator Level { get; private set; }

	private float repathTimer;
	private List<Vector2> path = new();
	private int pathIndex;
	private bool configured;
	private bool metricsRecorded;
	private uint originalCollisionLayer;
	private uint originalCollisionMask;

	// Метрики поведения агента.
	private double activeTimeSeconds;
	private float distanceTravelled;
	private int pathRecalculations;
	private int totalPathNodes;
	private int failedPathfinds;
	private double totalPathfindingMilliseconds;
	private double distanceToPlayerSum;
	private long distanceSamples;
	private float minimumDistanceToPlayer = float.MaxValue;
	private float finalDistanceToPlayer;
	private float lastPlannedPathLengthPixels;
	private float cumulativePlannedPathLengthPixels;
	private int adaptivePathRecalculations;
	private int lastSearchNodesExpanded;
	private long totalSearchNodesExpanded;
	private bool reachedTarget;
	private double timeToTargetSeconds = -1.0;

	public double ActiveTimeSeconds => activeTimeSeconds;
	public float DistanceTravelled => distanceTravelled;
	public int PathRecalculations => pathRecalculations;
	public int TotalPathNodes => totalPathNodes;
	public int FailedPathfinds => failedPathfinds;
	public double TotalPathfindingMilliseconds => totalPathfindingMilliseconds;
	public double AverageDistanceToPlayer => distanceSamples > 0
		? distanceToPlayerSum / distanceSamples
		: 0.0;
	public float MinimumDistanceToPlayer => minimumDistanceToPlayer == float.MaxValue
		? 0f
		: minimumDistanceToPlayer;
	public float FinalDistanceToPlayer => finalDistanceToPlayer;
	public float LastPlannedPathLengthPixels => lastPlannedPathLengthPixels;
	public float CumulativePlannedPathLengthPixels => cumulativePlannedPathLengthPixels;
	public int AdaptivePathRecalculations => adaptivePathRecalculations;
	public int LastSearchNodesExpanded => lastSearchNodesExpanded;
	public long TotalSearchNodesExpanded => totalSearchNodesExpanded;
	public bool ReachedTarget => reachedTarget;
	public double TimeToTargetSeconds => timeToTargetSeconds;

	public override void _Ready()
	{
		AddToGroup("agents");
		originalCollisionLayer = CollisionLayer;
		originalCollisionMask = CollisionMask;
		Player ??= GetTree().GetFirstNodeInGroup("player") as Node2D;
		Level ??= GetTree().GetFirstNodeInGroup("level_generator") as LevelGenerator;
	}

	public void Configure(Node2D player, LevelGenerator level)
	{
		Player = player;
		Level = level;
		configured = true;
		metricsRecorded = false;
		reachedTarget = false;
		CollisionLayer = originalCollisionLayer;
		CollisionMask = originalCollisionMask;
		SetPhysicsProcess(true);
		if (level != null)
			TargetReachDistance = Mathf.Max(1f, level.AgentTargetReachDistancePixels);
		repathTimer = 0f;
		path.Clear();
		pathIndex = 0;
		activeTimeSeconds = 0.0;
		distanceTravelled = 0f;
		pathRecalculations = 0;
		totalPathNodes = 0;
		failedPathfinds = 0;
		totalPathfindingMilliseconds = 0.0;
		distanceToPlayerSum = 0.0;
		distanceSamples = 0;
		minimumDistanceToPlayer = float.MaxValue;
		finalDistanceToPlayer = 0f;
		lastPlannedPathLengthPixels = 0f;
		cumulativePlannedPathLengthPixels = 0f;
		adaptivePathRecalculations = 0;
		lastSearchNodesExpanded = 0;
		totalSearchNodesExpanded = 0;
		reachedTarget = false;
		timeToTargetSeconds = -1.0;
	}

	public override void _PhysicsProcess(double delta)
	{
		if (!Visible || !configured || reachedTarget)
		{
			Velocity = Vector2.Zero;
			return;
		}

		if (Player == null || !GodotObject.IsInstanceValid(Player))
			Player = GetTree().GetFirstNodeInGroup("player") as Node2D;
		if (Level == null || !GodotObject.IsInstanceValid(Level))
			Level = GetTree().GetFirstNodeInGroup("level_generator") as LevelGenerator;

		activeTimeSeconds += delta;

		if (Player != null)
		{
			finalDistanceToPlayer = GlobalPosition.DistanceTo(Player.GlobalPosition);
			distanceToPlayerSum += finalDistanceToPlayer;
			distanceSamples++;
			if (finalDistanceToPlayer < minimumDistanceToPlayer)
				minimumDistanceToPlayer = finalDistanceToPlayer;

			if (finalDistanceToPlayer <= Mathf.Max(1f, TargetReachDistance))
			{
				reachedTarget = true;
				timeToTargetSeconds = activeTimeSeconds;
				Velocity = Vector2.Zero;

				// Достигший цели агент больше не блокирует подход остальных.
				SetDeferred("collision_layer", 0u);
				SetDeferred("collision_mask", 0u);
				Level?.NotifyAgentReachedTarget(this);
				return;
			}
		}

		Vector2 positionBeforeMovement = GlobalPosition;

		if (Player == null || Level == null)
		{
			Velocity = Vector2.Zero;
			MoveAndSlide();
			distanceTravelled += positionBeforeMovement.DistanceTo(GlobalPosition);
			return;
		}

		// Агент всегда преследует актуальную позицию игрока — без радиуса зрения и забывания.
		repathTimer -= (float)delta;
		if (repathTimer <= 0f)
		{
			RebuildPath();
			repathTimer = Mathf.Max(0.1f, RepathInterval);
		}

		MoveAlongPath();
		distanceTravelled += positionBeforeMovement.DistanceTo(GlobalPosition);
	}

	/// <summary>
	/// Сохраняет количество узлов, раскрытых при последнем поиске,
	/// и накапливает общее количество раскрытых узлов за эксперимент.
	/// Перед новым поиском вызов с нулём сбрасывает только последнее значение.
	/// </summary>
	public void RegisterSearchNodesExpanded(int expandedNodes)
	{
		int safeCount = System.Math.Max(0, expandedNodes);
		lastSearchNodesExpanded = safeCount;
		totalSearchNodesExpanded += safeCount;
	}

	private void RebuildPath()
	{
		if (Level == null || Player == null)
		{
			path.Clear();
			pathIndex = 0;
			return;
		}

		RegisterSearchNodesExpanded(0);
		ulong startUsec = Time.GetTicksUsec();
		path = Level.FindPathWorld(GlobalPosition, Player.GlobalPosition, this);
		ulong elapsedUsec = Time.GetTicksUsec() - startUsec;

		pathRecalculations++;
		if (Level.UseAdaptiveNavigationCosts)
			adaptivePathRecalculations++;
		totalPathfindingMilliseconds += elapsedUsec / 1000.0;
		totalPathNodes += path.Count;
		if (path.Count == 0)
			failedPathfinds++;

		lastPlannedPathLengthPixels = 0f;
		for (int i = 1; i < path.Count; i++)
			lastPlannedPathLengthPixels += path[i - 1].DistanceTo(path[i]);
		cumulativePlannedPathLengthPixels += lastPlannedPathLengthPixels;

		// Первый узел соответствует текущей клетке агента — его можно пропустить.
		pathIndex = path.Count > 1 ? 1 : 0;
	}

	private void MoveAlongPath()
	{
		while (pathIndex < path.Count &&
			   GlobalPosition.DistanceTo(path[pathIndex]) <= WaypointReachDistance)
		{
			pathIndex++;
		}

		if (pathIndex >= path.Count)
		{
			Velocity = Vector2.Zero;
			MoveAndSlide();
			return;
		}

		Vector2 direction = (path[pathIndex] - GlobalPosition).Normalized();
		Vector2 separation = CalculateSeparationVector();
		if (separation.LengthSquared() > 0.0001f)
		{
			direction = (direction + separation.Normalized() * Mathf.Max(0f, SeparationWeight)).Normalized();
		}

		Velocity = direction * Speed;
		MoveAndSlide();
	}

	private Vector2 CalculateSeparationVector()
	{
		Vector2 result = Vector2.Zero;
		float radius = Mathf.Max(1f, SeparationRadius);

		foreach (Node node in GetTree().GetNodesInGroup("agents"))
		{
			if (node is not Agent other || other == this || !other.Visible ||
				other.IsQueuedForDeletion() || other.reachedTarget)
				continue;

			Vector2 offset = GlobalPosition - other.GlobalPosition;
			float distance = offset.Length();
			if (distance <= 0.001f || distance >= radius)
				continue;

			float strength = (radius - distance) / radius;
			result += offset / distance * strength;
		}

		return result;
	}

	/// <summary>Завершает участие агента в эксперименте и сохраняет его метрики.</summary>
	public void FinalizeExperiment(string outcome)
	{
		Velocity = Vector2.Zero;
		SetPhysicsProcess(false);
		FinalizeMetrics(outcome);
	}

	/// <summary>Фиксирует метрики перед удалением агента.</summary>
	public void RegisterKilled()
	{
		FinalizeMetrics("killed");
		QueueFree();
	}

	public override void _ExitTree()
	{
		// Если сцену закрыли или агент удалён другим способом, сохраняем результат и для него.
		if (configured && !metricsRecorded)
			FinalizeMetrics("removed");
	}

	private void FinalizeMetrics(string outcome)
	{
		if (metricsRecorded || !configured)
			return;

		metricsRecorded = true;
		if (Level != null && GodotObject.IsInstanceValid(Level))
			Level.RecordAgentMetrics(this, outcome);
	}
}
