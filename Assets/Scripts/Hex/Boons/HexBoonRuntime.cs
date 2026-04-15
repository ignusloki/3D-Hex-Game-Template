using UnityEngine;

public readonly struct HexBoonChargeChangeResult
{
    public static HexBoonChargeChangeResult None { get; } = new(false, 0, 0, string.Empty);

    public HexBoonChargeChangeResult(bool changed, int previousCharges, int currentCharges, string message)
    {
        Changed = changed;
        PreviousCharges = previousCharges;
        CurrentCharges = currentCharges;
        Message = message ?? string.Empty;
    }

    public bool Changed { get; }
    public int PreviousCharges { get; }
    public int CurrentCharges { get; }
    public string Message { get; }
}

public sealed class HexBoonRuntimeState
{
    public HexBoonRuntimeState(HexBoonDefinition definition)
    {
        if (definition != null)
        {
            definition.Validate();
        }

        Definition = definition != null && definition.isEnabled ? definition : null;
        MaxCharges = Definition != null ? Definition.GetMaxCharges() : 0;
        CurrentCharges = Definition != null ? Definition.GetStartingCharges() : 0;
    }

    public HexBoonDefinition Definition { get; }
    public int CurrentCharges { get; private set; }
    public int MaxCharges { get; }
    public bool HasActiveBoon => Definition != null && Definition.isEnabled;

    public static HexBoonRuntimeState FromCurrentSelection()
    {
        return new HexBoonRuntimeState(HexBoonSelectionService.GetSelectedBoonDefinition());
    }

    public int GetVisibilityRadiusBonus()
    {
        return HasActiveBoon ? Definition.GetVisibilityRadiusBonus() : 0;
    }

    public HexMapGenerationModifiers GetMapGenerationModifiers()
    {
        if (!HasActiveBoon)
        {
            return HexMapGenerationModifiers.None;
        }

        return Definition.GetMapGenerationModifiers();
    }

    public bool TryConsumeObstacleIgnore(out HexBoonChargeChangeResult chargeChange)
    {
        chargeChange = HexBoonChargeChangeResult.None;
        if (!HasActiveBoon
            || !Definition.SupportsIgnoredObstaclePenalty()
            || CurrentCharges <= 0)
        {
            return false;
        }

        int previousCharges = CurrentCharges;
        CurrentCharges = Mathf.Max(0, CurrentCharges - 1);
        chargeChange = new HexBoonChargeChangeResult(
            true,
            previousCharges,
            CurrentCharges,
            $"Boon: {Definition.GetResolvedDisplayName()} ignored the obstacle penalty ({CurrentCharges}/{MaxCharges} charges left).");
        return true;
    }

    public bool TryRecharge(HexBoonRechargeTrigger trigger, out HexBoonChargeChangeResult chargeChange)
    {
        chargeChange = HexBoonChargeChangeResult.None;
        if (!HasActiveBoon
            || !Definition.CanRechargeOn(trigger)
            || MaxCharges <= 0
            || CurrentCharges >= MaxCharges)
        {
            return false;
        }

        int previousCharges = CurrentCharges;
        CurrentCharges = MaxCharges;
        chargeChange = new HexBoonChargeChangeResult(
            true,
            previousCharges,
            CurrentCharges,
            $"Boon: {Definition.GetResolvedDisplayName()} recharged at the pitstop ({CurrentCharges}/{MaxCharges}).");
        return true;
    }

    public string GetStatusLine()
    {
        if (!HasActiveBoon)
        {
            return string.Empty;
        }

        string name = Definition.GetResolvedDisplayName();
        HexMapGenerationModifiers mapGenerationModifiers = Definition.GetMapGenerationModifiers();
        return Definition.category switch
        {
            HexBoonCategory.Passive when Definition.GetVisibilityRadiusBonus() > 0
                => $"Boon: {name} (+{Definition.GetVisibilityRadiusBonus()} visibility)",
            HexBoonCategory.Rechargeable when Definition.SupportsIgnoredObstaclePenalty()
                => $"Boon: {name} ({CurrentCharges}/{MaxCharges} charges, recharges at pitstops)",
            HexBoonCategory.MapModifying when Definition.GetAdditionalPitstopCount() > 0
                => $"Boon: {name} (+{Definition.GetAdditionalPitstopCount()} pitstop this act)",
            HexBoonCategory.MapModifying when mapGenerationModifiers.HasTerrainModifiers || mapGenerationModifiers.HasLandmarkRequests
                => $"Boon: {name} (modifies terrain generation)",
            _ => $"Boon: {name}"
        };
    }
}
