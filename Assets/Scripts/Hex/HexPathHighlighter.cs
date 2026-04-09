using System.Collections.Generic;
using UnityEngine;

public sealed class HexPathHighlighter
{
    public void Reset(HexagonTile startTile, HexagonTile endTile, IReadOnlyList<HexTileData> path, MapGenerator mapGenerator)
    {
        startTile?.ResetMaterial();
        endTile?.ResetMaterial();

        if (path == null || mapGenerator == null)
        {
            return;
        }

        foreach (HexTileData tileData in path)
        {
            if (mapGenerator.TryGetTileView(tileData.Coordinates, out HexagonTile tileView))
            {
                tileView.ResetMaterial();
            }
        }
    }

    public void HighlightEndpoint(HexagonTile tile)
    {
        tile?.SelectAsStartOrEnd();
    }

    public void HighlightEndpoint(HexagonTile tile, Color color)
    {
        tile?.HighlightSelection(color);
    }

    public void HighlightPath(IReadOnlyList<HexTileData> path, HexagonTile startTile, HexagonTile endTile, MapGenerator mapGenerator)
    {
        if (path == null || mapGenerator == null || startTile == null || endTile == null)
        {
            return;
        }

        foreach (HexTileData tileData in path)
        {
            if (tileData.Coordinates.Equals(startTile.Coordinates) || tileData.Coordinates.Equals(endTile.Coordinates))
            {
                continue;
            }

            if (mapGenerator.TryGetTileView(tileData.Coordinates, out HexagonTile tile))
            {
                tile.HighlightedRoad();
            }
        }
    }
}
