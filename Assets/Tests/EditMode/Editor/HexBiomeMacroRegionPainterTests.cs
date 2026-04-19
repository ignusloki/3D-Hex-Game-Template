using NUnit.Framework;

public class HexBiomeMacroRegionPainterTests
{
    [Test]
    public void ApplyLandRegions_ReplacesPlainLandWithMacroBiomeRegions()
    {
        Biome[,] biomeMap = new Biome[10, 10];
        for (int row = 0; row < 10; row++)
        {
            for (int column = 0; column < 10; column++)
            {
                biomeMap[row, column] = Biome.grass;
            }
        }

        HexBiomeGenerationSettings settings = new()
        {
            enableGrass = true,
            enableForest = true,
            enableMountain = true,
            enableWater = true,
            enableDesert = false,
            forestMoistureThreshold = 0.4f,
            regionSettings = new HexBiomeRegionSettings
            {
                enableMacroRegions = true,
                restrictBaseToLandBiomes = true,
                fiveByFiveMinRegions = 3,
                fiveByFiveMaxRegions = 3,
                tenByTenMinRegions = 7,
                tenByTenMaxRegions = 7,
                boundaryNoiseStrength = 0.25f,
                localVariationChance = 0f,
                seedSpacingBias = 0.8f
            }
        };
        settings.Validate();

        HexBiomeMacroRegionPainter painter = new();
        painter.ApplyLandRegions(
            biomeMap,
            new HexGridData(10, 10),
            settings,
            Biome.grass,
            HexMapGenerationModifiers.None,
            new System.Random(12345));

        int forestCount = CountBiome(biomeMap, Biome.forest);
        int waterCount = CountBiome(biomeMap, Biome.water);
        int mountainCount = CountBiome(biomeMap, Biome.mountain);

        Assert.That(forestCount, Is.GreaterThan(0));
        Assert.That(waterCount, Is.EqualTo(0));
        Assert.That(mountainCount, Is.EqualTo(0));
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
