using System.Linq;
using NUnit.Framework;

public class HexBiomeMapGeneratorTests
{
    [Test]
    public void Generate_WithSameSeed_ProducesSameBiomeLayout()
    {
        HexBiomeGenerationSettings settings = CreateSettings();
        HexSpecialTileSettings specialTileSettings = CreateSpecialTileSettings();
        HexBiomeMapGenerator generator = new();

        HexBiomeMapResult firstMap = generator.Generate(20, 20, settings, specialTileSettings);
        HexBiomeMapResult secondMap = generator.Generate(20, 20, settings, specialTileSettings);

        for (int row = 0; row < 20; row++)
        {
            for (int column = 0; column < 20; column++)
            {
                Assert.That(firstMap.BiomeMap[row, column], Is.EqualTo(secondMap.BiomeMap[row, column]));
            }
        }

        Assert.That(firstMap.StartCoordinates, Is.EqualTo(secondMap.StartCoordinates));
        Assert.That(firstMap.GoalCoordinates, Is.EqualTo(secondMap.GoalCoordinates));
    }

    [TestCase(5, 5)]
    [TestCase(10, 10)]
    public void Generate_SupportsConfiguredSquareMapSizes(int rows, int columns)
    {
        HexBiomeGenerationSettings settings = CreateSettings();
        HexBiomeMapGenerator generator = new();

        HexBiomeMapResult result = generator.Generate(rows, columns, settings, CreateSpecialTileSettings());

        Assert.That(result.BiomeMap.GetLength(0), Is.EqualTo(rows));
        Assert.That(result.BiomeMap.GetLength(1), Is.EqualTo(columns));
        Assert.That(result.StartCoordinates.Column, Is.EqualTo(0));
        Assert.That(result.GoalCoordinates.Column, Is.EqualTo(columns - 1));
        Assert.That(result.StartCoordinates.Row == 0 || result.StartCoordinates.Row == rows - 1, Is.True);
        Assert.That(result.GoalCoordinates.Row, Is.EqualTo(rows - 1 - result.StartCoordinates.Row));
    }

    [Test]
    public void Generate_ProducesClusteredTerrainMoreOftenThanPureRandom()
    {
        HexBiomeGenerationSettings settings = CreateSettings();
        HexBiomeMapGenerator generator = new();
        Biome[,] biomeMap = generator.Generate(20, 20, settings, CreateSpecialTileSettings()).BiomeMap;
        HexGridData gridLayout = new(20, 20);

        float sameNeighborRatio = CalculateSameNeighborRatio(biomeMap, gridLayout);

        Assert.That(sameNeighborRatio, Is.GreaterThan(0.42f));
    }

    [Test]
    public void Generate_WithAnomaliesEnabled_ChangesSomeTilesComparedToBaseNoiseMap()
    {
        HexBiomeGenerationSettings baseSettings = CreateSettings();
        baseSettings.isolatedAnomalyChance = 0f;
        baseSettings.microPatchChance = 0f;

        HexBiomeGenerationSettings anomalySettings = CreateSettings();
        anomalySettings.isolatedAnomalyChance = 0.12f;
        anomalySettings.microPatchChance = 0.08f;

        HexBiomeMapGenerator generator = new();
        HexSpecialTileSettings specialTileSettings = CreateSpecialTileSettings();
        Biome[,] baselineMap = generator.Generate(20, 20, baseSettings, specialTileSettings).BiomeMap;
        Biome[,] anomalyMap = generator.Generate(20, 20, anomalySettings, specialTileSettings).BiomeMap;

        bool foundDifference = false;
        for (int row = 0; row < 20 && !foundDifference; row++)
        {
            for (int column = 0; column < 20; column++)
            {
                if (baselineMap[row, column] != anomalyMap[row, column])
                {
                    foundDifference = true;
                    break;
                }
            }
        }

        Assert.That(foundDifference, Is.True);
    }

    [Test]
    public void Generate_WithDesertDisabled_HasNoDesertTiles()
    {
        HexBiomeGenerationSettings settings = CreateSettings();
        settings.enableDesert = false;

        HexBiomeMapGenerator generator = new();
        Biome[,] biomeMap = generator.Generate(20, 20, settings, CreateSpecialTileSettings()).BiomeMap;

        for (int row = 0; row < 20; row++)
        {
            for (int column = 0; column < 20; column++)
            {
                Assert.That(biomeMap[row, column], Is.Not.EqualTo(Biome.desert));
            }
        }
    }

    [Test]
    public void Generate_EnforcesConfiguredStartAndGoalBiomes()
    {
        HexBiomeGenerationSettings settings = CreateSettings();
        HexSpecialTileSettings specialTileSettings = new()
        {
            enforceStartBiome = true,
            startBiome = Biome.grass,
            enforceGoalBiome = true,
            goalBiome = Biome.forest
        };

        HexBiomeMapGenerator generator = new();
        HexBiomeMapResult result = generator.Generate(20, 20, settings, specialTileSettings);

        Assert.That(result.StartCoordinates.Column, Is.EqualTo(0));
        Assert.That(result.GoalCoordinates.Column, Is.EqualTo(19));
        Assert.That(result.StartCoordinates.Row == 0 || result.StartCoordinates.Row == 19, Is.True);
        Assert.That(result.GoalCoordinates.Row, Is.EqualTo(19 - result.StartCoordinates.Row));
        Assert.That(result.BiomeMap[result.StartCoordinates.Row, result.StartCoordinates.Column], Is.EqualTo(Biome.grass));
        Assert.That(result.BiomeMap[result.GoalCoordinates.Row, result.GoalCoordinates.Column], Is.EqualTo(Biome.forest));
    }

    [Test]
    public void Generate_EnforcesSpecialGoalBiomeEvenWhenThatBiomeIsDisabledGlobally()
    {
        HexBiomeGenerationSettings settings = CreateSettings();
        settings.enableDesert = false;

        HexSpecialTileSettings specialTileSettings = new()
        {
            enforceStartBiome = true,
            startBiome = Biome.grass,
            enforceGoalBiome = true,
            goalBiome = Biome.desert
        };

        HexBiomeMapGenerator generator = new();
        HexBiomeMapResult result = generator.Generate(20, 20, settings, specialTileSettings);

        Assert.That(result.BiomeMap[result.GoalCoordinates.Row, result.GoalCoordinates.Column], Is.EqualTo(Biome.desert));
    }

    [Test]
    public void Generate_WithQualityRerollsEnabled_ReturnsAcceptableMap()
    {
        HexBiomeGenerationSettings settings = CreateSettings();
        settings.qualitySettings = new HexBiomeMapQualitySettings
        {
            enableQualityRerolls = true,
            maxGenerationAttempts = 20,
            maxDominantBiomeRatio = 0.68f,
            minDistinctBiomeCount = 3,
            minSecondaryRegionRatio = 0.1f,
            minMeaningfulRegionSize = 3,
            maxSmallSecondaryRegionRatio = 0.1f
        };

        HexSpecialTileSettings specialTileSettings = CreateSpecialTileSettings();
        HexBiomeMapGenerator generator = new();
        HexBiomeMapResult result = generator.Generate(20, 20, settings, specialTileSettings);
        HexBiomeMapQualityEvaluator evaluator = new();
        HexBiomeMapQualityReport report = evaluator.Evaluate(result.BiomeMap, new HexGridData(20, 20), settings, specialTileSettings);

        Assert.That(report.IsAcceptable, Is.True);
        Assert.That(generator.LastGenerationAttempts, Is.InRange(1, settings.qualitySettings.maxGenerationAttempts));
        Assert.That(generator.LastQualityReport, Is.Not.Null);
    }

    [Test]
    public void Generate_WithFeatureOverlaysEnabled_AddsStructuredTerrainToPlainBaseMap()
    {
        HexBiomeGenerationSettings settings = CreateSettings();
        settings.useRandomSeed = false;
        settings.seed = 24680;
        settings.enableDesert = false;
        settings.waterThreshold = 0f;
        settings.mountainThreshold = 1f;
        settings.forestMoistureThreshold = 1f;
        settings.desertMoistureThreshold = 0f;
        settings.desertHeatThreshold = 1f;
        settings.isolatedAnomalyChance = 0f;
        settings.microPatchChance = 0f;
        settings.regionSettings = new HexBiomeRegionSettings
        {
            enableMacroRegions = false
        };
        settings.featureSettings = new HexBiomeFeatureSettings
        {
            enableFeatureOverlays = true,
            minForestFeatureCount = 1,
            maxForestFeatureCount = 1,
            forestFeatureMinRatio = 0.18f,
            forestFeatureMaxRatio = 0.18f,
            minWaterFeatureCount = 1,
            maxWaterFeatureCount = 1,
            waterFeatureMinRatio = 0.05f,
            waterFeatureMaxRatio = 0.05f,
            minMountainFeatureCount = 1,
            maxMountainFeatureCount = 1,
            mountainFeatureMinRatio = 0.03f,
            mountainFeatureMaxRatio = 0.03f,
            featureEdgePadding = 1,
            compactnessBias = 0.8f,
            ridgeTurnChance = 0.3f,
            ridgeBranchChance = 0.1f
        };
        settings.qualitySettings = new HexBiomeMapQualitySettings
        {
            enableQualityRerolls = false,
            maxGenerationAttempts = 1,
            maxDominantBiomeRatio = 0.95f,
            minDistinctBiomeCount = 1,
            minSecondaryRegionRatio = 0f,
            minMeaningfulRegionSize = 1,
            maxSmallSecondaryRegionRatio = 0.3f
        };
        settings.Validate();

        HexBiomeMapGenerator generator = new();
        HexBiomeMapResult result = generator.Generate(20, 20, settings, CreateSpecialTileSettings());

        int forestCount = CountBiome(result.BiomeMap, Biome.forest);
        int waterCount = CountBiome(result.BiomeMap, Biome.water);
        int mountainCount = CountBiome(result.BiomeMap, Biome.mountain);

        Assert.That(forestCount, Is.GreaterThanOrEqualTo(50));
        Assert.That(waterCount, Is.GreaterThanOrEqualTo(10));
        Assert.That(mountainCount, Is.GreaterThanOrEqualTo(6));
    }

    private static HexBiomeGenerationSettings CreateSettings()
    {
        HexBiomeGenerationSettings settings = new()
        {
            enableGrass = true,
            enableForest = true,
            enableMountain = true,
            enableWater = true,
            enableDesert = true,
            useRandomSeed = false,
            seed = 12345,
            elevationFrequency = 0.11f,
            moistureFrequency = 0.095f,
            heatFrequency = 0.085f,
            noiseOctaves = 3,
            noisePersistence = 0.5f,
            noiseLacunarity = 2f,
            waterThreshold = 0.28f,
            mountainThreshold = 0.72f,
            forestMoistureThreshold = 0.58f,
            desertMoistureThreshold = 0.34f,
            desertHeatThreshold = 0.57f,
            isolatedAnomalyChance = 0.03f,
            microPatchChance = 0.03f,
            microPatchMinSize = 2,
            microPatchMaxSize = 4
        };

        settings.Validate();
        return settings;
    }

    private static HexSpecialTileSettings CreateSpecialTileSettings()
    {
        return new HexSpecialTileSettings
        {
            enforceStartBiome = true,
            startBiome = Biome.grass,
            enforceGoalBiome = true,
            goalBiome = Biome.grass
        };
    }

    private static float CalculateSameNeighborRatio(Biome[,] biomeMap, HexGridData gridLayout)
    {
        int samePairs = 0;
        int totalPairs = 0;

        for (int row = 0; row < biomeMap.GetLength(0); row++)
        {
            for (int column = 0; column < biomeMap.GetLength(1); column++)
            {
                HexCoordinates coordinates = new(row, column);
                foreach (HexCoordinates neighbor in gridLayout.GetNeighborCoordinates(coordinates)
                    .Where(neighbor => neighbor.Row > row || (neighbor.Row == row && neighbor.Column > column)))
                {
                    totalPairs++;
                    if (biomeMap[row, column] == biomeMap[neighbor.Row, neighbor.Column])
                    {
                        samePairs++;
                    }
                }
            }
        }

        return totalPairs > 0 ? (float)samePairs / totalPairs : 0f;
    }

    private static int CountBiome(Biome[,] biomeMap, Biome biome)
    {
        int count = 0;
        for (int row = 0; row < biomeMap.GetLength(0); row++)
        {
            for (int column = 0; column < biomeMap.GetLength(1); column++)
            {
                if (biomeMap[row, column] == biome)
                {
                    count++;
                }
            }
        }

        return count;
    }
}
