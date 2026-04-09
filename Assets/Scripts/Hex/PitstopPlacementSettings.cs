using UnityEngine;

[System.Serializable]
public sealed class PitstopPlacementSettings
{
    [Header("Counts")]
    [Min(0)] public int pitstopsOnFiveByFive = 4;
    [Min(0)] public int pitstopsOnTenByTen = 8;

    [Header("Spacing")]
    [Min(0)] public int minimumSpacingOnFiveByFive = 1;
    [Min(0)] public int minimumSpacingOnTenByTen = 2;

    [Header("Placement Rules")]
    public bool allowGrass = true;
    public bool allowForest = true;
    public bool allowMountain = false;
    public bool allowDesert = false;
    [Range(0f, 1f)] public float pathBias = 0.65f;
    [Range(0f, 1f)] public float lowCostBias = 0.25f;
    [Range(0f, 1f)] public float grassPreferenceBias = 0.2f;
    [Min(1)] public int preferredPathDistance = 2;
    [Range(0f, 0.25f)] public float randomJitter = 0.05f;

    public int GetDesiredCount(int rows, int columns)
    {
        return rows * columns <= 25 ? pitstopsOnFiveByFive : pitstopsOnTenByTen;
    }

    public int GetMinimumSpacing(int rows, int columns)
    {
        return rows * columns <= 25 ? minimumSpacingOnFiveByFive : minimumSpacingOnTenByTen;
    }

    public void Validate()
    {
        pitstopsOnFiveByFive = Mathf.Max(0, pitstopsOnFiveByFive);
        pitstopsOnTenByTen = Mathf.Max(0, pitstopsOnTenByTen);
        minimumSpacingOnFiveByFive = Mathf.Max(0, minimumSpacingOnFiveByFive);
        minimumSpacingOnTenByTen = Mathf.Max(0, minimumSpacingOnTenByTen);
        pathBias = Mathf.Clamp01(pathBias);
        lowCostBias = Mathf.Clamp01(lowCostBias);
        grassPreferenceBias = Mathf.Clamp01(grassPreferenceBias);
        preferredPathDistance = Mathf.Max(1, preferredPathDistance);
        randomJitter = Mathf.Clamp(randomJitter, 0f, 0.25f);
    }

    public bool IsAllowedBiome(Biome biome)
    {
        return biome switch
        {
            Biome.grass => allowGrass,
            Biome.forest => allowForest,
            Biome.mountain => allowMountain,
            Biome.desert => allowDesert,
            _ => false
        };
    }
}
