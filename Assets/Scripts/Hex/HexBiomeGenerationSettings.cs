using UnityEngine;

[System.Serializable]
public sealed class HexBiomeGenerationSettings
{
    [Header("Biome Usage")]
    public bool enableGrass = true;
    public bool enableForest = true;
    public bool enableMountain = true;
    public bool enableWater = true;
    public bool enableDesert = true;

    [Header("Seed")]
    public bool useRandomSeed = true;
    public int seed = 12345;

    [Header("Noise")]
    [Min(0.001f)] public float elevationFrequency = 0.12f;
    [Min(0.001f)] public float moistureFrequency = 0.1f;
    [Min(0.001f)] public float heatFrequency = 0.08f;
    [Range(1, 6)] public int noiseOctaves = 3;
    [Range(0.1f, 1f)] public float noisePersistence = 0.5f;
    [Min(1f)] public float noiseLacunarity = 2f;

    [Header("Biome Thresholds")]
    [Range(0f, 1f)] public float waterThreshold = 0.28f;
    [Range(0f, 1f)] public float mountainThreshold = 0.72f;
    [Range(0f, 1f)] public float forestMoistureThreshold = 0.6f;
    [Range(0f, 1f)] public float desertMoistureThreshold = 0.35f;
    [Range(0f, 1f)] public float desertHeatThreshold = 0.58f;

    [Header("Anomalies")]
    [Range(0f, 0.25f)] public float isolatedAnomalyChance = 0.025f;
    [Range(0f, 0.2f)] public float microPatchChance = 0.04f;
    [Min(1)] public int microPatchMinSize = 2;
    [Min(1)] public int microPatchMaxSize = 4;

    [Header("Regions")]
    public HexBiomeRegionSettings regionSettings = new();

    [Header("Features")]
    public HexBiomeFeatureSettings featureSettings = new();

    [Header("Quality")]
    public HexBiomeMapQualitySettings qualitySettings = new();

    public void Validate()
    {
        enableGrass = true;
        regionSettings ??= new HexBiomeRegionSettings();
        regionSettings.Validate();
        featureSettings ??= new HexBiomeFeatureSettings();
        featureSettings.Validate();
        qualitySettings ??= new HexBiomeMapQualitySettings();
        qualitySettings.Validate();

        elevationFrequency = Mathf.Max(0.001f, elevationFrequency);
        moistureFrequency = Mathf.Max(0.001f, moistureFrequency);
        heatFrequency = Mathf.Max(0.001f, heatFrequency);
        noiseOctaves = Mathf.Clamp(noiseOctaves, 1, 6);
        noisePersistence = Mathf.Clamp(noisePersistence, 0.1f, 1f);
        noiseLacunarity = Mathf.Max(1f, noiseLacunarity);

        waterThreshold = Mathf.Clamp01(waterThreshold);
        mountainThreshold = Mathf.Clamp01(mountainThreshold);
        if (mountainThreshold < waterThreshold + 0.05f)
        {
            mountainThreshold = Mathf.Min(1f, waterThreshold + 0.05f);
        }

        forestMoistureThreshold = Mathf.Clamp01(forestMoistureThreshold);
        desertMoistureThreshold = Mathf.Clamp01(desertMoistureThreshold);
        desertHeatThreshold = Mathf.Clamp01(desertHeatThreshold);

        isolatedAnomalyChance = Mathf.Clamp(isolatedAnomalyChance, 0f, 0.25f);
        microPatchChance = Mathf.Clamp(microPatchChance, 0f, 0.2f);
        microPatchMinSize = Mathf.Max(1, microPatchMinSize);
        microPatchMaxSize = Mathf.Max(microPatchMinSize, microPatchMaxSize);
    }
}
