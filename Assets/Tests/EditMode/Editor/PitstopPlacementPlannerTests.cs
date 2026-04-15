using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class PitstopPlacementPlannerTests
{
    [TestCase(0, 9)]
    [TestCase(9, 0)]
    public void GeneratePitstops_BuildsStructuredLargeMapLayoutsAcrossBothDirections(int startColumn, int goalColumn)
    {
        HexGridData gridData = CreateGrid(10, 10);
        HexPathfinder pathfinder = new(gridData);
        PitstopPlacementSettings settings = CreateSettings(TenByTenPitstopLayoutMode.FourStrategicAnchors);
        PitstopPlacementPlanner planner = new();
        HexCoordinates start = new(5, startColumn);
        HexCoordinates goal = new(5, goalColumn);

        PitstopLayoutResult result = planner.GeneratePitstops(
            gridData,
            pathfinder,
            start,
            goal,
            settings,
            new System.Random(12345));

        Assert.That(result.IsValid, Is.True, result.Summary);
        Assert.That(result.Coordinates.Count, Is.EqualTo(4));
        Assert.That(new HashSet<HexCoordinates>(result.Coordinates).Count, Is.EqualTo(4));
        AssertPairwiseSpacing(result.Coordinates, 4);
        AssertSafeDistanceFromEndpoints(result.Coordinates, start, goal, 1);

        List<HexCoordinates> earlyBand = FilterByProgress(result.Coordinates, start, goal, settings.earlyBandOnTenByTen);
        List<HexCoordinates> midBand = FilterByProgress(result.Coordinates, start, goal, settings.midBandOnTenByTen);
        List<HexCoordinates> lateBand = FilterByProgress(result.Coordinates, start, goal, settings.lateBandOnTenByTen);

        Assert.That(earlyBand.Count, Is.EqualTo(2));
        Assert.That(midBand.Count, Is.EqualTo(1));
        Assert.That(lateBand.Count, Is.EqualTo(1));
        Assert.That(GetLane(earlyBand[0], 10, settings), Is.Not.EqualTo(GetLane(earlyBand[1], 10, settings)));
        Assert.That(GetUniqueLaneCount(result.Coordinates, 10, settings), Is.GreaterThanOrEqualTo(3));
    }

    [TestCase(0, 9)]
    [TestCase(9, 0)]
    public void GeneratePitstops_BuildsSevenAnchorLargeMapLayoutsAcrossBothDirections(int startColumn, int goalColumn)
    {
        HexGridData gridData = CreateGrid(10, 10);
        HexPathfinder pathfinder = new(gridData);
        PitstopPlacementSettings settings = CreateSettings(TenByTenPitstopLayoutMode.SevenStrategicAnchors);
        PitstopPlacementPlanner planner = new();
        HexCoordinates start = new(5, startColumn);
        HexCoordinates goal = new(5, goalColumn);

        PitstopLayoutResult result = planner.GeneratePitstops(
            gridData,
            pathfinder,
            start,
            goal,
            settings,
            new System.Random(12345));

        Assert.That(result.IsValid, Is.True, result.Summary);
        Assert.That(result.Coordinates.Count, Is.EqualTo(7));
        Assert.That(new HashSet<HexCoordinates>(result.Coordinates).Count, Is.EqualTo(7));
        AssertPairwiseSpacing(result.Coordinates, 2);
        AssertSafeDistanceFromEndpoints(result.Coordinates, start, goal, 1);

        List<HexCoordinates> earlyBand = FilterByProgress(result.Coordinates, start, goal, settings.earlyBandOnTenByTen);
        List<HexCoordinates> midBand = FilterByProgress(result.Coordinates, start, goal, settings.midBandOnTenByTenSeven);
        List<HexCoordinates> lateBand = FilterByProgress(result.Coordinates, start, goal, settings.lateBandOnTenByTenSeven);

        Assert.That(earlyBand.Count, Is.EqualTo(2));
        Assert.That(midBand.Count, Is.EqualTo(3));
        Assert.That(lateBand.Count, Is.EqualTo(2));
        Assert.That(GetUniqueLaneCount(result.Coordinates, 10, settings), Is.GreaterThanOrEqualTo(3));
    }

    [TestCase(0, 9)]
    [TestCase(9, 0)]
    public void GeneratePitstops_AddsAnExtraAnchorToSevenAnchorLayoutsWithMainSceneSettings(int startColumn, int goalColumn)
    {
        HexGridData gridData = CreateGrid(10, 10);
        HexPathfinder pathfinder = new(gridData);
        PitstopPlacementSettings settings = CreateSettings(TenByTenPitstopLayoutMode.SevenStrategicAnchors);
        settings.candidatePoolSize = 10;
        settings.randomJitter = 0.05f;
        settings.minimumSpacingOnTenByTenSeven = 3;
        settings.earlyDistanceFromSpawn = new PitstopIntRange(3, 4);
        settings.lateDistanceFromGoal = new PitstopIntRange(3, 4);
        settings.Validate();

        PitstopPlacementPlanner planner = new();
        HexCoordinates start = new(5, startColumn);
        HexCoordinates goal = new(5, goalColumn);

        PitstopLayoutResult result = planner.GeneratePitstops(
            gridData,
            pathfinder,
            start,
            goal,
            settings,
            new System.Random(12345),
            new HexActMapModifiers(1, HexBoonMapPlacementBand.Mid));

        Assert.That(result.IsValid, Is.True, result.Summary);
        Assert.That(result.Coordinates.Count, Is.EqualTo(8));
        Assert.That(new HashSet<HexCoordinates>(result.Coordinates).Count, Is.EqualTo(8));
        AssertPairwiseSpacing(result.Coordinates, 3);
        AssertSafeDistanceFromEndpoints(result.Coordinates, start, goal, 1);
        Assert.That(GetUniqueLaneCount(result.Coordinates, 10, settings), Is.GreaterThanOrEqualTo(3));
    }

    [Test]
    public void GeneratePitstops_BuildsEarlyAndLateAnchorsOnSmallMaps()
    {
        HexGridData gridData = CreateGrid(5, 5);
        HexPathfinder pathfinder = new(gridData);
        PitstopPlacementSettings settings = CreateSettings(TenByTenPitstopLayoutMode.FourStrategicAnchors);
        PitstopPlacementPlanner planner = new();
        HexCoordinates start = new(2, 0);
        HexCoordinates goal = new(2, 4);

        PitstopLayoutResult result = planner.GeneratePitstops(
            gridData,
            pathfinder,
            start,
            goal,
            settings,
            new System.Random(9876));

        Assert.That(result.IsValid, Is.True, result.Summary);
        Assert.That(result.Coordinates.Count, Is.EqualTo(2));
        AssertPairwiseSpacing(result.Coordinates, 2);
        AssertSafeDistanceFromEndpoints(result.Coordinates, start, goal, 1);
        Assert.That(GetUniqueLaneCount(result.Coordinates, 5, settings), Is.GreaterThanOrEqualTo(2));

        List<HexCoordinates> earlyBand = FilterByProgress(result.Coordinates, start, goal, settings.earlyBandOnFiveByFive);
        List<HexCoordinates> lateBand = FilterByProgress(result.Coordinates, start, goal, settings.lateBandOnFiveByFive);

        Assert.That(earlyBand.Count, Is.EqualTo(1));
        Assert.That(lateBand.Count, Is.EqualTo(1));
    }

    [Test]
    public void GeneratePitstops_AddsAMidAnchorOnSmallMapsWhenActModifierRequestsExtraPitstop()
    {
        HexGridData gridData = CreateGrid(5, 5);
        HexPathfinder pathfinder = new(gridData);
        PitstopPlacementSettings settings = CreateSettings(TenByTenPitstopLayoutMode.FourStrategicAnchors);
        PitstopPlacementPlanner planner = new();
        HexCoordinates start = new(2, 0);
        HexCoordinates goal = new(2, 4);

        PitstopLayoutResult result = planner.GeneratePitstops(
            gridData,
            pathfinder,
            start,
            goal,
            settings,
            new System.Random(9876),
            new HexActMapModifiers(1, HexBoonMapPlacementBand.Mid));

        Assert.That(result.IsValid, Is.True, result.Summary);
        Assert.That(result.Coordinates.Count, Is.EqualTo(3));

        List<HexCoordinates> earlyBand = FilterByProgress(result.Coordinates, start, goal, settings.earlyBandOnFiveByFive);
        List<HexCoordinates> midBand = FilterByProgress(result.Coordinates, start, goal, settings.midBandOnFiveByFive);
        List<HexCoordinates> lateBand = FilterByProgress(result.Coordinates, start, goal, settings.lateBandOnFiveByFive);

        Assert.That(earlyBand.Count, Is.EqualTo(1));
        Assert.That(midBand.Count, Is.EqualTo(1));
        Assert.That(lateBand.Count, Is.EqualTo(1));
    }

    [TestCase(0, 9)]
    [TestCase(9, 0)]
    public void GeneratePitstops_AddsAnExtraMidAnchorOnLargeMapsWhenActModifierRequestsExtraPitstop(int startColumn, int goalColumn)
    {
        HexGridData gridData = CreateGrid(10, 10);
        HexPathfinder pathfinder = new(gridData);
        PitstopPlacementSettings settings = CreateSettings(TenByTenPitstopLayoutMode.FourStrategicAnchors);
        PitstopPlacementPlanner planner = new();
        HexCoordinates start = new(5, startColumn);
        HexCoordinates goal = new(5, goalColumn);

        PitstopLayoutResult result = planner.GeneratePitstops(
            gridData,
            pathfinder,
            start,
            goal,
            settings,
            new System.Random(12345),
            new HexActMapModifiers(1, HexBoonMapPlacementBand.Mid));

        Assert.That(result.IsValid, Is.True, result.Summary);
        Assert.That(result.Coordinates.Count, Is.EqualTo(5));

        List<HexCoordinates> midBand = FilterByProgress(result.Coordinates, start, goal, settings.midBandOnTenByTen);
        Assert.That(midBand.Count, Is.EqualTo(2));
    }

    private static PitstopPlacementSettings CreateSettings(TenByTenPitstopLayoutMode tenByTenLayoutMode)
    {
        PitstopPlacementSettings settings = new()
        {
            pitstopsOnFiveByFive = 2,
            tenByTenLayoutMode = tenByTenLayoutMode,
            minimumSpacingOnFiveByFive = 2,
            minimumSpacingOnTenByTen = 4,
            minimumSpacingOnTenByTenSeven = 2,
            spawnGoalAdjacencyBuffer = 1,
            minimumOpenNeighbors = 3,
            maxGenerationAttempts = 32,
            keepBestLayoutIfAllAttemptsFail = true,
            candidatePoolSize = 4,
            randomJitter = 0f,
            allowGrass = true,
            allowForest = true,
            allowMountain = false,
            allowDesert = false,
            earlyDistanceFromSpawn = new PitstopIntRange(2, 3),
            lateDistanceFromGoal = new PitstopIntRange(2, 3),
            earlyBandOnTenByTen = new PitstopFloatRange(0.2f, 0.3f),
            midBandOnTenByTen = new PitstopFloatRange(0.45f, 0.6f),
            lateBandOnTenByTen = new PitstopFloatRange(0.75f, 0.85f),
            midBandOnTenByTenSeven = new PitstopFloatRange(0.4f, 0.65f),
            lateBandOnTenByTenSeven = new PitstopFloatRange(0.65f, 0.85f),
            earlyBandOnFiveByFive = new PitstopFloatRange(0.2f, 0.35f),
            midBandOnFiveByFive = new PitstopFloatRange(0.42f, 0.58f),
            lateBandOnFiveByFive = new PitstopFloatRange(0.65f, 0.8f),
            topLaneCenter = 0.25f,
            middleLaneCenter = 0.5f,
            bottomLaneCenter = 0.75f,
            minimumUniqueLanesOnFiveByFive = 2,
            minimumUniqueLanesOnTenByTen = 3,
            minimumRowSpanOnFiveByFive = 1,
            minimumRowSpanOnTenByTen = 3,
            minimumAverageCorridorOffsetOnFiveByFive = 0.75f,
            minimumAverageCorridorOffsetOnTenByTen = 1f,
            minimumAverageCorridorOffsetOnTenByTenSeven = 0.75f,
            preferredCorridorOffset = 1f,
            lanePreferenceWeight = 0.8f,
            laneDiversityWeight = 0.7f,
            corridorOffsetWeight = 0.65f,
            edgeAvoidanceWeight = 0.35f,
            openNeighborWeight = 0.3f,
            distanceTargetWeight = 0.85f
        };
        settings.Validate();
        return settings;
    }

    private static HexGridData CreateGrid(int rows, int columns)
    {
        HexGridData grid = new(rows, columns);
        for (int row = 0; row < rows; row++)
        {
            for (int column = 0; column < columns; column++)
            {
                Biome biome = row % 3 == 0 ? Biome.forest : Biome.grass;
                grid.SetTile(new HexTileData(new HexCoordinates(row, column), biome, null));
            }
        }

        return grid;
    }

    private static void AssertPairwiseSpacing(IReadOnlyList<HexCoordinates> coordinates, int minimumSpacing)
    {
        for (int leftIndex = 0; leftIndex < coordinates.Count; leftIndex++)
        {
            for (int rightIndex = leftIndex + 1; rightIndex < coordinates.Count; rightIndex++)
            {
                Assert.That(coordinates[leftIndex].DistanceTo(coordinates[rightIndex]), Is.GreaterThanOrEqualTo(minimumSpacing));
            }
        }
    }

    private static void AssertSafeDistanceFromEndpoints(IReadOnlyList<HexCoordinates> coordinates, HexCoordinates start, HexCoordinates goal, int adjacencyBuffer)
    {
        for (int index = 0; index < coordinates.Count; index++)
        {
            Assert.That(coordinates[index].DistanceTo(start), Is.GreaterThan(adjacencyBuffer));
            Assert.That(coordinates[index].DistanceTo(goal), Is.GreaterThan(adjacencyBuffer));
        }
    }

    private static List<HexCoordinates> FilterByProgress(
        IReadOnlyList<HexCoordinates> coordinates,
        HexCoordinates start,
        HexCoordinates goal,
        PitstopFloatRange range)
    {
        List<HexCoordinates> filtered = new();
        for (int index = 0; index < coordinates.Count; index++)
        {
            float progress = Mathf.Clamp01(Mathf.InverseLerp(start.Column, goal.Column, coordinates[index].Column));
            if (range.Contains(progress))
            {
                filtered.Add(coordinates[index]);
            }
        }

        return filtered;
    }

    private static int GetUniqueLaneCount(IReadOnlyList<HexCoordinates> coordinates, int rows, PitstopPlacementSettings settings)
    {
        HashSet<int> lanes = new();
        for (int index = 0; index < coordinates.Count; index++)
        {
            lanes.Add((int)GetLane(coordinates[index], rows, settings));
        }

        return lanes.Count;
    }

    private static PitstopTestLane GetLane(HexCoordinates coordinates, int rows, PitstopPlacementSettings settings)
    {
        float rowProgress = rows <= 1 ? 0.5f : coordinates.Row / (float)(rows - 1);
        float topBoundary = (settings.topLaneCenter + settings.middleLaneCenter) * 0.5f;
        float bottomBoundary = (settings.middleLaneCenter + settings.bottomLaneCenter) * 0.5f;

        if (rowProgress <= topBoundary)
        {
            return PitstopTestLane.Top;
        }

        if (rowProgress >= bottomBoundary)
        {
            return PitstopTestLane.Bottom;
        }

        return PitstopTestLane.Middle;
    }

    private enum PitstopTestLane
    {
        Top,
        Middle,
        Bottom
    }
}
