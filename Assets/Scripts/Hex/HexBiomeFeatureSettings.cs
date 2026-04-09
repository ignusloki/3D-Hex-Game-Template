using UnityEngine;

[System.Serializable]
public sealed class HexBiomeFeatureSettings
{
    public bool enableFeatureOverlays = true;

    [Header("Forest Features")]
    [Range(0, 4)] public int minForestFeatureCount = 1;
    [Range(0, 4)] public int maxForestFeatureCount = 2;
    [Range(0f, 0.5f)] public float forestFeatureMinRatio = 0.12f;
    [Range(0f, 0.5f)] public float forestFeatureMaxRatio = 0.22f;

    [Header("Water Features")]
    [Range(0, 3)] public int minWaterFeatureCount = 0;
    [Range(0, 3)] public int maxWaterFeatureCount = 1;
    [Range(0f, 0.2f)] public float waterFeatureMinRatio = 0.03f;
    [Range(0f, 0.2f)] public float waterFeatureMaxRatio = 0.08f;

    [Header("Mountain Features")]
    [Range(0, 3)] public int minMountainFeatureCount = 0;
    [Range(0, 3)] public int maxMountainFeatureCount = 1;
    [Range(0f, 0.15f)] public float mountainFeatureMinRatio = 0.02f;
    [Range(0f, 0.15f)] public float mountainFeatureMaxRatio = 0.05f;

    [Header("Shape")]
    [Min(0)] public int featureEdgePadding = 1;
    [Range(0f, 2f)] public float compactnessBias = 0.8f;
    [Range(0f, 1f)] public float ridgeTurnChance = 0.35f;
    [Range(0f, 1f)] public float ridgeBranchChance = 0.2f;

    public void Validate()
    {
        minForestFeatureCount = Mathf.Clamp(minForestFeatureCount, 0, 4);
        maxForestFeatureCount = Mathf.Clamp(maxForestFeatureCount, minForestFeatureCount, 4);
        forestFeatureMinRatio = Mathf.Clamp(forestFeatureMinRatio, 0f, 0.5f);
        forestFeatureMaxRatio = Mathf.Clamp(forestFeatureMaxRatio, forestFeatureMinRatio, 0.5f);

        minWaterFeatureCount = Mathf.Clamp(minWaterFeatureCount, 0, 3);
        maxWaterFeatureCount = Mathf.Clamp(maxWaterFeatureCount, minWaterFeatureCount, 3);
        waterFeatureMinRatio = Mathf.Clamp(waterFeatureMinRatio, 0f, 0.2f);
        waterFeatureMaxRatio = Mathf.Clamp(waterFeatureMaxRatio, waterFeatureMinRatio, 0.2f);

        minMountainFeatureCount = Mathf.Clamp(minMountainFeatureCount, 0, 3);
        maxMountainFeatureCount = Mathf.Clamp(maxMountainFeatureCount, minMountainFeatureCount, 3);
        mountainFeatureMinRatio = Mathf.Clamp(mountainFeatureMinRatio, 0f, 0.15f);
        mountainFeatureMaxRatio = Mathf.Clamp(mountainFeatureMaxRatio, mountainFeatureMinRatio, 0.15f);

        featureEdgePadding = Mathf.Max(0, featureEdgePadding);
        compactnessBias = Mathf.Clamp(compactnessBias, 0f, 2f);
        ridgeTurnChance = Mathf.Clamp01(ridgeTurnChance);
        ridgeBranchChance = Mathf.Clamp01(ridgeBranchChance);
    }
}
