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

public sealed class PitstopPlacementPlanner
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

    private static bool TryBuildLayout(
        List<CandidateInfo> candidates,
        List<SlotRequest> slots,
        HexGridData gridData,
        PitstopPlacementSettings settings,
        System.Random random,
        out PlacedCandidate[] layout)
    {
        layout = new PlacedCandidate[slots.Count];
        int spacing = settings.GetMinimumSpacing(gridData.Rows, gridData.Columns);
        List<PlacedCandidate> placed = new(slots.Count);

        for (int slotIndex = 0; slotIndex < slots.Count; slotIndex++)
        {
            SlotRequest slot = slots[slotIndex];
            List<(CandidateInfo candidate, float score)> rankedCandidates = BuildRankedCandidates(
                candidates,
                placed,
                slot,
                spacing,
                settings,
                gridData,
                random);

            if (rankedCandidates.Count == 0)
            {
                layout = null;
                return false;
            }

            (CandidateInfo chosenCandidate, float chosenScore) = PickCandidate(rankedCandidates, settings, random);
            PlacedCandidate placedCandidate = new()
            {
                Slot = slot,
                Candidate = chosenCandidate,
                Score = chosenScore
            };

            placed.Add(placedCandidate);
            layout[slotIndex] = placedCandidate;
        }

        return true;
    }

    private static List<(CandidateInfo candidate, float score)> BuildRankedCandidates(
        List<CandidateInfo> candidates,
        List<PlacedCandidate> placed,
        SlotRequest slot,
        int requiredSpacing,
        PitstopPlacementSettings settings,
        HexGridData gridData,
        System.Random random)
    {
        List<(CandidateInfo candidate, float score)> rankedCandidates = new();
        for (int index = 0; index < candidates.Count; index++)
        {
            CandidateInfo candidate = candidates[index];
            if (!MatchesSlot(candidate, slot, settings))
            {
                continue;
            }

            if (!MeetsSpacing(candidate.Coordinates, placed, requiredSpacing))
            {
                continue;
            }

            float score = ScoreCandidate(candidate, slot, placed, gridData, settings, random);
            rankedCandidates.Add((candidate, score));
        }

        rankedCandidates.Sort((left, right) => right.score.CompareTo(left.score));
        return rankedCandidates;
    }

    private static bool MatchesSlot(CandidateInfo candidate, SlotRequest slot, PitstopPlacementSettings settings)
    {
        if (!slot.AllowedProgressRange.Contains(candidate.Progress))
        {
            return false;
        }

        return slot.Type switch
        {
            SlotType.EarlyTop => candidate.Lane == PitstopLane.Top,
            SlotType.EarlyBottom => candidate.Lane == PitstopLane.Bottom,
            SlotType.EarlyFlexible => true,
            SlotType.Mid => true,
            SlotType.MidTop => candidate.Lane == PitstopLane.Top,
            SlotType.MidMiddle => candidate.Lane == PitstopLane.Middle,
            SlotType.MidBottom => candidate.Lane == PitstopLane.Bottom,
            SlotType.Support => true,
            SlotType.Late => true,
            SlotType.LateTop => candidate.Lane == PitstopLane.Top,
            SlotType.LateBottom => candidate.Lane == PitstopLane.Bottom,
            _ => false
        };
    }

    private static bool MeetsSpacing(HexCoordinates coordinates, List<PlacedCandidate> placed, int requiredSpacing)
    {
        for (int index = 0; index < placed.Count; index++)
        {
            if (coordinates.DistanceTo(placed[index].Candidate.Coordinates) < requiredSpacing)
            {
                return false;
            }
        }

        return true;
    }

    private static (CandidateInfo candidate, float score) PickCandidate(
        List<(CandidateInfo candidate, float score)> rankedCandidates,
        PitstopPlacementSettings settings,
        System.Random random)
    {
        int poolSize = Mathf.Min(settings.candidatePoolSize, rankedCandidates.Count);
        float totalWeight = 0f;
        for (int index = 0; index < poolSize; index++)
        {
            totalWeight += Mathf.Max(0.001f, rankedCandidates[index].score + 3f);
        }

        float roll = (float)random.NextDouble() * totalWeight;
        for (int index = 0; index < poolSize; index++)
        {
            float weight = Mathf.Max(0.001f, rankedCandidates[index].score + 3f);
            if (roll <= weight)
            {
                return rankedCandidates[index];
            }

            roll -= weight;
        }

        return rankedCandidates[0];
    }

    private static float ScoreCandidate(
        CandidateInfo candidate,
        SlotRequest slot,
        List<PlacedCandidate> placed,
        HexGridData gridData,
        PitstopPlacementSettings settings,
        System.Random random)
    {
        float score = 0f;
        score += ScoreRangeCloseness(candidate.Progress, slot.ProgressRange);
        score += ScoreDistanceTargets(candidate, slot, settings) * settings.distanceTargetWeight;
        score += ScoreLanePreference(candidate, slot, placed, settings) * settings.lanePreferenceWeight;
        score += ScoreLaneDiversity(candidate, placed) * settings.laneDiversityWeight;
        score += ScoreCorridorOffset(candidate, settings) * settings.corridorOffsetWeight;
        score += ScoreOpenNeighbors(candidate, settings) * settings.openNeighborWeight;

        float edgeScore = ScoreEdgeDistance(candidate, gridData);
        if (slot.RelaxEdgeAvoidance)
        {
            edgeScore *= 0.5f;
        }

        score += edgeScore * settings.edgeAvoidanceWeight;
        score += (float)random.NextDouble() * settings.randomJitter;
        return score;
    }

    private static float ScoreDistanceTargets(CandidateInfo candidate, SlotRequest slot, PitstopPlacementSettings settings)
    {
        return slot.Type switch
        {
            SlotType.EarlyTop => ScoreIntRangeCloseness(candidate.DistanceToStart, settings.earlyDistanceFromSpawn),
            SlotType.EarlyBottom => ScoreIntRangeCloseness(candidate.DistanceToStart, settings.earlyDistanceFromSpawn),
            SlotType.EarlyFlexible => ScoreIntRangeCloseness(candidate.DistanceToStart, settings.earlyDistanceFromSpawn),
            SlotType.Late => ScoreIntRangeCloseness(candidate.DistanceToGoal, settings.lateDistanceFromGoal),
            SlotType.LateTop => ScoreIntRangeCloseness(candidate.DistanceToGoal, settings.lateDistanceFromGoal),
            SlotType.LateBottom => ScoreIntRangeCloseness(candidate.DistanceToGoal, settings.lateDistanceFromGoal),
            SlotType.Support => 0.5f,
            _ => 0.5f
        };
    }

    private static float ScoreLanePreference(
        CandidateInfo candidate,
        SlotRequest slot,
        List<PlacedCandidate> placed,
        PitstopPlacementSettings settings)
    {
        return slot.Type switch
        {
            SlotType.EarlyTop => ScoreLaneCloseness(candidate.RowProgress, settings.topLaneCenter),
            SlotType.EarlyBottom => ScoreLaneCloseness(candidate.RowProgress, settings.bottomLaneCenter),
            SlotType.EarlyFlexible => candidate.Lane == PitstopLane.Middle ? 0.2f : 1f,
            SlotType.Mid => ScoreMidLane(candidate, placed, settings),
            SlotType.MidTop => ScoreExplicitMidLane(candidate, placed, settings.topLaneCenter),
            SlotType.MidMiddle => ScoreExplicitMidLane(candidate, placed, settings.middleLaneCenter),
            SlotType.MidBottom => ScoreExplicitMidLane(candidate, placed, settings.bottomLaneCenter),
            SlotType.Support => ScoreSupportLane(candidate, placed, settings),
            SlotType.Late => ScoreLateLane(candidate, placed, settings),
            SlotType.LateTop => ScoreExplicitLateLane(candidate, placed, settings.topLaneCenter),
            SlotType.LateBottom => ScoreExplicitLateLane(candidate, placed, settings.bottomLaneCenter),
            _ => 0f
        };
    }

    private static float ScoreSupportLane(CandidateInfo candidate, List<PlacedCandidate> placed, PitstopPlacementSettings settings)
    {
        float score = ScoreLaneCloseness(candidate.RowProgress, settings.middleLaneCenter);
        if (candidate.Lane == PitstopLane.Top || candidate.Lane == PitstopLane.Bottom)
        {
            score -= 0.2f;
        }

        if (CountLaneMatches(candidate.Lane, placed) >= 2)
        {
            score -= 0.15f;
        }

        return score;
    }

    private static float ScoreExplicitMidLane(CandidateInfo candidate, List<PlacedCandidate> placed, float laneCenter)
    {
        float score = ScoreLaneCloseness(candidate.RowProgress, laneCenter);
        if (candidate.Lane == candidate.CorridorLane)
        {
            score -= 0.3f;
        }

        if (CountLaneMatches(candidate.Lane, placed) >= 2)
        {
            score -= 0.25f;
        }

        return score;
    }

    private static float ScoreMidLane(CandidateInfo candidate, List<PlacedCandidate> placed, PitstopPlacementSettings settings)
    {
        float score = ScoreLaneCloseness(candidate.RowProgress, settings.middleLaneCenter);
        if (candidate.Lane == candidate.CorridorLane)
        {
            score -= 0.45f;
        }

        if (placed.Count > 0 && CountLaneMatches(candidate.Lane, placed) >= 2)
        {
            score -= 0.35f;
        }

        return score;
    }

    private static float ScoreLateLane(CandidateInfo candidate, List<PlacedCandidate> placed, PitstopPlacementSettings settings)
    {
        float score = candidate.Lane == candidate.CorridorLane ? 0.15f : 1f;
        if (placed.Count > 0 && placed[placed.Count - 1].Candidate.Lane == candidate.Lane)
        {
            score -= 0.35f;
        }

        if (CountLaneMatches(candidate.Lane, placed) >= 2)
        {
            score -= 0.25f;
        }

        score += ScoreLaneCloseness(candidate.RowProgress, settings.middleLaneCenter) * 0.2f;
        return score;
    }

    private static float ScoreExplicitLateLane(CandidateInfo candidate, List<PlacedCandidate> placed, float laneCenter)
    {
        float score = ScoreLaneCloseness(candidate.RowProgress, laneCenter);
        if (candidate.Lane == candidate.CorridorLane)
        {
            score -= 0.25f;
        }

        if (placed.Count > 0 && placed[placed.Count - 1].Candidate.Lane == candidate.Lane)
        {
            score -= 0.2f;
        }

        return score;
    }

    private static int CountLaneMatches(PitstopLane lane, List<PlacedCandidate> placed)
    {
        int count = 0;
        for (int index = 0; index < placed.Count; index++)
        {
            if (placed[index].Candidate.Lane == lane)
            {
                count++;
            }
        }

        return count;
    }

    private static float ScoreLaneDiversity(CandidateInfo candidate, List<PlacedCandidate> placed)
    {
        if (placed.Count == 0)
        {
            return 0.5f;
        }

        for (int index = 0; index < placed.Count; index++)
        {
            if (placed[index].Candidate.Lane == candidate.Lane)
            {
                return 0f;
            }
        }

        return 1f;
    }

    private static float ScoreCorridorOffset(CandidateInfo candidate, PitstopPlacementSettings settings)
    {
        float maxDelta = Mathf.Max(1f, settings.preferredCorridorOffset + 2f);
        return 1f - Mathf.Clamp01(Mathf.Abs(candidate.CorridorDistance - settings.preferredCorridorOffset) / maxDelta);
    }

    private static float ScoreOpenNeighbors(CandidateInfo candidate, PitstopPlacementSettings settings)
    {
        if (candidate.OpenNeighborCount <= settings.minimumOpenNeighbors)
        {
            return 0f;
        }

        return Mathf.Clamp01((candidate.OpenNeighborCount - settings.minimumOpenNeighbors) / 3f);
    }

    private static float ScoreEdgeDistance(CandidateInfo candidate, HexGridData gridData)
    {
        int maxEdgeDistance = Mathf.Max(1, Mathf.Min(gridData.Rows, gridData.Columns) / 2);
        return Mathf.Clamp01(candidate.EdgeDistance / (float)maxEdgeDistance);
    }

    private static float ScoreRangeCloseness(float value, PitstopFloatRange range)
    {
        float halfWidth = Mathf.Max(0.0001f, (range.max - range.min) * 0.5f);
        return 1f - Mathf.Clamp01(Mathf.Abs(value - range.Center) / halfWidth);
    }

    private static float ScoreIntRangeCloseness(int value, PitstopIntRange range)
    {
        float halfWidth = Mathf.Max(0.5f, (range.max - range.min) * 0.5f);
        return 1f - Mathf.Clamp01(Mathf.Abs(value - range.Center) / halfWidth);
    }

    private static float ScoreLaneCloseness(float rowProgress, float laneCenter)
    {
        return 1f - Mathf.Clamp01(Mathf.Abs(rowProgress - laneCenter) / 0.35f);
    }

    private static (bool isValid, float score, string summary) EvaluateLayout(
        PlacedCandidate[] layout,
        HexGridData gridData,
        PitstopPlacementSettings settings,
        HexCoordinates start,
        HexCoordinates goal)
    {
        if (layout == null || layout.Length == 0)
        {
            return (false, float.MinValue, "No pitstops were placed.");
        }

        int minimumSpacing = settings.GetMinimumSpacing(gridData.Rows, gridData.Columns);
        HashSet<PitstopLane> lanes = new();
        int minRow = int.MaxValue;
        int maxRow = int.MinValue;
        float corridorDistanceSum = 0f;
        float score = 0f;

        for (int index = 0; index < layout.Length; index++)
        {
            CandidateInfo candidate = layout[index].Candidate;
            score += layout[index].Score;
            lanes.Add(candidate.Lane);
            minRow = Mathf.Min(minRow, candidate.Coordinates.Row);
            maxRow = Mathf.Max(maxRow, candidate.Coordinates.Row);
            corridorDistanceSum += candidate.CorridorDistance;

            if (candidate.DistanceToStart <= settings.spawnGoalAdjacencyBuffer
                || candidate.DistanceToGoal <= settings.spawnGoalAdjacencyBuffer)
            {
                return (false, score - 10f, "A pitstop was placed adjacent to the spawn or goal.");
            }

            if (candidate.OpenNeighborCount < settings.minimumOpenNeighbors)
            {
                return (false, score - 10f, "A pitstop did not have enough usable neighboring tiles.");
            }

            for (int compareIndex = index + 1; compareIndex < layout.Length; compareIndex++)
            {
                if (candidate.Coordinates.DistanceTo(layout[compareIndex].Candidate.Coordinates) < minimumSpacing)
                {
                    return (false, score - 10f, "Two pitstops were placed too close together.");
                }
            }
        }

        if (lanes.Count < settings.GetMinimumUniqueLanes(gridData.Rows, gridData.Columns))
        {
            return (false, score - 8f, "The layout did not create enough lane diversity.");
        }

        if ((maxRow - minRow) < settings.GetMinimumRowSpan(gridData.Rows, gridData.Columns))
        {
            return (false, score - 8f, "The layout was too compressed into a straight corridor.");
        }

        float averageCorridorDistance = corridorDistanceSum / layout.Length;
        if (averageCorridorDistance < settings.GetMinimumAverageCorridorOffset(gridData.Rows, gridData.Columns))
        {
            return (false, score - 8f, "The layout hugged the straight corridor too closely.");
        }

        if (!ValidateSlotIntention(layout, settings))
        {
            return (false, score - 8f, "The layout missed one of the strategic band-and-lane goals.");
        }

        return (true, score, $"Generated a valid pitstop layout with {layout.Length} anchors.");
    }

    private static bool ValidateSlotIntention(PlacedCandidate[] layout, PitstopPlacementSettings settings)
    {
        bool hasEarlyTop = false;
        bool hasEarlyBottom = false;
        bool hasMidTop = false;
        bool hasMidMiddle = false;
        bool hasMidBottom = false;
        bool hasLateTop = false;
        bool hasLateBottom = false;
        bool hasSpawnSupport = false;
        bool hasGoalSupport = false;

        for (int index = 0; index < layout.Length; index++)
        {
            SlotType type = layout[index].Slot.Type;
            if (type == SlotType.EarlyTop)
            {
                hasEarlyTop = layout[index].Candidate.Lane == PitstopLane.Top;
                hasSpawnSupport |= settings.earlyDistanceFromSpawn.Contains(layout[index].Candidate.DistanceToStart);
            }
            else if (type == SlotType.EarlyBottom)
            {
                hasEarlyBottom = layout[index].Candidate.Lane == PitstopLane.Bottom;
                hasSpawnSupport |= settings.earlyDistanceFromSpawn.Contains(layout[index].Candidate.DistanceToStart);
            }
            else if (type == SlotType.EarlyFlexible)
            {
                hasSpawnSupport |= settings.earlyDistanceFromSpawn.Contains(layout[index].Candidate.DistanceToStart);
            }
            else if (type == SlotType.Late)
            {
                hasGoalSupport |= settings.lateDistanceFromGoal.Contains(layout[index].Candidate.DistanceToGoal);
            }
            else if (type == SlotType.MidTop)
            {
                hasMidTop = layout[index].Candidate.Lane == PitstopLane.Top;
            }
            else if (type == SlotType.MidMiddle)
            {
                hasMidMiddle = layout[index].Candidate.Lane == PitstopLane.Middle;
            }
            else if (type == SlotType.MidBottom)
            {
                hasMidBottom = layout[index].Candidate.Lane == PitstopLane.Bottom;
            }
            else if (type == SlotType.LateTop)
            {
                hasLateTop = layout[index].Candidate.Lane == PitstopLane.Top;
                hasGoalSupport |= settings.lateDistanceFromGoal.Contains(layout[index].Candidate.DistanceToGoal);
            }
            else if (type == SlotType.LateBottom)
            {
                hasLateBottom = layout[index].Candidate.Lane == PitstopLane.Bottom;
                hasGoalSupport |= settings.lateDistanceFromGoal.Contains(layout[index].Candidate.DistanceToGoal);
            }
        }

        bool sevenAnchorLayout = layout.Length >= 7;
        if (sevenAnchorLayout)
        {
            return hasEarlyTop
                && hasEarlyBottom
                && hasMidTop
                && hasMidMiddle
                && hasMidBottom
                && hasLateTop
                && hasLateBottom
                && hasSpawnSupport
                && hasGoalSupport;
        }

        bool largeMapLayout = layout.Length >= 4;
        if (largeMapLayout && (!hasEarlyTop || !hasEarlyBottom))
        {
            return false;
        }

        return hasSpawnSupport && hasGoalSupport;
    }

    private static IReadOnlyList<HexCoordinates> ExtractCoordinates(
        IReadOnlyList<PlacedCandidate> layout,
        HexCoordinates start,
        HexCoordinates goal)
    {
        List<PlacedCandidate> ordered = new(layout.Count);
        for (int index = 0; index < layout.Count; index++)
        {
            ordered.Add(layout[index]);
        }

        ordered.Sort((left, right) =>
        {
            float leftProgress = CalculateProgress(left.Candidate.Coordinates, start, goal);
            float rightProgress = CalculateProgress(right.Candidate.Coordinates, start, goal);
            int progressCompare = leftProgress.CompareTo(rightProgress);
            return progressCompare != 0 ? progressCompare : left.Candidate.Coordinates.Row.CompareTo(right.Candidate.Coordinates.Row);
        });

        List<HexCoordinates> coordinates = new(ordered.Count);
        for (int index = 0; index < ordered.Count; index++)
        {
            coordinates.Add(ordered[index].Candidate.Coordinates);
        }

        return coordinates;
    }

    private static float CalculateProgress(HexCoordinates coordinates, HexCoordinates start, HexCoordinates goal)
    {
        if (start.Column == goal.Column)
        {
            return 0.5f;
        }

        return Mathf.Clamp01(Mathf.InverseLerp(start.Column, goal.Column, coordinates.Column));
    }

    private static float CalculateCorridorRow(float progress, HexCoordinates start, HexCoordinates goal, List<HexTileData> corridorTiles)
    {
        if (corridorTiles == null || corridorTiles.Count == 0)
        {
            return Mathf.Lerp(start.Row, goal.Row, progress);
        }

        float bestProgressDelta = float.MaxValue;
        float corridorRow = corridorTiles[0].Coordinates.Row;
        for (int index = 0; index < corridorTiles.Count; index++)
        {
            HexCoordinates coordinates = corridorTiles[index].Coordinates;
            float tileProgress = CalculateProgress(coordinates, start, goal);
            float progressDelta = Mathf.Abs(tileProgress - progress);
            if (progressDelta < bestProgressDelta)
            {
                bestProgressDelta = progressDelta;
                corridorRow = coordinates.Row;
            }
        }

        return corridorRow;
    }

    private static int CalculateCorridorDistance(HexCoordinates coordinates, List<HexTileData> corridorTiles, float corridorRow)
    {
        if (corridorTiles == null || corridorTiles.Count == 0)
        {
            HexCoordinates corridorCoordinates = new(Mathf.RoundToInt(corridorRow), coordinates.Column);
            return coordinates.DistanceTo(corridorCoordinates);
        }

        int minimumDistance = int.MaxValue;
        for (int index = 0; index < corridorTiles.Count; index++)
        {
            int distance = coordinates.DistanceTo(corridorTiles[index].Coordinates);
            if (distance < minimumDistance)
            {
                minimumDistance = distance;
            }
        }

        return minimumDistance == int.MaxValue ? 0 : minimumDistance;
    }

    private static int CountUsableNeighbors(HexGridData gridData, HexCoordinates coordinates)
    {
        int usableNeighbors = 0;
        foreach (HexTileData neighbor in gridData.GetNeighbors(coordinates))
        {
            if (neighbor.IsPassable)
            {
                usableNeighbors++;
            }
        }

        return usableNeighbors;
    }

    private static int GetEdgeDistance(HexCoordinates coordinates, HexGridData gridData)
    {
        int top = coordinates.Row;
        int bottom = gridData.Rows - 1 - coordinates.Row;
        int left = coordinates.Column;
        int right = gridData.Columns - 1 - coordinates.Column;
        return Mathf.Min(Mathf.Min(top, bottom), Mathf.Min(left, right));
    }

    private static PitstopLane ResolveLane(float rowProgress, PitstopPlacementSettings settings)
    {
        float topBoundary = (settings.topLaneCenter + settings.middleLaneCenter) * 0.5f;
        float bottomBoundary = (settings.middleLaneCenter + settings.bottomLaneCenter) * 0.5f;

        if (rowProgress <= topBoundary)
        {
            return PitstopLane.Top;
        }

        if (rowProgress >= bottomBoundary)
        {
            return PitstopLane.Bottom;
        }

        return PitstopLane.Middle;
    }

    private static PitstopLane ResolveLaneFromRow(float row, int rows, PitstopPlacementSettings settings)
    {
        float rowProgress = rows <= 1 ? 0.5f : row / Mathf.Max(1, rows - 1f);
        return ResolveLane(rowProgress, settings);
    }
}
