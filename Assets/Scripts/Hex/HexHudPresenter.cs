using System.Collections.Generic;
using UnityEngine.UI;

public sealed class HexHudPresenter
{
    private readonly Text statusText;
    private readonly Text tileDetailsText;
    private readonly Text hintText;

    public HexHudPresenter(Text statusText, Text tileDetailsText, Text hintText)
    {
        this.statusText = statusText;
        this.tileDetailsText = tileDetailsText;
        this.hintText = hintText;
    }

    public void ShowAwaitingStart()
    {
        SetText(statusText, "Select a departure tile.");
        SetText(hintText, "First click picks your origin. Second click previews the cheapest route.");
    }

    public void ShowCaravanIdle(HexagonTile caravanTile)
    {
        if (caravanTile == null)
        {
            SetText(statusText, "Caravan ready.");
            SetText(hintText, "Reach the goal marker on the right edge. Click a tile to inspect it, or click the caravan tile to plan a move.");
            return;
        }

        SetText(
            statusText,
            $"Caravan ready at {FormatCoordinates(caravanTile.Coordinates)}.");
        SetText(hintText, "Reach the goal marker on the right edge. Click the caravan tile to plan a move.");
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
        SetText(hintText, "Click the caravan tile to begin route planning toward the goal marker.");
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
        SetText(hintText, "Click a destination tile to preview the route. Click the caravan tile again to cancel.");
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
        SetText(hintText, "Choose a destination tile to preview route time.");
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
        SetText(hintText, $"Preview cost: {totalTravelCost} days. Click a new origin to plan another route.");
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
        SetText(hintText, $"Cost: {totalTravelCost} supplies over {stepCount} steps. Click the same tile again to move.");
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
        SetText(hintText, "Click a different tile to preview another route, or click the caravan tile to cancel.");
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
            $"Not enough supplies for {FormatCoordinates(destinationTile.Coordinates)}.");
        SetText(hintText, $"Need {travelCost} supplies and only have {remainingResources}. Pick a cheaper route or cancel.");
    }

    public void ShowMoveComplete(HexagonTile tile, int travelCost, int remainingResources)
    {
        if (tile == null)
        {
            ShowCaravanIdle(null);
            return;
        }

        SetText(
            statusText,
            $"Caravan moved to {FormatCoordinates(tile.Coordinates)}.");
        SetText(hintText, $"Spent {travelCost} supplies. {remainingResources} left. Click the caravan tile to plan the next move.");
    }

    public void ShowVictory(HexagonTile tile, int remainingResources)
    {
        if (tile == null)
        {
            SetText(statusText, "Goal reached.");
            SetText(hintText, "Run complete. Press Play again to restart.");
            return;
        }

        SetText(
            statusText,
            $"Goal reached at {FormatCoordinates(tile.Coordinates)}.");
        SetText(hintText, $"Run complete with {remainingResources} supplies left. Press Play again to restart.");
    }

    public void ShowDefeat(HexagonTile tile)
    {
        if (tile == null)
        {
            SetText(statusText, "Supplies depleted.");
            SetText(hintText, "The caravan cannot continue. Press Play again to restart.");
            return;
        }

        SetText(
            statusText,
            $"Supplies depleted at {FormatCoordinates(tile.Coordinates)}.");
        SetText(hintText, "The caravan cannot continue. Press Play again to restart.");
    }

    public void ShowNoPath()
    {
        SetText(statusText, "No route found for that pair of tiles.");
        SetText(hintText, "Pick a different destination or start a new route.");
    }

    public void ShowTileDetails(HexagonTile tile, PitstopSite pitstopSite = null)
    {
        if (tile == null)
        {
            ResetTileDetails();
            return;
        }

        HexTileData tileData = tile.TileData;
        string biome = FormatBiome(tileData?.Biome ?? Biome.grass);
        string travelCost = (tileData?.TravelCost ?? tile.travelCost).ToString();
        string passability = (tileData?.IsPassable ?? tile.canTravelThrough) ? "Passable" : "Blocked";

        string details = $"Tile {FormatCoordinates(tile.Coordinates)}\nTerrain: {biome}\nTravel Cost: {travelCost}\n{passability}";
        if (pitstopSite != null)
        {
            string refuelStatus = pitstopSite.HasRefuelPoint ? "Yes" : "No";
            string specialEventDescription = string.IsNullOrWhiteSpace(pitstopSite.SpecialEventDescription)
                ? "placeholder"
                : pitstopSite.SpecialEventDescription;

            details += $"\nRefuel Point: {refuelStatus}\nSpecial Event: {specialEventDescription}";
        }

        SetText(tileDetailsText, details);
    }

    public void ResetTileDetails()
    {
        SetText(tileDetailsText, "Click a tile to inspect terrain cost.");
    }

    private static void SetText(Text target, string value)
    {
        if (target != null)
        {
            target.text = value;
        }
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
}
