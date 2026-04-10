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
            result.AppliedEffects.Add(new PitstopResourceEffectResult(effect.resourceType, effect.amount));
        }

        site.RegisterVisit();
        return result;
    }
}
