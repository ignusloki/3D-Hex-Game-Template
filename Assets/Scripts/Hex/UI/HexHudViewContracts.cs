public interface IHexHudView
{
    void SetStatusText(string value);
    void SetHintText(string value);
    void ShowInspectorEmptyState();
    void ShowBiomeInspector(HexInspectorBiomeDisplayData data);
    void ShowPitstopInspector(HexInspectorPitstopDisplayData data);
    void ShowGenericInspector(HexInspectorGenericDisplayData data);
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

public readonly struct HexInspectorPitstopDisplayData
{
    public HexInspectorPitstopDisplayData(
        string title,
        string type,
        string hex,
        string terrain,
        string travelCost,
        string working,
        string destroyed,
        string refuel,
        string repeatable,
        string visited,
        string description)
    {
        Title = string.IsNullOrWhiteSpace(title) ? "Pitstop" : title;
        Type = string.IsNullOrWhiteSpace(type) ? "Pitstop" : type;
        Hex = string.IsNullOrWhiteSpace(hex) ? "--" : hex;
        Terrain = string.IsNullOrWhiteSpace(terrain) ? "Unknown" : terrain;
        TravelCost = string.IsNullOrWhiteSpace(travelCost) ? "--" : travelCost;
        Working = string.IsNullOrWhiteSpace(working) ? "No" : working;
        Destroyed = string.IsNullOrWhiteSpace(destroyed) ? "No" : destroyed;
        Refuel = string.IsNullOrWhiteSpace(refuel) ? "No" : refuel;
        Repeatable = string.IsNullOrWhiteSpace(repeatable) ? "No" : repeatable;
        Visited = string.IsNullOrWhiteSpace(visited) ? "No" : visited;
        Description = string.IsNullOrWhiteSpace(description) ? "The caravan pauses at a roadside stop." : description;
    }

    public string Title { get; }
    public string Type { get; }
    public string Hex { get; }
    public string Terrain { get; }
    public string TravelCost { get; }
    public string Working { get; }
    public string Destroyed { get; }
    public string Refuel { get; }
    public string Repeatable { get; }
    public string Visited { get; }
    public string Description { get; }
}

public readonly struct HexInspectorGenericDisplayData
{
    public HexInspectorGenericDisplayData(
        string title,
        string subtitle,
        string hex,
        string terrain,
        string travelCost,
        string description)
    {
        Title = string.IsNullOrWhiteSpace(title) ? "Selected Hex" : title;
        Subtitle = string.IsNullOrWhiteSpace(subtitle) ? "Context" : subtitle;
        Hex = string.IsNullOrWhiteSpace(hex) ? "--" : hex;
        Terrain = string.IsNullOrWhiteSpace(terrain) ? "Unknown" : terrain;
        TravelCost = string.IsNullOrWhiteSpace(travelCost) ? "--" : travelCost;
        Description = string.IsNullOrWhiteSpace(description) ? "No additional details." : description;
    }

    public string Title { get; }
    public string Subtitle { get; }
    public string Hex { get; }
    public string Terrain { get; }
    public string TravelCost { get; }
    public string Description { get; }
}
