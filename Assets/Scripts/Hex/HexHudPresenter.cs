using System.Collections.Generic;
using UnityEngine.UI;

public sealed class HexHudPresenter
{
    private readonly Text statusText;
    private readonly Text tileDetailsText;
    private readonly Text hintText;
    private readonly Text pitstopInfoText;

    public HexHudPresenter(Text statusText, Text tileDetailsText, Text hintText, Text pitstopInfoText = null)
    {
        this.statusText = statusText;
        this.tileDetailsText = tileDetailsText;
        this.hintText = hintText;
        this.pitstopInfoText = pitstopInfoText;
    }

    public void ShowAwaitingStart()
    {
        SetText(statusText, "Select a departure tile.");
        ClearHintText();
    }

    public void ShowCaravanIdle(HexagonTile caravanTile)
    {
        if (caravanTile == null)
        {
            SetText(statusText, "Caravan ready.");
            ClearHintText();
            return;
        }

        SetText(
            statusText,
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

        SetText(
            statusText,
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

        SetText(
            statusText,
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

        SetText(
            statusText,
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

        SetText(
            statusText,
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

        SetText(
            statusText,
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

        SetText(
            statusText,
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

        SetText(
            statusText,
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

        SetText(
            statusText,
            $"Tile {FormatCoordinates(destinationTile.Coordinates)} is outside caravan range.");
        ClearHintText();
    }

    public void ShowInsufficientResources(HexagonTile destinationTile, int travelCost, int remainingResources)
    {
        if (destinationTile == null)
        {
            ShowNoPath();
            return;
        }

        SetText(
            statusText,
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

        SetText(
            statusText,
            $"Caravan moved to {FormatCoordinates(tile.Coordinates)}.");
        ClearHintText();
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

        SetText(pitstopInfoText, FormatPitstopPanel(eventResult.Site, eventResult));
        SetText(
            statusText,
            $"{eventResult.Title} at {FormatCoordinates(tile.Coordinates)}.");
        ClearHintText();
    }

    public void ShowPitstopRevisit(HexagonTile tile, PitstopSite site, CaravanResourceSnapshot resources)
    {
        if (tile == null || site == null)
        {
            ShowCaravanIdle(tile);
            return;
        }

        SetText(pitstopInfoText, FormatPitstopPanel(site));
        SetText(
            statusText,
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

        SetText(
            statusText,
            $"{contactResult.Obstacle.Definition.displayName} struck at {FormatCoordinates(tile.Coordinates)}.");
        ClearHintText();
    }

    public void ShowVictory(HexagonTile tile, CaravanResourceSnapshot resources)
    {
        if (tile == null)
        {
            SetText(statusText, "Goal reached.");
            ClearHintText();
            return;
        }

        SetText(
            statusText,
            $"Goal reached at {FormatCoordinates(tile.Coordinates)}.");
        ClearHintText();
    }

    public void ShowDefeat(HexagonTile tile, string defeatReason)
    {
        if (tile == null)
        {
            SetText(statusText, defeatReason);
            ClearHintText();
            return;
        }

        SetText(
            statusText,
            $"{defeatReason} at {FormatCoordinates(tile.Coordinates)}.");
        ClearHintText();
    }

    public void ShowNoPath()
    {
        SetText(statusText, "No route found for that pair of tiles.");
        ClearHintText();
    }

    public void ShowTileDetails(HexagonTile tile, PitstopSite pitstopSite = null, HexObstacleInstance visibleObstacle = null)
    {
        if (tile == null)
        {
            ResetTileDetails();
            return;
        }

        HexTileData tileData = tile.TileData;
        string biome = FormatBiome(tileData?.Biome ?? Biome.grass);
        string travelCost = (tileData?.TravelCost ?? tile.travelCost).ToString();

        string details = $"Tile {FormatCoordinates(tile.Coordinates)}\nTerrain: {biome}\nTravel Cost: {travelCost}";
        details = AppendObstacleDetails(details, visibleObstacle);

        SetText(tileDetailsText, details);
        SetText(pitstopInfoText, FormatPitstopPanel(pitstopSite));
    }

    public void ShowUnknownTileDetails(HexagonTile tile, PitstopSite pitstopSite = null)
    {
        if (tile == null)
        {
            ResetTileDetails();
            return;
        }

        string details = $"Tile {FormatCoordinates(tile.Coordinates)}\nTerrain: Unknown\nTravel Cost: Unknown\nVisibility: Unseen";
        SetText(tileDetailsText, details);
        SetText(pitstopInfoText, FormatPitstopPanel(pitstopSite));
    }

    public void ResetTileDetails()
    {
        SetText(tileDetailsText, "Click a tile to inspect terrain cost.");
        SetText(pitstopInfoText, DefaultPitstopPanelText);
    }

    private const string DefaultPitstopPanelText = "Pitstop Info\nSelect a pitstop to inspect its stop effect.";

    private static void SetText(Text target, string value)
    {
        if (target != null)
        {
            target.text = value;
        }
    }

    private void ClearHintText()
    {
        SetText(hintText, string.Empty);
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

        string panel = $"Pitstop Info\n{title}\nRefuel: {refuelStatus}\nRepeatable: {repeatableStatus}\nVisited: {visitedStatus}\n{description}";
        if (eventResult == null || !eventResult.Triggered || !eventResult.EffectsApplied)
        {
            return panel;
        }

        return $"{panel}\nReward: {FormatEffectList(eventResult.AppliedEffects)}";
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
}
