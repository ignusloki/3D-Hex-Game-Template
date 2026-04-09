using System.Collections.Generic;
using UnityEngine;

public sealed class PitstopPlacementPlanner
{
    private sealed class CandidateInfo
    {
        public CandidateInfo(HexTileData tile, float progress, int pathDistance)
        {
            Tile = tile;
            Progress = progress;
            PathDistance = pathDistance;
        }

        public HexTileData Tile { get; }
        public float Progress { get; }
        public int PathDistance { get; }
    }

    public IReadOnlyList<HexCoordinates> Plan(
        HexGridData gridData,
        HexPathfinder pathfinder,
        HexCoordinates start,
        HexCoordinates goal,
        PitstopPlacementSettings settings,
        System.Random random)
    {
        settings ??= new PitstopPlacementSettings();
        settings.Validate();

        if (gridData == null)
        {
            return new List<HexCoordinates>();
        }

        List<HexTileData> pathTiles = pathfinder?.FindPath(start, goal) is IReadOnlyList<HexTileData> path
            ? new List<HexTileData>(path)
            : new List<HexTileData>();

        List<CandidateInfo> candidates = BuildCandidates(gridData, start, goal, settings, pathTiles);
        if (candidates.Count == 0)
        {
            return new List<HexCoordinates>();
        }

        int desiredCount = Mathf.Min(settings.GetDesiredCount(gridData.Rows, gridData.Columns), candidates.Count);
        if (desiredCount <= 0)
        {
            return new List<HexCoordinates>();
        }

        List<HexCoordinates> placements = new(desiredCount);
        bool[] filledSlots = new bool[desiredCount];
        int spacing = settings.GetMinimumSpacing(gridData.Rows, gridData.Columns);

        while (spacing >= 0 && placements.Count < desiredCount)
        {
            for (int slotIndex = 0; slotIndex < desiredCount; slotIndex++)
            {
                if (filledSlots[slotIndex])
                {
                    continue;
                }

                if (TryPickCandidateForSlot(slotIndex, desiredCount, candidates, placements, settings, spacing, random, out HexCoordinates coordinates))
                {
                    placements.Add(coordinates);
                    filledSlots[slotIndex] = true;
                }
            }

            spacing--;
        }

        if (placements.Count < desiredCount)
        {
            FillRemainingCandidates(desiredCount, candidates, placements, settings, random);
        }

        placements.Sort((left, right) =>
        {
            int columnCompare = left.Column.CompareTo(right.Column);
            return columnCompare != 0 ? columnCompare : left.Row.CompareTo(right.Row);
        });

        return placements;
    }

    private static List<CandidateInfo> BuildCandidates(
        HexGridData gridData,
        HexCoordinates start,
        HexCoordinates goal,
        PitstopPlacementSettings settings,
        List<HexTileData> pathTiles)
    {
        List<CandidateInfo> candidates = new();
        float maxColumn = Mathf.Max(1, gridData.Columns - 1);

        foreach (HexTileData tile in gridData.Tiles)
        {
            if (tile == null
                || !tile.IsPassable
                || tile.Coordinates.Equals(start)
                || tile.Coordinates.Equals(goal)
                || !settings.IsAllowedBiome(tile.Biome))
            {
                continue;
            }

            float progress = tile.Coordinates.Column / maxColumn;
            int pathDistance = GetMinimumPathDistance(tile.Coordinates, pathTiles);
            candidates.Add(new CandidateInfo(tile, progress, pathDistance));
        }

        return candidates;
    }

    private static int GetMinimumPathDistance(HexCoordinates coordinates, List<HexTileData> pathTiles)
    {
        if (pathTiles == null || pathTiles.Count == 0)
        {
            return 0;
        }

        int minDistance = int.MaxValue;
        foreach (HexTileData pathTile in pathTiles)
        {
            int distance = coordinates.DistanceTo(pathTile.Coordinates);
            if (distance < minDistance)
            {
                minDistance = distance;
            }
        }

        return minDistance == int.MaxValue ? 0 : minDistance;
    }

    private static bool TryPickCandidateForSlot(
        int slotIndex,
        int slotCount,
        List<CandidateInfo> candidates,
        List<HexCoordinates> placements,
        PitstopPlacementSettings settings,
        int requiredSpacing,
        System.Random random,
        out HexCoordinates coordinates)
    {
        float desiredProgress = (slotIndex + 1f) / (slotCount + 1f);
        CandidateInfo bestCandidate = null;
        float bestScore = float.MinValue;

        foreach (CandidateInfo candidate in candidates)
        {
            if (placements.Contains(candidate.Tile.Coordinates))
            {
                continue;
            }

            if (!MeetsSpacingRequirement(candidate.Tile.Coordinates, placements, requiredSpacing))
            {
                continue;
            }

            float score = ScoreCandidate(candidate, desiredProgress, settings, random);
            if (score > bestScore)
            {
                bestScore = score;
                bestCandidate = candidate;
            }
        }

        coordinates = bestCandidate?.Tile.Coordinates ?? default;
        return bestCandidate != null;
    }

    private static void FillRemainingCandidates(
        int desiredCount,
        List<CandidateInfo> candidates,
        List<HexCoordinates> placements,
        PitstopPlacementSettings settings,
        System.Random random)
    {
        while (placements.Count < desiredCount)
        {
            CandidateInfo bestCandidate = null;
            float bestScore = float.MinValue;

            foreach (CandidateInfo candidate in candidates)
            {
                if (placements.Contains(candidate.Tile.Coordinates))
                {
                    continue;
                }

                float score = ScoreCandidate(candidate, candidate.Progress, settings, random);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestCandidate = candidate;
                }
            }

            if (bestCandidate == null)
            {
                return;
            }

            placements.Add(bestCandidate.Tile.Coordinates);
        }
    }

    private static bool MeetsSpacingRequirement(HexCoordinates candidate, List<HexCoordinates> placements, int requiredSpacing)
    {
        for (int index = 0; index < placements.Count; index++)
        {
            if (candidate.DistanceTo(placements[index]) < requiredSpacing)
            {
                return false;
            }
        }

        return true;
    }

    private static float ScoreCandidate(CandidateInfo candidate, float desiredProgress, PitstopPlacementSettings settings, System.Random random)
    {
        float progressScore = 1f - Mathf.Abs(candidate.Progress - desiredProgress);
        float pathScore = 1f - Mathf.Clamp01(candidate.PathDistance / (float)settings.preferredPathDistance);
        float costScore = 1f / (1f + Mathf.Max(0, candidate.Tile.TravelCost - 1));
        float grassBonus = candidate.Tile.Biome == Biome.grass ? 1f : 0f;

        return (progressScore * 1.5f)
            + (pathScore * settings.pathBias)
            + (costScore * settings.lowCostBias)
            + (grassBonus * settings.grassPreferenceBias)
            + ((float)random.NextDouble() * settings.randomJitter);
    }
}
