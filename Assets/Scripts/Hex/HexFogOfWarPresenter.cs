public sealed class HexFogOfWarPresenter
{
    public void Apply(MapGenerator mapGenerator, HexFogOfWarState fogState, HexFogOfWarSettings settings)
    {
        if (mapGenerator == null || mapGenerator.GridData == null || fogState == null || settings == null)
        {
            return;
        }

        foreach (HexTileData tileData in mapGenerator.GridData.Tiles)
        {
            if (tileData == null || !mapGenerator.TryGetTileView(tileData.Coordinates, out HexagonTile tileView) || tileView == null)
            {
                continue;
            }

            tileView.ApplyFogState(fogState.GetKnowledgeState(tileData.Coordinates), settings);
        }
    }
}
