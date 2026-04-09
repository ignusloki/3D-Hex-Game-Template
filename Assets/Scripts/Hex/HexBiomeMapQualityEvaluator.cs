using System.Collections.Generic;
using UnityEngine;

public sealed class HexBiomeMapQualityEvaluator
{
    private readonly struct RegionInfo
    {
        public RegionInfo(Biome biome, int size)
        {
            Biome = biome;
            Size = size;
        }

        public Biome Biome { get; }
        public int Size { get; }
    }

    public HexBiomeMapQualityReport Evaluate(
        Biome[,] biomeMap,
        HexGridData gridLayout,
        HexBiomeGenerationSettings generationSettings,
        HexSpecialTileSettings specialTileSettings)
    {
        HexBiomeMapQualitySettings qualitySettings = generationSettings?.qualitySettings ?? new HexBiomeMapQualitySettings();
        qualitySettings.Validate();

        int rows = biomeMap.GetLength(0);
        int columns = biomeMap.GetLength(1);
        int totalTiles = rows * columns;

        Dictionary<Biome, int> biomeCounts = new();
        List<RegionInfo> regions = CollectRegions(biomeMap, gridLayout, biomeCounts);

        Biome dominantBiome = Biome.grass;
        int dominantCount = 0;
        foreach ((Biome biome, int count) in biomeCounts)
        {
            if (count > dominantCount)
            {
                dominantBiome = biome;
                dominantCount = count;
            }
        }

        int maxPossibleDistinctBiomes = CountPossibleBiomes(generationSettings, specialTileSettings);
        int requiredDistinctBiomes = Mathf.Min(qualitySettings.minDistinctBiomeCount, Mathf.Max(1, maxPossibleDistinctBiomes));
        float requiredSecondaryRegionRatio = requiredDistinctBiomes > 1 ? qualitySettings.minSecondaryRegionRatio : 0f;

        int largestSecondaryRegion = 0;
        int smallSecondaryTiles = 0;
        foreach (RegionInfo region in regions)
        {
            if (region.Biome == dominantBiome)
            {
                continue;
            }

            if (region.Size > largestSecondaryRegion)
            {
                largestSecondaryRegion = region.Size;
            }

            if (region.Size < qualitySettings.minMeaningfulRegionSize)
            {
                smallSecondaryTiles += region.Size;
            }
        }

        float dominantBiomeRatio = totalTiles > 0 ? dominantCount / (float)totalTiles : 0f;
        float largestSecondaryRegionRatio = totalTiles > 0 ? largestSecondaryRegion / (float)totalTiles : 0f;
        float smallSecondaryRegionRatio = totalTiles > 0 ? smallSecondaryTiles / (float)totalTiles : 0f;
        int distinctBiomeCount = biomeCounts.Count;

        bool isAcceptable = dominantBiomeRatio <= qualitySettings.maxDominantBiomeRatio
            && distinctBiomeCount >= requiredDistinctBiomes
            && largestSecondaryRegionRatio >= requiredSecondaryRegionRatio
            && smallSecondaryRegionRatio <= qualitySettings.maxSmallSecondaryRegionRatio;

        float score = 1f;
        score -= Mathf.Max(0f, dominantBiomeRatio - qualitySettings.maxDominantBiomeRatio) * 2.5f;
        score -= Mathf.Max(0f, requiredSecondaryRegionRatio - largestSecondaryRegionRatio) * 2f;
        score -= Mathf.Max(0f, smallSecondaryRegionRatio - qualitySettings.maxSmallSecondaryRegionRatio) * 1.5f;

        if (distinctBiomeCount < requiredDistinctBiomes)
        {
            score -= 0.2f * (requiredDistinctBiomes - distinctBiomeCount);
        }

        score = Mathf.Clamp01(score);

        return new HexBiomeMapQualityReport(
            isAcceptable,
            score,
            dominantBiome,
            dominantBiomeRatio,
            distinctBiomeCount,
            largestSecondaryRegionRatio,
            smallSecondaryRegionRatio);
    }

    private static List<RegionInfo> CollectRegions(Biome[,] biomeMap, HexGridData gridLayout, Dictionary<Biome, int> biomeCounts)
    {
        int rows = biomeMap.GetLength(0);
        int columns = biomeMap.GetLength(1);
        bool[,] visited = new bool[rows, columns];
        List<RegionInfo> regions = new();

        for (int row = 0; row < rows; row++)
        {
            for (int column = 0; column < columns; column++)
            {
                if (visited[row, column])
                {
                    continue;
                }

                Biome biome = biomeMap[row, column];
                int regionSize = FloodFillRegion(new HexCoordinates(row, column), biome, biomeMap, gridLayout, visited);
                regions.Add(new RegionInfo(biome, regionSize));

                if (biomeCounts.TryGetValue(biome, out int currentCount))
                {
                    biomeCounts[biome] = currentCount + regionSize;
                }
                else
                {
                    biomeCounts[biome] = regionSize;
                }
            }
        }

        return regions;
    }

    private static int FloodFillRegion(
        HexCoordinates start,
        Biome biome,
        Biome[,] biomeMap,
        HexGridData gridLayout,
        bool[,] visited)
    {
        Queue<HexCoordinates> frontier = new();
        frontier.Enqueue(start);
        visited[start.Row, start.Column] = true;

        int regionSize = 0;
        while (frontier.Count > 0)
        {
            HexCoordinates current = frontier.Dequeue();
            regionSize++;

            foreach (HexCoordinates neighbor in gridLayout.GetNeighborCoordinates(current))
            {
                if (visited[neighbor.Row, neighbor.Column] || biomeMap[neighbor.Row, neighbor.Column] != biome)
                {
                    continue;
                }

                visited[neighbor.Row, neighbor.Column] = true;
                frontier.Enqueue(neighbor);
            }
        }

        return regionSize;
    }

    private static int CountPossibleBiomes(HexBiomeGenerationSettings generationSettings, HexSpecialTileSettings specialTileSettings)
    {
        HashSet<Biome> possibleBiomes = new();

        if (generationSettings == null)
        {
            return 1;
        }

        if (generationSettings.enableGrass)
        {
            possibleBiomes.Add(Biome.grass);
        }

        if (generationSettings.enableForest)
        {
            possibleBiomes.Add(Biome.forest);
        }

        if (generationSettings.enableMountain)
        {
            possibleBiomes.Add(Biome.mountain);
        }

        if (generationSettings.enableWater)
        {
            possibleBiomes.Add(Biome.water);
        }

        if (generationSettings.enableDesert)
        {
            possibleBiomes.Add(Biome.desert);
        }

        if (specialTileSettings?.enforceStartBiome == true)
        {
            possibleBiomes.Add(specialTileSettings.startBiome);
        }

        if (specialTileSettings?.enforceGoalBiome == true)
        {
            possibleBiomes.Add(specialTileSettings.goalBiome);
        }

        return possibleBiomes.Count;
    }
}
