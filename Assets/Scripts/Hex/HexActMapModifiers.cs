public readonly struct HexActMapModifiers
{
    public static HexActMapModifiers None { get; } = new(0, HexBoonMapPlacementBand.Default);

    public HexActMapModifiers(int extraPitstopCount, HexBoonMapPlacementBand extraPitstopPlacementBand)
    {
        ExtraPitstopCount = extraPitstopCount < 0 ? 0 : extraPitstopCount;
        ExtraPitstopPlacementBand = extraPitstopPlacementBand;
    }

    public int ExtraPitstopCount { get; }
    public HexBoonMapPlacementBand ExtraPitstopPlacementBand { get; }
    public bool HasAny => ExtraPitstopCount > 0;
}
