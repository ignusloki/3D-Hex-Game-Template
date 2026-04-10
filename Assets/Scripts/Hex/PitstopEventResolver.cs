public static class PitstopEventResolver
{
    public static PitstopEventResult ResolveArrival(PitstopSite site, PitstopEventDefinition definition, CaravanResourceState resources)
    {
        if (site == null || definition == null || resources == null)
        {
            return PitstopEventResult.Empty;
        }

        bool isFirstVisit = !site.Visited;
        PitstopEventResult result = new()
        {
            Site = site,
            Definition = definition,
            IsFirstVisit = isFirstVisit,
            Triggered = isFirstVisit || definition.repeatable
        };

        if (!result.Triggered)
        {
            site.RegisterVisit();
            return result;
        }

        foreach (PitstopResourceEffect effect in definition.GetEffects(isFirstVisit))
        {
            if (effect == null || effect.amount == 0)
            {
                continue;
            }

            resources.ApplyDelta(effect.resourceType, effect.amount);
            PitstopResourceEffectResult effectResult = new(effect.resourceType, effect.amount);
            result.EntryEffects.Add(effectResult);
            result.AppliedEffects.Add(effectResult);
        }

        site.RegisterVisit();
        return result;
    }

    public static PitstopEventResult ResolveChoice(PitstopEventResult result, PitstopEncounterOption option, CaravanResourceState resources)
    {
        if (result == null || option == null || resources == null)
        {
            return PitstopEventResult.Empty;
        }

        result.RequiresChoice = false;
        result.SelectedOption = option;
        option.Validate();

        foreach (PitstopResourceEffect effect in option.resourceEffects)
        {
            if (effect == null || effect.amount == 0)
            {
                continue;
            }

            resources.ApplyDelta(effect.resourceType, effect.amount);
            PitstopResourceEffectResult effectResult = new(effect.resourceType, effect.amount);
            result.ChoiceEffects.Add(effectResult);
            result.AppliedEffects.Add(effectResult);
        }

        return result;
    }
}
