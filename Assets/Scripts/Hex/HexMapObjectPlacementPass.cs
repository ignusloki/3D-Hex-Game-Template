using System.Collections.Generic;
using UnityEngine;

public sealed class HexMapObjectPlacementPass
{
    private readonly PitstopPlacementPlanner pitstopPlacementPlanner = new();

    private sealed class MapObjectPlacementDiagnostics
    {
        public int Requested;
        public int Placed;
        public int RejectedReserved;
        public int RejectedBiome;
        public int RejectedEndpointDistance;
        public int RejectedEdgePadding;
        public int RejectedBand;
        public int RejectedReservedDistance;

        public string GetSummary()
        {
            return $"Map object summary: placed={Placed}/{Requested}, reserved={RejectedReserved}, biome={RejectedBiome}, endpointDistance={RejectedEndpointDistance}, edgePadding={RejectedEdgePadding}, band={RejectedBand}, reservedDistance={RejectedReservedDistance}.";
        }
    }

    public HexMapObjectPlacementPlanResult PlanPlacements(
        HexGridData gridData,
        HexPathfinder pathfinder,
        HexCoordinates start,
        HexCoordinates goal,
        PitstopPlacementSettings settings,
        HexMapGenerationModifiers generationModifiers,
        HexMapPlacementReservations reservations,
        System.Random random)
    {
        settings ??= new PitstopPlacementSettings();
        settings.Validate();
        random ??= new System.Random();
        generationModifiers = generationModifiers.HasAny ? generationModifiers : HexMapGenerationModifiers.None;

        HexMapPlacementReservations workingReservations = reservations?.Clone() ?? new HexMapPlacementReservations();
        PitstopLayoutResult layoutResult = pitstopPlacementPlanner.GeneratePitstops(
            gridData,
            pathfinder,
            start,
            goal,
            settings,
            random,
            generationModifiers,
            workingReservations);

        HexMapObjectPlacementCollection placements = new();
        IReadOnlyList<HexCoordinates> coordinates = layoutResult.Coordinates;
        List<PitstopKind> kindSequence = BuildKindSequence(coordinates.Count, random);
        for (int index = 0; index < coordinates.Count; index++)
        {
            PitstopKind kind = kindSequence[index];
            HexCoordinates coordinatesForPitstop = coordinates[index];
            placements.Add(new HexMapObjectPlacement(
                HexMapObjectType.Pitstop,
                coordinatesForPitstop,
                "pitstop",
                kind.ToString(),
                kind.ToString()));
            workingReservations.Reserve(coordinatesForPitstop, HexMapPlacementReservationLayer.Pitstop, "Pitstop");
        }

        MapObjectPlacementDiagnostics mapObjectDiagnostics = new();
        PlaceRequestedMapObjects(
            placements,
            gridData,
            start,
            goal,
            generationModifiers.MapObjectPlacementRequests,
            workingReservations,
            random,
            mapObjectDiagnostics);

        bool allRequestedObjectsPlaced = mapObjectDiagnostics.Requested == 0 || mapObjectDiagnostics.Placed >= mapObjectDiagnostics.Requested;
        float objectCompletionRatio = mapObjectDiagnostics.Requested <= 0
            ? 1f
            : mapObjectDiagnostics.Placed / (float)mapObjectDiagnostics.Requested;
        float combinedScore = layoutResult.Score + objectCompletionRatio;
        bool isValid = layoutResult.IsValid && allRequestedObjectsPlaced;
        string summary = layoutResult.Summary;
        if (mapObjectDiagnostics.Requested > 0)
        {
            summary += $" Requested map objects: {mapObjectDiagnostics.Placed}/{mapObjectDiagnostics.Requested} placed.";
        }

        string diagnosticsSummary = layoutResult.DiagnosticsSummary;
        if (mapObjectDiagnostics.Requested > 0)
        {
            diagnosticsSummary = string.IsNullOrWhiteSpace(diagnosticsSummary)
                ? mapObjectDiagnostics.GetSummary()
                : $"{diagnosticsSummary} {mapObjectDiagnostics.GetSummary()}";
        }

        return new HexMapObjectPlacementPlanResult(
            placements,
            workingReservations,
            isValid,
            combinedScore,
            layoutResult.AttemptsUsed,
            summary,
            diagnosticsSummary);
    }

    public HexMapObjectPlacementPlanResult PlanPitstops(
        HexGridData gridData,
        HexPathfinder pathfinder,
        HexCoordinates start,
        HexCoordinates goal,
        PitstopPlacementSettings settings,
        HexMapGenerationModifiers generationModifiers,
        HexMapPlacementReservations reservations,
        System.Random random)
    {
        return PlanPlacements(gridData, pathfinder, start, goal, settings, generationModifiers, reservations, random);
    }

    private static List<PitstopKind> BuildKindSequence(int count, System.Random random)
    {
        List<PitstopKind> sequence = new(count);
        PitstopKind[] availableKinds =
        {
            PitstopKind.Mill,
            PitstopKind.WallTower,
            PitstopKind.Mansion
        };

        for (int index = 0; index < count; index++)
        {
            sequence.Add(availableKinds[index % availableKinds.Length]);
        }

        for (int index = sequence.Count - 1; index > 0; index--)
        {
            int swapIndex = random.Next(index + 1);
            (sequence[index], sequence[swapIndex]) = (sequence[swapIndex], sequence[index]);
        }

        return sequence;
    }

    private static void PlaceRequestedMapObjects(
        HexMapObjectPlacementCollection placements,
        HexGridData gridData,
        HexCoordinates start,
        HexCoordinates goal,
        HexMapObjectPlacementRequest[] requests,
        HexMapPlacementReservations reservations,
        System.Random random,
        MapObjectPlacementDiagnostics diagnostics)
    {
        if (gridData == null || requests == null || requests.Length == 0)
        {
            return;
        }

        for (int requestIndex = 0; requestIndex < requests.Length; requestIndex++)
        {
            HexMapObjectPlacementRequest request = requests[requestIndex];
            if (request == null || !request.IsUsable)
            {
                continue;
            }

            request.definition.Validate();
            diagnostics.Requested += request.count;

            for (int placementIndex = 0; placementIndex < request.count; placementIndex++)
            {
                if (TryPlaceMapObject(gridData, start, goal, request, reservations, random, diagnostics, out HexMapObjectPlacement placement))
                {
                    placements.Add(placement);
                    diagnostics.Placed++;

                    if (request.definition.blocksPlacementOfOtherObjects)
                    {
                        reservations.Reserve(placement.Coordinates, HexMapPlacementReservationLayer.MapObject, request.definition.id);
                    }
                }
            }
        }
    }

    private static bool TryPlaceMapObject(
        HexGridData gridData,
        HexCoordinates start,
        HexCoordinates goal,
        HexMapObjectPlacementRequest request,
        HexMapPlacementReservations reservations,
        System.Random random,
        MapObjectPlacementDiagnostics diagnostics,
        out HexMapObjectPlacement placement)
    {
        placement = default;
        HexMapObjectDefinition definition = request.definition;
        if (definition == null)
        {
            return false;
        }

        HexTileData bestTile = null;
        float bestScore = float.MinValue;
        bool anyReserved = false;
        bool anyBiomeRejected = false;
        bool anyEndpointRejected = false;
        bool anyEdgeRejected = false;
        bool anyBandRejected = false;
        bool anyReservedDistanceRejected = false;

        foreach (HexTileData tile in gridData.Tiles)
        {
            if (tile == null || !tile.IsPassable)
            {
                continue;
            }

            HexCoordinates coordinates = tile.Coordinates;
            if (reservations != null && reservations.IsReserved(coordinates))
            {
                anyReserved = true;
                continue;
            }

            if (!definition.IsBaseBiomeAllowed(tile.Biome))
            {
                anyBiomeRejected = true;
                continue;
            }

            if (coordinates.DistanceTo(start) < definition.minDistanceFromStart
                || coordinates.DistanceTo(goal) < definition.minDistanceFromGoal)
            {
                anyEndpointRejected = true;
                continue;
            }

            if (GetEdgeDistance(coordinates, gridData.Rows, gridData.Columns) < definition.edgePadding)
            {
                anyEdgeRejected = true;
                continue;
            }

            if (!MatchesPlacementBand(coordinates, start, goal, request.placementBand))
            {
                anyBandRejected = true;
                continue;
            }

            if (definition.minDistanceFromReservedTiles > 0
                && GetMinimumReservedDistance(coordinates, reservations) < definition.minDistanceFromReservedTiles)
            {
                anyReservedDistanceRejected = true;
                continue;
            }

            float score = ScoreMapObjectPlacement(coordinates, start, goal, request.placementBand, gridData.Rows, gridData.Columns, random);
            if (score > bestScore)
            {
                bestScore = score;
                bestTile = tile;
            }
        }

        diagnostics.RejectedReserved += anyReserved ? 1 : 0;
        diagnostics.RejectedBiome += anyBiomeRejected ? 1 : 0;
        diagnostics.RejectedEndpointDistance += anyEndpointRejected ? 1 : 0;
        diagnostics.RejectedEdgePadding += anyEdgeRejected ? 1 : 0;
        diagnostics.RejectedBand += anyBandRejected ? 1 : 0;
        diagnostics.RejectedReservedDistance += anyReservedDistanceRejected ? 1 : 0;

        if (bestTile == null)
        {
            return false;
        }

        placement = new HexMapObjectPlacement(
            definition.objectType,
            bestTile.Coordinates,
            definition.id,
            definition.GetResolvedDisplayName(),
            definition.id,
            definition);
        return true;
    }

    private static bool MatchesPlacementBand(
        HexCoordinates coordinates,
        HexCoordinates start,
        HexCoordinates goal,
        HexBoonMapPlacementBand placementBand)
    {
        if (placementBand == HexBoonMapPlacementBand.Default)
        {
            return true;
        }

        float progress = GetProgress(coordinates, start, goal);
        return placementBand switch
        {
            HexBoonMapPlacementBand.Early => progress >= 0.12f && progress <= 0.42f,
            HexBoonMapPlacementBand.Mid => progress >= 0.32f && progress <= 0.68f,
            HexBoonMapPlacementBand.Late => progress >= 0.58f && progress <= 0.9f,
            _ => true
        };
    }

    private static float ScoreMapObjectPlacement(
        HexCoordinates coordinates,
        HexCoordinates start,
        HexCoordinates goal,
        HexBoonMapPlacementBand placementBand,
        int rows,
        int columns,
        System.Random random)
    {
        float progress = GetProgress(coordinates, start, goal);
        float targetProgress = placementBand switch
        {
            HexBoonMapPlacementBand.Early => 0.25f,
            HexBoonMapPlacementBand.Mid => 0.5f,
            HexBoonMapPlacementBand.Late => 0.75f,
            _ => 0.5f
        };

        float bandScore = placementBand == HexBoonMapPlacementBand.Default
            ? 1f
            : 1f - Mathf.Clamp01(Mathf.Abs(progress - targetProgress) / 0.3f);

        float edgeDistance = GetEdgeDistance(coordinates, rows, columns);
        float edgeScore = Mathf.Clamp01(edgeDistance / 3f);
        float jitter = random != null ? (float)random.NextDouble() * 0.1f : 0f;
        return bandScore + (edgeScore * 0.25f) + jitter;
    }

    private static float GetProgress(HexCoordinates coordinates, HexCoordinates start, HexCoordinates goal)
    {
        int totalDistance = Mathf.Max(1, start.DistanceTo(goal));
        int remainingDistance = coordinates.DistanceTo(goal);
        return Mathf.Clamp01(1f - (remainingDistance / (float)totalDistance));
    }

    private static int GetEdgeDistance(HexCoordinates coordinates, int rows, int columns)
    {
        int top = coordinates.Row;
        int bottom = Mathf.Max(0, rows - 1 - coordinates.Row);
        int left = coordinates.Column;
        int right = Mathf.Max(0, columns - 1 - coordinates.Column);
        return Mathf.Min(Mathf.Min(top, bottom), Mathf.Min(left, right));
    }

    private static int GetMinimumReservedDistance(HexCoordinates coordinates, HexMapPlacementReservations reservations)
    {
        if (reservations == null)
        {
            return int.MaxValue;
        }

        int bestDistance = int.MaxValue;
        foreach (HexMapPlacementReservation reservation in reservations.All)
        {
            int distance = coordinates.DistanceTo(reservation.Coordinates);
            if (distance < bestDistance)
            {
                bestDistance = distance;
            }
        }

        return bestDistance;
    }
}
