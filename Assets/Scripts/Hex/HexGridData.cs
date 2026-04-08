using System.Collections.Generic;

public sealed class HexGridData
{
    private readonly Dictionary<HexCoordinates, HexTileData> tiles = new();

    public int Rows { get; }
    public int Columns { get; }

    public HexGridData(int rows, int columns)
    {
        Rows = rows;
        Columns = columns;
    }

    public IEnumerable<HexTileData> Tiles => tiles.Values;

    public void SetTile(HexTileData tile)
    {
        tiles[tile.Coordinates] = tile;
    }

    public bool TryGetTile(HexCoordinates coordinates, out HexTileData tile)
    {
        return tiles.TryGetValue(coordinates, out tile);
    }

    public bool IsInside(HexCoordinates coordinates)
    {
        return coordinates.Row >= 0
            && coordinates.Row < Rows
            && coordinates.Column >= 0
            && coordinates.Column < Columns;
    }

    public IEnumerable<HexCoordinates> GetNeighborCoordinates(HexCoordinates coordinates)
    {
        bool evenRow = coordinates.Row % 2 == 0;

        HexCoordinates[] candidates =
        {
            new(coordinates.Row, coordinates.Column + 1),
            new(coordinates.Row, coordinates.Column - 1),
            new(coordinates.Row - 1, evenRow ? coordinates.Column : coordinates.Column + 1),
            new(coordinates.Row + 1, evenRow ? coordinates.Column : coordinates.Column + 1),
            new(coordinates.Row - 1, evenRow ? coordinates.Column - 1 : coordinates.Column),
            new(coordinates.Row + 1, evenRow ? coordinates.Column - 1 : coordinates.Column)
        };

        foreach (HexCoordinates candidate in candidates)
        {
            if (IsInside(candidate))
            {
                yield return candidate;
            }
        }
    }

    public IEnumerable<HexTileData> GetNeighbors(HexCoordinates coordinates)
    {
        foreach (HexCoordinates neighborCoordinates in GetNeighborCoordinates(coordinates))
        {
            if (TryGetTile(neighborCoordinates, out HexTileData neighbor))
            {
                yield return neighbor;
            }
        }
    }
}
