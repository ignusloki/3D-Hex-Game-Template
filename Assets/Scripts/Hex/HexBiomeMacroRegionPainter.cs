using System.Collections.Generic;
using UnityEngine;

public sealed class HexBiomeMacroRegionPainter
{
    private readonly struct RegionSeed
    {
        public RegionSeed(HexCoordinates coordinates, Biome biome, float noiseBias)
        {
            Coordinates = coordinates;
            Biome = biome;
            NoiseBias = noiseBias;
        }

        public HexCoordinates Coordinates { get; }
        public Biome Biome { get; }
        public float NoiseBias { get; }
    }

    public void ApplyLandRegions(
        Biome[,] biomeMap,
        HexGridData gridLayout,
        HexBiomeGenerationSettings generationSettings,
        Biome fallbackBiome,
        HexMapGenerationModifiers modifiers,
        System.Random random)
    {
        HexBiomeRegionSettings regionSettings = generationSettings?.regionSettings ?? new HexBiomeRegionSettings();
        regionSettings.Validate();

        if (!regionSettings.enableMacroRegions)
        {
            return;
        }

        List<Biome> availableLandBiomes = BuildAvailableLandBiomes(generationSettings, fallbackBiome);
        if (availableLandBiomes.Count == 0)
        {
            return;
        }

        int regionCount = Mathf.Min(
            regionSettings.GetRegionCount(biomeMap.GetLength(0), biomeMap.GetLength(1), random),
            biomeMap.GetLength(0) * biomeMap.GetLength(1));

        List<RegionSeed> regionSeeds = CreateSeeds(
            regionCount,
            gridLayout,
            availableLandBiomes,
            generationSettings,
            regionSettings,
            fallbackBiome,
            modifiers,
            random);
        if (regionSeeds.Count == 0)
        {
            return;
        }

        int rows = biomeMap.GetLength(0);
        int columns = biomeMap.GetLength(1);

        for (int row = 0; row < rows; row++)
        {
            for (int column = 0; column < columns; column++)
            {
                HexCoordinates coordinates = new(row, column);
                Biome currentBiome = biomeMap[row, column];

                if (!regionSettings.restrictBaseToLandBiomes && !IsLandBiome(currentBiome))
                {
                    continue;
                }

                int bestIndex = -1;
                int secondIndex = -1;
                float bestScore = float.MaxValue;
                float secondScore = float.MaxValue;

                for (int index = 0; index < regionSeeds.Count; index++)
                {
                    RegionSeed seed = regionSeeds[index];
                    float score = coordinates.DistanceTo(seed.Coordinates) + seed.NoiseBias;

                    if (score < bestScore)
                    {
                        secondScore = bestScore;
                        secondIndex = bestIndex;
                        bestScore = score;
                        bestIndex = index;
                    }
                    else if (score < secondScore)
                    {
                        secondScore = score;
                        secondIndex = index;
                    }
                }

                if (bestIndex < 0)
                {
                    continue;
                }

                Biome targetBiome = regionSeeds[bestIndex].Biome;
                if (secondIndex >= 0)
                {
                    float boundaryDelta = secondScore - bestScore;
                    float boundaryBlend = regionSettings.boundaryNoiseStrength + ((float)random.NextDouble() * 0.35f);
                    if (boundaryDelta <= boundaryBlend && random.NextDouble() < 0.35d)
                    {
                        targetBiome = regionSeeds[secondIndex].Biome;
                    }
                }

                if (currentBiome == targetBiome)
                {
                    continue;
                }

                if (IsLandBiome(currentBiome) && random.NextDouble() < regionSettings.localVariationChance)
                {
                    continue;
                }

                biomeMap[row, column] = targetBiome;
            }
        }
    }

    private static List<RegionSeed> CreateSeeds(
        int regionCount,
        HexGridData gridLayout,
        List<Biome> availableLandBiomes,
        HexBiomeGenerationSettings generationSettings,
        HexBiomeRegionSettings regionSettings,
        Biome fallbackBiome,
        HexMapGenerationModifiers modifiers,
        System.Random random)
    {
        List<HexCoordinates> candidates = new();
        for (int row = 0; row < gridLayout.Rows; row++)
        {
            for (int column = 0; column < gridLayout.Columns; column++)
            {
                candidates.Add(new HexCoordinates(row, column));
            }
        }

        List<RegionSeed> seeds = new();
        if (candidates.Count == 0)
        {
            return seeds;
        }

        HexCoordinates firstCoordinates = candidates[random.Next(candidates.Count)];
        seeds.Add(new RegionSeed(
            firstCoordinates,
            ChooseLandBiome(availableLandBiomes, generationSettings, fallbackBiome, modifiers, random),
            (float)random.NextDouble() * regionSettings.boundaryNoiseStrength));

        while (seeds.Count < regionCount)
        {
            HexCoordinates bestCandidate = default;
            float bestDistance = float.MinValue;

            foreach (HexCoordinates candidate in candidates)
            {
                float minDistance = float.MaxValue;
                foreach (RegionSeed seed in seeds)
                {
                    float distance = candidate.DistanceTo(seed.Coordinates);
                    if (distance < minDistance)
                    {
                        minDistance = distance;
                    }
                }

                float score = minDistance + ((float)random.NextDouble() * regionSettings.seedSpacingBias);
                if (score > bestDistance)
                {
                    bestDistance = score;
                    bestCandidate = candidate;
                }
            }

            seeds.Add(new RegionSeed(
                bestCandidate,
                ChooseLandBiome(availableLandBiomes, generationSettings, fallbackBiome, modifiers, random),
                (float)random.NextDouble() * regionSettings.boundaryNoiseStrength));
        }

        return seeds;
    }

    private static Biome ChooseLandBiome(
        List<Biome> availableLandBiomes,
        HexBiomeGenerationSettings generationSettings,
        Biome fallbackBiome,
        HexMapGenerationModifiers modifiers,
        System.Random random)
    {
        float totalWeight = 0f;
        float[] weights = new float[availableLandBiomes.Count];

        for (int index = 0; index < availableLandBiomes.Count; index++)
        {
            float weight = GetBiomeWeight(availableLandBiomes[index], generationSettings, fallbackBiome, modifiers);
            weights[index] = weight;
            totalWeight += weight;
        }

        if (totalWeight <= 0f)
        {
            return availableLandBiomes[random.Next(availableLandBiomes.Count)];
        }

        float roll = (float)random.NextDouble() * totalWeight;
        float cumulative = 0f;

        for (int index = 0; index < availableLandBiomes.Count; index++)
        {
            cumulative += weights[index];
            if (roll <= cumulative)
            {
                return availableLandBiomes[index];
            }
        }

        return availableLandBiomes[^1];
    }

    private static float GetBiomeWeight(
        Biome biome,
        HexBiomeGenerationSettings settings,
        Biome fallbackBiome,
        HexMapGenerationModifiers modifiers)
    {
        float weight = biome switch
        {
            Biome.forest => Mathf.Clamp(1.2f + ((0.65f - settings.forestMoistureThreshold) * 3.5f), 0.2f, 4f),
            Biome.desert => settings.enableDesert
                ? Mathf.Clamp(0.25f + (settings.desertMoistureThreshold * 2f) + ((1f - settings.desertHeatThreshold) * 2f), 0.1f, 3f)
                : 0f,
            _ => 1f
        };

        if (biome == fallbackBiome && IsLandBiome(biome))
        {
            weight *= 3.5f;
        }
        else if (fallbackBiome != Biome.grass && biome == Biome.grass)
        {
            weight *= 0.2f;
        }

        weight *= biome switch
        {
            Biome.grass => modifiers.GrassRegionWeightMultiplier,
            Biome.forest => modifiers.ForestRegionWeightMultiplier,
            Biome.desert => modifiers.DesertRegionWeightMultiplier,
            _ => 1f
        };

        return weight;
    }

    private static List<Biome> BuildAvailableLandBiomes(HexBiomeGenerationSettings generationSettings, Biome fallbackBiome)
    {
        List<Biome> available = new();

        if (generationSettings.enableGrass)
        {
            available.Add(Biome.grass);
        }

        if (generationSettings.enableForest)
        {
            available.Add(Biome.forest);
        }

        if (generationSettings.enableDesert)
        {
            available.Add(Biome.desert);
        }

        if (available.Count == 0)
        {
            available.Add(fallbackBiome);
        }

        return available;
    }

    private static bool IsLandBiome(Biome biome)
    {
        return biome == Biome.grass || biome == Biome.forest || biome == Biome.desert;
    }
}
