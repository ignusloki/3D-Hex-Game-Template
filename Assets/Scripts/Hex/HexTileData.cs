public sealed class HexTileData
{
    public HexCoordinates Coordinates { get; }
    public Biome Biome { get; private set; }
    public HexScriptableObject Properties { get; private set; }
    public int TravelCost { get; private set; }
    public bool IsPassable { get; private set; }
    public bool IsOccupied { get; private set; }

    public HexTileData(HexCoordinates coordinates, Biome biome, HexScriptableObject properties)
    {
        Coordinates = coordinates;
        ApplyBiome(biome, properties);
    }

    public bool IsAvailable => IsPassable && !IsOccupied;

    public void ApplyBiome(Biome biome, HexScriptableObject properties)
    {
        Biome = biome;
        Properties = properties;
        TravelCost = properties != null ? properties.travelCost : 0;
        IsPassable = properties == null || properties.passable;
    }

    public void SetOccupied(bool occupied)
    {
        IsOccupied = occupied;
    }
}
