using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public sealed class HexObstacleController : MonoBehaviour
{
    [SerializeField] private HexObstacleSpawnSettings spawnSettings = new();
    [SerializeField] private List<HexObstacleDefinition> obstacleDefinitions = new();
    [SerializeField] private GameObject obstacleVisualPrefab;
    [Min(0f)] [SerializeField] private float visualHeight = 0.45f;

    private readonly HexObstacleSpawnPlanner spawnPlanner = new();
    private readonly HexObstaclePresenter presenter = new();
    private readonly Dictionary<HexCoordinates, HexObstacleInstance> activeObstacles = new();
    private readonly HashSet<HexCoordinates> protectedHexes = new();
    private readonly System.Random random = new();
    private HashSet<HexCoordinates> currentlyVisible = new();
    private int consecutiveFailedEligibleSpawnRolls;
    private HexObstaclePressureContext pressureContext = HexObstaclePressureContext.Empty;

    private MapGenerator mapGenerator;
    private PitstopSpawner pitstopSpawner;
    private HexFogOfWarController fogOfWarController;

    public event Action<HexObstacleTurnResult> ObstaclesUpdated;

    public IReadOnlyDictionary<HexCoordinates, HexObstacleInstance> ActiveObstacles => activeObstacles;
    public bool IsInitialized => mapGenerator != null && fogOfWarController != null;

    private void OnValidate()
    {
        spawnSettings ??= new HexObstacleSpawnSettings();
        spawnSettings.Validate();
        visualHeight = Mathf.Max(0f, visualHeight);
        obstacleVisualPrefab ??= LoadDefaultObstacleVisualPrefab();
        EnsureDefinitions();
    }

    private void OnDisable()
    {
        presenter.Clear();
        activeObstacles.Clear();
        currentlyVisible.Clear();
        consecutiveFailedEligibleSpawnRolls = 0;
        pressureContext = HexObstaclePressureContext.Empty;
    }

    public void Initialize(MapGenerator mapGenerator, PitstopSpawner pitstopSpawner, HexFogOfWarController fogOfWarController)
    {
        this.mapGenerator = mapGenerator;
        this.pitstopSpawner = pitstopSpawner;
        this.fogOfWarController = fogOfWarController;
        spawnSettings ??= new HexObstacleSpawnSettings();
        spawnSettings.Validate();
        obstacleVisualPrefab ??= LoadDefaultObstacleVisualPrefab();
        EnsureDefinitions();
        activeObstacles.Clear();
        currentlyVisible.Clear();
        consecutiveFailedEligibleSpawnRolls = 0;
        pressureContext = HexObstaclePressureContext.Empty;
        presenter.Clear();
    }

    public void SetPressureContext(HexObstaclePressureContext pressureContext)
    {
        this.pressureContext = pressureContext ?? HexObstaclePressureContext.Empty;
    }

    public void SyncVisibility(HexFogUpdateResult fogUpdate)
    {
        currentlyVisible = fogUpdate?.VisibleNow != null
            ? new HashSet<HexCoordinates>(fogUpdate.VisibleNow)
            : new HashSet<HexCoordinates>();

        presenter.Apply(mapGenerator, activeObstacles, currentlyVisible, obstacleVisualPrefab, visualHeight);
    }

    public HexObstacleTurnResult ProcessTurn(HexFogUpdateResult fogUpdate, HexCoordinates caravanCoordinates, CaravanResourceSnapshot resourceSnapshot)
    {
        HexObstacleTurnResult turnResult = new();
        if (!IsInitialized || fogUpdate == null)
        {
            return turnResult;
        }

        currentlyVisible = new HashSet<HexCoordinates>(fogUpdate.VisibleNow);
        turnResult.EnteredVisibilityCount = fogUpdate.EnteredVisibility.Count;

        DespawnObstaclesThatLeftVisibility(fogUpdate.LeftVisibility, turnResult);

        foreach (HexCoordinates coordinates in fogUpdate.EnteredVisibility)
        {
            if (activeObstacles.TryGetValue(coordinates, out HexObstacleInstance visibleObstacle) && visibleObstacle != null)
            {
                turnResult.RevealedObstacles.Add(visibleObstacle);
            }
        }

        if (activeObstacles.Remove(caravanCoordinates, out HexObstacleInstance contactedObstacle))
        {
            turnResult.ContactResult = HexObstaclePenaltyResolver.Resolve(contactedObstacle, resourceSnapshot);
            turnResult.DespawnedObstacles.Add(new HexObstacleDespawnInfo(contactedObstacle, HexObstacleDespawnReason.ConsumedByCaravan));
        }

        TrySpawnObstacle(fogUpdate, caravanCoordinates, turnResult);
        PopulateQueryCollections(turnResult);
        presenter.Apply(mapGenerator, activeObstacles, currentlyVisible, obstacleVisualPrefab, visualHeight);
        ObstaclesUpdated?.Invoke(turnResult);
        return turnResult;
    }

    public void SetProtectedHexes(IEnumerable<HexCoordinates> coordinates)
    {
        protectedHexes.Clear();
        if (coordinates == null)
        {
            return;
        }

        foreach (HexCoordinates coordinatesValue in coordinates)
        {
            protectedHexes.Add(coordinatesValue);
        }
    }

    public void RegisterProtectedHex(HexCoordinates coordinates)
    {
        protectedHexes.Add(coordinates);
    }

    public void UnregisterProtectedHex(HexCoordinates coordinates)
    {
        protectedHexes.Remove(coordinates);
    }

    public bool TryGetActiveObstacle(HexCoordinates coordinates, out HexObstacleInstance obstacle)
    {
        return activeObstacles.TryGetValue(coordinates, out obstacle);
    }

    public bool TryGetVisibleObstacle(HexCoordinates coordinates, out HexObstacleInstance obstacle)
    {
        if (currentlyVisible.Contains(coordinates) && activeObstacles.TryGetValue(coordinates, out obstacle))
        {
            return true;
        }

        obstacle = null;
        return false;
    }

    private void DespawnObstaclesThatLeftVisibility(IReadOnlyCollection<HexCoordinates> leftVisibility, HexObstacleTurnResult turnResult)
    {
        if (leftVisibility == null)
        {
            return;
        }

        foreach (HexCoordinates coordinates in leftVisibility)
        {
            if (!activeObstacles.Remove(coordinates, out HexObstacleInstance obstacle))
            {
                continue;
            }

            turnResult.DespawnedObstacles.Add(new HexObstacleDespawnInfo(obstacle, HexObstacleDespawnReason.VisibilityLost));
        }
    }

    private void TrySpawnObstacle(HexFogUpdateResult fogUpdate, HexCoordinates caravanCoordinates, HexObstacleTurnResult turnResult)
    {
        if (fogUpdate.EnteredVisibility.Count == 0)
        {
            LogSpawnDebug("Skipped obstacle spawn roll because no new hexes entered visibility.");
            return;
        }

        int effectiveMaxActiveObstacles = GetEffectiveMaxActiveObstacles();
        if (activeObstacles.Count >= effectiveMaxActiveObstacles)
        {
            LogSpawnDebug(
                $"Skipped obstacle spawn roll because the active cap is full ({activeObstacles.Count}/{effectiveMaxActiveObstacles}).");
            return;
        }

        turnResult.SpawnRollPerformed = true;
        bool forceSpawnFromPity = spawnSettings.enableSpawnPity
            && consecutiveFailedEligibleSpawnRolls >= spawnSettings.failedRollsBeforeGuaranteedSpawn;
        float spawnChance = spawnSettings.CalculateSpawnChance(fogUpdate.EnteredVisibility.Count);

        LogSpawnDebug(
            $"Eligible obstacle roll. Entered visibility: {fogUpdate.EnteredVisibility.Count}. " +
            $"Active obstacles: {activeObstacles.Count}/{effectiveMaxActiveObstacles}. " +
            $"Failure streak: {consecutiveFailedEligibleSpawnRolls}. " +
            $"Chance: {spawnChance:P0}. " +
            $"Pity forced: {forceSpawnFromPity}.");

        if (forceSpawnFromPity)
        {
            LogSpawnDebug(
                $"Obstacle pity activated after {consecutiveFailedEligibleSpawnRolls} failed eligible rolls. " +
                "This attempt will force a spawn if any valid candidate exists.");
        }

        IReadOnlyCollection<HexCoordinates> pitstopCoordinates = pitstopSpawner?.SpawnedSites != null
            ? new HashSet<HexCoordinates>(pitstopSpawner.SpawnedSites.Keys)
            : new HashSet<HexCoordinates>();
        HashSet<HexCoordinates> effectiveProtectedHexes = new(protectedHexes);
        if (pressureContext?.AdditionalProtectedHexes != null)
        {
            foreach (HexCoordinates coordinates in pressureContext.AdditionalProtectedHexes)
            {
                effectiveProtectedHexes.Add(coordinates);
            }
        }

        HexObstacleSpawnPlan spawnPlan = spawnPlanner.TryPlanSpawn(
            mapGenerator.GridData,
            spawnSettings,
            pressureContext,
            activeObstacles,
            fogUpdate.VisibleNow,
            fogUpdate.EnteredVisibility,
            pitstopCoordinates,
            effectiveProtectedHexes,
            caravanCoordinates,
            mapGenerator.StartCoordinates,
            mapGenerator.GoalCoordinates,
            obstacleDefinitions,
            random,
            forceSpawnFromPity);

        turnResult.SpawnChance = spawnPlan.Chance;
        if (!spawnPlan.ShouldSpawn || spawnPlan.Definition == null)
        {
            consecutiveFailedEligibleSpawnRolls = Mathf.Min(
                consecutiveFailedEligibleSpawnRolls + 1,
                spawnSettings.failedRollsBeforeGuaranteedSpawn);

            LogSpawnDebug(
                $"Obstacle roll failed. Reason: {spawnPlan.FailureReason}. " +
                $"Candidates: {spawnPlan.CandidateCount}. " +
                $"Rule set: visible + adjacent to caravan + on the move frontier + non-water. " +
                $"Failure streak is now {consecutiveFailedEligibleSpawnRolls}.");
            return;
        }

        HexObstacleInstance obstacle = new(spawnPlan.Coordinates, spawnPlan.Definition);
        activeObstacles[spawnPlan.Coordinates] = obstacle;
        turnResult.SpawnedObstacles.Add(obstacle);
        turnResult.SpawnRollSucceeded = true;
        consecutiveFailedEligibleSpawnRolls = 0;

        string spawnSource = spawnPlan.ForcedByPity ? "forced by pity" : "rolled normally";
        LogSpawnDebug(
            $"Obstacle spawned: {obstacle.Definition.displayName} at {obstacle.Coordinates}. " +
            $"Candidates: {spawnPlan.CandidateCount}. " +
            $"Result: {spawnSource}. Failure streak reset.");
    }

    private void PopulateQueryCollections(HexObstacleTurnResult turnResult)
    {
        foreach (HexObstacleInstance obstacle in activeObstacles.Values)
        {
            if (obstacle == null)
            {
                continue;
            }

            turnResult.ActiveObstacles.Add(obstacle);
            if (currentlyVisible.Contains(obstacle.Coordinates))
            {
                turnResult.VisibleObstacles.Add(obstacle);
            }
        }
    }

    private void EnsureDefinitions()
    {
        obstacleDefinitions ??= new List<HexObstacleDefinition>();
        if (obstacleDefinitions.Count == 0)
        {
            obstacleDefinitions = HexObstacleDefinition.CreateDefaultSet();
        }

        for (int index = obstacleDefinitions.Count - 1; index >= 0; index--)
        {
            if (obstacleDefinitions[index] == null)
            {
                obstacleDefinitions.RemoveAt(index);
                continue;
            }

            obstacleDefinitions[index].Validate();
        }

        if (obstacleDefinitions.Count == 0)
        {
            obstacleDefinitions = HexObstacleDefinition.CreateDefaultSet();
        }
    }

    private static GameObject LoadDefaultObstacleVisualPrefab()
    {
#if UNITY_EDITOR
        return AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Kay/Proto/Characters/Barbarian.fbx");
#else
        return null;
#endif
    }

    private void LogSpawnDebug(string message)
    {
        if (!spawnSettings.enableSpawnDebugLogging)
        {
            return;
        }

        Debug.Log($"[ObstacleSystem] {message}", this);
    }

    private int GetEffectiveMaxActiveObstacles()
    {
        int pressuredMinimum = pressureContext != null ? pressureContext.MinimumMaxActiveObstacles : 0;
        return Mathf.Max(spawnSettings.maxActiveObstacles, pressuredMinimum);
    }
}
