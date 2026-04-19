using NUnit.Framework;
using UnityEngine;

public class HexBoonRuntimeStateTests
{
    [Test]
    public void PassiveHunterBoon_ReportsVisibilityBonusFromDefinition()
    {
        HexBoonDefinition definition = CreatePassiveBoon();
        HexBoonRuntimeState runtime = new(definition);

        Assert.That(runtime.HasActiveBoon, Is.True);
        Assert.That(runtime.GetVisibilityRadiusBonus(), Is.EqualTo(1));
        Assert.That(runtime.GetStatusLine(), Does.Contain("+1 visibility"));
    }

    [Test]
    public void RechargeableHunterBoon_ConsumesChargeAndRechargesAtPitstop()
    {
        HexBoonDefinition definition = CreateRechargeableBoon();
        HexBoonRuntimeState runtime = new(definition);

        Assert.That(runtime.CurrentCharges, Is.EqualTo(1));
        Assert.That(runtime.TryConsumeObstacleIgnore(out HexBoonChargeChangeResult spentCharge), Is.True);
        Assert.That(spentCharge.CurrentCharges, Is.EqualTo(0));
        Assert.That(runtime.CurrentCharges, Is.EqualTo(0));
        Assert.That(runtime.TryConsumeObstacleIgnore(out _), Is.False);

        Assert.That(runtime.TryRecharge(HexBoonRechargeTrigger.PitstopArrival, out HexBoonChargeChangeResult recharge), Is.True);
        Assert.That(recharge.CurrentCharges, Is.EqualTo(1));
        Assert.That(runtime.CurrentCharges, Is.EqualTo(1));
    }

    [Test]
    public void MapHunterBoon_ExposesExtraPitstopModifier()
    {
        HexBoonDefinition definition = CreateMapModifierBoon();
        HexBoonRuntimeState runtime = new(definition);

        HexMapGenerationModifiers modifiers = runtime.GetMapGenerationModifiers();
        Assert.That(modifiers.ExtraPitstopCount, Is.EqualTo(1));
        Assert.That(modifiers.ExtraPitstopPlacementBand, Is.EqualTo(HexBoonMapPlacementBand.Mid));
        Assert.That(runtime.GetStatusLine(), Does.Contain("+1 pitstop"));
    }

    [Test]
    public void MapBoon_ExposesTerrainGenerationModifiers()
    {
        HexBoonDefinition definition = ScriptableObject.CreateInstance<HexBoonDefinition>();
        definition.displayName = "Waters of Mercy";
        definition.category = HexBoonCategory.MapModifying;
        definition.mapModifier.waterThresholdDelta = 0.1f;
        definition.Validate();

        HexBoonRuntimeState runtime = new(definition);
        HexMapGenerationModifiers modifiers = runtime.GetMapGenerationModifiers();

        Assert.That(modifiers.WaterThresholdDelta, Is.EqualTo(0.1f).Within(0.0001f));
        Assert.That(runtime.GetStatusLine(), Does.Contain("modifies terrain generation"));
    }

    [Test]
    public void MultipleBoons_StackVisibilityAndMapGenerationModifiers()
    {
        HexBoonDefinition passiveDefinition = CreatePassiveBoon();
        HexBoonDefinition mapDefinition = CreateMapModifierBoon();

        HexBoonRuntimeState runtime = new(new[] { passiveDefinition, mapDefinition });
        HexMapGenerationModifiers modifiers = runtime.GetMapGenerationModifiers();

        Assert.That(runtime.HasActiveBoons, Is.True);
        Assert.That(runtime.GetVisibilityRadiusBonus(), Is.EqualTo(1));
        Assert.That(modifiers.ExtraPitstopCount, Is.EqualTo(1));
        Assert.That(runtime.GetStatusLine(), Does.StartWith("Boons:"));
        Assert.That(runtime.GetStatusLine(), Does.Contain("Pillar of Fire"));
        Assert.That(runtime.GetStatusLine(), Does.Contain("Stations of the March"));
    }

    [Test]
    public void HunterBoon_ExposesBiomeVisibilityRules()
    {
        HexBoonDefinition definition = ScriptableObject.CreateInstance<HexBoonDefinition>();
        definition.displayName = "The Green Veil";
        definition.category = HexBoonCategory.MapModifying;
        definition.archetypeFamily = HexNemesisArchetype.Hunter;
        definition.mapModifier.grassRegionWeightMultiplier = 1.3f;
        definition.nemesisModifier.visibilityRules = new[]
        {
            new HexBoonNemesisVisibilityRuleData
            {
                visibilityMode = HexBoonNemesisVisibilityMode.Hidden,
                affectedBiomes = new[] { Biome.grass }
            }
        };
        definition.Validate();

        HexBoonRuntimeState runtime = new(definition);
        HexNemesisRuntimeModifiers modifiers = runtime.GetNemesisRuntimeModifiers(HexNemesisArchetype.Hunter);

        Assert.That(modifiers.GetVisibilityModeForBiome(Biome.grass), Is.EqualTo(HexBoonNemesisVisibilityMode.Hidden));
        Assert.That(modifiers.GetVisibilityModeForBiome(Biome.desert), Is.EqualTo(HexBoonNemesisVisibilityMode.None));
        Assert.That(runtime.GetMapGenerationModifiers().GrassRegionWeightMultiplier, Is.EqualTo(1.3f).Within(0.0001f));
    }

    [Test]
    public void Boon_ActStartGrantAppliesOnceToSnapshot()
    {
        HexBoonDefinition definition = ScriptableObject.CreateInstance<HexBoonDefinition>();
        definition.displayName = "Ember Under Ash";
        definition.archetypeFamily = HexNemesisArchetype.Hunter;
        definition.actStartGrant.food = 5;
        definition.actStartGrant.morale = 3;
        definition.actStartGrant.gold = 3;
        definition.Validate();

        CaravanResourceSnapshot result = definition.ApplyActStartGrant(new CaravanResourceSnapshot(10, 2, 1));

        Assert.That(definition.HasActStartGrant(), Is.True);
        Assert.That(result.Food, Is.EqualTo(15));
        Assert.That(result.Morale, Is.EqualTo(5));
        Assert.That(result.Gold, Is.EqualTo(4));
    }

    [Test]
    public void HunterBoon_ExposesStartPressureModifier()
    {
        HexBoonDefinition definition = ScriptableObject.CreateInstance<HexBoonDefinition>();
        definition.displayName = "Ember Under Ash";
        definition.archetypeFamily = HexNemesisArchetype.Hunter;
        definition.nemesisModifier.startStepsTowardCaravanSpawn = 2;
        definition.Validate();

        HexBoonRuntimeState runtime = new(definition);
        HexNemesisRuntimeModifiers modifiers = runtime.GetNemesisRuntimeModifiers(HexNemesisArchetype.Hunter);

        Assert.That(modifiers.ResolveStartStepsTowardCaravanSpawn(), Is.EqualTo(2));
    }

    private static HexBoonDefinition CreatePassiveBoon()
    {
        HexBoonDefinition definition = ScriptableObject.CreateInstance<HexBoonDefinition>();
        definition.displayName = "Pillar of Fire";
        definition.category = HexBoonCategory.Passive;
        definition.archetypeFamily = HexNemesisArchetype.Hunter;
        definition.passive.effectType = HexBoonPassiveEffectType.VisibilityRadiusBonus;
        definition.passive.magnitude = 1;
        definition.Validate();
        return definition;
    }

    private static HexBoonDefinition CreateRechargeableBoon()
    {
        HexBoonDefinition definition = ScriptableObject.CreateInstance<HexBoonDefinition>();
        definition.displayName = "Staff of the Guide";
        definition.category = HexBoonCategory.Rechargeable;
        definition.archetypeFamily = HexNemesisArchetype.Hunter;
        definition.rechargeable.effectType = HexBoonRechargeableEffectType.IgnoreObstaclePenalty;
        definition.rechargeable.maxCharges = 1;
        definition.rechargeable.startingCharges = 1;
        definition.rechargeable.rechargeTrigger = HexBoonRechargeTrigger.PitstopArrival;
        definition.Validate();
        return definition;
    }

    private static HexBoonDefinition CreateMapModifierBoon()
    {
        HexBoonDefinition definition = ScriptableObject.CreateInstance<HexBoonDefinition>();
        definition.displayName = "Stations of the March";
        definition.category = HexBoonCategory.MapModifying;
        definition.archetypeFamily = HexNemesisArchetype.Hunter;
        definition.mapModifier.effectType = HexBoonMapModifierType.AdditionalPitstops;
        definition.mapModifier.magnitude = 1;
        definition.mapModifier.placementBand = HexBoonMapPlacementBand.Mid;
        definition.Validate();
        return definition;
    }
}
