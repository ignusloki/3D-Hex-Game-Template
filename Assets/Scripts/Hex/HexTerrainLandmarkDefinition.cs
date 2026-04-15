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
