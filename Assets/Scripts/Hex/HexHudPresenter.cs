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

        string details = $"Tile {FormatCoordinates(tile.Coordinates)}\nTerrain: {biome}\nTravel Cost: {travelCost}";
        details = AppendObstacleDetails(details, visibleObstacle);
        details = AppendExtraDetails(details, extraDetails);

        SetTileDetailsText(details);
        SetPitstopInfoText(FormatPitstopPanel(pitstopSite));
    }

    public void ShowUnknownTileDetails(HexagonTile tile, PitstopSite pitstopSite = null, string extraDetails = null)
    {
        if (tile == null)
        {
            ResetTileDetails();
            return;
        }

        string details = $"Tile {FormatCoordinates(tile.Coordinates)}\nTerrain: Unknown\nTravel Cost: Unknown\nVisibility: Unseen";
        details = AppendExtraDetails(details, extraDetails);
        SetTileDetailsText(details);
        SetPitstopInfoText(FormatPitstopPanel(pitstopSite));
    }

    public void ResetTileDetails()
    {
        runtimeView?.ShowInspectorEmptyState();
    }

    private const string DefaultPitstopPanelText = "Pitstop Info\nSelect a pitstop to inspect its stop effect.";

    private void SetStatusText(string value)
    {
        runtimeView?.SetStatusText(value);
    }

    private void SetTileDetailsText(string value)
    {
        runtimeView?.SetTileDetailsText(value);
    }

    private void SetHintText(string value)
    {
        runtimeView?.SetHintText(value);
    }

    private void SetPitstopInfoText(string value)
    {
        runtimeView?.SetPitstopInfoText(value);
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

    private static string AppendObstacleDetails(string details, HexObstacleInstance visibleObstacle)
    {
        if (visibleObstacle == null || visibleObstacle.Definition == null)
        {
            return details;
        }

        HexObstacleDefinition definition = visibleObstacle.Definition;
        string resourceLabel = FormatResourceType(definition.primaryResource);
        string obstacleLine = $"{definition.displayName} ({resourceLabel} -{definition.primaryDrainAmount})";
        if (definition.penaltyMode == HexObstaclePenaltyMode.FallbackDrainIfPrimaryUnavailable)
        {
            obstacleLine += $" / fallback {FormatResourceType(definition.fallbackResource)} -{definition.fallbackDrainAmount}";
        }

        return $"{details}\nObstacle: {obstacleLine}";
    }

    private static string AppendExtraDetails(string details, string extraDetails)
    {
        return string.IsNullOrWhiteSpace(extraDetails) ? details : $"{details}\n{extraDetails}";
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

    private static string FormatPitstopEffectSummary(PitstopEventResult eventResult)
    {
        if (eventResult == null)
        {
            return "The caravan pauses at a roadside stop.";
        }

        if (eventResult.AppliedEffects.Count == 0)
        {
            return string.IsNullOrWhiteSpace(eventResult.Description)
                ? "The caravan pauses at a roadside stop."
                : eventResult.Description;
        }

        List<string> effectParts = new(eventResult.AppliedEffects.Count);
        for (int index = 0; index < eventResult.AppliedEffects.Count; index++)
        {
            PitstopResourceEffectResult effect = eventResult.AppliedEffects[index];
            string sign = effect.Amount >= 0 ? "+" : string.Empty;
            effectParts.Add($"{sign}{effect.Amount} {FormatResourceType(effect.ResourceType)}");
        }

        return $"{eventResult.Description} Gained {string.Join(", ", effectParts)}.";
    }

    private static string FormatPitstopPanel(PitstopSite pitstopSite, PitstopEventResult eventResult = null)
    {
        if (pitstopSite == null)
        {
            return DefaultPitstopPanelText;
        }

        string title = string.IsNullOrWhiteSpace(pitstopSite.EventTitle) ? pitstopSite.Kind.ToString() : pitstopSite.EventTitle;
        string refuelStatus = pitstopSite.HasRefuelPoint ? "Yes" : "No";
        string repeatableStatus = pitstopSite.Repeatable ? "Yes" : "No";
        string visitedStatus = pitstopSite.Visited ? $"Yes ({pitstopSite.VisitCount})" : "No";
        string description = string.IsNullOrWhiteSpace(pitstopSite.SpecialEventDescription)
            ? "placeholder"
            : pitstopSite.SpecialEventDescription;

        string destroyedStatus = pitstopSite.IsDestroyed ? "Yes" : "No";
        string panel = $"Pitstop Info\n{title}\nDestroyed: {destroyedStatus}\nRefuel: {refuelStatus}\nRepeatable: {repeatableStatus}\nVisited: {visitedStatus}\n{description}";
        if (eventResult == null || !eventResult.Triggered || !eventResult.EffectsApplied)
        {
            return panel;
        }

        if (eventResult.SelectedOption != null)
        {
            string outcomeText = string.IsNullOrWhiteSpace(eventResult.OutcomeText) ? eventResult.SelectedOption.label : eventResult.OutcomeText;
            return $"{panel}\nEvent: {eventResult.Title}\nChoice: {eventResult.SelectedOption.label}\nOutcome: {outcomeText}\nReward: {FormatEffectList(eventResult.AppliedEffects)}";
        }

        return $"{panel}\nEvent: {eventResult.Title}\nReward: {FormatEffectList(eventResult.AppliedEffects)}";
    }

    private static string FormatEffectList(IReadOnlyList<PitstopResourceEffectResult> effects)
    {
        if (effects == null || effects.Count == 0)
        {
            return "None";
        }

        List<string> parts = new(effects.Count);
        for (int index = 0; index < effects.Count; index++)
        {
            PitstopResourceEffectResult effect = effects[index];
            string sign = effect.Amount >= 0 ? "+" : string.Empty;
            parts.Add($"{sign}{effect.Amount} {FormatResourceType(effect.ResourceType)}");
        }

        return string.Join(", ", parts);
    }

    private static string FormatNemesisArchetype(HexNemesisArchetype archetype)
    {
        string raw = archetype.ToString();
        return char.ToUpperInvariant(raw[0]) + raw[1..];
    }
}
