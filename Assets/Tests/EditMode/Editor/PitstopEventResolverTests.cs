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
}
