using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class HexBiomeMapGenerator
{
    private struct TerrainSample
    {
        public float Elevation;
        public float Moisture;
        public float Heat;
    }

    private struct NoiseOffsets
    {
        public Vector2 Elevation;
        public Vector2 Moisture;
        public Vector2 Heat;
    }

    public int LastResolvedSeed { get; private set; }
    public int LastGenerationAttempts { get; private set; }
    public bool LastGenerationMetQualityThreshold { get; private set; }
    public HexBiomeMapQualityReport LastQualityReport { get; private set; }

    private readonly HexBiomeMacroRegionPainter regionPainter = new();
    private readonly HexBiomeFeaturePainter featurePainter = new();
    private readonly HexTerrainLandmarkStampPass terrainLandmarkStampPass = new();
    private readonly HexBiomeMapQualityEvaluator qualityEvaluator = new();

    public HexBiomeMapResult Generate(
        HexMapGenerationContext context)
    {
        context ??= new HexMapGenerationContext(
            1,
            1,
            new HexBiomeGenerationSettings(),
            new HexSpecialTileSettings(),
            HexMapGenerationModifiers.None);

        return GenerateInternal(context);
    }

    public HexBiomeMapResult Generate(
        int rows,
        int columns,
        HexBiomeGenerationSettings settings,
        HexSpecialTileSettings specialTileSettings)
    {
        return Generate(new HexMapGenerationContext(
            rows,
            columns,
            settings,
            specialTileSettings,
            HexMapGenerationModifiers.None));
    }

    private HexBiomeMapResult GenerateInternal(HexMapGenerationContext context)
    {
        HexBiomeGenerationSettings settings = context.ResolvedBiomeSettings;
        HexSpecialTileSettings specialTileSettings = context.SpecialTileSettings;

        if (context.EnableDebugLogging)
        {
            Debug.Log($"[MapGen] Starting generation: {context.GetDebugSummary()}");
        }

        LastResolvedSeed = settings.useRandomSeed ? Environment.TickCount : settings.seed;
        HexGridData gridLayout = new(context.Rows, context.Columns);
        HexBiomeMapQualitySettings qualitySettings = settings.qualitySettings ?? new HexBiomeMapQualitySettings();
        qualitySettings.Validate();

        int maxAttempts = qualitySettings.enableQualityRerolls ? qualitySettings.maxGenerationAttempts : 1;
        HexBiomeMapResult bestResult = null;
        HexBiomeMapQualityReport bestReport = null;

        for (int attemptIndex = 0; attemptIndex < maxAttempts; attemptIndex++)
        {
            int attemptSeed = ResolveAttemptSeed(LastResolvedSeed, attemptIndex);
            HexBiomeMapResult candidate = GenerateCandidate(context, settings, specialTileSettings, gridLayout, attemptSeed);
            HexBiomeMapQualityReport report = qualityEvaluator.Evaluate(candidate.BiomeMap, gridLayout, settings, specialTileSettings);

            if (context.EnableVerbosePhaseLogging)
            {
                Debug.Log(
                    $"[MapGen] Attempt {attemptIndex + 1}/{maxAttempts} seed={attemptSeed} score={report.Score:F2} acceptable={report.IsAcceptable} dominant={report.DominantBiome} ({report.DominantBiomeRatio:P0}).");
            }

            if (bestReport == null || report.Score > bestReport.Score)
            {
                bestResult = candidate;
                bestReport = report;
            }

            if (!qualitySettings.enableQualityRerolls || report.IsAcceptable)
            {
                LastGenerationAttempts = attemptIndex + 1;
                LastGenerationMetQualityThreshold = report.IsAcceptable;
                LastQualityReport = report;

                if (context.EnableDebugLogging)
                {
                    Debug.Log($"[MapGen] Accepted attempt {LastGenerationAttempts}/{maxAttempts} with seed {attemptSeed} and score {report.Score:F2}.");
                }

                return candidate;
            }
        }

        LastGenerationAttempts = maxAttempts;
        LastGenerationMetQualityThreshold = false;
        LastQualityReport = bestReport;

        if (context.EnableDebugLogging)
        {
            Debug.LogWarning($"[MapGen] Falling back to best-effort map after {maxAttempts} attempt(s). Best score={bestReport?.Score:F2}.");
        }

        return bestResult;
    }

    private HexBiomeMapResult GenerateCandidate(
        HexMapGenerationContext context,
        HexBiomeGenerationSettings settings,
        HexSpecialTileSettings specialTileSettings,
        HexGridData gridLayout,
        int attemptSeed)
    {
        int rows = context.Rows;
        int columns = context.Columns;
        System.Random random = new(attemptSeed);
        NoiseOffsets offsets = CreateNoiseOffsets(random);
        Biome[,] biomeMap = new Biome[rows, columns];

        for (int row = 0; row < rows; row++)
        {
            for (int column = 0; column < columns; column++)
            {
                HexCoordinates coordinates = new(row, column);
                TerrainSample sample = SampleTerrain(coordinates, settings, offsets);
                biomeMap[row, column] = ClassifyBiome(sample, settings);
            }
        }

        regionPainter.ApplyLandRegions(biomeMap, gridLayout, settings, random);
        featurePainter.ApplyFeatures(biomeMap, gridLayout, settings, random);
        ApplyMicroPatches(biomeMap, gridLayout, settings, random);
        ApplyIsolatedAnomalies(biomeMap, settings, random);

        (HexCoordinates startCoordinates, HexCoordinates goalCoordinates) = ChooseOppositeCornerCoordinates(rows, columns, random);
        HexMapPlacementReservations reservations = new();
        reservations.Reserve(startCoordinates, HexMapPlacementReservationLayer.StartGoal, "Start");
        reservations.Reserve(goalCoordinates, HexMapPlacementReservationLayer.StartGoal, "Goal");
        terrainLandmarkStampPass.ApplyLandmarks(
            biomeMap,
            gridLayout,
            context,
            reservations,
            startCoordinates,
            goalCoordinates,
            random);

        if (specialTileSettings.enforceStartBiome)
        {
            biomeMap[startCoordinates.Row, startCoordinates.Column] = ResolveSpecialTileBiome(
                specialTileSettings.startBiome);
        }

        if (specialTileSettings.enforceGoalBiome)
        {
            biomeMap[goalCoordinates.Row, goalCoordinates.Column] = ResolveSpecialTileBiome(
                specialTileSettings.goalBiome);
        }

        return new HexBiomeMapResult(biomeMap, startCoordinates, goalCoordinates, reservations);
    }

    private static TerrainSample SampleTerrain(HexCoordinates coordinates, HexBiomeGenerationSettings settings, NoiseOffsets offsets)
    {
        Vector2 samplePosition = HexNoiseUtility.ToSamplePosition(coordinates);

        return new TerrainSample
        {
            Elevation = HexNoiseUtility.SampleFractalNoise(
                samplePosition,
                settings.elevationFrequency,
                settings.noiseOctaves,
                settings.noisePersistence,
                settings.noiseLacunarity,
                offsets.Elevation),
            Moisture = HexNoiseUtility.SampleFractalNoise(
                samplePosition,
                settings.moistureFrequency,
                settings.noiseOctaves,
                settings.noisePersistence,
                settings.noiseLacunarity,
                offsets.Moisture),
            Heat = HexNoiseUtility.SampleFractalNoise(
                samplePosition,
                settings.heatFrequency,
                settings.noiseOctaves,
                settings.noisePersistence,
                settings.noiseLacunarity,
                offsets.Heat)
        };
    }

    private static Biome ClassifyBiome(TerrainSample sample, HexBiomeGenerationSettings settings)
    {
        if (sample.Elevation <= settings.waterThreshold)
        {
            return ResolveAllowedBiome(Biome.water, sample, settings);
        }

        if (sample.Elevation >= settings.mountainThreshold)
        {
            return ResolveAllowedBiome(Biome.mountain, sample, settings);
        }

        if (sample.Moisture <= settings.desertMoistureThreshold && sample.Heat >= settings.desertHeatThreshold)
        {
            return ResolveAllowedBiome(Biome.desert, sample, settings);
        }

        if (sample.Moisture >= settings.forestMoistureThreshold)
        {
            return ResolveAllowedBiome(Biome.forest, sample, settings);
        }

        return Biome.grass;
    }

    private static void ApplyMicroPatches(Biome[,] biomeMap, HexGridData gridLayout, HexBiomeGenerationSettings settings, System.Random random)
    {
        int rows = biomeMap.GetLength(0);
        int columns = biomeMap.GetLength(1);
        int patchAttempts = Mathf.RoundToInt(rows * columns * settings.microPatchChance);

        for (int attempt = 0; attempt < patchAttempts; attempt++)
        {
            HexCoordinates center = new(random.Next(rows), random.Next(columns));
            Biome targetBiome = PickAnomalyBiome(biomeMap[center.Row, center.Column], settings, random);
            int patchSize = random.Next(settings.microPatchMinSize, settings.microPatchMaxSize + 1);

            Queue<HexCoordinates> frontier = new();
            HashSet<HexCoordinates> visited = new();
            frontier.Enqueue(center);
            visited.Add(center);

            int paintedTiles = 0;
            while (frontier.Count > 0 && paintedTiles < patchSize)
            {
                HexCoordinates current = frontier.Dequeue();
                biomeMap[current.Row, current.Column] = targetBiome;
                paintedTiles++;

                foreach (HexCoordinates neighbor in Shuffle(gridLayout.GetNeighborCoordinates(current), random))
                {
                    if (visited.Add(neighbor))
                    {
                        frontier.Enqueue(neighbor);
                    }
                }
            }
        }
    }

    private static void ApplyIsolatedAnomalies(Biome[,] biomeMap, HexBiomeGenerationSettings settings, System.Random random)
    {
        int rows = biomeMap.GetLength(0);
        int columns = biomeMap.GetLength(1);

        for (int row = 0; row < rows; row++)
        {
            for (int column = 0; column < columns; column++)
            {
                if (random.NextDouble() > settings.isolatedAnomalyChance)
                {
                    continue;
                }

                biomeMap[row, column] = PickAnomalyBiome(biomeMap[row, column], settings, random);
            }
        }
    }

    private static Biome PickAnomalyBiome(Biome currentBiome, HexBiomeGenerationSettings settings, System.Random random)
    {
        Biome[] anomalyCandidates = currentBiome switch
        {
            Biome.water => new[] { Biome.grass, Biome.forest, Biome.mountain },
            Biome.mountain => new[] { Biome.grass, Biome.desert, Biome.forest },
            Biome.desert => new[] { Biome.grass, Biome.forest, Biome.mountain },
            Biome.forest => new[] { Biome.desert, Biome.grass, Biome.water },
            _ => new[] { Biome.forest, Biome.desert, Biome.water, Biome.mountain }
        };

        List<Biome> enabledCandidates = new();
        foreach (Biome candidate in anomalyCandidates)
        {
            if (IsBiomeEnabled(candidate, settings))
            {
                enabledCandidates.Add(candidate);
            }
        }

        if (enabledCandidates.Count == 0)
        {
            return currentBiome;
        }

        return enabledCandidates[random.Next(enabledCandidates.Count)];
    }

    private static NoiseOffsets CreateNoiseOffsets(System.Random random)
    {
        return new NoiseOffsets
        {
            Elevation = CreateOffset(random),
            Moisture = CreateOffset(random),
            Heat = CreateOffset(random)
        };
    }

    private static Vector2 CreateOffset(System.Random random)
    {
        return new Vector2(
            (float)(random.NextDouble() * 10000d),
            (float)(random.NextDouble() * 10000d));
    }

    private static int ResolveAttemptSeed(int baseSeed, int attemptIndex)
    {
        return unchecked(baseSeed + (attemptIndex * 7919));
    }

    private static IEnumerable<HexCoordinates> Shuffle(IEnumerable<HexCoordinates> coordinates, System.Random random)
    {
        List<HexCoordinates> shuffled = new(coordinates);
        for (int index = shuffled.Count - 1; index > 0; index--)
        {
            int swapIndex = random.Next(index + 1);
            (shuffled[index], shuffled[swapIndex]) = (shuffled[swapIndex], shuffled[index]);
        }

        return shuffled;
    }

    private static (HexCoordinates startCoordinates, HexCoordinates goalCoordinates) ChooseOppositeCornerCoordinates(
        int rows,
        int columns,
        System.Random random)
    {
        bool useTopLeftStart = random.Next(0, 2) == 0;
        int topRow = 0;
        int bottomRow = Mathf.Max(0, rows - 1);
        int leftColumn = 0;
        int rightColumn = Mathf.Max(0, columns - 1);

        return useTopLeftStart
            ? (new HexCoordinates(topRow, leftColumn), new HexCoordinates(bottomRow, rightColumn))
            : (new HexCoordinates(bottomRow, leftColumn), new HexCoordinates(topRow, rightColumn));
    }

    private static Biome ResolveSpecialTileBiome(Biome preferredBiome)
    {
        return preferredBiome;
    }

    private static Biome ResolveAllowedBiome(Biome preferredBiome, TerrainSample sample, HexBiomeGenerationSettings settings)
    {
        if (IsBiomeEnabled(preferredBiome, settings))
        {
            return preferredBiome;
        }

        List<Biome> fallbackCandidates = new();

        if (sample.Moisture >= settings.forestMoistureThreshold)
        {
            fallbackCandidates.Add(Biome.forest);
        }

        if (sample.Elevation <= settings.waterThreshold)
        {
            fallbackCandidates.Add(Biome.water);
        }

        if (sample.Elevation >= settings.mountainThreshold)
        {
            fallbackCandidates.Add(Biome.mountain);
        }

        if (sample.Moisture <= settings.desertMoistureThreshold && sample.Heat >= settings.desertHeatThreshold)
        {
            fallbackCandidates.Add(Biome.desert);
        }

        fallbackCandidates.Add(Biome.grass);
        fallbackCandidates.Add(Biome.forest);
        fallbackCandidates.Add(Biome.mountain);
        fallbackCandidates.Add(Biome.water);
        fallbackCandidates.Add(Biome.desert);

        foreach (Biome candidate in fallbackCandidates)
        {
            if (IsBiomeEnabled(candidate, settings))
            {
                return candidate;
            }
        }

        return Biome.grass;
    }

    private static bool IsBiomeEnabled(Biome biome, HexBiomeGenerationSettings settings)
    {
        return biome switch
        {
            Biome.grass => settings.enableGrass,
            Biome.forest => settings.enableForest,
            Biome.mountain => settings.enableMountain,
            Biome.water => settings.enableWater,
            Biome.desert => settings.enableDesert,
            _ => false
        };
    }
}

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
