using System.Collections.Generic;
using NUnit.Framework;

public class PitstopPlacementPlannerTests
{
    [TestCase(5, 5, 4, 1, 3)]
    [TestCase(10, 10, 8, 2, 6)]
    public void Plan_ReturnsDistributedPitstopsAcrossTheMap(
        int rows,
        int columns,
        int expectedCount,
        int minimumSpacing,
        int minimumDistinctColumns)
    {
        HexGridData gridData = CreateGrid(rows, columns);
        HexPathfinder pathfinder = new(gridData);
        PitstopPlacementSettings settings = new()
        {
            pitstopsOnFiveByFive = 4,
            pitstopsOnTenByTen = 8,
            minimumSpacingOnFiveByFive = 1,
            minimumSpacingOnTenByTen = 2,
            allowGrass = true,
            allowForest = true,
            allowMountain = false,
            allowDesert = false,
            pathBias = 0.65f,
            lowCostBias = 0.25f,
            grassPreferenceBias = 0.2f,
            preferredPathDistance = 2,
            randomJitter = 0f
        };
        settings.Validate();

        PitstopPlacementPlanner planner = new();
        HexCoordinates start = new(rows / 2, 0);
        HexCoordinates goal = new(rows / 2, columns - 1);

        IReadOnlyList<HexCoordinates> placements = planner.Plan(
            gridData,
            pathfinder,
            start,
            goal,
            settings,
            new System.Random(12345));

        Assert.That(placements.Count, Is.EqualTo(expectedCount));
        Assert.That(new HashSet<HexCoordinates>(placements).Count, Is.EqualTo(expectedCount));
        Assert.That(placements, Has.None.EqualTo(start));
        Assert.That(placements, Has.None.EqualTo(goal));
        Assert.That(GetDistinctColumnCount(placements), Is.GreaterThanOrEqualTo(minimumDistinctColumns));

        for (int i = 0; i < placements.Count; i++)
        {
            for (int j = i + 1; j < placements.Count; j++)
            {
                Assert.That(placements[i].DistanceTo(placements[j]), Is.GreaterThanOrEqualTo(minimumSpacing));
            }
        }
    }

    private static HexGridData CreateGrid(int rows, int columns)
    {
        HexGridData grid = new(rows, columns);
        for (int row = 0; row < rows; row++)
        {
            for (int column = 0; column < columns; column++)
            {
                Biome biome = column % 4 == 0 ? Biome.forest : Biome.grass;
                grid.SetTile(new HexTileData(new HexCoordinates(row, column), biome, null));
            }
        }

        return grid;
    }

    private static int GetDistinctColumnCount(IReadOnlyList<HexCoordinates> placements)
    {
        HashSet<int> distinctColumns = new();
        for (int index = 0; index < placements.Count; index++)
        {
            distinctColumns.Add(placements[index].Column);
        }

        return distinctColumns.Count;
    }
}
