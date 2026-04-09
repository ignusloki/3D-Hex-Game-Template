public sealed class HexBiomeMapResult
{
    public HexBiomeMapResult(Biome[,] biomeMap, HexCoordinates startCoordinates, HexCoordinates goalCoordinates)
    {
        BiomeMap = biomeMap;
        StartCoordinates = startCoordinates;
        GoalCoordinates = goalCoordinates;
    }

    public Biome[,] BiomeMap { get; }
    public HexCoordinates StartCoordinates { get; }
    public HexCoordinates GoalCoordinates { get; }
}
