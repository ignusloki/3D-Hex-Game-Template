public sealed class HexBiomeMapQualityReport
{
    public HexBiomeMapQualityReport(
        bool isAcceptable,
        float score,
        Biome dominantBiome,
        float dominantBiomeRatio,
        int distinctBiomeCount,
        float largestSecondaryRegionRatio,
        float smallSecondaryRegionRatio)
    {
        IsAcceptable = isAcceptable;
        Score = score;
        DominantBiome = dominantBiome;
        DominantBiomeRatio = dominantBiomeRatio;
        DistinctBiomeCount = distinctBiomeCount;
        LargestSecondaryRegionRatio = largestSecondaryRegionRatio;
        SmallSecondaryRegionRatio = smallSecondaryRegionRatio;
    }

    public bool IsAcceptable { get; }
    public float Score { get; }
    public Biome DominantBiome { get; }
    public float DominantBiomeRatio { get; }
    public int DistinctBiomeCount { get; }
    public float LargestSecondaryRegionRatio { get; }
    public float SmallSecondaryRegionRatio { get; }
}
