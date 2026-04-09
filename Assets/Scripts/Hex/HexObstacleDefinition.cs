using System.Collections.Generic;
using UnityEngine;

public enum HexObstaclePenaltyMode
{
    DirectDrain,
    FallbackDrainIfPrimaryUnavailable
}

[System.Serializable]
public sealed class HexObstacleBiomePreferences
{
    [Min(0f)] public float grassWeight = 1f;
    [Min(0f)] public float forestWeight = 1.15f;
    [Min(0f)] public float mountainWeight = 1f;
    [Min(0f)] public float waterWeight = 0f;
    [Min(0f)] public float desertWeight = 0.85f;

    public void Validate()
    {
        grassWeight = Mathf.Max(0f, grassWeight);
        forestWeight = Mathf.Max(0f, forestWeight);
        mountainWeight = Mathf.Max(0f, mountainWeight);
        waterWeight = Mathf.Max(0f, waterWeight);
        desertWeight = Mathf.Max(0f, desertWeight);
    }

    public float GetWeight(Biome biome)
    {
        return biome switch
        {
            Biome.grass => grassWeight,
            Biome.forest => forestWeight,
            Biome.mountain => mountainWeight,
            Biome.water => waterWeight,
            Biome.desert => desertWeight,
            _ => 1f
        };
    }
}

[System.Serializable]
public sealed class HexObstacleDefinition
{
    public string id = "wolves";
    public string displayName = "Wolves";
    public HexObstaclePenaltyMode penaltyMode = HexObstaclePenaltyMode.DirectDrain;
    public CaravanResourceType primaryResource = CaravanResourceType.Food;
    [Min(1)] public int primaryDrainAmount = 1;
    public CaravanResourceType fallbackResource = CaravanResourceType.Morale;
    [Min(1)] public int fallbackDrainAmount = 1;
    [Min(0f)] public float spawnWeight = 1f;
    public HexObstacleBiomePreferences biomePreferences = new();
    public Color visualColor = new(0.75f, 0.2f, 0.2f, 1f);
    [Min(0.1f)] public float visualScale = 0.24f;

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            id = displayName.ToLowerInvariant().Replace(' ', '-');
        }

        if (string.IsNullOrWhiteSpace(displayName))
        {
            displayName = id;
        }

        primaryDrainAmount = Mathf.Max(1, primaryDrainAmount);
        fallbackDrainAmount = Mathf.Max(1, fallbackDrainAmount);
        spawnWeight = Mathf.Max(0f, spawnWeight);
        visualScale = Mathf.Max(0.1f, visualScale);
        biomePreferences ??= new HexObstacleBiomePreferences();
        biomePreferences.Validate();
    }

    public float GetBiomeWeight(Biome biome)
    {
        return biomePreferences?.GetWeight(biome) ?? 1f;
    }

    public static List<HexObstacleDefinition> CreateDefaultSet()
    {
        return new List<HexObstacleDefinition>
        {
            new()
            {
                id = "wolves",
                displayName = "Wolves",
                penaltyMode = HexObstaclePenaltyMode.DirectDrain,
                primaryResource = CaravanResourceType.Food,
                primaryDrainAmount = 1,
                spawnWeight = 1.2f,
                biomePreferences = new HexObstacleBiomePreferences
                {
                    grassWeight = 1f,
                    forestWeight = 1.35f,
                    mountainWeight = 0.8f,
                    waterWeight = 0f,
                    desertWeight = 0.5f
                },
                visualColor = new Color(0.72f, 0.2f, 0.2f, 1f),
                visualScale = 0.22f
            },
            new()
            {
                id = "bandits",
                displayName = "Bandits",
                penaltyMode = HexObstaclePenaltyMode.FallbackDrainIfPrimaryUnavailable,
                primaryResource = CaravanResourceType.Gold,
                primaryDrainAmount = 1,
                fallbackResource = CaravanResourceType.Morale,
                fallbackDrainAmount = 1,
                spawnWeight = 1f,
                biomePreferences = new HexObstacleBiomePreferences
                {
                    grassWeight = 1.1f,
                    forestWeight = 0.9f,
                    mountainWeight = 1.1f,
                    waterWeight = 0f,
                    desertWeight = 1f
                },
                visualColor = new Color(0.88f, 0.68f, 0.12f, 1f),
                visualScale = 0.24f
            },
            new()
            {
                id = "ruined-caravan",
                displayName = "Ruined Caravan",
                penaltyMode = HexObstaclePenaltyMode.DirectDrain,
                primaryResource = CaravanResourceType.Morale,
                primaryDrainAmount = 1,
                spawnWeight = 0.9f,
                biomePreferences = new HexObstacleBiomePreferences
                {
                    grassWeight = 0.9f,
                    forestWeight = 1.05f,
                    mountainWeight = 1.15f,
                    waterWeight = 0f,
                    desertWeight = 0.95f
                },
                visualColor = new Color(0.42f, 0.45f, 0.5f, 1f),
                visualScale = 0.2f
            }
        };
    }
}
