using UnityEngine;

[System.Serializable]
public sealed class HexSpecialTileSettings
{
    public bool enforceStartBiome = true;
    public Biome startBiome = Biome.grass;
    public bool enforceGoalBiome = true;
    public Biome goalBiome = Biome.grass;
}
