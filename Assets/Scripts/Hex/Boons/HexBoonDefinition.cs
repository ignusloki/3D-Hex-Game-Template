using UnityEngine;

public enum HexBoonCategory
{
    Passive,
    Rechargeable,
    MapModifying
}

public enum HexBoonPassiveEffectType
{
    None,
    VisibilityRadiusBonus
}

public enum HexBoonRechargeableEffectType
{
    None,
    IgnoreObstaclePenalty
}

public enum HexBoonRechargeTrigger
{
    None,
    PitstopArrival
}

public enum HexBoonMapModifierType
{
    None,
    AdditionalPitstops
}

public enum HexBoonMapPlacementBand
{
    Default,
    Early,
    Mid,
    Late
}

[System.Serializable]
public sealed class HexBoonPassiveEffectData
{
    public HexBoonPassiveEffectType effectType = HexBoonPassiveEffectType.None;
    [Min(0)] public int magnitude;

    public void Validate()
    {
        magnitude = Mathf.Max(0, magnitude);
    }
}

[System.Serializable]
public sealed class HexBoonRechargeableEffectData
{
    public HexBoonRechargeableEffectType effectType = HexBoonRechargeableEffectType.None;
    [Min(0)] public int maxCharges;
    [Min(0)] public int startingCharges;
    public HexBoonRechargeTrigger rechargeTrigger = HexBoonRechargeTrigger.None;

    public void Validate()
    {
        maxCharges = Mathf.Max(0, maxCharges);
        startingCharges = Mathf.Clamp(startingCharges, 0, maxCharges);
    }
}

[System.Serializable]
public sealed class HexBoonMapModifierData
{
    public HexBoonMapModifierType effectType = HexBoonMapModifierType.None;
    [Min(0)] public int magnitude;
    public HexBoonMapPlacementBand placementBand = HexBoonMapPlacementBand.Default;
    [Header("Terrain Thresholds")]
    [Range(-0.25f, 0.25f)] public float waterThresholdDelta;
    [Header("Feature Count Multipliers")]
    [Min(0f)] public float forestFeatureCountMultiplier = 1f;
    [Min(0f)] public float waterFeatureCountMultiplier = 1f;
    [Min(0f)] public float mountainFeatureCountMultiplier = 1f;
    [Header("Feature Size Multipliers")]
    [Min(0f)] public float forestFeatureSizeMultiplier = 1f;
    [Min(0f)] public float waterFeatureSizeMultiplier = 1f;
    [Min(0f)] public float mountainFeatureSizeMultiplier = 1f;
    [Header("Terrain Landmarks")]
    public HexTerrainLandmarkPlacementRequest[] terrainLandmarkRequests = System.Array.Empty<HexTerrainLandmarkPlacementRequest>();
    [Header("Map Objects")]
    public HexMapObjectPlacementRequest[] mapObjectPlacementRequests = System.Array.Empty<HexMapObjectPlacementRequest>();

    public void Validate()
    {
        magnitude = Mathf.Max(0, magnitude);
        waterThresholdDelta = Mathf.Clamp(waterThresholdDelta, -0.25f, 0.25f);
        forestFeatureCountMultiplier = Mathf.Max(0f, forestFeatureCountMultiplier);
        waterFeatureCountMultiplier = Mathf.Max(0f, waterFeatureCountMultiplier);
        mountainFeatureCountMultiplier = Mathf.Max(0f, mountainFeatureCountMultiplier);
        forestFeatureSizeMultiplier = Mathf.Max(0f, forestFeatureSizeMultiplier);
        waterFeatureSizeMultiplier = Mathf.Max(0f, waterFeatureSizeMultiplier);
        mountainFeatureSizeMultiplier = Mathf.Max(0f, mountainFeatureSizeMultiplier);
        terrainLandmarkRequests ??= System.Array.Empty<HexTerrainLandmarkPlacementRequest>();
        mapObjectPlacementRequests ??= System.Array.Empty<HexMapObjectPlacementRequest>();

        for (int index = 0; index < terrainLandmarkRequests.Length; index++)
        {
            terrainLandmarkRequests[index]?.Validate();
        }

        for (int index = 0; index < mapObjectPlacementRequests.Length; index++)
        {
            mapObjectPlacementRequests[index]?.Validate();
        }
    }
}

[CreateAssetMenu(
    fileName = "BoonDefinition",
    menuName = "Hex/Boons/Boon Definition")]
public sealed class HexBoonDefinition : ScriptableObject
{
    public string id = "boon";
    public string displayName = "New Boon";
    [TextArea(2, 4)] public string description = string.Empty;
    [TextArea(2, 4)] public string flavorText = string.Empty;
    public Sprite icon;
    public HexNemesisArchetype archetypeFamily = HexNemesisArchetype.Hunter;
    public HexBoonCategory category = HexBoonCategory.Passive;
    public bool isEnabled = true;

    [Header("Passive")]
    public HexBoonPassiveEffectData passive = new();

    [Header("Rechargeable")]
    public HexBoonRechargeableEffectData rechargeable = new();

    [Header("Map Modifiers")]
    public HexBoonMapModifierData mapModifier = new();

    private void OnValidate()
    {
        Validate();
    }

    public void Validate()
    {
        displayName = string.IsNullOrWhiteSpace(displayName) ? "New Boon" : displayName.Trim();
        id = SanitizeId(string.IsNullOrWhiteSpace(id) ? displayName : id);
        description ??= string.Empty;
        flavorText ??= string.Empty;
        passive ??= new HexBoonPassiveEffectData();
        rechargeable ??= new HexBoonRechargeableEffectData();
        mapModifier ??= new HexBoonMapModifierData();
        passive.Validate();
        rechargeable.Validate();
        mapModifier.Validate();
    }

    public string GetResolvedDisplayName()
    {
        return string.IsNullOrWhiteSpace(displayName) ? "Boon" : displayName;
    }

    public int GetVisibilityRadiusBonus()
    {
        if (!isEnabled
            || category != HexBoonCategory.Passive
            || passive == null
            || passive.effectType != HexBoonPassiveEffectType.VisibilityRadiusBonus)
        {
            return 0;
        }

        return passive.magnitude;
    }

    public bool SupportsIgnoredObstaclePenalty()
    {
        return isEnabled
            && category == HexBoonCategory.Rechargeable
            && rechargeable != null
            && rechargeable.effectType == HexBoonRechargeableEffectType.IgnoreObstaclePenalty
            && rechargeable.maxCharges > 0;
    }

    public int GetMaxCharges()
    {
        return SupportsIgnoredObstaclePenalty() ? rechargeable.maxCharges : 0;
    }

    public int GetStartingCharges()
    {
        return SupportsIgnoredObstaclePenalty()
            ? Mathf.Clamp(rechargeable.startingCharges, 0, rechargeable.maxCharges)
            : 0;
    }

    public bool CanRechargeOn(HexBoonRechargeTrigger trigger)
    {
        return SupportsIgnoredObstaclePenalty()
            && rechargeable.rechargeTrigger == trigger;
    }

    public int GetAdditionalPitstopCount()
    {
        if (!isEnabled
            || category != HexBoonCategory.MapModifying
            || mapModifier == null
            || mapModifier.effectType != HexBoonMapModifierType.AdditionalPitstops)
        {
            return 0;
        }

        return mapModifier.magnitude;
    }

    public HexBoonMapPlacementBand GetAdditionalPitstopPlacementBand()
    {
        if (GetAdditionalPitstopCount() <= 0 || mapModifier == null)
        {
            return HexBoonMapPlacementBand.Default;
        }

        return mapModifier.placementBand;
    }

    public HexMapGenerationModifiers GetMapGenerationModifiers()
    {
        if (!isEnabled || category != HexBoonCategory.MapModifying)
        {
            return HexMapGenerationModifiers.None;
        }

        return new HexMapGenerationModifiers(
            GetAdditionalPitstopCount(),
            GetAdditionalPitstopPlacementBand(),
            mapModifier.waterThresholdDelta,
            mapModifier.forestFeatureCountMultiplier,
            mapModifier.waterFeatureCountMultiplier,
            mapModifier.mountainFeatureCountMultiplier,
            mapModifier.forestFeatureSizeMultiplier,
            mapModifier.waterFeatureSizeMultiplier,
            mapModifier.mountainFeatureSizeMultiplier,
            mapModifier.terrainLandmarkRequests,
            mapModifier.mapObjectPlacementRequests);
    }

    private static string SanitizeId(string rawValue)
    {
        string normalized = string.IsNullOrWhiteSpace(rawValue) ? "boon" : rawValue.Trim().ToLowerInvariant();
        return normalized.Replace(' ', '-');
    }
}
