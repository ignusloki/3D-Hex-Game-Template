using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class HexTerrainLandmarkCell
{
    public int axialQ;
    public int axialR;
    public Biome biome = Biome.water;
}

[Serializable]
public sealed class HexTerrainLandmarkPlacementRequest
{
    public bool isEnabled = true;
    public HexTerrainLandmarkDefinition definition;
    [Min(1)] public int count = 1;
    public HexBoonMapPlacementBand placementBand = HexBoonMapPlacementBand.Default;

    public bool IsUsable => isEnabled && definition != null && definition.isEnabled && definition.HasFootprint && count > 0;

    public void Validate()
    {
        count = Mathf.Max(1, count);
    }

    public HexTerrainLandmarkPlacementRequest Clone()
    {
        return new HexTerrainLandmarkPlacementRequest
        {
            isEnabled = isEnabled,
            definition = definition,
            count = count,
            placementBand = placementBand
        };
    }

    public string GetResolvedDisplayName()
    {
        return definition != null ? definition.GetResolvedDisplayName() : "Missing Terrain Landmark";
    }
}

[CreateAssetMenu(
    fileName = "TerrainLandmarkDefinition",
    menuName = "Hex/Map/Terrain Landmark Definition")]
public sealed class HexTerrainLandmarkDefinition : ScriptableObject
{
    public string id = "terrain-landmark";
    public string displayName = "Terrain Landmark";
    [TextArea(2, 4)] public string description = string.Empty;
    public bool isEnabled = true;
    [Min(0)] public int edgePadding = 1;
    [Min(0)] public int minDistanceFromStart = 2;
    [Min(0)] public int minDistanceFromGoal = 2;
    public Biome[] allowedBaseBiomes = Array.Empty<Biome>();
    public Biome[] forbiddenBaseBiomes = Array.Empty<Biome>();
    public HexTerrainLandmarkCell[] footprint = new[] { new HexTerrainLandmarkCell() };

    public bool HasFootprint => footprint != null && footprint.Length > 0;

    private void OnValidate()
    {
        Validate();
    }

    public void Validate()
    {
        displayName = string.IsNullOrWhiteSpace(displayName) ? "Terrain Landmark" : displayName.Trim();
        id = SanitizeId(string.IsNullOrWhiteSpace(id) ? displayName : id);
        description ??= string.Empty;
        edgePadding = Mathf.Max(0, edgePadding);
        minDistanceFromStart = Mathf.Max(0, minDistanceFromStart);
        minDistanceFromGoal = Mathf.Max(0, minDistanceFromGoal);
        allowedBaseBiomes ??= Array.Empty<Biome>();
        forbiddenBaseBiomes ??= Array.Empty<Biome>();

        if (footprint == null || footprint.Length == 0)
        {
            footprint = new[] { new HexTerrainLandmarkCell() };
        }

        for (int index = 0; index < footprint.Length; index++)
        {
            footprint[index] ??= new HexTerrainLandmarkCell();
        }
    }

    public string GetResolvedDisplayName()
    {
        return string.IsNullOrWhiteSpace(displayName) ? "Terrain Landmark" : displayName;
    }

    public bool IsBaseBiomeAllowed(Biome biome)
    {
        if (forbiddenBaseBiomes != null)
        {
            for (int index = 0; index < forbiddenBaseBiomes.Length; index++)
            {
                if (forbiddenBaseBiomes[index] == biome)
                {
                    return false;
                }
            }
        }

        if (allowedBaseBiomes == null || allowedBaseBiomes.Length == 0)
        {
            return true;
        }

        for (int index = 0; index < allowedBaseBiomes.Length; index++)
        {
            if (allowedBaseBiomes[index] == biome)
            {
                return true;
            }
        }

        return false;
    }

    private static string SanitizeId(string rawValue)
    {
        string normalized = string.IsNullOrWhiteSpace(rawValue) ? "terrain-landmark" : rawValue.Trim().ToLowerInvariant();
        return normalized.Replace(' ', '-');
    }
}

public enum HexMapObjectType
{
    Pitstop,
    Outpost,
    QuestMarker,
    RelicSite,
    Other
}

[Serializable]
public sealed class HexMapObjectPlacementRequest
{
    public bool isEnabled = true;
    public HexMapObjectDefinition definition;
    [Min(1)] public int count = 1;
    public HexBoonMapPlacementBand placementBand = HexBoonMapPlacementBand.Default;

    public bool IsUsable => isEnabled && definition != null && definition.isEnabled && count > 0;

    public void Validate()
    {
        count = Mathf.Max(1, count);
    }

    public HexMapObjectPlacementRequest Clone()
    {
        return new HexMapObjectPlacementRequest
        {
            isEnabled = isEnabled,
            definition = definition,
            count = count,
            placementBand = placementBand
        };
    }
}

[CreateAssetMenu(
    fileName = "MapObjectDefinition",
    menuName = "Hex/Map/Map Object Definition")]
public sealed class HexMapObjectDefinition : ScriptableObject
{
    public string id = "map-object";
    public string displayName = "Map Object";
    [TextArea(2, 4)] public string description = string.Empty;
    public bool isEnabled = true;
    public HexMapObjectType objectType = HexMapObjectType.Other;
    public GameObject prefab;
    [Min(0f)] public float visualHeightOffset;
    [Min(0.1f)] public float visualScale = 1f;
    public bool blocksPlacementOfOtherObjects = true;
    public bool blocksMovement;
    public string[] gameplayTags = Array.Empty<string>();

    private void OnValidate()
    {
        Validate();
    }

    public void Validate()
    {
        displayName = string.IsNullOrWhiteSpace(displayName) ? "Map Object" : displayName.Trim();
        id = SanitizeId(string.IsNullOrWhiteSpace(id) ? displayName : id);
        description ??= string.Empty;
        visualHeightOffset = Mathf.Max(0f, visualHeightOffset);
        visualScale = Mathf.Max(0.1f, visualScale);
        gameplayTags ??= Array.Empty<string>();
    }

    private static string SanitizeId(string rawValue)
    {
        string normalized = string.IsNullOrWhiteSpace(rawValue) ? "map-object" : rawValue.Trim().ToLowerInvariant();
        return normalized.Replace(' ', '-');
    }
}

public readonly struct HexMapObjectPlacement
{
    public HexMapObjectPlacement(
        HexMapObjectType objectType,
        HexCoordinates coordinates,
        string sourceId,
        string displayName,
        string variantId = "")
    {
        ObjectType = objectType;
        Coordinates = coordinates;
        SourceId = string.IsNullOrWhiteSpace(sourceId) ? objectType.ToString() : sourceId.Trim();
        DisplayName = string.IsNullOrWhiteSpace(displayName) ? SourceId : displayName.Trim();
        VariantId = variantId ?? string.Empty;
    }

    public HexMapObjectType ObjectType { get; }
    public HexCoordinates Coordinates { get; }
    public string SourceId { get; }
    public string DisplayName { get; }
    public string VariantId { get; }

    public bool TryGetPitstopKind(out PitstopKind kind)
    {
        if (ObjectType != HexMapObjectType.Pitstop)
        {
            kind = default;
            return false;
        }

        return Enum.TryParse(VariantId, out kind);
    }
}

public sealed class HexMapObjectPlacementCollection
{
    private readonly List<HexMapObjectPlacement> placements = new();

    public int Count => placements.Count;
    public IReadOnlyList<HexMapObjectPlacement> All => placements;

    public void Add(HexMapObjectPlacement placement)
    {
        placements.Add(placement);
    }

    public void AddRange(IEnumerable<HexMapObjectPlacement> items)
    {
        if (items == null)
        {
            return;
        }

        foreach (HexMapObjectPlacement item in items)
        {
            placements.Add(item);
        }
    }

    public List<HexMapObjectPlacement> GetByType(HexMapObjectType objectType)
    {
        List<HexMapObjectPlacement> filtered = new();
        for (int index = 0; index < placements.Count; index++)
        {
            if (placements[index].ObjectType == objectType)
            {
                filtered.Add(placements[index]);
            }
        }

        return filtered;
    }

    public HexMapObjectPlacementCollection Clone()
    {
        HexMapObjectPlacementCollection clone = new();
        clone.AddRange(placements);
        return clone;
    }
}

public sealed class HexMapObjectPlacementPlanResult
{
    public static HexMapObjectPlacementPlanResult Empty { get; } = new(
        new HexMapObjectPlacementCollection(),
        new HexMapPlacementReservations(),
        false,
        float.MinValue,
        0,
        "No map-object placements were generated.",
        string.Empty);

    public HexMapObjectPlacementPlanResult(
        HexMapObjectPlacementCollection placements,
        HexMapPlacementReservations placementReservations,
        bool isValid,
        float score,
        int attemptsUsed,
        string summary,
        string diagnosticsSummary)
    {
        Placements = placements?.Clone() ?? new HexMapObjectPlacementCollection();
        PlacementReservations = placementReservations?.Clone() ?? new HexMapPlacementReservations();
        IsValid = isValid;
        Score = score;
        AttemptsUsed = attemptsUsed;
        Summary = summary ?? string.Empty;
        DiagnosticsSummary = diagnosticsSummary ?? string.Empty;
    }

    public HexMapObjectPlacementCollection Placements { get; }
    public HexMapPlacementReservations PlacementReservations { get; }
    public bool IsValid { get; }
    public float Score { get; }
    public int AttemptsUsed { get; }
    public string Summary { get; }
    public string DiagnosticsSummary { get; }
}
