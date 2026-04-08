using System.Collections.Generic;

public sealed class HexPathSelectionState
{
    public HexagonTile StartTile { get; private set; }
    public HexagonTile EndTile { get; private set; }
    public IReadOnlyList<HexTileData> Path { get; private set; }
    public bool IsSelectingStart { get; private set; } = true;

    public void BeginSelection(HexagonTile startTile)
    {
        StartTile = startTile;
        EndTile = null;
        Path = null;
        IsSelectingStart = false;
    }

    public IReadOnlyList<HexTileData> CompleteSelection(HexagonTile endTile, MapGenerator mapGenerator)
    {
        EndTile = endTile;
        IsSelectingStart = true;
        Path = mapGenerator?.FindPath(StartTile.Coordinates, EndTile.Coordinates);
        return Path;
    }

    public void Reset()
    {
        StartTile = null;
        EndTile = null;
        Path = null;
        IsSelectingStart = true;
    }
}
