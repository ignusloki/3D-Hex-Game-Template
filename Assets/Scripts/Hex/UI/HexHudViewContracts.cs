public interface IHexHudView
{
    void SetStatusText(string value);
    void SetTileDetailsText(string value);
    void SetHintText(string value);
    void SetPitstopInfoText(string value);
    void ShowInspectorEmptyState();
    void ShowBiomeInspector(HexInspectorBiomeDisplayData data);
}

public interface IHexTravelTimeView
{
    void SetTravelTimeText(string value);
}

public readonly struct HexInspectorBiomeDisplayData
{
    public HexInspectorBiomeDisplayData(string hex, string biome, string travelCost, string description)
    {
        Hex = string.IsNullOrWhiteSpace(hex) ? "--" : hex;
        Biome = string.IsNullOrWhiteSpace(biome) ? "Unknown" : biome;
        TravelCost = string.IsNullOrWhiteSpace(travelCost) ? "--" : travelCost;
        Description = string.IsNullOrWhiteSpace(description) ? "No additional terrain details." : description;
    }

    public string Hex { get; }
    public string Biome { get; }
    public string TravelCost { get; }
    public string Description { get; }
}
