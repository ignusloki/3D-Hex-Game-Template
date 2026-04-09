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
    private readonly HexBiomeMapQualityEvaluator qualityEvaluator = new();

    public HexBiomeMapResult Generate(
        int rows,
        int columns,
        HexBiomeGenerationSettings settings,
        HexSpecialTileSettings specialTileSettings)
    {
        settings ??= new HexBiomeGenerationSettings();
        settings.Validate();
        specialTileSettings ??= new HexSpecialTileSettings();

        LastResolvedSeed = settings.useRandomSeed ? Environment.TickCount : settings.seed;
        HexGridData gridLayout = new(rows, columns);
        HexBiomeMapQualitySettings qualitySettings = settings.qualitySettings ?? new HexBiomeMapQualitySettings();
        qualitySettings.Validate();

        int maxAttempts = qualitySettings.enableQualityRerolls ? qualitySettings.maxGenerationAttempts : 1;
        HexBiomeMapResult bestResult = null;
        HexBiomeMapQualityReport bestReport = null;

        for (int attemptIndex = 0; attemptIndex < maxAttempts; attemptIndex++)
        {
            int attemptSeed = ResolveAttemptSeed(LastResolvedSeed, attemptIndex);
            HexBiomeMapResult candidate = GenerateCandidate(rows, columns, settings, specialTileSettings, gridLayout, attemptSeed);
            HexBiomeMapQualityReport report = qualityEvaluator.Evaluate(candidate.BiomeMap, gridLayout, settings, specialTileSettings);

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
                return candidate;
            }
        }

        LastGenerationAttempts = maxAttempts;
        LastGenerationMetQualityThreshold = false;
        LastQualityReport = bestReport;
        return bestResult;
    }

    private HexBiomeMapResult GenerateCandidate(
        int rows,
        int columns,
        HexBiomeGenerationSettings settings,
        HexSpecialTileSettings specialTileSettings,
        HexGridData gridLayout,
        int attemptSeed)
    {
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

        HexCoordinates startCoordinates = ChooseEdgeCoordinates(rows, 0, random);
        HexCoordinates goalCoordinates = ChooseEdgeCoordinates(rows, columns - 1, random);

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

        return new HexBiomeMapResult(biomeMap, startCoordinates, goalCoordinates);
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

    private static HexCoordinates ChooseEdgeCoordinates(int rows, int column, System.Random random)
    {
        return new HexCoordinates(random.Next(rows), column);
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
