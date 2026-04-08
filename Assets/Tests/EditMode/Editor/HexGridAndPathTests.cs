using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

public class HexGridAndPathTests
{
    private readonly List<HexScriptableObject> createdProperties = new();

    [TearDown]
    public void TearDown()
    {
        foreach (HexScriptableObject properties in createdProperties)
        {
            Object.DestroyImmediate(properties);
        }

        createdProperties.Clear();
    }

    [Test]
    public void DistanceTo_ReturnsExpectedHexDistance()
    {
        HexCoordinates origin = new(1, 1);
        HexCoordinates same = new(1, 1);
        HexCoordinates directNeighbor = new(1, 2);
        HexCoordinates fartherTile = new(2, 0);

        Assert.That(origin.DistanceTo(same), Is.EqualTo(0));
        Assert.That(origin.DistanceTo(directNeighbor), Is.EqualTo(1));
        Assert.That(origin.DistanceTo(fartherTile), Is.EqualTo(2));
    }

    [Test]
    public void GetNeighborCoordinates_ReturnsSixNeighborsForCenterTile()
    {
        HexGridData grid = BuildGrid(3, 3, _ => CreateProperties(Biome.grass, 1, true));

        List<HexCoordinates> neighbors = grid.GetNeighborCoordinates(new HexCoordinates(1, 1)).ToList();

        Assert.That(neighbors, Has.Count.EqualTo(6));
        Assert.That(neighbors.Distinct().Count(), Is.EqualTo(6));
        Assert.That(neighbors.All(neighbor => neighbor.DistanceTo(new HexCoordinates(1, 1)) == 1), Is.True);
    }

    [Test]
    public void FindPath_AvoidsBlockedTiles()
    {
        HexGridData grid = BuildGrid(3, 3, coordinates =>
        {
            if (coordinates.Equals(new HexCoordinates(1, 1)))
            {
                return CreateProperties(Biome.water, 1, false);
            }

            return CreateProperties(Biome.grass, 1, true);
        });

        HexPathfinder pathfinder = new(grid);
        IReadOnlyList<HexTileData> path = pathfinder.FindPath(new HexCoordinates(1, 0), new HexCoordinates(1, 2));

        Assert.That(path, Is.Not.Null);
        Assert.That(path.First().Coordinates, Is.EqualTo(new HexCoordinates(1, 0)));
        Assert.That(path.Last().Coordinates, Is.EqualTo(new HexCoordinates(1, 2)));
        Assert.That(path.Any(tile => tile.Coordinates.Equals(new HexCoordinates(1, 1))), Is.False);
    }

    [Test]
    public void GetReachableTiles_RespectsBudgetAndOccupancy()
    {
        HexGridData grid = BuildGrid(3, 3, coordinates =>
        {
            if (coordinates.Equals(new HexCoordinates(1, 2)))
            {
                return CreateProperties(Biome.mountain, 3, true);
            }

            return CreateProperties(Biome.grass, 1, true);
        });

        grid.TryGetTile(new HexCoordinates(0, 1), out HexTileData occupiedTile);
        occupiedTile.SetOccupied(true);

        HexPathfinder pathfinder = new(grid);
        IReadOnlyList<HexTileData> reachableTiles = pathfinder.GetReachableTiles(new HexCoordinates(1, 1), 2);
        HashSet<HexCoordinates> reachableCoordinates = reachableTiles.Select(tile => tile.Coordinates).ToHashSet();

        Assert.That(reachableCoordinates.Contains(new HexCoordinates(0, 1)), Is.False);
        Assert.That(reachableCoordinates.Contains(new HexCoordinates(1, 2)), Is.False);
        Assert.That(reachableCoordinates.Contains(new HexCoordinates(1, 0)), Is.True);
        Assert.That(reachableCoordinates.Contains(new HexCoordinates(2, 1)), Is.True);
    }

    private HexGridData BuildGrid(int rows, int columns, System.Func<HexCoordinates, HexScriptableObject> propertyFactory)
    {
        HexGridData grid = new(rows, columns);

        for (int row = 0; row < rows; row++)
        {
            for (int column = 0; column < columns; column++)
            {
                HexCoordinates coordinates = new(row, column);
                HexScriptableObject properties = propertyFactory(coordinates);
                grid.SetTile(new HexTileData(coordinates, properties.type, properties));
            }
        }

        return grid;
    }

    private HexScriptableObject CreateProperties(Biome biome, int travelCost, bool passable)
    {
        HexScriptableObject properties = ScriptableObject.CreateInstance<HexScriptableObject>();
        properties.type = biome;
        properties.travelCost = travelCost;
        properties.passable = passable;
        createdProperties.Add(properties);
        return properties;
    }
}
