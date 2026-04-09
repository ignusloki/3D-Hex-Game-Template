using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class HexBiomeFeaturePainter
{
    public void ApplyFeatures(
        Biome[,] biomeMap,
        HexGridData gridLayout,
        HexBiomeGenerationSettings generationSettings,
        System.Random random)
    {
        HexBiomeFeatureSettings featureSettings = generationSettings?.featureSettings ?? new HexBiomeFeatureSettings();
        featureSettings.Validate();

        if (!featureSettings.enableFeatureOverlays)
        {
            return;
        }

        int totalTiles = biomeMap.GetLength(0) * biomeMap.GetLength(1);

        if (generationSettings.enableForest)
        {
            PaintBlobFeatures(
                biomeMap,
                gridLayout,
                random,
                featureSettings,
                Biome.forest,
                ResolveFeatureCount(featureSettings.minForestFeatureCount, featureSettings.maxForestFeatureCount, random),
                ResolveFeatureSize(totalTiles, featureSettings.forestFeatureMinRatio, featureSettings.forestFeatureMaxRatio, random),
                CanPaintForestOver);
        }

        if (generationSettings.enableWater)
        {
            PaintBlobFeatures(
                biomeMap,
                gridLayout,
                random,
                featureSettings,
                Biome.water,
                ResolveFeatureCount(featureSettings.minWaterFeatureCount, featureSettings.maxWaterFeatureCount, random),
                ResolveFeatureSize(totalTiles, featureSettings.waterFeatureMinRatio, featureSettings.waterFeatureMaxRatio, random),
                CanPaintWaterOver);
        }

        if (generationSettings.enableMountain)
        {
            PaintRidgeFeatures(
                biomeMap,
                gridLayout,
                random,
                featureSettings,
                ResolveFeatureCount(featureSettings.minMountainFeatureCount, featureSettings.maxMountainFeatureCount, random),
                ResolveFeatureSize(totalTiles, featureSettings.mountainFeatureMinRatio, featureSettings.mountainFeatureMaxRatio, random));
        }
    }

    private static void PaintBlobFeatures(
        Biome[,] biomeMap,
        HexGridData gridLayout,
        System.Random random,
        HexBiomeFeatureSettings featureSettings,
        Biome targetBiome,
        int featureCount,
        int featureSize,
        Func<Biome, bool> canReplace)
    {
        for (int featureIndex = 0; featureIndex < featureCount; featureIndex++)
        {
            if (!TryPickSeed(biomeMap, gridLayout, random, featureSettings, targetBiome, canReplace, out HexCoordinates seed))
            {
                continue;
            }

            PaintCompactBlob(biomeMap, gridLayout, random, featureSettings, targetBiome, seed, featureSize, canReplace);
        }
    }

    private static void PaintRidgeFeatures(
        Biome[,] biomeMap,
        HexGridData gridLayout,
        System.Random random,
        HexBiomeFeatureSettings featureSettings,
        int featureCount,
        int featureSize)
    {
        for (int featureIndex = 0; featureIndex < featureCount; featureIndex++)
        {
            if (!TryPickSeed(biomeMap, gridLayout, random, featureSettings, Biome.mountain, CanPaintMountainOver, out HexCoordinates seed))
            {
                continue;
            }

            PaintRidge(biomeMap, gridLayout, random, featureSettings, seed, featureSize);
        }
    }

    private static void PaintCompactBlob(
        Biome[,] biomeMap,
        HexGridData gridLayout,
        System.Random random,
        HexBiomeFeatureSettings featureSettings,
        Biome targetBiome,
        HexCoordinates seed,
        int targetSize,
        Func<Biome, bool> canReplace)
    {
        List<HexCoordinates> frontier = new() { seed };
        HashSet<HexCoordinates> queued = new() { seed };
        int painted = 0;

        while (frontier.Count > 0 && painted < targetSize)
        {
            int frontierIndex = ChooseBestFrontierIndex(frontier, biomeMap, gridLayout, targetBiome, seed, featureSettings, random);
            HexCoordinates current = frontier[frontierIndex];
            frontier.RemoveAt(frontierIndex);

            if (!canReplace(biomeMap[current.Row, current.Column]) && biomeMap[current.Row, current.Column] != targetBiome)
            {
                continue;
            }

            if (biomeMap[current.Row, current.Column] != targetBiome)
            {
                biomeMap[current.Row, current.Column] = targetBiome;
                painted++;
            }

            foreach (HexCoordinates neighbor in Shuffle(gridLayout.GetNeighborCoordinates(current), random))
            {
                if (queued.Contains(neighbor))
                {
                    continue;
                }

                Biome neighborBiome = biomeMap[neighbor.Row, neighbor.Column];
                if (neighborBiome == targetBiome || canReplace(neighborBiome))
                {
                    frontier.Add(neighbor);
                    queued.Add(neighbor);
                }
            }
        }
    }

    private static void PaintRidge(
        Biome[,] biomeMap,
        HexGridData gridLayout,
        System.Random random,
        HexBiomeFeatureSettings featureSettings,
        HexCoordinates seed,
        int targetSize)
    {
        HexCoordinates current = seed;
        HexCoordinates? previous = null;
        HashSet<HexCoordinates> paintedCoordinates = new();

        for (int step = 0; step < targetSize; step++)
        {
            if (CanPaintMountainOver(biomeMap[current.Row, current.Column]) || biomeMap[current.Row, current.Column] == Biome.mountain)
            {
                biomeMap[current.Row, current.Column] = Biome.mountain;
                paintedCoordinates.Add(current);
            }

            if (random.NextDouble() <= featureSettings.ridgeBranchChance)
            {
                TryPaintBranch(biomeMap, gridLayout, random, current, paintedCoordinates);
            }

            List<HexCoordinates> candidates = new();
            foreach (HexCoordinates neighbor in gridLayout.GetNeighborCoordinates(current))
            {
                if (paintedCoordinates.Contains(neighbor))
                {
                    continue;
                }

                if (!CanPaintMountainOver(biomeMap[neighbor.Row, neighbor.Column]) && biomeMap[neighbor.Row, neighbor.Column] != Biome.mountain)
                {
                    continue;
                }

                candidates.Add(neighbor);
            }

            if (candidates.Count == 0)
            {
                break;
            }

            HexCoordinates next = ChooseRidgeStep(candidates, current, previous, random, featureSettings);
            previous = current;
            current = next;
        }
    }

    private static void TryPaintBranch(
        Biome[,] biomeMap,
        HexGridData gridLayout,
        System.Random random,
        HexCoordinates current,
        HashSet<HexCoordinates> paintedCoordinates)
    {
        List<HexCoordinates> branchCandidates = new();
        foreach (HexCoordinates neighbor in gridLayout.GetNeighborCoordinates(current))
        {
            if (!paintedCoordinates.Contains(neighbor) && CanPaintMountainOver(biomeMap[neighbor.Row, neighbor.Column]))
            {
                branchCandidates.Add(neighbor);
            }
        }

        if (branchCandidates.Count == 0)
        {
            return;
        }

        HexCoordinates branch = branchCandidates[random.Next(branchCandidates.Count)];
        biomeMap[branch.Row, branch.Column] = Biome.mountain;
        paintedCoordinates.Add(branch);
    }

    private static HexCoordinates ChooseRidgeStep(
        List<HexCoordinates> candidates,
        HexCoordinates current,
        HexCoordinates? previous,
        System.Random random,
        HexBiomeFeatureSettings featureSettings)
    {
        if (previous == null || random.NextDouble() < featureSettings.ridgeTurnChance)
        {
            return candidates[random.Next(candidates.Count)];
        }

        int bestIndex = 0;
        float bestScore = float.MinValue;
        Vector2 lastDirection = new(current.Row - previous.Value.Row, current.Column - previous.Value.Column);

        for (int index = 0; index < candidates.Count; index++)
        {
            HexCoordinates candidate = candidates[index];
            Vector2 candidateDirection = new(candidate.Row - current.Row, candidate.Column - current.Column);
            float score = Vector2.Dot(lastDirection.normalized, candidateDirection.normalized) + (float)random.NextDouble() * 0.25f;

            if (score > bestScore)
            {
                bestScore = score;
                bestIndex = index;
            }
        }

        return candidates[bestIndex];
    }

    private static bool TryPickSeed(
        Biome[,] biomeMap,
        HexGridData gridLayout,
        System.Random random,
        HexBiomeFeatureSettings featureSettings,
        Biome targetBiome,
        Func<Biome, bool> canReplace,
        out HexCoordinates seed)
    {
        if (TryPickSeedFromCandidates(biomeMap, gridLayout, random, featureSettings, targetBiome, canReplace, true, out seed))
        {
            return true;
        }

        return TryPickSeedFromCandidates(biomeMap, gridLayout, random, featureSettings, targetBiome, canReplace, false, out seed);
    }

    private static bool TryPickSeedFromCandidates(
        Biome[,] biomeMap,
        HexGridData gridLayout,
        System.Random random,
        HexBiomeFeatureSettings featureSettings,
        Biome targetBiome,
        Func<Biome, bool> canReplace,
        bool respectPadding,
        out HexCoordinates seed)
    {
        List<HexCoordinates> candidates = new();
        int rows = biomeMap.GetLength(0);
        int columns = biomeMap.GetLength(1);

        for (int row = 0; row < rows; row++)
        {
            for (int column = 0; column < columns; column++)
            {
                if (respectPadding && IsInsideEdgePadding(row, column, rows, columns, featureSettings.featureEdgePadding))
                {
                    continue;
                }

                Biome currentBiome = biomeMap[row, column];
                if (currentBiome != targetBiome && !canReplace(currentBiome))
                {
                    continue;
                }

                candidates.Add(new HexCoordinates(row, column));
            }
        }

        if (candidates.Count == 0)
        {
            seed = default;
            return false;
        }

        int bestIndex = 0;
        float bestScore = float.MinValue;
        for (int index = 0; index < candidates.Count; index++)
        {
            HexCoordinates candidate = candidates[index];
            float score = ScoreSeedCandidate(candidate, targetBiome, biomeMap, gridLayout, random);
            if (score > bestScore)
            {
                bestScore = score;
                bestIndex = index;
            }
        }

        seed = candidates[bestIndex];
        return true;
    }

    private static float ScoreSeedCandidate(
        HexCoordinates coordinates,
        Biome targetBiome,
        Biome[,] biomeMap,
        HexGridData gridLayout,
        System.Random random)
    {
        Biome currentBiome = biomeMap[coordinates.Row, coordinates.Column];
        float score = currentBiome == targetBiome ? 3f : 1f;

        foreach (HexCoordinates neighbor in gridLayout.GetNeighborCoordinates(coordinates))
        {
            Biome neighborBiome = biomeMap[neighbor.Row, neighbor.Column];
            if (neighborBiome == targetBiome)
            {
                score += 1.25f;
            }
            else if (neighborBiome == Biome.grass)
            {
                score += 0.25f;
            }
        }

        return score + (float)random.NextDouble() * 0.5f;
    }

    private static int ChooseBestFrontierIndex(
        List<HexCoordinates> frontier,
        Biome[,] biomeMap,
        HexGridData gridLayout,
        Biome targetBiome,
        HexCoordinates seed,
        HexBiomeFeatureSettings featureSettings,
        System.Random random)
    {
        int bestIndex = 0;
        float bestScore = float.MinValue;

        for (int index = 0; index < frontier.Count; index++)
        {
            HexCoordinates candidate = frontier[index];
            float score = ScoreFrontierTile(candidate, biomeMap, gridLayout, targetBiome, seed, featureSettings, random);
            if (score > bestScore)
            {
                bestScore = score;
                bestIndex = index;
            }
        }

        return bestIndex;
    }

    private static float ScoreFrontierTile(
        HexCoordinates candidate,
        Biome[,] biomeMap,
        HexGridData gridLayout,
        Biome targetBiome,
        HexCoordinates seed,
        HexBiomeFeatureSettings featureSettings,
        System.Random random)
    {
        int sameBiomeNeighbors = 0;
        int compatibleNeighbors = 0;

        foreach (HexCoordinates neighbor in gridLayout.GetNeighborCoordinates(candidate))
        {
            Biome neighborBiome = biomeMap[neighbor.Row, neighbor.Column];
            if (neighborBiome == targetBiome)
            {
                sameBiomeNeighbors++;
            }
            else if (neighborBiome == Biome.grass || neighborBiome == Biome.forest)
            {
                compatibleNeighbors++;
            }
        }

        float distancePenalty = seed.DistanceTo(candidate) * 0.18f;
        float cohesionScore = (sameBiomeNeighbors * featureSettings.compactnessBias) + (compatibleNeighbors * 0.15f);
        return cohesionScore - distancePenalty + (float)random.NextDouble() * 0.5f;
    }

    private static int ResolveFeatureCount(int minCount, int maxCount, System.Random random)
    {
        if (maxCount <= minCount)
        {
            return minCount;
        }

        return random.Next(minCount, maxCount + 1);
    }

    private static int ResolveFeatureSize(int totalTiles, float minRatio, float maxRatio, System.Random random)
    {
        float ratio = Mathf.Approximately(minRatio, maxRatio)
            ? minRatio
            : Mathf.Lerp(minRatio, maxRatio, (float)random.NextDouble());

        return Mathf.Max(1, Mathf.RoundToInt(totalTiles * ratio));
    }

    private static bool IsInsideEdgePadding(int row, int column, int rows, int columns, int edgePadding)
    {
        if (edgePadding <= 0)
        {
            return false;
        }

        return row < edgePadding
            || column < edgePadding
            || row >= rows - edgePadding
            || column >= columns - edgePadding;
    }

    private static bool CanPaintForestOver(Biome biome)
    {
        return biome == Biome.grass || biome == Biome.forest || biome == Biome.desert;
    }

    private static bool CanPaintWaterOver(Biome biome)
    {
        return biome == Biome.grass || biome == Biome.forest || biome == Biome.desert || biome == Biome.water;
    }

    private static bool CanPaintMountainOver(Biome biome)
    {
        return biome == Biome.grass || biome == Biome.forest || biome == Biome.desert || biome == Biome.mountain;
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
}
