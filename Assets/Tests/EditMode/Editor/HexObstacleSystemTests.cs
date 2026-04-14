using System.Collections.Generic;
using NUnit.Framework;

public class HexObstacleSystemTests
{
    [Test]
    public void ResolveContact_UsesFallbackWhenBanditsCannotStealGold()
    {
        HexObstacleDefinition definition = new()
        {
            id = "bandits",
            displayName = "Bandits",
            penaltyMode = HexObstaclePenaltyMode.FallbackDrainIfPrimaryUnavailable,
            primaryResource = CaravanResourceType.Gold,
            primaryDrainAmount = 1,
            fallbackResource = CaravanResourceType.Morale,
            fallbackDrainAmount = 1
        };
        definition.Validate();

        HexObstacleInstance obstacle = new(new HexCoordinates(1, 1), definition);
        HexObstacleContactResult result = HexObstaclePenaltyResolver.Resolve(obstacle, new CaravanResourceSnapshot(10, 3, 0));

        Assert.That(result.HasContact, Is.True);
        Assert.That(result.AffectedResource, Is.EqualTo(CaravanResourceType.Morale));
        Assert.That(result.AmountDrained, Is.EqualTo(1));
        Assert.That(result.UsedFallbackResource, Is.True);
    }

    [Test]
    public void TryPlanSpawn_UsesOnlyVisibleRevealedAdjacentNonWaterTiles()
    {
        HexGridData gridData = new(5, 5);
        for (int row = 0; row < 5; row++)
        {
            for (int column = 0; column < 5; column++)
            {
                Biome biome = row == 2 && column == 3 ? Biome.water : Biome.grass;
                gridData.SetTile(new HexTileData(new HexCoordinates(row, column), biome, null));
            }
        }

        HexObstacleSpawnSettings settings = new()
        {
            baseSpawnChance = 1f,
            bonusChancePerEnteredHex = 0f,
            maxSpawnChance = 1f,
            visibleNeighborWeight = 1f,
            enteredNeighborWeight = 1f,
            edgePenaltyWeight = 0f,
            randomJitter = 0f
        };
        settings.Validate();

        List<HexObstacleDefinition> definitions = HexObstacleDefinition.CreateDefaultSet();
        HexObstacleSpawnPlanner planner = new();
        HexCoordinates caravanCoordinates = new(2, 2);
        HashSet<HexCoordinates> visible = new()
        {
            new HexCoordinates(2, 3),
            new HexCoordinates(2, 1),
            new HexCoordinates(1, 2),
            new HexCoordinates(3, 2),
            new HexCoordinates(1, 1),
            new HexCoordinates(3, 1)
        };
        HashSet<HexCoordinates> entered = new(visible);
        HashSet<HexCoordinates> pitstops = new() { new HexCoordinates(2, 1) };
        HashSet<HexCoordinates> protectedHexes = new() { new HexCoordinates(1, 2) };
        Dictionary<HexCoordinates, HexObstacleInstance> active = new()
        {
            [new HexCoordinates(3, 2)] = new HexObstacleInstance(new HexCoordinates(3, 2), definitions[0])
        };

        HexObstacleSpawnPlan plan = planner.TryPlanSpawn(
            gridData,
            settings,
            HexObstaclePressureContext.Empty,
            active,
            visible,
            entered,
            pitstops,
            protectedHexes,
            caravanCoordinates,
            new HexCoordinates(1, 1),
            new HexCoordinates(4, 4),
            definitions,
            new System.Random(1234));

        Assert.That(plan.ShouldSpawn, Is.True);
        Assert.That(plan.Coordinates, Is.EqualTo(new HexCoordinates(3, 1)));
    }

    [Test]
    public void TryPlanSpawn_ForceSpawnBypassesChanceRoll()
    {
        HexGridData gridData = new(5, 5);
        for (int row = 0; row < 5; row++)
        {
            for (int column = 0; column < 5; column++)
            {
                gridData.SetTile(new HexTileData(new HexCoordinates(row, column), Biome.grass, null));
            }
        }

        HexObstacleSpawnSettings settings = new()
        {
            baseSpawnChance = 0f,
            bonusChancePerEnteredHex = 0f,
            maxSpawnChance = 0f,
            randomJitter = 0f
        };
        settings.Validate();

        List<HexObstacleDefinition> definitions = HexObstacleDefinition.CreateDefaultSet();
        HexObstacleSpawnPlanner planner = new();

        HexObstacleSpawnPlan plan = planner.TryPlanSpawn(
            gridData,
            settings,
            HexObstaclePressureContext.Empty,
            new Dictionary<HexCoordinates, HexObstacleInstance>(),
            new HashSet<HexCoordinates> { new HexCoordinates(2, 1) },
            new HashSet<HexCoordinates> { new HexCoordinates(2, 1) },
            new HashSet<HexCoordinates>(),
            new HashSet<HexCoordinates>(),
            new HexCoordinates(2, 2),
            new HexCoordinates(0, 0),
            new HexCoordinates(4, 4),
            definitions,
            new System.Random(1234),
            true);

        Assert.That(plan.ShouldSpawn, Is.True);
        Assert.That(plan.Definition, Is.Not.Null);
        Assert.That(plan.Coordinates, Is.EqualTo(new HexCoordinates(2, 1)));
    }
}
