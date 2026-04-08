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
        int totalTravelCost = 0;

        foreach (HexTileData tile in path)
        {
            totalTravelCost += tile.TravelCost;
        }

        SetText(
            statusText,
            $"Route ready: {stepCount} steps to {FormatCoordinates(destination.Coordinates)}.");
        SetText(hintText, $"Preview cost: {totalTravelCost} days. Click a new origin to plan another route.");
    }

    public void ShowNoPath()
    {
        SetText(statusText, "No route found for that pair of tiles.");
        SetText(hintText, "Pick a different destination or start a new route.");
    }

    public void ShowTileDetails(HexagonTile tile)
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

        SetText(
            tileDetailsText,
            $"Tile {FormatCoordinates(tile.Coordinates)}\nTerrain: {biome}\nTravel Cost: {travelCost}\n{passability}");
    }

    public void ResetTileDetails()
    {
        SetText(tileDetailsText, "Hover a tile to inspect terrain cost.");
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
