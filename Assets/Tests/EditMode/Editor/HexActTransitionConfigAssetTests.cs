using NUnit.Framework;
using UnityEngine;

public class HexActTransitionConfigAssetTests
{
    [Test]
    public void GetBoonOptionsForCompletedAct_ReturnsConfiguredActOneOptions()
    {
        HexBoonDefinition hunterBoon = CreateBoon("pillar-of-fire", "Pillar of Fire", HexNemesisArchetype.Hunter);
        HexBoonDefinition echoBoon = CreateBoon("echo-sign", "Echo Sign", HexNemesisArchetype.Echo);
        HexActTransitionConfigAsset config = CreateConfig(hunterBoon, echoBoon);

        HexBoonDefinition[] options = config.GetBoonOptionsForCompletedAct(1, HexNemesisArchetype.None, System.Array.Empty<HexBoonDefinition>());

        Assert.That(options, Has.Length.EqualTo(2));
        Assert.That(options, Has.Exactly(1).SameAs(hunterBoon));
        Assert.That(options, Has.Exactly(1).SameAs(echoBoon));
    }

    [Test]
    public void GetBoonOptionsForCompletedAct_FiltersToLockedFamilyAndPrefersUnselectedBoons()
    {
        HexBoonDefinition hunterBoonA = CreateBoon("pillar-of-fire", "Pillar of Fire", HexNemesisArchetype.Hunter);
        HexBoonDefinition hunterBoonB = CreateBoon("staff-of-the-guide", "Staff of the Guide", HexNemesisArchetype.Hunter);
        HexBoonDefinition echoBoon = CreateBoon("echo-sign", "Echo Sign", HexNemesisArchetype.Echo);
        HexActTransitionConfigAsset config = CreateConfig(hunterBoonA, hunterBoonB, echoBoon);

        HexBoonDefinition[] options = config.GetBoonOptionsForCompletedAct(2, HexNemesisArchetype.Hunter, new[] { hunterBoonA });

        Assert.That(options, Has.Length.EqualTo(1));
        Assert.That(options[0], Is.SameAs(hunterBoonB));
    }

    private static HexActTransitionConfigAsset CreateConfig(params HexBoonDefinition[] boons)
    {
        HexActTransitionConfigAsset config = ScriptableObject.CreateInstance<HexActTransitionConfigAsset>();
        config.act1ToAct2 = new HexActTransitionStepDefinition
        {
            boonOptions = boons
        };
        config.act2ToAct3 = new HexActTransitionStepDefinition
        {
            boonOptions = boons
        };
        config.OnValidateFromSceneController();
        return config;
    }

    private static HexBoonDefinition CreateBoon(string id, string displayName, HexNemesisArchetype family)
    {
        HexBoonDefinition definition = ScriptableObject.CreateInstance<HexBoonDefinition>();
        definition.id = id;
        definition.displayName = displayName;
        definition.archetypeFamily = family;
        definition.isEnabled = true;
        definition.Validate();
        return definition;
    }
}
