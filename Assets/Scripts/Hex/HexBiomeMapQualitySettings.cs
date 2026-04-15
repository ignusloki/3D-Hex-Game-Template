using UnityEngine;

[System.Serializable]
public sealed class HexBiomeMapQualitySettings
{
    public bool enableQualityRerolls = true;
    [Min(1)] public int maxGenerationAttempts = 10;
    [Range(0.4f, 0.95f)] public float maxDominantBiomeRatio = 0.68f;
    [Range(1, 5)] public int minDistinctBiomeCount = 3;
    [Range(0f, 0.4f)] public float minSecondaryRegionRatio = 0.12f;
    [Min(1)] public int minMeaningfulRegionSize = 3;
    [Range(0f, 0.3f)] public float maxSmallSecondaryRegionRatio = 0.08f;

    public HexBiomeMapQualitySettings Clone()
    {
        return new HexBiomeMapQualitySettings
        {
            enableQualityRerolls = enableQualityRerolls,
            maxGenerationAttempts = maxGenerationAttempts,
            maxDominantBiomeRatio = maxDominantBiomeRatio,
            minDistinctBiomeCount = minDistinctBiomeCount,
            minSecondaryRegionRatio = minSecondaryRegionRatio,
            minMeaningfulRegionSize = minMeaningfulRegionSize,
            maxSmallSecondaryRegionRatio = maxSmallSecondaryRegionRatio
        };
    }

    public void Validate()
    {
        maxGenerationAttempts = Mathf.Max(1, maxGenerationAttempts);
        maxDominantBiomeRatio = Mathf.Clamp(maxDominantBiomeRatio, 0.4f, 0.95f);
        minDistinctBiomeCount = Mathf.Clamp(minDistinctBiomeCount, 1, 5);
        minSecondaryRegionRatio = Mathf.Clamp(minSecondaryRegionRatio, 0f, 0.4f);
        minMeaningfulRegionSize = Mathf.Max(1, minMeaningfulRegionSize);
        maxSmallSecondaryRegionRatio = Mathf.Clamp(maxSmallSecondaryRegionRatio, 0f, 0.3f);
    }
}
