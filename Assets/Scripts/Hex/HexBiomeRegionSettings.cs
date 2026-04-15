using UnityEngine;

[System.Serializable]
public sealed class HexBiomeRegionSettings
{
    public bool enableMacroRegions = true;
    public bool restrictBaseToLandBiomes = true;

    [Header("Region Counts")]
    [Range(1, 8)] public int fiveByFiveMinRegions = 3;
    [Range(1, 8)] public int fiveByFiveMaxRegions = 4;
    [Range(2, 16)] public int tenByTenMinRegions = 6;
    [Range(2, 16)] public int tenByTenMaxRegions = 9;

    [Header("Shape")]
    [Range(0f, 2f)] public float boundaryNoiseStrength = 0.55f;
    [Range(0f, 1f)] public float localVariationChance = 0.08f;
    [Range(0f, 2f)] public float seedSpacingBias = 0.75f;

    public HexBiomeRegionSettings Clone()
    {
        return new HexBiomeRegionSettings
        {
            enableMacroRegions = enableMacroRegions,
            restrictBaseToLandBiomes = restrictBaseToLandBiomes,
            fiveByFiveMinRegions = fiveByFiveMinRegions,
            fiveByFiveMaxRegions = fiveByFiveMaxRegions,
            tenByTenMinRegions = tenByTenMinRegions,
            tenByTenMaxRegions = tenByTenMaxRegions,
            boundaryNoiseStrength = boundaryNoiseStrength,
            localVariationChance = localVariationChance,
            seedSpacingBias = seedSpacingBias
        };
    }

    public void Validate()
    {
        fiveByFiveMinRegions = Mathf.Clamp(fiveByFiveMinRegions, 1, 8);
        fiveByFiveMaxRegions = Mathf.Clamp(fiveByFiveMaxRegions, fiveByFiveMinRegions, 8);
        tenByTenMinRegions = Mathf.Clamp(tenByTenMinRegions, 2, 16);
        tenByTenMaxRegions = Mathf.Clamp(tenByTenMaxRegions, tenByTenMinRegions, 16);

        boundaryNoiseStrength = Mathf.Clamp(boundaryNoiseStrength, 0f, 2f);
        localVariationChance = Mathf.Clamp01(localVariationChance);
        seedSpacingBias = Mathf.Clamp(seedSpacingBias, 0f, 2f);
    }

    public int GetRegionCount(int rows, int columns, System.Random random)
    {
        int tileCount = rows * columns;
        bool isSmallMap = tileCount <= 25;

        int min = isSmallMap ? fiveByFiveMinRegions : tenByTenMinRegions;
        int max = isSmallMap ? fiveByFiveMaxRegions : tenByTenMaxRegions;

        if (max <= min)
        {
            return min;
        }

        return random.Next(min, max + 1);
    }
}
