using System.Collections.Generic;

public readonly struct PitstopResourceEffectResult
{
    public PitstopResourceEffectResult(CaravanResourceType resourceType, int amount)
    {
        ResourceType = resourceType;
        Amount = amount;
    }

    public CaravanResourceType ResourceType { get; }
    public int Amount { get; }
}

public sealed class PitstopEventResult
{
    public static PitstopEventResult Empty { get; } = new();

    public PitstopSite Site { get; set; }
    public PitstopEventDefinition Definition { get; set; }
    public bool IsFirstVisit { get; set; }
    public bool Triggered { get; set; }
    public bool EffectsApplied => AppliedEffects.Count > 0;
    public List<PitstopResourceEffectResult> AppliedEffects { get; } = new();

    public string Title => Definition?.title ?? Site?.EventTitle ?? "Pitstop";
    public string Description => Definition?.description ?? Site?.SpecialEventDescription ?? string.Empty;
}
