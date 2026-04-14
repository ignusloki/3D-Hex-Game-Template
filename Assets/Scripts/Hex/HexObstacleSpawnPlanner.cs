using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public sealed class HexObstacleSpawnSettings
{
    [Min(0)] public int maxActiveObstacles = 2;
    public bool enableSpawnPity = true;
    [Min(0)] public int failedRollsBeforeGuaranteedSpawn = 2;
    public bool enableSpawnDebugLogging;
    [Range(0f, 1f)] public float baseSpawnChance = 0.12f;
    [Range(0f, 1f)] public float bonusChancePerEnteredHex = 0.08f;
    [Range(0f, 1f)] public float maxSpawnChance = 0.45f;
    [Min(0f)] public float visibleNeighborWeight = 0.75f;
    [Min(0f)] public float enteredNeighborWeight = 1.4f;
    [Min(0f)] public float edgePenaltyWeight = 0.15f;
    [Range(0f, 0.5f)] public float randomJitter = 0.08f;
    [Min(0f)] public float minimumCandidateWeight = 0.05f;

    public void Validate()
    {
        maxActiveObstacles = Mathf.Max(0, maxActiveObstacles);
        failedRollsBeforeGuaranteedSpawn = Mathf.Max(0, failedRollsBeforeGuaranteedSpawn);
        baseSpawnChance = Mathf.Clamp01(baseSpawnChance);
        bonusChancePerEnteredHex = Mathf.Clamp01(bonusChancePerEnteredHex);
        maxSpawnChance = Mathf.Clamp01(maxSpawnChance);
        if (maxSpawnChance < baseSpawnChance)
        {
            maxSpawnChance = baseSpawnChance;
        }

        visibleNeighborWeight = Mathf.Max(0f, visibleNeighborWeight);
        enteredNeighborWeight = Mathf.Max(0f, enteredNeighborWeight);
        edgePenaltyWeight = Mathf.Max(0f, edgePenaltyWeight);
        randomJitter = Mathf.Clamp(randomJitter, 0f, 0.5f);
        minimumCandidateWeight = Mathf.Max(0f, minimumCandidateWeight);
    }

    public float CalculateSpawnChance(int enteredVisibilityCount)
    {
        if (enteredVisibilityCount <= 0)
        {
            return 0f;
        }

        return Mathf.Min(baseSpawnChance + (enteredVisibilityCount * bonusChancePerEnteredHex), maxSpawnChance);
    }
}

public sealed class HexObstacleSpawnPlan
{
    public static HexObstacleSpawnPlan None { get; } = new();

    public HexObstacleSpawnPlan(
        bool shouldSpawn,
        HexCoordinates coordinates,
        HexObstacleDefinition definition,
        float chance,
        int candidateCount,
        bool forcedByPity,
        HexObstacleSpawnFailureReason failureReason)
    {
        ShouldSpawn = shouldSpawn;
        Coordinates = coordinates;
        Definition = definition;
        Chance = chance;
        CandidateCount = candidateCount;
        ForcedByPity = forcedByPity;
        FailureReason = failureReason;
    }

    private HexObstacleSpawnPlan()
    {
    }

    public bool ShouldSpawn { get; }
    public HexCoordinates Coordinates { get; }
    public HexObstacleDefinition Definition { get; }
    public float Chance { get; }
    public int CandidateCount { get; }
    public bool ForcedByPity { get; }
    public HexObstacleSpawnFailureReason FailureReason { get; }
}

public enum HexObstacleSpawnFailureReason
{
    None,
    ChanceRollFailed,
    NoValidCandidates,
    NoValidDefinitions
}

public sealed class HexObstacleSpawnPlanner
{
    private sealed class CandidateInfo
    {
        public HexTileData TileData;
        public float Weight;
        public int AdjacentVisible;
        public int AdjacentEntered;
    }

    public HexObstacleSpawnPlan TryPlanSpawn(
        HexGridData gridData,
        HexObstacleSpawnSettings settings,
        HexObstaclePressureContext pressureContext,
        IReadOnlyDictionary<HexCoordinates, HexObstacleInstance> activeObstacles,
        IReadOnlyCollection<HexCoordinates> visibleNow,
        IReadOnlyCollection<HexCoordinates> enteredVisibility,
        IReadOnlyCollection<HexCoordinates> pitstopCoordinates,
        IReadOnlyCollection<HexCoordinates> protectedCoordinates,
        HexCoordinates caravanCoordinates,
        HexCoordinates startCoordinates,
        HexCoordinates goalCoordinates,
        IReadOnlyList<HexObstacleDefinition> definitions,
        System.Random random,
        bool forceSpawn = false)
    {
        settings ??= new HexObstacleSpawnSettings();
        settings.Validate();
        random ??= new System.Random();

        if (gridData == null
            || definitions == null
            || definitions.Count == 0
            || visibleNow == null
            || enteredVisibility == null)
        {
            return HexObstacleSpawnPlan.None;
        }

        int enteredCount = enteredVisibility.Count;
        float spawnChance = settings.CalculateSpawnChance(enteredCount);
        if (HasPressureNearEnteredTiles(enteredSet: new HashSet<HexCoordinates>(enteredVisibility), pressureContext))
        {
            spawnChance = Mathf.Clamp01(spawnChance + pressureContext.PressureSpawnChanceBonus);
        }

        if (!forceSpawn && spawnChance <= 0f)
        {
            return HexObstacleSpawnPlan.None;
        }

        if (!forceSpawn && (float)random.NextDouble() > spawnChance)
        {
            return new HexObstacleSpawnPlan(false, default, null, spawnChance, 0, false, HexObstacleSpawnFailureReason.ChanceRollFailed);
        }

        HashSet<HexCoordinates> visibleSet = new(visibleNow);
        HashSet<HexCoordinates> enteredSet = new(enteredVisibility);
        HashSet<HexCoordinates> pitstopSet = pitstopCoordinates != null ? new HashSet<HexCoordinates>(pitstopCoordinates) : new HashSet<HexCoordinates>();
        HashSet<HexCoordinates> protectedSet = protectedCoordinates != null ? new HashSet<HexCoordinates>(protectedCoordinates) : new HashSet<HexCoordinates>();
        IReadOnlyDictionary<HexCoordinates, HexObstacleInstance> activeSet = activeObstacles ?? new Dictionary<HexCoordinates, HexObstacleInstance>();

        List<CandidateInfo> candidates = BuildCandidates(
            gridData,
            settings,
            pressureContext,
            activeSet,
            visibleSet,
            enteredSet,
            pitstopSet,
            protectedSet,
            caravanCoordinates,
            startCoordinates,
            goalCoordinates,
            random);

        if (candidates.Count == 0)
        {
            return new HexObstacleSpawnPlan(false, default, null, spawnChance, 0, forceSpawn, HexObstacleSpawnFailureReason.NoValidCandidates);
        }

        CandidateInfo chosenCandidate = PickCandidate(candidates, random);
        HexObstacleDefinition definition = PickDefinition(chosenCandidate.TileData.Biome, definitions, random);
        if (definition == null)
        {
            return new HexObstacleSpawnPlan(false, default, null, spawnChance, candidates.Count, forceSpawn, HexObstacleSpawnFailureReason.NoValidDefinitions);
        }

        return new HexObstacleSpawnPlan(true, chosenCandidate.TileData.Coordinates, definition, spawnChance, candidates.Count, forceSpawn, HexObstacleSpawnFailureReason.None);
    }

    private static List<CandidateInfo> BuildCandidates(
        HexGridData gridData,
        HexObstacleSpawnSettings settings,
        HexObstaclePressureContext pressureContext,
        IReadOnlyDictionary<HexCoordinates, HexObstacleInstance> activeObstacles,
        HashSet<HexCoordinates> visibleSet,
        HashSet<HexCoordinates> enteredSet,
        HashSet<HexCoordinates> pitstopSet,
        HashSet<HexCoordinates> protectedSet,
        HexCoordinates caravanCoordinates,
        HexCoordinates startCoordinates,
        HexCoordinates goalCoordinates,
        System.Random random)
    {
        List<CandidateInfo> candidates = new();
        foreach (HexCoordinates visibleCoordinates in visibleSet)
        {
            if (caravanCoordinates.DistanceTo(visibleCoordinates) != 1
                || !gridData.TryGetTile(visibleCoordinates, out HexTileData tile))
            {
                continue;
            }

            if (!IsValidSpawnTile(
                    tile,
                    activeObstacles,
                    pitstopSet,
                    protectedSet,
                    caravanCoordinates,
                    startCoordinates,
                    goalCoordinates))
            {
                continue;
            }

            CountFrontierAdjacency(tile.Coordinates, gridData, visibleSet, enteredSet, out int adjacentVisible, out int adjacentEntered);
            bool isFrontierCandidate = enteredSet.Contains(tile.Coordinates) || adjacentEntered > 0;
            if (!isFrontierCandidate)
            {
                continue;
            }

            float weight = ScoreCandidate(tile, gridData, settings, pressureContext, adjacentVisible, adjacentEntered, random);
            if (weight < settings.minimumCandidateWeight)
            {
                continue;
            }

            candidates.Add(new CandidateInfo
            {
                TileData = tile,
                Weight = weight,
                AdjacentVisible = adjacentVisible,
                AdjacentEntered = adjacentEntered
            });
        }

        return candidates;
    }

    private static bool IsValidSpawnTile(
        HexTileData tile,
        IReadOnlyDictionary<HexCoordinates, HexObstacleInstance> activeObstacles,
        HashSet<HexCoordinates> pitstopSet,
        HashSet<HexCoordinates> protectedSet,
        HexCoordinates caravanCoordinates,
        HexCoordinates startCoordinates,
        HexCoordinates goalCoordinates)
    {
        if (tile == null
            || !tile.IsPassable
            || tile.Biome == Biome.water
            || tile.Coordinates.Equals(caravanCoordinates)
            || tile.Coordinates.Equals(startCoordinates)
            || tile.Coordinates.Equals(goalCoordinates)
            || pitstopSet.Contains(tile.Coordinates)
            || protectedSet.Contains(tile.Coordinates)
            || activeObstacles.ContainsKey(tile.Coordinates))
        {
            return false;
        }

        return true;
    }

    private static float ScoreCandidate(
        HexTileData tile,
        HexGridData gridData,
        HexObstacleSpawnSettings settings,
        HexObstaclePressureContext pressureContext,
        int adjacentVisible,
        int adjacentEntered,
        System.Random random)
    {
        int edgeDistance = GetEdgeDistance(tile.Coordinates, gridData);
        float weight = 1f;
        weight += adjacentVisible * settings.visibleNeighborWeight;
        weight += adjacentEntered * settings.enteredNeighborWeight;
        if (IsNearPressure(tile.Coordinates, pressureContext))
        {
            weight += pressureContext.PressureCandidateWeightBonus;
        }

        weight -= edgeDistance == 0 ? settings.edgePenaltyWeight : 0f;
        weight += (float)random.NextDouble() * settings.randomJitter;
        return Mathf.Max(0f, weight);
    }

    private static bool HasPressureNearEnteredTiles(HashSet<HexCoordinates> enteredSet, HexObstaclePressureContext pressureContext)
    {
        if (enteredSet == null || enteredSet.Count == 0 || pressureContext == null || pressureContext.PressureHexes == null)
        {
            return false;
        }

        foreach (HexCoordinates coordinates in enteredSet)
        {
            if (IsNearPressure(coordinates, pressureContext))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsNearPressure(HexCoordinates coordinates, HexObstaclePressureContext pressureContext)
    {
        if (pressureContext == null || pressureContext.PressureHexes == null)
        {
            return false;
        }

        foreach (HexCoordinates pressureHex in pressureContext.PressureHexes)
        {
            if (coordinates.DistanceTo(pressureHex) <= pressureContext.PressureInfluenceRadius)
            {
                return true;
            }
        }

        return false;
    }

    private static void CountFrontierAdjacency(
        HexCoordinates coordinates,
        HexGridData gridData,
        HashSet<HexCoordinates> visibleSet,
        HashSet<HexCoordinates> enteredSet,
        out int adjacentVisible,
        out int adjacentEntered)
    {
        adjacentVisible = 0;
        adjacentEntered = 0;
        foreach (HexCoordinates neighborCoordinates in gridData.GetNeighborCoordinates(coordinates))
        {
            if (visibleSet.Contains(neighborCoordinates))
            {
                adjacentVisible++;
            }

            if (enteredSet.Contains(neighborCoordinates))
            {
                adjacentEntered++;
            }
        }
    }

    private static CandidateInfo PickCandidate(List<CandidateInfo> candidates, System.Random random)
    {
        float totalWeight = 0f;
        for (int index = 0; index < candidates.Count; index++)
        {
            totalWeight += Mathf.Max(0.001f, candidates[index].Weight);
        }

        float roll = (float)random.NextDouble() * totalWeight;
        for (int index = 0; index < candidates.Count; index++)
        {
            float weight = Mathf.Max(0.001f, candidates[index].Weight);
            if (roll <= weight)
            {
                return candidates[index];
            }

            roll -= weight;
        }

        return candidates[0];
    }

    private static HexObstacleDefinition PickDefinition(Biome biome, IReadOnlyList<HexObstacleDefinition> definitions, System.Random random)
    {
        List<(HexObstacleDefinition definition, float weight)> weightedDefinitions = new();
        float totalWeight = 0f;
        for (int index = 0; index < definitions.Count; index++)
        {
            HexObstacleDefinition definition = definitions[index];
            if (definition == null)
            {
                continue;
            }

            float weight = Mathf.Max(0f, definition.spawnWeight) * Mathf.Max(0f, definition.GetBiomeWeight(biome));
            if (weight <= 0f)
            {
                continue;
            }

            weightedDefinitions.Add((definition, weight));
            totalWeight += weight;
        }

        if (weightedDefinitions.Count == 0 || totalWeight <= 0f)
        {
            return null;
        }

        float roll = (float)random.NextDouble() * totalWeight;
        for (int index = 0; index < weightedDefinitions.Count; index++)
        {
            if (roll <= weightedDefinitions[index].weight)
            {
                return weightedDefinitions[index].definition;
            }

            roll -= weightedDefinitions[index].weight;
        }

        return weightedDefinitions[0].definition;
    }

    private static int GetEdgeDistance(HexCoordinates coordinates, HexGridData gridData)
    {
        int top = coordinates.Row;
        int bottom = gridData.Rows - 1 - coordinates.Row;
        int left = coordinates.Column;
        int right = gridData.Columns - 1 - coordinates.Column;
        return Mathf.Min(Mathf.Min(top, bottom), Mathf.Min(left, right));
    }
}
