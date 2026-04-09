using UnityEngine;

public enum TenByTenPitstopLayoutMode
{
    FourStrategicAnchors,
    SevenStrategicAnchors
}

[System.Serializable]
public struct PitstopFloatRange
{
    [Range(0f, 1f)] public float min;
    [Range(0f, 1f)] public float max;

    public PitstopFloatRange(float min, float max)
    {
        this.min = min;
        this.max = max;
    }

    public float Center => (min + max) * 0.5f;

    public bool Contains(float value)
    {
        return value >= min && value <= max;
    }

    public void Validate()
    {
        min = Mathf.Clamp01(min);
        max = Mathf.Clamp01(max);
        if (max < min)
        {
            max = min;
        }
    }
}

[System.Serializable]
public struct PitstopIntRange
{
    [Min(0)] public int min;
    [Min(0)] public int max;

    public PitstopIntRange(int min, int max)
    {
        this.min = min;
        this.max = max;
    }

    public float Center => (min + max) * 0.5f;

    public bool Contains(int value)
    {
        return value >= min && value <= max;
    }

    public void Validate()
    {
        min = Mathf.Max(0, min);
        max = Mathf.Max(min, max);
    }
}

[System.Serializable]
public sealed class PitstopPlacementSettings
{
    [Header("Counts")]
    [Min(0)] public int pitstopsOnFiveByFive = 2;
    [HideInInspector] [Min(0)] public int pitstopsOnTenByTen = 4;

    [Header("10x10 Layout")]
    public TenByTenPitstopLayoutMode tenByTenLayoutMode = TenByTenPitstopLayoutMode.FourStrategicAnchors;

    [Header("Attempts")]
    [Min(1)] public int maxGenerationAttempts = 32;
    public bool keepBestLayoutIfAllAttemptsFail = true;
    [Min(1)] public int candidatePoolSize = 4;
    [Range(0f, 0.25f)] public float randomJitter = 0.05f;

    [Header("Map Retry")]
    public bool regenerateMapUntilValidLayout = true;
    public bool retryMapUntilValidLayoutWithoutLimit = true;
    [Min(0)] public int maxMapRegenerationAttempts = 25;

    [Header("Biome Filters")]
    public bool allowGrass = true;
    public bool allowForest = true;
    public bool allowMountain = false;
    public bool allowDesert = false;

    [Header("Spacing")]
    [Min(0)] public int minimumSpacingOnFiveByFive = 2;
    [Min(0)] public int minimumSpacingOnTenByTen = 4;
    [Min(0)] public int minimumSpacingOnTenByTenSeven = 2;
    [Min(0)] public int spawnGoalAdjacencyBuffer = 1;
    [Min(0)] public int minimumOpenNeighbors = 3;

    [Header("Distance Targets")]
    public PitstopIntRange earlyDistanceFromSpawn = new(2, 3);
    public PitstopIntRange lateDistanceFromGoal = new(2, 3);

    [Header("10x10 Progress Bands")]
    public PitstopFloatRange earlyBandOnTenByTen = new(0.2f, 0.3f);
    public PitstopFloatRange midBandOnTenByTen = new(0.45f, 0.6f);
    public PitstopFloatRange lateBandOnTenByTen = new(0.75f, 0.85f);
    public PitstopFloatRange midBandOnTenByTenSeven = new(0.4f, 0.65f);
    public PitstopFloatRange lateBandOnTenByTenSeven = new(0.65f, 0.85f);

    [Header("5x5 Progress Bands")]
    public PitstopFloatRange earlyBandOnFiveByFive = new(0.2f, 0.35f);
    public PitstopFloatRange lateBandOnFiveByFive = new(0.65f, 0.8f);

    [Header("Lanes")]
    [Range(0f, 1f)] public float topLaneCenter = 0.25f;
    [Range(0f, 1f)] public float middleLaneCenter = 0.5f;
    [Range(0f, 1f)] public float bottomLaneCenter = 0.75f;

    [Header("Validation Targets")]
    [Min(1)] public int minimumUniqueLanesOnFiveByFive = 2;
    [Min(1)] public int minimumUniqueLanesOnTenByTen = 3;
    [Min(0)] public int minimumRowSpanOnFiveByFive = 1;
    [Min(0)] public int minimumRowSpanOnTenByTen = 3;
    [Min(0f)] public float minimumAverageCorridorOffsetOnFiveByFive = 0.75f;
    [Min(0f)] public float minimumAverageCorridorOffsetOnTenByTen = 1f;
    [Min(0f)] public float minimumAverageCorridorOffsetOnTenByTenSeven = 0.75f;

    [Header("Scoring")]
    [Min(0f)] public float preferredCorridorOffset = 1f;
    [Range(0f, 2f)] public float lanePreferenceWeight = 0.8f;
    [Range(0f, 2f)] public float laneDiversityWeight = 0.7f;
    [Range(0f, 2f)] public float corridorOffsetWeight = 0.65f;
    [Range(0f, 2f)] public float edgeAvoidanceWeight = 0.35f;
    [Range(0f, 2f)] public float openNeighborWeight = 0.3f;
    [Range(0f, 2f)] public float distanceTargetWeight = 0.85f;

    public int GetDesiredCount(int rows, int columns)
    {
        return IsSmallMap(rows, columns)
            ? pitstopsOnFiveByFive
            : (UseSevenPitstopLayout(rows, columns) ? 7 : 4);
    }

    public int GetMinimumSpacing(int rows, int columns)
    {
        if (IsSmallMap(rows, columns))
        {
            return minimumSpacingOnFiveByFive;
        }

        return UseSevenPitstopLayout(rows, columns)
            ? minimumSpacingOnTenByTenSeven
            : minimumSpacingOnTenByTen;
    }

    public int GetMinimumUniqueLanes(int rows, int columns)
    {
        return rows * columns <= 25 ? minimumUniqueLanesOnFiveByFive : minimumUniqueLanesOnTenByTen;
    }

    public int GetMinimumRowSpan(int rows, int columns)
    {
        return rows * columns <= 25 ? minimumRowSpanOnFiveByFive : minimumRowSpanOnTenByTen;
    }

    public float GetMinimumAverageCorridorOffset(int rows, int columns)
    {
        if (IsSmallMap(rows, columns))
        {
            return minimumAverageCorridorOffsetOnFiveByFive;
        }

        return UseSevenPitstopLayout(rows, columns)
            ? minimumAverageCorridorOffsetOnTenByTenSeven
            : minimumAverageCorridorOffsetOnTenByTen;
    }

    public PitstopFloatRange GetEarlyBand(int rows, int columns)
    {
        return rows * columns <= 25 ? earlyBandOnFiveByFive : earlyBandOnTenByTen;
    }

    public PitstopFloatRange GetMidBand(int rows, int columns)
    {
        return UseSevenPitstopLayout(rows, columns)
            ? midBandOnTenByTenSeven
            : midBandOnTenByTen;
    }

    public PitstopFloatRange GetLateBand(int rows, int columns)
    {
        if (IsSmallMap(rows, columns))
        {
            return lateBandOnFiveByFive;
        }

        return UseSevenPitstopLayout(rows, columns)
            ? lateBandOnTenByTenSeven
            : lateBandOnTenByTen;
    }

    public void Validate()
    {
        pitstopsOnFiveByFive = Mathf.Max(0, pitstopsOnFiveByFive);
        pitstopsOnTenByTen = tenByTenLayoutMode == TenByTenPitstopLayoutMode.SevenStrategicAnchors ? 7 : 4;
        maxGenerationAttempts = Mathf.Max(1, maxGenerationAttempts);
        candidatePoolSize = Mathf.Max(1, candidatePoolSize);
        randomJitter = Mathf.Clamp(randomJitter, 0f, 0.25f);
        maxMapRegenerationAttempts = Mathf.Max(0, maxMapRegenerationAttempts);

        minimumSpacingOnFiveByFive = Mathf.Max(0, minimumSpacingOnFiveByFive);
        minimumSpacingOnTenByTen = Mathf.Max(0, minimumSpacingOnTenByTen);
        minimumSpacingOnTenByTenSeven = Mathf.Max(0, minimumSpacingOnTenByTenSeven);
        spawnGoalAdjacencyBuffer = Mathf.Max(0, spawnGoalAdjacencyBuffer);
        minimumOpenNeighbors = Mathf.Max(0, minimumOpenNeighbors);

        earlyDistanceFromSpawn.Validate();
        lateDistanceFromGoal.Validate();
        earlyBandOnFiveByFive.Validate();
        earlyBandOnTenByTen.Validate();
        midBandOnTenByTen.Validate();
        lateBandOnFiveByFive.Validate();
        lateBandOnTenByTen.Validate();
        midBandOnTenByTenSeven.Validate();
        lateBandOnTenByTenSeven.Validate();

        topLaneCenter = Mathf.Clamp01(topLaneCenter);
        middleLaneCenter = Mathf.Clamp01(middleLaneCenter);
        bottomLaneCenter = Mathf.Clamp01(bottomLaneCenter);

        if (middleLaneCenter < topLaneCenter)
        {
            middleLaneCenter = topLaneCenter;
        }

        if (bottomLaneCenter < middleLaneCenter)
        {
            bottomLaneCenter = middleLaneCenter;
        }

        minimumUniqueLanesOnFiveByFive = Mathf.Max(1, minimumUniqueLanesOnFiveByFive);
        minimumUniqueLanesOnTenByTen = Mathf.Max(1, minimumUniqueLanesOnTenByTen);
        minimumRowSpanOnFiveByFive = Mathf.Max(0, minimumRowSpanOnFiveByFive);
        minimumRowSpanOnTenByTen = Mathf.Max(0, minimumRowSpanOnTenByTen);
        minimumAverageCorridorOffsetOnFiveByFive = Mathf.Max(0f, minimumAverageCorridorOffsetOnFiveByFive);
        minimumAverageCorridorOffsetOnTenByTen = Mathf.Max(0f, minimumAverageCorridorOffsetOnTenByTen);
        minimumAverageCorridorOffsetOnTenByTenSeven = Mathf.Max(0f, minimumAverageCorridorOffsetOnTenByTenSeven);

        preferredCorridorOffset = Mathf.Max(0f, preferredCorridorOffset);
        lanePreferenceWeight = Mathf.Clamp(lanePreferenceWeight, 0f, 2f);
        laneDiversityWeight = Mathf.Clamp(laneDiversityWeight, 0f, 2f);
        corridorOffsetWeight = Mathf.Clamp(corridorOffsetWeight, 0f, 2f);
        edgeAvoidanceWeight = Mathf.Clamp(edgeAvoidanceWeight, 0f, 2f);
        openNeighborWeight = Mathf.Clamp(openNeighborWeight, 0f, 2f);
        distanceTargetWeight = Mathf.Clamp(distanceTargetWeight, 0f, 2f);
    }

    public bool IsAllowedBiome(Biome biome)
    {
        return biome switch
        {
            Biome.grass => allowGrass,
            Biome.forest => allowForest,
            Biome.mountain => allowMountain,
            Biome.desert => allowDesert,
            _ => false
        };
    }

    public bool CanRetryMap(int rerollsUsed)
    {
        if (!regenerateMapUntilValidLayout)
        {
            return false;
        }

        if (retryMapUntilValidLayoutWithoutLimit)
        {
            return true;
        }

        return rerollsUsed < maxMapRegenerationAttempts;
    }

    public bool UseSevenPitstopLayout(int rows, int columns)
    {
        return !IsSmallMap(rows, columns)
            && tenByTenLayoutMode == TenByTenPitstopLayoutMode.SevenStrategicAnchors;
    }

    private static bool IsSmallMap(int rows, int columns)
    {
        return rows * columns <= 25;
    }
}
