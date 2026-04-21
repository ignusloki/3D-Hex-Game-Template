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

public enum HexBoonNemesisStartLocationOverride
{
    None,
    GoalHex
}

public enum HexBoonNemesisVisibilityMode
{
    None,
    Hidden,
    Obscured
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
    [Header("Land Biome Weight Multipliers")]
    [Min(0f)] public float grassRegionWeightMultiplier = 1f;
    [Min(0f)] public float forestRegionWeightMultiplier = 1f;
    [Min(0f)] public float desertRegionWeightMultiplier = 1f;
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
        grassRegionWeightMultiplier = Mathf.Max(0f, grassRegionWeightMultiplier);
        forestRegionWeightMultiplier = Mathf.Max(0f, forestRegionWeightMultiplier);
        desertRegionWeightMultiplier = Mathf.Max(0f, desertRegionWeightMultiplier);
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

[System.Serializable]
public sealed class HexBoonNemesisVisibilityRuleData
{
    public HexBoonNemesisVisibilityMode visibilityMode = HexBoonNemesisVisibilityMode.None;
    public Biome[] affectedBiomes = System.Array.Empty<Biome>();

    public void Validate()
    {
        affectedBiomes ??= System.Array.Empty<Biome>();
    }

    public bool IsUsable =>
        visibilityMode != HexBoonNemesisVisibilityMode.None
        && affectedBiomes != null
        && affectedBiomes.Length > 0;
}

[System.Serializable]
public sealed class HexBoonNemesisModifierData
{
    [Header("Pace Overrides")]
    [Min(0)] public int caravanMovesPerActivationOverride;
    [Min(0)] public int stepsPerActivationOverride;
    [Header("Position Overrides")]
    public HexBoonNemesisStartLocationOverride startLocationOverride = HexBoonNemesisStartLocationOverride.None;
    [Min(0)] public int startStepsTowardCaravanSpawn;
    [Header("Visibility Overrides")]
    public HexBoonNemesisVisibilityRuleData[] visibilityRules = System.Array.Empty<HexBoonNemesisVisibilityRuleData>();

    public void Validate()
    {
        caravanMovesPerActivationOverride = Mathf.Max(0, caravanMovesPerActivationOverride);
        stepsPerActivationOverride = Mathf.Max(0, stepsPerActivationOverride);
        startStepsTowardCaravanSpawn = Mathf.Max(0, startStepsTowardCaravanSpawn);
        visibilityRules ??= System.Array.Empty<HexBoonNemesisVisibilityRuleData>();

        for (int index = 0; index < visibilityRules.Length; index++)
        {
            visibilityRules[index]?.Validate();
        }
    }

    public HexNemesisVisibilityRule[] GetRuntimeVisibilityRules()
    {
        if (visibilityRules == null || visibilityRules.Length == 0)
        {
            return System.Array.Empty<HexNemesisVisibilityRule>();
        }

        System.Collections.Generic.List<HexNemesisVisibilityRule> rules = new(visibilityRules.Length);
        for (int index = 0; index < visibilityRules.Length; index++)
        {
            HexBoonNemesisVisibilityRuleData rule = visibilityRules[index];
            if (rule == null || !rule.IsUsable)
            {
                continue;
            }

            rules.Add(new HexNemesisVisibilityRule(rule.visibilityMode, rule.affectedBiomes));
        }

        return rules.Count == 0 ? System.Array.Empty<HexNemesisVisibilityRule>() : rules.ToArray();
    }
}

[System.Serializable]
public sealed class HexBoonActStartGrantData
{
    public int food;
    public int morale;
    public int gold;

    public void Validate()
    {
    }

    public bool HasAny => food != 0 || morale != 0 || gold != 0;

    public CaravanResourceSnapshot ApplyTo(CaravanResourceSnapshot snapshot)
    {
        return new CaravanResourceSnapshot(
            snapshot.Food + food,
            snapshot.Morale + morale,
            snapshot.Gold + gold);
    }
}

[System.Serializable]
public sealed class HexBoonKeywordPresentationData
{
    public string label = string.Empty;
    [TextArea(2, 3)] public string explanation = string.Empty;

    public void Validate()
    {
        label = string.IsNullOrWhiteSpace(label) ? string.Empty : label.Trim();
        explanation = string.IsNullOrWhiteSpace(explanation) ? string.Empty : explanation.Trim();
    }

    public bool IsUsable => !string.IsNullOrWhiteSpace(label);
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

    [Header("Presentation")]
    public string cardSummary = string.Empty;
    public HexBoonKeywordPresentationData[] keywords = System.Array.Empty<HexBoonKeywordPresentationData>();

    [Header("Passive")]
    public HexBoonPassiveEffectData passive = new();

    [Header("Rechargeable")]
    public HexBoonRechargeableEffectData rechargeable = new();

    [Header("Map Modifiers")]
    public HexBoonMapModifierData mapModifier = new();

    [Header("Nemesis Modifiers")]
    public HexBoonNemesisModifierData nemesisModifier = new();

    [Header("Act Start Grant")]
    public HexBoonActStartGrantData actStartGrant = new();

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
        cardSummary = string.IsNullOrWhiteSpace(cardSummary) ? string.Empty : cardSummary.Trim();
        keywords ??= System.Array.Empty<HexBoonKeywordPresentationData>();
        passive ??= new HexBoonPassiveEffectData();
        rechargeable ??= new HexBoonRechargeableEffectData();
        mapModifier ??= new HexBoonMapModifierData();
        nemesisModifier ??= new HexBoonNemesisModifierData();
        actStartGrant ??= new HexBoonActStartGrantData();

        for (int index = 0; index < keywords.Length; index++)
        {
            keywords[index]?.Validate();
        }

        passive.Validate();
        rechargeable.Validate();
        mapModifier.Validate();
        nemesisModifier.Validate();
        actStartGrant.Validate();
    }

    public string GetResolvedDisplayName()
    {
        return string.IsNullOrWhiteSpace(displayName) ? "Boon" : displayName;
    }

    public string GetCardSummary()
    {
        if (!string.IsNullOrWhiteSpace(cardSummary))
        {
            return cardSummary.Trim();
        }

        string keywordLine = GetKeywordLine();
        return string.IsNullOrWhiteSpace(keywordLine) ? "No boon summary listed." : keywordLine;
    }

    public string GetKeywordLine()
    {
        if (keywords == null || keywords.Length == 0)
        {
            return BuildFallbackKeywordLine();
        }

        System.Collections.Generic.List<string> labels = new();
        for (int index = 0; index < keywords.Length; index++)
        {
            HexBoonKeywordPresentationData keyword = keywords[index];
            if (keyword == null || !keyword.IsUsable)
            {
                continue;
            }

            labels.Add(keyword.label.Trim());
        }

        return labels.Count == 0 ? BuildFallbackKeywordLine() : string.Join(", ", labels);
    }

    public string GetKeywordExplanationText()
    {
        if (keywords == null || keywords.Length == 0)
        {
            return string.Empty;
        }

        System.Collections.Generic.List<string> lines = new();
        for (int index = 0; index < keywords.Length; index++)
        {
            HexBoonKeywordPresentationData keyword = keywords[index];
            if (keyword == null || !keyword.IsUsable)
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(keyword.explanation))
            {
                lines.Add(keyword.label.Trim());
            }
            else
            {
                lines.Add($"{keyword.label.Trim()}: {keyword.explanation.Trim()}");
            }
        }

        return lines.Count == 0 ? string.Empty : string.Join("\n", lines);
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
            mapModifier.grassRegionWeightMultiplier,
            mapModifier.forestRegionWeightMultiplier,
            mapModifier.desertRegionWeightMultiplier,
            mapModifier.terrainLandmarkRequests,
            mapModifier.mapObjectPlacementRequests);
    }

    public HexNemesisRuntimeModifiers GetNemesisRuntimeModifiers()
    {
        if (!isEnabled || nemesisModifier == null)
        {
            return HexNemesisRuntimeModifiers.None;
        }

        return new HexNemesisRuntimeModifiers(
            archetypeFamily,
            nemesisModifier.caravanMovesPerActivationOverride,
            nemesisModifier.stepsPerActivationOverride,
            nemesisModifier.startLocationOverride,
            nemesisModifier.startStepsTowardCaravanSpawn,
            nemesisModifier.GetRuntimeVisibilityRules());
    }

    public bool HasActStartGrant()
    {
        return isEnabled && actStartGrant != null && actStartGrant.HasAny;
    }

    public CaravanResourceSnapshot ApplyActStartGrant(CaravanResourceSnapshot snapshot)
    {
        if (!HasActStartGrant())
        {
            return snapshot;
        }

        return actStartGrant.ApplyTo(snapshot);
    }

    private static string SanitizeId(string rawValue)
    {
        string normalized = string.IsNullOrWhiteSpace(rawValue) ? "boon" : rawValue.Trim().ToLowerInvariant();
        return normalized.Replace(' ', '-');
    }

    private string BuildFallbackKeywordLine()
    {
        if (HasActStartGrant())
        {
            bool hasPursuitPenalty = nemesisModifier != null && nemesisModifier.startStepsTowardCaravanSpawn > 0;
            return hasPursuitPenalty ? "Supply Surge, Pursuit" : "Supply Surge";
        }

        if (GetVisibilityRadiusBonus() > 0)
        {
            return "Hazard Sight";
        }

        if (SupportsIgnoredObstaclePenalty())
        {
            return CanRechargeOn(HexBoonRechargeTrigger.PitstopArrival) ? "Detour, Recharge" : "Detour";
        }

        if (GetAdditionalPitstopCount() > 0)
        {
            return "Pitstop Expansion";
        }

        bool hasGrassGrowth = mapModifier != null && mapModifier.grassRegionWeightMultiplier > 1.05f;
        bool hasVisibilityRule = nemesisModifier != null
            && nemesisModifier.visibilityRules != null
            && nemesisModifier.visibilityRules.Length > 0;
        if (hasGrassGrowth && hasVisibilityRule)
        {
            return "Growth, Concealment";
        }

        bool slowsNemesis = nemesisModifier != null && nemesisModifier.stepsPerActivationOverride > 0;
        bool startsOnGoal = nemesisModifier != null && nemesisModifier.startLocationOverride == HexBoonNemesisStartLocationOverride.GoalHex;
        if (slowsNemesis && startsOnGoal)
        {
            return "Slow Pursuit, Guarded Goal";
        }

        return string.Empty;
    }
}
