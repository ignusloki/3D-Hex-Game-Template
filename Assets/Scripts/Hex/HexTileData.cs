public sealed class HexTileData
{
    public HexCoordinates Coordinates { get; }
    public Biome Biome { get; }
    public HexScriptableObject Properties { get; }
    public int TravelCost { get; }
    public bool IsPassable { get; }
    public bool IsOccupied { get; private set; }

    public HexTileData(HexCoordinates coordinates, Biome biome, HexScriptableObject properties)
    {
        Coordinates = coordinates;
        Biome = biome;
        Properties = properties;
        TravelCost = properties != null ? properties.travelCost : 0;
        IsPassable = properties == null || properties.passable;
    }

    public bool IsAvailable => IsPassable && !IsOccupied;

    public void SetOccupied(bool occupied)
    {
        IsOccupied = occupied;
    }
}
