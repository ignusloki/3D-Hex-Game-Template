using System.Collections.Generic;

public sealed class HexHudPresenter
{
    private readonly IHexHudView runtimeView;

    public HexHudPresenter(IHexHudView runtimeView)
    {
        this.runtimeView = runtimeView;
    }

    public void ShowAwaitingStart()
    {
        SetStatusText("Select a departure tile.");
        ClearHintText();
    }

    public void ShowCaravanIdle(HexagonTile caravanTile)
    {
        if (caravanTile == null)
        {
            SetStatusText("Caravan ready.");
            ClearHintText();
            return;
        }

        SetStatusText(
            $"Caravan ready at {FormatCoordinates(caravanTile.Coordinates)}.");
        ClearHintText();
    }

    public void ShowInspectingTile(HexagonTile tile)
    {
        if (tile == null)
        {
            ShowCaravanIdle(null);
            return;
        }

        SetStatusText(
            $"Inspecting {FormatCoordinates(tile.Coordinates)} ({FormatBiome(tile.TileData?.Biome ?? Biome.grass)}).");
        ClearHintText();
    }

    public void ShowInspectingUnknownTile(HexagonTile tile)
    {
        if (tile == null)
        {
            ShowCaravanIdle(null);
            return;
        }

        SetStatusText(
            $"Inspecting {FormatCoordinates(tile.Coordinates)} (Unknown).");
        ClearHintText();
    }

    public void ShowCaravanSelected(HexagonTile caravanTile)
    {
        if (caravanTile == null)
        {
            ShowCaravanIdle(null);
            return;
        }

        SetStatusText(
            $"Caravan selected at {FormatCoordinates(caravanTile.Coordinates)}.");
        ClearHintText();
    }

    public void ShowAwaitingDestination(HexagonTile startTile)
    {
        if (startTile == null)
        {
            ShowAwaitingStart();
            return;
        }

        SetStatusText(
            $"Origin: {FormatCoordinates(startTile.Coordinates)} ({FormatBiome(startTile.TileData?.Biome ?? Biome.grass)})");
        ClearHintText();
    }

    public void ShowPathPreview(IReadOnlyList<HexTileData> path)
    {
        if (path == null || path.Count == 0)
        {
            ShowNoPath();
            return;
        }

        HexTileData destination = path[path.Count - 1];
        int stepCount = path.Count - 1;
        int totalTravelCost = HexPathMetrics.GetTravelCost(path);

        SetStatusText(
            $"Route ready: {stepCount} steps to {FormatCoordinates(destination.Coordinates)}.");
        ClearHintText();
    }

    public void ShowDestinationPreview(HexagonTile destinationTile, IReadOnlyList<HexTileData> path)
    {
        if (destinationTile == null || path == null || path.Count == 0)
        {
            ShowNoPath();
            return;
        }

        int stepCount = path.Count - 1;
        int totalTravelCost = HexPathMetrics.GetTravelCost(path);

        SetStatusText(
            $"Previewing route to {FormatCoordinates(destinationTile.Coordinates)}.");
        ClearHintText();
    }

    public void ShowUnreachableDestination(HexagonTile destinationTile)
    {
        if (destinationTile == null)
        {
            ShowNoPath();
            return;
        }

        SetStatusText(
            $"No route to {FormatCoordinates(destinationTile.Coordinates)}.");
        ClearHintText();
    }

    public void ShowOutOfRangeDestination(HexagonTile destinationTile)
    {
        if (destinationTile == null)
        {
            ShowNoPath();
            return;
        }

        SetStatusText(
            $"Tile {FormatCoordinates(destinationTile.Coordinates)} is outside caravan range.");
        ClearHintText();
    }

    public void ShowNemesisBlockedDestination(HexagonTile destinationTile)
    {
        if (destinationTile == null)
        {
            ShowNoPath();
            return;
        }

        SetStatusText(
            $"The Echo seals {FormatCoordinates(destinationTile.Coordinates)}. Choose another route.");
        ClearHintText();
    }

    public void ShowInsufficientResources(HexagonTile destinationTile, int travelCost, int remainingResources)
    {
        if (destinationTile == null)
        {
            ShowNoPath();
            return;
        }

        SetStatusText(
            $"Not enough food for {FormatCoordinates(destinationTile.Coordinates)}.");
        ClearHintText();
    }

    public void ShowMoveComplete(HexagonTile tile, int travelCost, CaravanResourceSnapshot resources)
    {
        if (tile == null)
        {
            ShowCaravanIdle(null);
            return;
        }

        SetStatusText(
            $"Caravan moved to {FormatCoordinates(tile.Coordinates)}.");
        ClearHintText();
    }

    public void ShowNemesisUpdate(HexagonTile tile, HexNemesisTurnResult turnResult)
    {
        if (tile == null || turnResult == null || !turnResult.Active)
        {
            ShowCaravanIdle(tile);
            return;
        }

        if (turnResult.DestroyedPitstops.Count > 0)
        {
            HexCoordinates coordinates = turnResult.DestroyedPitstops[0].Coordinates;
            SetStatusText($"Corruptor ruined a pitstop at {FormatCoordinates(coordinates)}.");
            ClearHintText();
            return;
        }

        if (turnResult.Archetype == HexNemesisArchetype.Echo && turnResult.EchoBlockedCoordinates.HasValue)
        {
            SetStatusText($"Echo seals {FormatCoordinates(turnResult.EchoBlockedCoordinates.Value)} behind the caravan.");
            ClearHintText();
            return;
        }

        if (turnResult.CurrentCoordinates.HasValue)
        {
            if (turnResult.CurrentCoordinatesVisibilityMode == HexBoonNemesisVisibilityMode.Hidden)
            {
                SetStatusText($"{FormatNemesisArchetype(turnResult.Archetype)} slips out of sight.");
            }
            else if (turnResult.CurrentCoordinatesVisibilityMode == HexBoonNemesisVisibilityMode.Obscured)
            {
                SetStatusText($"{FormatNemesisArchetype(turnResult.Archetype)} is obscured at {FormatCoordinates(turnResult.CurrentCoordinates.Value)}.");
            }
            else
            {
                SetStatusText($"{FormatNemesisArchetype(turnResult.Archetype)} advances to {FormatCoordinates(turnResult.CurrentCoordinates.Value)}.");
            }

            ClearHintText();
            return;
        }

        ShowMoveComplete(tile, 0, default);
    }

    public void ShowPitstopEvent(HexagonTile tile, PitstopEventResult eventResult, CaravanResourceSnapshot resources)
    {
        if (tile == null || eventResult == null || eventResult.Site == null)
        {
            ShowCaravanIdle(tile);
            return;
        }

        if (!eventResult.Triggered)
        {
            ShowPitstopRevisit(tile, eventResult.Site, resources);
            return;
        }

        runtimeView?.ShowPitstopInspector(BuildPitstopDisplayData(tile, eventResult.Site));
        SetStatusText(
            $"{eventResult.Title} at {FormatCoordinates(tile.Coordinates)}.");
        ClearHintText();
    }

    public void ShowPitstopChoiceResolved(HexagonTile tile, PitstopEventResult eventResult, CaravanResourceSnapshot resources)
    {
        if (tile == null || eventResult == null || eventResult.Site == null)
        {
            ShowCaravanIdle(tile);
            return;
        }

        string title = eventResult.Title;
        if (eventResult.SelectedOption != null)
        {
            title = $"{title}: {eventResult.SelectedOption.label}";
        }

        runtimeView?.ShowPitstopInspector(BuildPitstopDisplayData(tile, eventResult.Site));
        SetStatusText(title);
        ClearHintText();
    }

    public void ShowPitstopRevisit(HexagonTile tile, PitstopSite site, CaravanResourceSnapshot resources)
    {
        if (tile == null || site == null)
        {
            ShowCaravanIdle(tile);
            return;
        }

        runtimeView?.ShowPitstopInspector(BuildPitstopDisplayData(tile, site));
        SetStatusText(
            $"Returned to {site.EventTitle} at {FormatCoordinates(tile.Coordinates)}.");
        ClearHintText();
    }

    public void ShowObstacleEncounter(HexagonTile tile, HexObstacleContactResult contactResult, CaravanResourceSnapshot resources)
    {
        if (tile == null || !contactResult.HasContact || contactResult.Obstacle == null)
        {
            ShowCaravanIdle(tile);
            return;
        }

        string resourceLabel = FormatResourceType(contactResult.AffectedResource);
        string fallbackText = contactResult.UsedFallbackResource ? " Fallback penalty applied." : string.Empty;

        SetStatusText(
            $"{contactResult.Obstacle.Definition.displayName} struck at {FormatCoordinates(tile.Coordinates)}.");
        ClearHintText();
    }

    public void ShowVictory(HexagonTile tile, CaravanResourceSnapshot resources)
    {
        if (tile == null)
        {
            SetStatusText("Goal reached.");
            ClearHintText();
            return;
        }

        SetStatusText(
            $"Goal reached at {FormatCoordinates(tile.Coordinates)}.");
        ClearHintText();
    }

    public void ShowDefeat(HexagonTile tile, string defeatReason)
    {
        if (tile == null)
        {
            SetStatusText(defeatReason);
            ClearHintText();
            return;
        }

        SetStatusText(
            $"{defeatReason} at {FormatCoordinates(tile.Coordinates)}.");
        ClearHintText();
    }

    public void ShowNoPath()
    {
        SetStatusText("No route found for that pair of tiles.");
        ClearHintText();
    }

    public void ShowTileDetails(HexagonTile tile, PitstopSite pitstopSite = null, HexObstacleInstance visibleObstacle = null, string extraDetails = null)
    {
        if (tile == null)
        {
            ResetTileDetails();
            return;
        }

        HexTileData tileData = tile.TileData;
        string biome = FormatBiome(tileData?.Biome ?? Biome.grass);
        string travelCost = (tileData?.TravelCost ?? tile.travelCost).ToString();

        if (pitstopSite != null && visibleObstacle == null && string.IsNullOrWhiteSpace(extraDetails))
        {
            runtimeView?.ShowPitstopInspector(BuildPitstopDisplayData(tile, pitstopSite));
            return;
        }

        if (pitstopSite == null && visibleObstacle == null && string.IsNullOrWhiteSpace(extraDetails))
        {
            runtimeView?.ShowBiomeInspector(new HexInspectorBiomeDisplayData(
                FormatCoordinates(tile.Coordinates),
                biome,
                travelCost,
                FormatBiomeDescription(tileData?.Biome ?? Biome.grass)));
            return;
        }

        runtimeView?.ShowGenericInspector(BuildGenericTileDisplayData(
            tile,
            biome,
            travelCost,
            pitstopSite,
            visibleObstacle,
            extraDetails));
    }

    public void ShowUnknownTileDetails(HexagonTile tile, PitstopSite pitstopSite = null, string extraDetails = null)
    {
        if (tile == null)
        {
            ResetTileDetails();
            return;
        }

        runtimeView?.ShowGenericInspector(BuildUnknownTileDisplayData(tile, pitstopSite, extraDetails));
    }

    public void ResetTileDetails()
    {
        runtimeView?.ShowInspectorEmptyState();
    }

    private void SetStatusText(string value)
    {
        runtimeView?.SetStatusText(value);
    }

    private void SetHintText(string value)
    {
        runtimeView?.SetHintText(value);
    }

    private void ClearHintText()
    {
        SetHintText(string.Empty);
    }

    public void ShowHint(string message)
    {
        SetHintText(string.IsNullOrWhiteSpace(message) ? string.Empty : message);
    }

    private static string FormatCoordinates(HexCoordinates coordinates)
    {
        return $"{coordinates.Row},{coordinates.Column}";
    }

    private static string FormatBiome(Biome biome)
    {
        string raw = biome.ToString();
        return char.ToUpperInvariant(raw[0]) + raw[1..];
    }

    private static string FormatBiomeDescription(Biome biome)
    {
        return biome switch
        {
            Biome.forest => "Dense woodland. Travel is slower here.",
            Biome.mountain => "Broken highland terrain. Crossing it costs more.",
            Biome.water => "Open water blocks normal caravan travel.",
            Biome.desert => "Dry open ground. The route is exposed and demanding.",
            _ => "Open grassland. Travel is straightforward here."
        };
    }

    private static HexInspectorPitstopDisplayData BuildPitstopDisplayData(HexagonTile tile, PitstopSite pitstopSite)
    {
        HexTileData tileData = tile?.TileData;
        Biome biome = tileData?.Biome ?? Biome.grass;
        string title = string.IsNullOrWhiteSpace(pitstopSite.EventTitle) ? FormatPitstopKindName(pitstopSite.Kind) : pitstopSite.EventTitle;
        string description = string.IsNullOrWhiteSpace(pitstopSite.SpecialEventDescription)
            || string.Equals(pitstopSite.SpecialEventDescription, "placeholder", System.StringComparison.OrdinalIgnoreCase)
                ? "The caravan pauses at a roadside stop."
                : pitstopSite.SpecialEventDescription;

        return new HexInspectorPitstopDisplayData(
            title,
            $"Pitstop • {FormatPitstopKindName(pitstopSite.Kind)}",
            FormatCoordinates(tile.Coordinates),
            FormatBiome(biome),
            (tileData?.TravelCost ?? tile.travelCost).ToString(),
            pitstopSite.IsDestroyed ? "No" : "Yes",
            pitstopSite.IsDestroyed ? "Yes" : "No",
            pitstopSite.HasRefuelPoint ? "Yes" : "No",
            pitstopSite.Repeatable ? "Yes" : "No",
            pitstopSite.Visited ? "Yes" : "No",
            description);
    }

    private static string FormatPitstopKindName(PitstopKind kind)
    {
        string raw = kind.ToString();
        System.Text.StringBuilder builder = new(raw.Length + 4);
        for (int index = 0; index < raw.Length; index++)
        {
            char character = raw[index];
            if (index > 0 && char.IsUpper(character))
            {
                builder.Append(' ');
            }

            builder.Append(character);
        }

        return builder.ToString();
    }

    private static HexInspectorGenericDisplayData BuildGenericTileDisplayData(
        HexagonTile tile,
        string biome,
        string travelCost,
        PitstopSite pitstopSite,
        HexObstacleInstance visibleObstacle,
        string extraDetails)
    {
        string title = pitstopSite != null
            ? ResolvePitstopTitle(pitstopSite)
            : biome;
        string subtitle = visibleObstacle != null
            ? "Hazard Context"
            : pitstopSite != null
                ? "Pitstop Context"
                : "Selected Hex";

        List<string> descriptions = new();
        if (visibleObstacle != null)
        {
            descriptions.Add(FormatObstacleSummary(visibleObstacle));
        }

        if (pitstopSite != null)
        {
            descriptions.Add($"Pitstop: {ResolvePitstopTitle(pitstopSite)}.");
        }

        AddExtraDescriptionLines(descriptions, extraDetails);
        if (descriptions.Count == 0)
        {
            descriptions.Add(FormatBiomeDescription(tile?.TileData?.Biome ?? Biome.grass));
        }

        return new HexInspectorGenericDisplayData(
            title,
            subtitle,
            FormatCoordinates(tile.Coordinates),
            biome,
            travelCost,
            string.Join(" ", descriptions));
    }

    private static HexInspectorGenericDisplayData BuildUnknownTileDisplayData(HexagonTile tile, PitstopSite pitstopSite, string extraDetails)
    {
        List<string> descriptions = new()
        {
            "This hex is outside caravan sight."
        };

        if (pitstopSite != null)
        {
            descriptions.Add($"Possible pitstop: {ResolvePitstopTitle(pitstopSite)}.");
        }

        AddExtraDescriptionLines(descriptions, extraDetails);

        return new HexInspectorGenericDisplayData(
            "Unknown",
            "Unseen Hex",
            FormatCoordinates(tile.Coordinates),
            "Unknown",
            "Unknown",
            string.Join(" ", descriptions));
    }

    private static string FormatObstacleSummary(HexObstacleInstance visibleObstacle)
    {
        if (visibleObstacle == null || visibleObstacle.Definition == null)
        {
            return "Hazard details are unavailable.";
        }

        HexObstacleDefinition definition = visibleObstacle.Definition;
        string resourceLabel = FormatResourceType(definition.primaryResource);
        string summary = $"{definition.displayName}: -{definition.primaryDrainAmount} {resourceLabel}";
        if (definition.penaltyMode == HexObstaclePenaltyMode.FallbackDrainIfPrimaryUnavailable)
        {
            summary += $"; fallback -{definition.fallbackDrainAmount} {FormatResourceType(definition.fallbackResource)}";
        }

        return $"{summary}.";
    }

    private static void AddExtraDescriptionLines(List<string> descriptions, string extraDetails)
    {
        if (string.IsNullOrWhiteSpace(extraDetails))
        {
            return;
        }

        string[] lines = extraDetails.Split('\n');
        for (int index = 0; index < lines.Length; index++)
        {
            string line = lines[index]?.Trim();
            if (!string.IsNullOrWhiteSpace(line))
            {
                descriptions.Add(line.EndsWith(".", System.StringComparison.Ordinal) ? line : $"{line}.");
            }
        }
    }

    private static string ResolvePitstopTitle(PitstopSite pitstopSite)
    {
        if (pitstopSite == null)
        {
            return "Pitstop";
        }

        return string.IsNullOrWhiteSpace(pitstopSite.EventTitle)
            ? FormatPitstopKindName(pitstopSite.Kind)
            : pitstopSite.EventTitle;
    }

    private static string FormatResourceType(CaravanResourceType resourceType)
    {
        return resourceType switch
        {
            CaravanResourceType.Food => "Food",
            CaravanResourceType.Morale => "Morale",
            CaravanResourceType.Gold => "Gold",
            _ => resourceType.ToString()
        };
    }

    private static string FormatResourceSummary(CaravanResourceSnapshot resources)
    {
        return $"Food {resources.Food}, Morale {resources.Morale}, Gold {resources.Gold}";
    }

    private static string FormatNemesisArchetype(HexNemesisArchetype archetype)
    {
        string raw = archetype.ToString();
        return char.ToUpperInvariant(raw[0]) + raw[1..];
    }
}
