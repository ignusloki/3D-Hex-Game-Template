using NUnit.Framework;

public sealed class PitstopEventResolverTests
{
    [Test]
    public void FirstVisit_AppliesConfiguredRewards_AndMarksSiteVisited()
    {
        PitstopSite site = new UnityEngine.GameObject("Pitstop").AddComponent<PitstopSite>();
        site.Initialize(PitstopKind.Mill, new HexCoordinates(2, 3));

        CaravanResourceState resources = new();
        resources.Initialize(5, 2, 1);

        PitstopEventDefinition definition = PitstopEventDefinition.CreateDefaultSet().Find(item => item.kind == PitstopKind.Mill);

        PitstopEventResult result = PitstopEventResolver.ResolveArrival(site, definition, resources);

        Assert.That(result.Triggered, Is.True);
        Assert.That(result.IsFirstVisit, Is.True);
        Assert.That(result.EffectsApplied, Is.True);
        Assert.That(result.EntryEffects.Count, Is.EqualTo(1));
        Assert.That(resources.Food, Is.EqualTo(8));
        Assert.That(site.Visited, Is.True);
        Assert.That(site.VisitCount, Is.EqualTo(1));
    }

    [Test]
    public void RepeatVisit_OnNonRepeatableSite_DoesNotApplyRewardsAgain()
    {
        PitstopSite site = new UnityEngine.GameObject("Pitstop").AddComponent<PitstopSite>();
        site.Initialize(PitstopKind.Mansion, new HexCoordinates(1, 1));

        CaravanResourceState resources = new();
        resources.Initialize(5, 2, 1);

        PitstopEventDefinition definition = PitstopEventDefinition.CreateDefaultSet().Find(item => item.kind == PitstopKind.Mansion);

        PitstopEventResolver.ResolveArrival(site, definition, resources);
        PitstopEventResult secondResult = PitstopEventResolver.ResolveArrival(site, definition, resources);

        Assert.That(secondResult.Triggered, Is.False);
        Assert.That(secondResult.EffectsApplied, Is.False);
        Assert.That(resources.Gold, Is.EqualTo(3));
        Assert.That(site.VisitCount, Is.EqualTo(2));
    }

    [Test]
    public void ResolveChoice_AppliesChoiceEffects_AndClearsPendingState()
    {
        PitstopSite site = new UnityEngine.GameObject("Pitstop").AddComponent<PitstopSite>();
        site.Initialize(PitstopKind.WallTower, new HexCoordinates(4, 4));

        CaravanResourceState resources = new();
        resources.Initialize(5, 2, 1);

        PitstopEventDefinition definition = PitstopEventDefinition.CreateDefaultSet().Find(item => item.kind == PitstopKind.WallTower);
        PitstopEventResult arrivalResult = PitstopEventResolver.ResolveArrival(site, definition, resources);
        PitstopEncounterOption option = new()
        {
            label = "Take the watchfire meal",
            outcomeText = "The guards share a hot meal before dawn.",
            resourceEffects =
            {
                new PitstopResourceEffect { resourceType = CaravanResourceType.Food, amount = 1 },
                new PitstopResourceEffect { resourceType = CaravanResourceType.Morale, amount = 1 }
            }
        };

        arrivalResult.RequiresChoice = true;
        PitstopEventResult resolvedResult = PitstopEventResolver.ResolveChoice(arrivalResult, option, resources);

        Assert.That(resolvedResult.RequiresChoice, Is.False);
        Assert.That(resolvedResult.SelectedOption, Is.EqualTo(option));
        Assert.That(resources.Food, Is.EqualTo(6));
        Assert.That(resources.Morale, Is.EqualTo(4));
        Assert.That(resolvedResult.EntryEffects.Count, Is.EqualTo(1));
        Assert.That(resolvedResult.AppliedEffects.Count, Is.EqualTo(3));
        Assert.That(resolvedResult.ChoiceEffects.Count, Is.EqualTo(2));
    }

    [Test]
    public void ResolveChoice_DoesNotApply_WhenOptionCannotBeAfforded()
    {
        PitstopSite site = new UnityEngine.GameObject("Pitstop").AddComponent<PitstopSite>();
        site.Initialize(PitstopKind.Mansion, new HexCoordinates(5, 5));

        CaravanResourceState resources = new();
        resources.Initialize(4, 3, 0);

        PitstopEventDefinition definition = PitstopEventDefinition.CreateDefaultSet().Find(item => item.kind == PitstopKind.Mansion);
        PitstopEventResult arrivalResult = PitstopEventResolver.ResolveArrival(site, definition, resources);
        PitstopEncounterOption option = new()
        {
            label = "Pay the host",
            outcomeText = "The estate expects coin.",
            resourceEffects =
            {
                new PitstopResourceEffect { resourceType = CaravanResourceType.Gold, amount = -1 },
                new PitstopResourceEffect { resourceType = CaravanResourceType.Food, amount = 1 }
            }
        };

        arrivalResult.RequiresChoice = true;
        PitstopEventResult resolvedResult = PitstopEventResolver.ResolveChoice(arrivalResult, option, resources);

        Assert.That(resolvedResult.RequiresChoice, Is.True);
        Assert.That(resolvedResult.SelectedOption, Is.Null);
        Assert.That(resources.Gold, Is.EqualTo(2));
        Assert.That(resources.Food, Is.EqualTo(4));
        Assert.That(resolvedResult.ChoiceEffects.Count, Is.EqualTo(0));
    }

    [Test]
    public void Selector_SkipsEncounter_WhenResourcesDoNotMatchSelectionRules()
    {
        PitstopEncounterAsset gatedEncounter = CreateEncounter("food-low", PitstopKind.Mill);
        gatedEncounter.selectionRules.minimumFood = 0;
        gatedEncounter.selectionRules.maximumFood = 4;
        gatedEncounter.Validate();

        PitstopEncounterAsset fallbackEncounter = CreateEncounter("fallback", PitstopKind.Mill);
        fallbackEncounter.Validate();

        PitstopSite site = new UnityEngine.GameObject("Pitstop").AddComponent<PitstopSite>();
        site.Initialize(PitstopKind.Mill, new HexCoordinates(1, 1));

        PitstopEncounterSelectionContext context = new(
            site,
            true,
            1,
            new CaravanResourceSnapshot(8, 3, 2),
            new System.Collections.Generic.HashSet<string>());

        bool found = PitstopEncounterSelector.TryChooseEncounter(
            new System.Collections.Generic.List<PitstopEncounterAsset> { gatedEncounter, fallbackEncounter },
            context,
            out PitstopEncounterAsset chosenEncounter);

        Assert.That(found, Is.True);
        Assert.That(chosenEncounter, Is.EqualTo(fallbackEncounter));
    }

    [Test]
    public void Selector_SkipsEncounter_WhenItAlreadyTriggeredThisRun_AndRepeatsAreDisallowed()
    {
        PitstopEncounterAsset firstEncounter = CreateEncounter("seen-event", PitstopKind.Mansion);
        firstEncounter.selectionRules.allowRepeatSelectionInRun = false;
        firstEncounter.Validate();

        PitstopEncounterAsset otherEncounter = CreateEncounter("fresh-event", PitstopKind.Mansion);
        otherEncounter.Validate();

        PitstopSite site = new UnityEngine.GameObject("Pitstop").AddComponent<PitstopSite>();
        site.Initialize(PitstopKind.Mansion, new HexCoordinates(3, 3));

        PitstopEncounterSelectionContext context = new(
            site,
            true,
            2,
            new CaravanResourceSnapshot(6, 4, 5),
            new System.Collections.Generic.HashSet<string> { "seen-event" });

        bool found = PitstopEncounterSelector.TryChooseEncounter(
            new System.Collections.Generic.List<PitstopEncounterAsset> { firstEncounter, otherEncounter },
            context,
            out PitstopEncounterAsset chosenEncounter);

        Assert.That(found, Is.True);
        Assert.That(chosenEncounter, Is.EqualTo(otherEncounter));
    }

    [Test]
    public void Selector_SkipsEncounter_WhenNoChoiceCanBeAfforded()
    {
        PitstopEncounterAsset expensiveEncounter = CreateEncounter("expensive", PitstopKind.WallTower);
        expensiveEncounter.options[0].resourceEffects = new System.Collections.Generic.List<PitstopResourceEffect>
        {
            new() { resourceType = CaravanResourceType.Gold, amount = -1 },
            new() { resourceType = CaravanResourceType.Food, amount = 2 }
        };
        expensiveEncounter.Validate();

        PitstopEncounterAsset fallbackEncounter = CreateEncounter("fallback-free", PitstopKind.WallTower);
        fallbackEncounter.options[0].resourceEffects = new System.Collections.Generic.List<PitstopResourceEffect>
        {
            new() { resourceType = CaravanResourceType.Morale, amount = 1 }
        };
        fallbackEncounter.Validate();

        PitstopSite site = new UnityEngine.GameObject("Pitstop").AddComponent<PitstopSite>();
        site.Initialize(PitstopKind.WallTower, new HexCoordinates(2, 2));

        PitstopEncounterSelectionContext context = new(
            site,
            true,
            1,
            new CaravanResourceSnapshot(5, 3, 0),
            new System.Collections.Generic.HashSet<string>());

        bool found = PitstopEncounterSelector.TryChooseEncounter(
            new System.Collections.Generic.List<PitstopEncounterAsset> { expensiveEncounter, fallbackEncounter },
            context,
            out PitstopEncounterAsset chosenEncounter);

        Assert.That(found, Is.True);
        Assert.That(chosenEncounter, Is.EqualTo(fallbackEncounter));
    }

    private static PitstopEncounterAsset CreateEncounter(string eventId, PitstopKind kind)
    {
        PitstopEncounterAsset encounter = UnityEngine.ScriptableObject.CreateInstance<PitstopEncounterAsset>();
        encounter.eventId = eventId;
        encounter.pitstopKind = kind;
        encounter.selectionWeight = 1f;
        encounter.options = new System.Collections.Generic.List<PitstopEncounterOption>
        {
            new()
            {
                label = "Continue",
                outcomeText = "The caravan moves on."
            }
        };
        encounter.Validate();
        return encounter;
    }
}
