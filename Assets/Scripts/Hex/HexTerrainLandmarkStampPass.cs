using System.Collections.Generic;
using UnityEngine;

public sealed class HexTerrainLandmarkStampPass
{
    private readonly struct CellPlacement
    {
        public CellPlacement(HexCoordinates coordinates, Biome biome)
        {
            Coordinates = coordinates;
            Biome = biome;
        }

        public HexCoordinates Coordinates { get; }
        public Biome Biome { get; }
    }

    private readonly struct FootprintPlacement
    {
        public FootprintPlacement(HexCoordinates anchor, IReadOnlyList<CellPlacement> cells, float score)
        {
            Anchor = anchor;
            Cells = cells;
            Score = score;
        }

        public HexCoordinates Anchor { get; }
        public IReadOnlyList<CellPlacement> Cells { get; }
        public float Score { get; }
    }

    public void ApplyLandmarks(
        Biome[,] biomeMap,
        HexGridData gridLayout,
        HexMapGenerationContext context,
        HexMapPlacementReservations reservations,
        HexCoordinates startCoordinates,
        HexCoordinates goalCoordinates,
        System.Random random)
    {
        HexTerrainLandmarkPlacementRequest[] requests = context?.Modifiers.TerrainLandmarkRequests;
        if (biomeMap == null
            || gridLayout == null
            || context == null
            || reservations == null
            || requests == null
            || requests.Length == 0)
        {
            return;
        }

        int placedCount = 0;
        int requestedCount = 0;

        for (int requestIndex = 0; requestIndex < requests.Length; requestIndex++)
        {
            HexTerrainLandmarkPlacementRequest request = requests[requestIndex];
            if (request == null || !request.IsUsable)
            {
                continue;
            }

            requestedCount += request.count;

            for (int placementIndex = 0; placementIndex < request.count; placementIndex++)
            {
                if (TryPlaceRequest(
                    biomeMap,
                    gridLayout,
                    request,
                    reservations,
                    startCoordinates,
                    goalCoordinates,
                    random,
                    out FootprintPlacement placement,
                    out string failureReason))
                {
                    ApplyFootprint(biomeMap, placement.Cells);
                    for (int coordinateIndex = 0; coordinateIndex < placement.Cells.Count; coordinateIndex++)
                    {
                        reservations.Reserve(
                            placement.Cells[coordinateIndex].Coordinates,
                            HexMapPlacementReservationLayer.TerrainLandmark,
                            request.GetResolvedDisplayName());
                    }

                    placedCount++;
                    if (context.EnableVerbosePhaseLogging)
                    {
                        Debug.Log(
                            $"[TerrainStamp] Placed {request.GetResolvedDisplayName()} at {placement.Anchor} with {placement.Cells.Count} tiles (score {placement.Score:F2}).");
                    }
                }
                else if (context.EnableDebugLogging)
                {
                    Debug.LogWarning($"[TerrainStamp] Could not place {request.GetResolvedDisplayName()}: {failureReason}");
                }
            }
        }

        if (context.EnableDebugLogging && requestedCount > 0)
        {
            Debug.Log($"[TerrainStamp] Applied {placedCount}/{requestedCount} requested terrain landmark stamp(s).");
        }
    }

    private static bool TryPlaceRequest(
        Biome[,] biomeMap,
        HexGridData gridLayout,
        HexTerrainLandmarkPlacementRequest request,
        HexMapPlacementReservations reservations,
        HexCoordinates startCoordinates,
        HexCoordinates goalCoordinates,
        System.Random random,
        out FootprintPlacement placement,
        out string failureReason)
    {
        placement = default;
        failureReason = "no valid anchors";

        List<FootprintPlacement> validPlacements = new();
        for (int row = 0; row < gridLayout.Rows; row++)
        {
            for (int column = 0; column < gridLayout.Columns; column++)
            {
                HexCoordinates anchor = new(row, column);
                if (!MatchesPlacementBand(anchor, startCoordinates, goalCoordinates, request.placementBand))
                {
                    continue;
                }

                if (!TryBuildFootprint(anchor, request.definition, out List<HexCoordinates> footprint))
                {
                    continue;
                }

                List<CellPlacement> cellPlacements = BuildCellPlacements(footprint, request.definition);

                if (!IsValidFootprint(
                    biomeMap,
                    gridLayout,
                    request.definition,
                    cellPlacements,
                    reservations,
                    startCoordinates,
                    goalCoordinates))
                {
                    continue;
                }

                float score = ScorePlacement(cellPlacements, anchor, gridLayout, startCoordinates, goalCoordinates, request.placementBand, random);
                validPlacements.Add(new FootprintPlacement(anchor, cellPlacements, score));
            }
        }

        if (validPlacements.Count == 0)
        {
            return false;
        }

        validPlacements.Sort((left, right) => right.Score.CompareTo(left.Score));
        int selectionPoolSize = Mathf.Min(3, validPlacements.Count);
        placement = validPlacements[random.Next(selectionPoolSize)];
        failureReason = string.Empty;
        return true;
    }

    private static bool TryBuildFootprint(
        HexCoordinates anchor,
        HexTerrainLandmarkDefinition definition,
        out List<HexCoordinates> coordinates)
    {
        coordinates = new();
        if (definition == null || !definition.HasFootprint)
        {
            return false;
        }

        HashSet<HexCoordinates> uniqueCoordinates = new();
        for (int index = 0; index < definition.footprint.Length; index++)
        {
            HexTerrainLandmarkCell cell = definition.footprint[index];
            if (cell == null)
            {
                continue;
            }

            HexCoordinates coordinatesForCell = ApplyAxialOffset(anchor, cell.axialQ, cell.axialR);
            if (!uniqueCoordinates.Add(coordinatesForCell))
            {
                coordinates.Clear();
                return false;
            }

            coordinates.Add(coordinatesForCell);
        }

        return coordinates.Count > 0;
    }

    private static List<CellPlacement> BuildCellPlacements(
        IReadOnlyList<HexCoordinates> footprint,
        HexTerrainLandmarkDefinition definition)
    {
        List<CellPlacement> cellPlacements = new(footprint.Count);
        int footprintIndex = 0;
        for (int index = 0; index < definition.footprint.Length; index++)
        {
            HexTerrainLandmarkCell cell = definition.footprint[index];
            if (cell == null)
            {
                continue;
            }

            if (footprintIndex >= footprint.Count)
            {
                break;
            }

            cellPlacements.Add(new CellPlacement(footprint[footprintIndex], cell.biome));
            footprintIndex++;
        }

        return cellPlacements;
    }

    private static bool IsValidFootprint(
        Biome[,] biomeMap,
        HexGridData gridLayout,
        HexTerrainLandmarkDefinition definition,
        List<CellPlacement> footprint,
        HexMapPlacementReservations reservations,
        HexCoordinates startCoordinates,
        HexCoordinates goalCoordinates)
    {
        for (int index = 0; index < footprint.Count; index++)
        {
            HexCoordinates coordinates = footprint[index].Coordinates;
            if (!gridLayout.IsInside(coordinates))
            {
                return false;
            }

            if (reservations.IsReserved(coordinates))
            {
                return false;
            }

            if (GetEdgeDistance(coordinates, gridLayout.Rows, gridLayout.Columns) < definition.edgePadding)
            {
                return false;
            }

            if (coordinates.DistanceTo(startCoordinates) < definition.minDistanceFromStart
                || coordinates.DistanceTo(goalCoordinates) < definition.minDistanceFromGoal)
            {
                return false;
            }

            if (!definition.IsBaseBiomeAllowed(biomeMap[coordinates.Row, coordinates.Column]))
            {
                return false;
            }
        }

        return true;
    }

    private static void ApplyFootprint(
        Biome[,] biomeMap,
        IReadOnlyList<CellPlacement> footprint)
    {
        for (int index = 0; index < footprint.Count; index++)
        {
            CellPlacement cell = footprint[index];
            biomeMap[cell.Coordinates.Row, cell.Coordinates.Column] = cell.Biome;
        }
    }

    private static float ScorePlacement(
        IReadOnlyList<CellPlacement> footprint,
        HexCoordinates anchor,
        HexGridData gridLayout,
        HexCoordinates startCoordinates,
        HexCoordinates goalCoordinates,
        HexBoonMapPlacementBand placementBand,
        System.Random random)
    {
        float progress = CalculateProgress(anchor, startCoordinates, goalCoordinates);
        float targetProgress = placementBand switch
        {
            HexBoonMapPlacementBand.Early => 0.28f,
            HexBoonMapPlacementBand.Late => 0.72f,
            _ => 0.5f
        };

        float progressScore = 1f - Mathf.Abs(progress - targetProgress);
        float edgeScore = 0f;
        for (int index = 0; index < footprint.Count; index++)
        {
            edgeScore += GetEdgeDistance(footprint[index].Coordinates, gridLayout.Rows, gridLayout.Columns);
        }

        float averageEdgeScore = footprint.Count == 0 ? 0f : edgeScore / footprint.Count;
        return progressScore + (averageEdgeScore * 0.1f) + ((float)random.NextDouble() * 0.15f);
    }

    private static bool MatchesPlacementBand(
        HexCoordinates coordinates,
        HexCoordinates startCoordinates,
        HexCoordinates goalCoordinates,
        HexBoonMapPlacementBand placementBand)
    {
        if (placementBand == HexBoonMapPlacementBand.Default)
        {
            return true;
        }

        float progress = CalculateProgress(coordinates, startCoordinates, goalCoordinates);
        return placementBand switch
        {
            HexBoonMapPlacementBand.Early => progress >= 0.15f && progress <= 0.42f,
            HexBoonMapPlacementBand.Late => progress >= 0.58f && progress <= 0.85f,
            _ => progress >= 0.32f && progress <= 0.68f
        };
    }

    private static float CalculateProgress(HexCoordinates coordinates, HexCoordinates start, HexCoordinates goal)
    {
        if (start.Column == goal.Column)
        {
            return 0.5f;
        }

        return Mathf.Clamp01(Mathf.InverseLerp(start.Column, goal.Column, coordinates.Column));
    }

    private static int GetEdgeDistance(HexCoordinates coordinates, int rows, int columns)
    {
        int topDistance = coordinates.Row;
        int bottomDistance = rows - 1 - coordinates.Row;
        int leftDistance = coordinates.Column;
        int rightDistance = columns - 1 - coordinates.Column;
        return Mathf.Min(Mathf.Min(topDistance, bottomDistance), Mathf.Min(leftDistance, rightDistance));
    }

    private static HexCoordinates ApplyAxialOffset(HexCoordinates anchor, int axialQOffset, int axialROffset)
    {
        (int anchorQ, int anchorR) = ToAxial(anchor);
        return FromAxial(anchorQ + axialQOffset, anchorR + axialROffset);
    }

    private static (int q, int r) ToAxial(HexCoordinates coordinates)
    {
        int q = coordinates.Column - ((coordinates.Row - (coordinates.Row & 1)) / 2);
        return (q, coordinates.Row);
    }

    private static HexCoordinates FromAxial(int q, int r)
    {
        int column = q + ((r - (r & 1)) / 2);
        return new HexCoordinates(r, column);
    }
}
