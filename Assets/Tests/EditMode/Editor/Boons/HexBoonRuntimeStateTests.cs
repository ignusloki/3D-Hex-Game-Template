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

        HexActMapModifiers modifiers = runtime.GetActMapModifiers();
        Assert.That(modifiers.ExtraPitstopCount, Is.EqualTo(1));
        Assert.That(modifiers.ExtraPitstopPlacementBand, Is.EqualTo(HexBoonMapPlacementBand.Mid));
        Assert.That(runtime.GetStatusLine(), Does.Contain("+1 pitstop"));
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
