using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class PitstopLayoutResult
{
    public static PitstopLayoutResult Empty { get; } = new(
        new List<HexCoordinates>(),
        false,
        float.MinValue,
        0,
        "No valid pitstop layout was generated.",
        string.Empty);

    public PitstopLayoutResult(
        IReadOnlyList<HexCoordinates> coordinates,
        bool isValid,
        float score,
        int attemptsUsed,
        string summary,
        string diagnosticsSummary)
    {
        Coordinates = coordinates ?? new List<HexCoordinates>();
        IsValid = isValid;
        Score = score;
        AttemptsUsed = attemptsUsed;
        Summary = summary ?? string.Empty;
        DiagnosticsSummary = diagnosticsSummary ?? string.Empty;
    }

    public IReadOnlyList<HexCoordinates> Coordinates { get; }
    public bool IsValid { get; }
    public float Score { get; }
    public int AttemptsUsed { get; }
    public string Summary { get; }
    public string DiagnosticsSummary { get; }
}

public sealed class PitstopCandidateDiagnostics
{
    public int TotalTilesEvaluated { get; private set; }
    public int RejectedImpassable { get; private set; }
    public int RejectedEndpointTile { get; private set; }
    public int RejectedReserved { get; private set; }
    public int RejectedBiome { get; private set; }
    public int RejectedEndpointBuffer { get; private set; }
    public int RejectedOpenNeighbors { get; private set; }
    public int AcceptedCandidates { get; private set; }

    public void CountImpassable()
    {
        TotalTilesEvaluated++;
        RejectedImpassable++;
    }

    public void CountEndpointTile()
    {
        TotalTilesEvaluated++;
        RejectedEndpointTile++;
    }

    public void CountReserved()
    {
        TotalTilesEvaluated++;
        RejectedReserved++;
    }

    public void CountBiome()
    {
        TotalTilesEvaluated++;
        RejectedBiome++;
    }

    public void CountEndpointBuffer()
    {
        TotalTilesEvaluated++;
        RejectedEndpointBuffer++;
    }

    public void CountOpenNeighbors()
    {
        TotalTilesEvaluated++;
        RejectedOpenNeighbors++;
    }

    public void CountAccepted()
    {
        TotalTilesEvaluated++;
        AcceptedCandidates++;
    }

    public string GetSummary()
    {
        return $"Pitstop candidate summary: accepted={AcceptedCandidates}/{TotalTilesEvaluated}, reserved={RejectedReserved}, biome={RejectedBiome}, endpointTiles={RejectedEndpointTile}, endpointBuffer={RejectedEndpointBuffer}, openNeighbors={RejectedOpenNeighbors}, impassable={RejectedImpassable}.";
    }
}

public sealed partial class PitstopPlacementPlanner
{
    private enum PitstopLane
    {
        Top,
        Middle,
        Bottom
    }

    private enum SlotType
    {
        EarlyTop,
        EarlyBottom,
        EarlyFlexible,
        Mid,
        MidTop,
        MidMiddle,
        MidBottom,
        Support,
        Late,
        LateTop,
        LateBottom
    }

    private sealed class CandidateInfo
    {
        public HexCoordinates Coordinates;
        public float Progress;
        public float RowProgress;
        public float CorridorRow;
        public int CorridorDistance;
        public int DistanceToStart;
        public int DistanceToGoal;
        public int OpenNeighborCount;
        public int EdgeDistance;
        public bool IsEdge;
        public PitstopLane Lane;
        public PitstopLane CorridorLane;
    }

    private sealed class SlotRequest
    {
        public SlotType Type;
        public PitstopFloatRange ProgressRange;
        public PitstopFloatRange AllowedProgressRange;
        public bool RelaxEdgeAvoidance;
    }

    private sealed class PlacedCandidate
    {
        public SlotRequest Slot;
        public CandidateInfo Candidate;
        public float Score;
    }

    public PitstopLayoutResult GeneratePitstops(
        HexGridData gridData,
        HexPathfinder pathfinder,
        HexCoordinates start,
        HexCoordinates goal,
        PitstopPlacementSettings settings,
        System.Random random,
        HexMapGenerationModifiers? generationModifiers = null,
        HexMapPlacementReservations reservations = null)
    {
        settings ??= new PitstopPlacementSettings();
        settings.Validate();
        random ??= new System.Random();
        HexMapGenerationModifiers resolvedGenerationModifiers = generationModifiers ?? HexMapGenerationModifiers.None;

        if (gridData == null)
        {
            return PitstopLayoutResult.Empty;
        }

        List<CandidateInfo> candidates = BuildCandidates(gridData, pathfinder, start, goal, settings, reservations, out PitstopCandidateDiagnostics diagnostics);
        if (candidates.Count == 0)
        {
            return new PitstopLayoutResult(
                new List<HexCoordinates>(),
                false,
                float.MinValue,
                0,
                "No valid pitstop candidates were generated.",
                diagnostics.GetSummary());
        }

        List<SlotRequest> slots = BuildSlotRequests(gridData.Rows, gridData.Columns, settings, resolvedGenerationModifiers);
        if (slots.Count == 0)
        {
            return new PitstopLayoutResult(
                new List<HexCoordinates>(),
                false,
                float.MinValue,
                0,
                "No pitstop slots were requested for this map.",
                diagnostics.GetSummary());
        }

        PlacedCandidate[] bestLayout = null;
        float bestScore = float.MinValue;
        string bestSummary = "No candidate layout reached the validation thresholds.";

        for (int attempt = 1; attempt <= settings.maxGenerationAttempts; attempt++)
        {
            if (!TryBuildLayout(candidates, slots, gridData, settings, random, out PlacedCandidate[] layout))
            {
                continue;
            }

            (bool isValid, float score, string summary) = EvaluateLayout(layout, gridData, settings, start, goal);
            if (score > bestScore)
            {
                bestLayout = layout;
                bestScore = score;
                bestSummary = summary;
            }

            if (isValid)
            {
                return new PitstopLayoutResult(
                    ExtractCoordinates(layout, start, goal),
                    true,
                    score,
                    attempt,
                    summary,
                    diagnostics.GetSummary());
            }
        }

        if (bestLayout != null && settings.keepBestLayoutIfAllAttemptsFail)
        {
            return new PitstopLayoutResult(
                ExtractCoordinates(bestLayout, start, goal),
                false,
                bestScore,
                settings.maxGenerationAttempts,
                bestSummary,
                diagnostics.GetSummary());
        }

        return new PitstopLayoutResult(
            new List<HexCoordinates>(),
            false,
            float.MinValue,
            settings.maxGenerationAttempts,
            "No valid pitstop layout was generated.",
            diagnostics.GetSummary());
    }

    public IReadOnlyList<HexCoordinates> Plan(
        HexGridData gridData,
        HexPathfinder pathfinder,
        HexCoordinates start,
        HexCoordinates goal,
        PitstopPlacementSettings settings,
        System.Random random,
        HexMapGenerationModifiers? generationModifiers = null,
        HexMapPlacementReservations reservations = null)
    {
        return GeneratePitstops(gridData, pathfinder, start, goal, settings, random, generationModifiers, reservations).Coordinates;
    }

    private static List<CandidateInfo> BuildCandidates(
        HexGridData gridData,
        HexPathfinder pathfinder,
        HexCoordinates start,
        HexCoordinates goal,
        PitstopPlacementSettings settings,
        HexMapPlacementReservations reservations,
        out PitstopCandidateDiagnostics diagnostics)
    {
        diagnostics = new PitstopCandidateDiagnostics();
        List<HexTileData> corridorTiles = pathfinder?.FindPath(start, goal) is IReadOnlyList<HexTileData> path
            ? new List<HexTileData>(path)
            : new List<HexTileData>();

        List<CandidateInfo> candidates = new();
        foreach (HexTileData tile in gridData.Tiles)
        {
            if (tile == null)
            {
                continue;
            }

            if (!tile.IsPassable)
            {
                diagnostics.CountImpassable();
                continue;
            }

            if (tile.Coordinates.Equals(start) || tile.Coordinates.Equals(goal))
            {
                diagnostics.CountEndpointTile();
                continue;
            }

            if (reservations != null && reservations.IsReserved(tile.Coordinates))
            {
                diagnostics.CountReserved();
                continue;
            }

            if (!settings.IsAllowedBiome(tile.Biome))
            {
                diagnostics.CountBiome();
                continue;
            }

            CandidateInfo candidate = BuildCandidateInfo(tile, gridData, corridorTiles, start, goal, settings);
            if (candidate.DistanceToStart <= settings.spawnGoalAdjacencyBuffer
                || candidate.DistanceToGoal <= settings.spawnGoalAdjacencyBuffer)
            {
                diagnostics.CountEndpointBuffer();
                continue;
            }

            if (candidate.OpenNeighborCount < settings.minimumOpenNeighbors)
            {
                diagnostics.CountOpenNeighbors();
                continue;
            }

            candidates.Add(candidate);
            diagnostics.CountAccepted();
        }

        return candidates;
    }

    private static CandidateInfo BuildCandidateInfo(
        HexTileData tile,
        HexGridData gridData,
        List<HexTileData> corridorTiles,
        HexCoordinates start,
        HexCoordinates goal,
        PitstopPlacementSettings settings)
    {
        float progress = CalculateProgress(tile.Coordinates, start, goal);
        float rowProgress = gridData.Rows <= 1 ? 0.5f : tile.Coordinates.Row / (float)(gridData.Rows - 1);
        float corridorRow = CalculateCorridorRow(progress, start, goal, corridorTiles);
        int corridorDistance = CalculateCorridorDistance(tile.Coordinates, corridorTiles, corridorRow);
        int edgeDistance = GetEdgeDistance(tile.Coordinates, gridData);

        return new CandidateInfo
        {
            Coordinates = tile.Coordinates,
            Progress = progress,
            RowProgress = rowProgress,
            CorridorRow = corridorRow,
            CorridorDistance = corridorDistance,
            DistanceToStart = tile.Coordinates.DistanceTo(start),
            DistanceToGoal = tile.Coordinates.DistanceTo(goal),
            OpenNeighborCount = CountUsableNeighbors(gridData, tile.Coordinates),
            EdgeDistance = edgeDistance,
            IsEdge = edgeDistance == 0,
            Lane = ResolveLane(rowProgress, settings),
            CorridorLane = ResolveLaneFromRow(corridorRow, gridData.Rows, settings)
        };
    }

    private static List<SlotRequest> BuildSlotRequests(
        int rows,
        int columns,
        PitstopPlacementSettings settings,
        HexMapGenerationModifiers generationModifiers)
    {
        int desiredCount = settings.GetDesiredCount(rows, columns, generationModifiers.ExtraPitstopCount);
        bool isSmallMap = rows * columns <= 25;
        List<SlotRequest> slots = new();

        if (isSmallMap)
        {
            if (desiredCount >= 1)
            {
                slots.Add(CreateSlot(SlotType.EarlyFlexible, settings.GetEarlyBand(rows, columns), true));
            }

            if (desiredCount >= 3)
            {
                slots.Add(CreateSlot(SlotType.Mid, settings.GetMidBand(rows, columns), false));
            }

            if (desiredCount >= 2)
            {
                slots.Add(CreateSlot(SlotType.Late, settings.GetLateBand(rows, columns), true));
            }

            AppendAdditionalSlots(slots, desiredCount - slots.Count, rows, columns, settings, generationModifiers.ExtraPitstopPlacementBand);
            return slots;
        }

        if (settings.UseSevenPitstopLayout(rows, columns))
        {
            slots.Add(CreateSlot(SlotType.EarlyTop, settings.GetEarlyBand(rows, columns), true));
            slots.Add(CreateSlot(SlotType.EarlyBottom, settings.GetEarlyBand(rows, columns), true));
            slots.Add(CreateSlot(SlotType.MidTop, settings.GetMidBand(rows, columns), false));
            slots.Add(CreateSlot(SlotType.MidMiddle, settings.GetMidBand(rows, columns), false));
            slots.Add(CreateSlot(SlotType.MidBottom, settings.GetMidBand(rows, columns), false));
            slots.Add(CreateSlot(SlotType.LateTop, settings.GetLateBand(rows, columns), true));
            slots.Add(CreateSlot(SlotType.LateBottom, settings.GetLateBand(rows, columns), true));

            AppendAdditionalSlots(slots, desiredCount - slots.Count, rows, columns, settings, generationModifiers.ExtraPitstopPlacementBand);
            return slots;
        }

        if (desiredCount >= 1)
        {
            slots.Add(CreateSlot(SlotType.EarlyTop, settings.GetEarlyBand(rows, columns), true));
        }

        if (desiredCount >= 2)
        {
            slots.Add(CreateSlot(SlotType.EarlyBottom, settings.GetEarlyBand(rows, columns), true));
        }

        if (desiredCount >= 3)
        {
            slots.Add(CreateSlot(SlotType.Mid, settings.GetMidBand(rows, columns), false));
        }

        if (desiredCount >= 4)
        {
            slots.Add(CreateSlot(SlotType.Late, settings.GetLateBand(rows, columns), true));
        }

        AppendAdditionalSlots(slots, desiredCount - slots.Count, rows, columns, settings, generationModifiers.ExtraPitstopPlacementBand);

        return slots;
    }

    private static SlotRequest CreateSlot(
        SlotType type,
        PitstopFloatRange progressRange,
        bool relaxEdgeAvoidance,
        PitstopFloatRange? allowedProgressRange = null)
    {
        return new SlotRequest
        {
            Type = type,
            ProgressRange = progressRange,
            AllowedProgressRange = allowedProgressRange ?? progressRange,
            RelaxEdgeAvoidance = relaxEdgeAvoidance
        };
    }

    private static void AppendAdditionalSlots(
        List<SlotRequest> slots,
        int extraSlotCount,
        int rows,
        int columns,
        PitstopPlacementSettings settings,
        HexBoonMapPlacementBand placementBand)
    {
        if (extraSlotCount <= 0)
        {
            return;
        }

        int insertionIndex = GetAdditionalSlotInsertionIndex(slots, placementBand);
        for (int extraIndex = 0; extraIndex < extraSlotCount; extraIndex++)
        {
            SlotRequest slot = CreateAdditionalSlot(rows, columns, settings, placementBand);
            int targetIndex = Mathf.Clamp(insertionIndex + extraIndex, 0, slots.Count);
            slots.Insert(targetIndex, slot);
        }
    }

    private static int GetAdditionalSlotInsertionIndex(List<SlotRequest> slots, HexBoonMapPlacementBand placementBand)
    {
        if (placementBand == HexBoonMapPlacementBand.Early)
        {
            return CountContiguousMatchingSlots(slots, IsEarlySlot);
        }

        if (placementBand == HexBoonMapPlacementBand.Late)
        {
            return slots.Count;
        }

        for (int index = 0; index < slots.Count; index++)
        {
            if (IsLateSlot(slots[index].Type))
            {
                return index;
            }
        }

        return slots.Count;
    }

    private static int CountContiguousMatchingSlots(List<SlotRequest> slots, Func<SlotType, bool> predicate)
    {
        int count = 0;
        while (count < slots.Count && predicate(slots[count].Type))
        {
            count++;
        }

        return count;
    }

    private static bool IsEarlySlot(SlotType type)
    {
        return type == SlotType.EarlyTop
            || type == SlotType.EarlyBottom
            || type == SlotType.EarlyFlexible;
    }

    private static bool IsLateSlot(SlotType type)
    {
        return type == SlotType.Late
            || type == SlotType.LateTop
            || type == SlotType.LateBottom;
    }

    private static SlotRequest CreateAdditionalSlot(
        int rows,
        int columns,
        PitstopPlacementSettings settings,
        HexBoonMapPlacementBand placementBand)
    {
        SlotType type = placementBand switch
        {
            HexBoonMapPlacementBand.Early => SlotType.EarlyFlexible,
            HexBoonMapPlacementBand.Late => SlotType.Late,
            _ => SlotType.Mid
        };

        if (type == SlotType.Mid && settings.UseSevenPitstopLayout(rows, columns))
        {
            PitstopFloatRange supportPreferredRange = settings.GetLateBand(rows, columns);
            PitstopFloatRange supportAllowedRange = settings.GetAdditionalPitstopAllowedBand(rows, columns, HexBoonMapPlacementBand.Late);
            return CreateSlot(SlotType.Support, supportPreferredRange, false, supportAllowedRange);
        }

        PitstopFloatRange progressRange = placementBand switch
        {
            HexBoonMapPlacementBand.Early => settings.GetEarlyBand(rows, columns),
            HexBoonMapPlacementBand.Late => settings.GetLateBand(rows, columns),
            _ => settings.GetMidBand(rows, columns)
        };

        PitstopFloatRange allowedProgressRange = settings.GetAdditionalPitstopAllowedBand(rows, columns, placementBand);
        return CreateSlot(type, progressRange, type != SlotType.Mid, allowedProgressRange);
    }
}
