using NUnit.Framework;

public class HexBiomeMapQualityEvaluatorTests
{
    [Test]
    public void Evaluate_RejectsDominantSingleBlobLayout()
    {
        Biome[,] biomeMap =
        {
            { Biome.grass, Biome.grass, Biome.grass, Biome.grass },
            { Biome.grass, Biome.grass, Biome.grass, Biome.grass },
            { Biome.grass, Biome.grass, Biome.grass, Biome.grass },
            { Biome.grass, Biome.grass, Biome.mountain, Biome.water }
        };

        HexBiomeGenerationSettings settings = CreateSettings();
        HexBiomeMapQualityEvaluator evaluator = new();
        HexBiomeMapQualityReport report = evaluator.Evaluate(
            biomeMap,
            new HexGridData(4, 4),
            settings,
            CreateSpecialTileSettings());

        Assert.That(report.IsAcceptable, Is.False);
        Assert.That(report.DominantBiome, Is.EqualTo(Biome.grass));
        Assert.That(report.DominantBiomeRatio, Is.GreaterThan(0.68f));
    }

    [Test]
    public void Evaluate_AcceptsBalancedFeatureLayout()
    {
        Biome[,] biomeMap =
        {
            { Biome.forest, Biome.forest, Biome.forest, Biome.grass },
            { Biome.forest, Biome.forest, Biome.grass, Biome.grass },
            { Biome.water, Biome.water, Biome.grass, Biome.grass },
            { Biome.water, Biome.mountain, Biome.grass, Biome.grass }
        };

        HexBiomeGenerationSettings settings = CreateSettings();
        HexBiomeMapQualityEvaluator evaluator = new();
        HexBiomeMapQualityReport report = evaluator.Evaluate(
            biomeMap,
            new HexGridData(4, 4),
            settings,
            CreateSpecialTileSettings());

        Assert.That(report.IsAcceptable, Is.True);
        Assert.That(report.DistinctBiomeCount, Is.EqualTo(4));
        Assert.That(report.LargestSecondaryRegionRatio, Is.GreaterThanOrEqualTo(0.18f));
    }

    private static HexBiomeGenerationSettings CreateSettings()
    {
        HexBiomeGenerationSettings settings = new()
        {
            enableGrass = true,
            enableForest = true,
            enableMountain = true,
            enableWater = true,
            enableDesert = false,
            qualitySettings = new HexBiomeMapQualitySettings
            {
                enableQualityRerolls = true,
                maxGenerationAttempts = 10,
                maxDominantBiomeRatio = 0.68f,
                minDistinctBiomeCount = 3,
                minSecondaryRegionRatio = 0.12f,
                minMeaningfulRegionSize = 3,
                maxSmallSecondaryRegionRatio = 0.08f
            }
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
}
