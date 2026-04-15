using System;
using System.Collections.Generic;
using UnityEngine;

public readonly struct HexMapGenerationModifiers
{
    public static HexMapGenerationModifiers None { get; } = new(
        0,
        HexBoonMapPlacementBand.Default,
        0f,
        1f,
        1f,
        1f,
        1f,
        1f,
        1f,
        null);

    public HexMapGenerationModifiers(
        int extraPitstopCount,
        HexBoonMapPlacementBand extraPitstopPlacementBand,
        float waterThresholdDelta = 0f,
        float forestFeatureCountMultiplier = 1f,
        float waterFeatureCountMultiplier = 1f,
        float mountainFeatureCountMultiplier = 1f,
        float forestFeatureSizeMultiplier = 1f,
        float waterFeatureSizeMultiplier = 1f,
        float mountainFeatureSizeMultiplier = 1f,
        HexTerrainLandmarkPlacementRequest[] terrainLandmarkRequests = null)
    {
        ExtraPitstopCount = Mathf.Max(0, extraPitstopCount);
        ExtraPitstopPlacementBand = extraPitstopPlacementBand;
        WaterThresholdDelta = waterThresholdDelta;
        ForestFeatureCountMultiplier = SanitizeMultiplier(forestFeatureCountMultiplier);
        WaterFeatureCountMultiplier = SanitizeMultiplier(waterFeatureCountMultiplier);
        MountainFeatureCountMultiplier = SanitizeMultiplier(mountainFeatureCountMultiplier);
        ForestFeatureSizeMultiplier = SanitizeMultiplier(forestFeatureSizeMultiplier);
        WaterFeatureSizeMultiplier = SanitizeMultiplier(waterFeatureSizeMultiplier);
        MountainFeatureSizeMultiplier = SanitizeMultiplier(mountainFeatureSizeMultiplier);
        TerrainLandmarkRequests = SanitizeRequests(terrainLandmarkRequests);
    }

    public int ExtraPitstopCount { get; }
    public HexBoonMapPlacementBand ExtraPitstopPlacementBand { get; }
    public float WaterThresholdDelta { get; }
    public float ForestFeatureCountMultiplier { get; }
    public float WaterFeatureCountMultiplier { get; }
    public float MountainFeatureCountMultiplier { get; }
    public float ForestFeatureSizeMultiplier { get; }
    public float WaterFeatureSizeMultiplier { get; }
    public float MountainFeatureSizeMultiplier { get; }
    public HexTerrainLandmarkPlacementRequest[] TerrainLandmarkRequests { get; }

    public bool HasPitstopModifiers => ExtraPitstopCount > 0;
    public bool HasLandmarkRequests => TerrainLandmarkRequests != null && TerrainLandmarkRequests.Length > 0;

    public bool HasTerrainModifiers =>
        !Mathf.Approximately(WaterThresholdDelta, 0f)
        || !Mathf.Approximately(ForestFeatureCountMultiplier, 1f)
        || !Mathf.Approximately(WaterFeatureCountMultiplier, 1f)
        || !Mathf.Approximately(MountainFeatureCountMultiplier, 1f)
        || !Mathf.Approximately(ForestFeatureSizeMultiplier, 1f)
        || !Mathf.Approximately(WaterFeatureSizeMultiplier, 1f)
        || !Mathf.Approximately(MountainFeatureSizeMultiplier, 1f);

    public bool HasAny => HasPitstopModifiers || HasTerrainModifiers || HasLandmarkRequests;

    public string GetDebugSummary()
    {
        if (!HasAny)
        {
            return "none";
        }

        System.Text.StringBuilder summary = new();
        if (HasPitstopModifiers)
        {
            summary.Append($"+{ExtraPitstopCount} pitstop(s) [{ExtraPitstopPlacementBand}]");
        }

        if (HasTerrainModifiers)
        {
            if (summary.Length > 0)
            {
                summary.Append("; ");
            }

            summary.Append($"waterThresholdDelta={WaterThresholdDelta:+0.###;-0.###;0}");
            summary.Append($", featureCount x[{ForestFeatureCountMultiplier:0.##}, {WaterFeatureCountMultiplier:0.##}, {MountainFeatureCountMultiplier:0.##}]");
            summary.Append($", featureSize x[{ForestFeatureSizeMultiplier:0.##}, {WaterFeatureSizeMultiplier:0.##}, {MountainFeatureSizeMultiplier:0.##}]");
        }

        if (HasLandmarkRequests)
        {
            if (summary.Length > 0)
            {
                summary.Append("; ");
            }

            summary.Append("landmarks=");
            for (int index = 0; index < TerrainLandmarkRequests.Length; index++)
            {
                if (index > 0)
                {
                    summary.Append(", ");
                }

                HexTerrainLandmarkPlacementRequest request = TerrainLandmarkRequests[index];
                summary.Append($"{request.GetResolvedDisplayName()}x{request.count} [{request.placementBand}]");
            }
        }

        return summary.ToString();
    }

    public HexMapGenerationModifiers Combine(HexMapGenerationModifiers other)
    {
        if (!HasAny)
        {
            return other;
        }

        if (!other.HasAny)
        {
            return this;
        }

        HexBoonMapPlacementBand placementBand = other.ExtraPitstopCount > 0
            ? other.ExtraPitstopPlacementBand
            : ExtraPitstopPlacementBand;

        return new HexMapGenerationModifiers(
            ExtraPitstopCount + other.ExtraPitstopCount,
            placementBand,
            WaterThresholdDelta + other.WaterThresholdDelta,
            ForestFeatureCountMultiplier * other.ForestFeatureCountMultiplier,
            WaterFeatureCountMultiplier * other.WaterFeatureCountMultiplier,
            MountainFeatureCountMultiplier * other.MountainFeatureCountMultiplier,
            ForestFeatureSizeMultiplier * other.ForestFeatureSizeMultiplier,
            WaterFeatureSizeMultiplier * other.WaterFeatureSizeMultiplier,
            MountainFeatureSizeMultiplier * other.MountainFeatureSizeMultiplier,
            CombineRequests(TerrainLandmarkRequests, other.TerrainLandmarkRequests));
    }

    private static float SanitizeMultiplier(float value)
    {
        return float.IsNaN(value) || float.IsInfinity(value)
            ? 1f
            : Mathf.Max(0f, value);
    }

    private static HexTerrainLandmarkPlacementRequest[] SanitizeRequests(HexTerrainLandmarkPlacementRequest[] requests)
    {
        if (requests == null || requests.Length == 0)
        {
            return Array.Empty<HexTerrainLandmarkPlacementRequest>();
        }

        List<HexTerrainLandmarkPlacementRequest> sanitized = new(requests.Length);
        for (int index = 0; index < requests.Length; index++)
        {
            HexTerrainLandmarkPlacementRequest request = requests[index];
            if (request == null)
            {
                continue;
            }

            request.Validate();
            if (request.IsUsable)
            {
                sanitized.Add(request.Clone());
            }
        }

        return sanitized.Count == 0 ? Array.Empty<HexTerrainLandmarkPlacementRequest>() : sanitized.ToArray();
    }

    private static HexTerrainLandmarkPlacementRequest[] CombineRequests(
        HexTerrainLandmarkPlacementRequest[] left,
        HexTerrainLandmarkPlacementRequest[] right)
    {
        bool hasLeft = left != null && left.Length > 0;
        bool hasRight = right != null && right.Length > 0;

        if (!hasLeft && !hasRight)
        {
            return Array.Empty<HexTerrainLandmarkPlacementRequest>();
        }

        if (!hasLeft)
        {
            return SanitizeRequests(right);
        }

        if (!hasRight)
        {
            return SanitizeRequests(left);
        }

        HexTerrainLandmarkPlacementRequest[] combined = new HexTerrainLandmarkPlacementRequest[left.Length + right.Length];
        Array.Copy(left, 0, combined, 0, left.Length);
        Array.Copy(right, 0, combined, left.Length, right.Length);
        return SanitizeRequests(combined);
    }
}

public sealed class HexMapGenerationContext
{
    public HexMapGenerationContext(
        int rows,
        int columns,
        HexBiomeGenerationSettings biomeSettings,
        HexSpecialTileSettings specialTileSettings,
        HexMapGenerationModifiers modifiers,
        int? fixedSeedOverride = null,
        bool enableDebugLogging = false,
        bool enableVerbosePhaseLogging = false)
    {
        Rows = Mathf.Max(1, rows);
        Columns = Mathf.Max(1, columns);
        Modifiers = modifiers;
        EnableDebugLogging = enableDebugLogging;
        EnableVerbosePhaseLogging = enableVerbosePhaseLogging;

        BiomeSettings = biomeSettings?.Clone() ?? new HexBiomeGenerationSettings();
        if (fixedSeedOverride.HasValue && !BiomeSettings.useRandomSeed)
        {
            BiomeSettings.seed = fixedSeedOverride.Value;
        }

        BiomeSettings.Validate();
        ResolvedBiomeSettings = CreateResolvedBiomeSettings(BiomeSettings, Modifiers);
        SpecialTileSettings = specialTileSettings?.Clone() ?? new HexSpecialTileSettings();
    }

    public int Rows { get; }
    public int Columns { get; }
    public HexBiomeGenerationSettings BiomeSettings { get; }
    public HexBiomeGenerationSettings ResolvedBiomeSettings { get; }
    public HexSpecialTileSettings SpecialTileSettings { get; }
    public HexMapGenerationModifiers Modifiers { get; }
    public bool EnableDebugLogging { get; }
    public bool EnableVerbosePhaseLogging { get; }

    public string GetDebugSummary()
    {
        return $"size={Rows}x{Columns}, seedMode={(ResolvedBiomeSettings.useRandomSeed ? "random" : ResolvedBiomeSettings.seed.ToString())}, modifiers={Modifiers.GetDebugSummary()}";
    }

    private static HexBiomeGenerationSettings CreateResolvedBiomeSettings(
        HexBiomeGenerationSettings baseSettings,
        HexMapGenerationModifiers modifiers)
    {
        HexBiomeGenerationSettings resolved = baseSettings?.Clone() ?? new HexBiomeGenerationSettings();
        ApplyThresholdModifiers(resolved, modifiers);
        ApplyFeatureModifiers(resolved.featureSettings, modifiers);
        resolved.Validate();
        return resolved;
    }

    private static void ApplyThresholdModifiers(HexBiomeGenerationSettings settings, HexMapGenerationModifiers modifiers)
    {
        settings.waterThreshold += modifiers.WaterThresholdDelta;
    }

    private static void ApplyFeatureModifiers(HexBiomeFeatureSettings featureSettings, HexMapGenerationModifiers modifiers)
    {
        featureSettings.minForestFeatureCount = ScaleCount(featureSettings.minForestFeatureCount, modifiers.ForestFeatureCountMultiplier);
        featureSettings.maxForestFeatureCount = ScaleCount(featureSettings.maxForestFeatureCount, modifiers.ForestFeatureCountMultiplier);
        featureSettings.minWaterFeatureCount = ScaleCount(featureSettings.minWaterFeatureCount, modifiers.WaterFeatureCountMultiplier);
        featureSettings.maxWaterFeatureCount = ScaleCount(featureSettings.maxWaterFeatureCount, modifiers.WaterFeatureCountMultiplier);
        featureSettings.minMountainFeatureCount = ScaleCount(featureSettings.minMountainFeatureCount, modifiers.MountainFeatureCountMultiplier);
        featureSettings.maxMountainFeatureCount = ScaleCount(featureSettings.maxMountainFeatureCount, modifiers.MountainFeatureCountMultiplier);

        featureSettings.forestFeatureMinRatio *= modifiers.ForestFeatureSizeMultiplier;
        featureSettings.forestFeatureMaxRatio *= modifiers.ForestFeatureSizeMultiplier;
        featureSettings.waterFeatureMinRatio *= modifiers.WaterFeatureSizeMultiplier;
        featureSettings.waterFeatureMaxRatio *= modifiers.WaterFeatureSizeMultiplier;
        featureSettings.mountainFeatureMinRatio *= modifiers.MountainFeatureSizeMultiplier;
        featureSettings.mountainFeatureMaxRatio *= modifiers.MountainFeatureSizeMultiplier;
    }

    private static int ScaleCount(int value, float multiplier)
    {
        if (value <= 0)
        {
            return 0;
        }

        return Mathf.Max(0, Mathf.RoundToInt(value * multiplier));
    }
}

[Obsolete("Use HexMapGenerationModifiers instead.")]
public readonly struct HexActMapModifiers
{
    private readonly HexMapGenerationModifiers modifiers;

    public static HexActMapModifiers None { get; } = new(0, HexBoonMapPlacementBand.Default);

    public HexActMapModifiers(int extraPitstopCount, HexBoonMapPlacementBand extraPitstopPlacementBand)
    {
        modifiers = new HexMapGenerationModifiers(extraPitstopCount, extraPitstopPlacementBand);
    }

    public int ExtraPitstopCount => modifiers.ExtraPitstopCount;
    public HexBoonMapPlacementBand ExtraPitstopPlacementBand => modifiers.ExtraPitstopPlacementBand;
    public bool HasAny => modifiers.HasPitstopModifiers;

    public HexMapGenerationModifiers ToMapGenerationModifiers()
    {
        return modifiers;
    }
}
