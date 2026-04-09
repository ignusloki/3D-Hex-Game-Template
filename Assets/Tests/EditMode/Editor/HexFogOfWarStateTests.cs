using System.Collections.Generic;
using NUnit.Framework;

public class HexFogOfWarStateTests
{
    [Test]
    public void UpdateVisibility_TracksVisibleEnteredLeftAndRememberedTiles()
    {
        HexGridData gridData = CreateGrid(5, 5);
        HexFogOfWarState fogState = new();
        fogState.Initialize(gridData, null);

        HexFogUpdateResult firstUpdate = fogState.UpdateVisibility(new HexCoordinates(2, 2), 1);
        Assert.That(firstUpdate.VisibleNow.Count, Is.EqualTo(7));
        Assert.That(firstUpdate.EnteredVisibility.Count, Is.EqualTo(7));
        Assert.That(firstUpdate.LeftVisibility.Count, Is.EqualTo(0));
        Assert.That(fogState.IsCurrentlyVisible(new HexCoordinates(2, 2)), Is.True);
        Assert.That(fogState.HasBeenDiscovered(new HexCoordinates(1, 2)), Is.True);

        HexFogUpdateResult secondUpdate = fogState.UpdateVisibility(new HexCoordinates(2, 4), 1);
        Assert.That(secondUpdate.EnteredVisibility.Count, Is.GreaterThan(0));
        Assert.That(secondUpdate.LeftVisibility, Does.Contain(new HexCoordinates(2, 1)));
        Assert.That(fogState.IsRemembered(new HexCoordinates(2, 1)), Is.True);
        Assert.That(fogState.ShouldShowTerrain(new HexCoordinates(2, 1)), Is.True);
    }

    [Test]
    public void AlwaysKnownSpecials_RemainKnownWhileTerrainStaysHiddenUntilDiscovered()
    {
        HexGridData gridData = CreateGrid(5, 5);
        HexCoordinates farPitstop = new(4, 4);
        HexFogOfWarState fogState = new();
        fogState.Initialize(gridData, new[] { farPitstop });

        fogState.UpdateVisibility(new HexCoordinates(0, 0), 1);

        Assert.That(fogState.IsAlwaysKnownSpecial(farPitstop), Is.True);
        Assert.That(fogState.ShouldShowSpecialTile(farPitstop), Is.True);
        Assert.That(fogState.ShouldShowTerrain(farPitstop), Is.False);
        Assert.That(fogState.ShouldShowObstacle(farPitstop), Is.False);
        Assert.That(fogState.GetKnowledgeState(farPitstop), Is.EqualTo(HexFogKnowledgeState.Unseen));
    }

    [Test]
    public void ObstaclesOnlyFollowCurrentVisibility()
    {
        HexGridData gridData = CreateGrid(5, 5);
        HexCoordinates obstacleTile = new(2, 2);
        HexFogOfWarState fogState = new();
        fogState.Initialize(gridData, null);

        fogState.UpdateVisibility(new HexCoordinates(2, 2), 1);
        Assert.That(fogState.ShouldShowObstacle(obstacleTile), Is.True);

        fogState.UpdateVisibility(new HexCoordinates(4, 4), 1);
        Assert.That(fogState.ShouldShowObstacle(obstacleTile), Is.False);
        Assert.That(fogState.IsRemembered(obstacleTile), Is.True);
    }

    private static HexGridData CreateGrid(int rows, int columns)
    {
        HexGridData gridData = new(rows, columns);
        for (int row = 0; row < rows; row++)
        {
            for (int column = 0; column < columns; column++)
            {
                gridData.SetTile(new HexTileData(new HexCoordinates(row, column), Biome.grass, null));
            }
        }

        return gridData;
    }
}
