using System.Collections.Generic;
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

internal sealed class HexActiveBoonState
{
    public HexActiveBoonState(HexBoonDefinition definition)
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
    public bool HasRechargeableCharges => Definition != null && Definition.SupportsIgnoredObstaclePenalty();

    public bool TryConsumeObstacleIgnore(out HexBoonChargeChangeResult chargeChange)
    {
        chargeChange = HexBoonChargeChangeResult.None;
        if (Definition == null
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
        if (Definition == null
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
}

public sealed class HexBoonRuntimeState
{
    private readonly List<HexActiveBoonState> activeBoons = new();

    public HexBoonRuntimeState(HexBoonDefinition definition)
        : this(definition != null ? new[] { definition } : null)
    {
    }

    public HexBoonRuntimeState(IEnumerable<HexBoonDefinition> definitions)
    {
        if (definitions == null)
        {
            return;
        }

        HashSet<string> seenIds = new(System.StringComparer.OrdinalIgnoreCase);
        foreach (HexBoonDefinition definition in definitions)
        {
            if (definition == null)
            {
                continue;
            }

            definition.Validate();
            if (!definition.isEnabled || !seenIds.Add(definition.id))
            {
                continue;
            }

            activeBoons.Add(new HexActiveBoonState(definition));
        }
    }

    public HexBoonDefinition Definition => activeBoons.Count > 0 ? activeBoons[0].Definition : null;
    public int CurrentCharges => GetPrimaryRechargeableBoonState()?.CurrentCharges ?? 0;
    public int MaxCharges => GetPrimaryRechargeableBoonState()?.MaxCharges ?? 0;
    public bool HasActiveBoons => activeBoons.Count > 0;
    public bool HasActiveBoon => HasActiveBoons;

    public static HexBoonRuntimeState FromCurrentSelection()
    {
        return new HexBoonRuntimeState(HexBoonSelectionService.GetSelectedBoonDefinitions());
    }

    public int GetVisibilityRadiusBonus()
    {
        int totalBonus = 0;
        for (int index = 0; index < activeBoons.Count; index++)
        {
            HexBoonDefinition definition = activeBoons[index].Definition;
            if (definition == null)
            {
                continue;
            }

            totalBonus += definition.GetVisibilityRadiusBonus();
        }

        return totalBonus;
    }

    public HexMapGenerationModifiers GetMapGenerationModifiers()
    {
        HexMapGenerationModifiers modifiers = HexMapGenerationModifiers.None;
        for (int index = 0; index < activeBoons.Count; index++)
        {
            HexBoonDefinition definition = activeBoons[index].Definition;
            if (definition == null)
            {
                continue;
            }

            modifiers = modifiers.Combine(definition.GetMapGenerationModifiers());
        }

        return modifiers;
    }

    public HexNemesisRuntimeModifiers GetNemesisRuntimeModifiers(HexNemesisArchetype archetype)
    {
        HexNemesisRuntimeModifiers modifiers = HexNemesisRuntimeModifiers.None;
        for (int index = 0; index < activeBoons.Count; index++)
        {
            HexBoonDefinition definition = activeBoons[index].Definition;
            if (definition == null || definition.archetypeFamily != archetype)
            {
                continue;
            }

            modifiers = modifiers.Combine(definition.GetNemesisRuntimeModifiers());
        }

        return modifiers;
    }

    public bool TryConsumeObstacleIgnore(out HexBoonChargeChangeResult chargeChange)
    {
        chargeChange = HexBoonChargeChangeResult.None;
        for (int index = 0; index < activeBoons.Count; index++)
        {
            HexActiveBoonState activeBoon = activeBoons[index];
            if (activeBoon.TryConsumeObstacleIgnore(out chargeChange))
            {
                return true;
            }
        }

        return false;
    }

    public bool TryRecharge(HexBoonRechargeTrigger trigger, out HexBoonChargeChangeResult chargeChange)
    {
        chargeChange = HexBoonChargeChangeResult.None;
        List<string> messages = null;
        int previousCharges = 0;
        int currentCharges = 0;
        bool changed = false;

        for (int index = 0; index < activeBoons.Count; index++)
        {
            HexActiveBoonState activeBoon = activeBoons[index];
            if (!activeBoon.TryRecharge(trigger, out HexBoonChargeChangeResult recharge))
            {
                continue;
            }

            if (!changed)
            {
                previousCharges = recharge.PreviousCharges;
            }

            changed = true;
            currentCharges = recharge.CurrentCharges;
            messages ??= new List<string>();
            if (!string.IsNullOrWhiteSpace(recharge.Message))
            {
                messages.Add(recharge.Message);
            }
        }

        if (!changed)
        {
            return false;
        }

        string combinedMessage = messages != null && messages.Count > 0
            ? string.Join("\n", messages)
            : string.Empty;
        chargeChange = new HexBoonChargeChangeResult(true, previousCharges, currentCharges, combinedMessage);
        return true;
    }

    public string GetStatusLine()
    {
        if (!HasActiveBoons)
        {
            return string.Empty;
        }

        List<string> segments = new(activeBoons.Count);
        for (int index = 0; index < activeBoons.Count; index++)
        {
            HexActiveBoonState activeBoon = activeBoons[index];
            HexBoonDefinition definition = activeBoon.Definition;
            if (definition == null)
            {
                continue;
            }

            HexMapGenerationModifiers mapGenerationModifiers = definition.GetMapGenerationModifiers();
            string name = definition.GetResolvedDisplayName();
            string segment = definition.category switch
            {
                HexBoonCategory.Passive when definition.GetVisibilityRadiusBonus() > 0
                    => $"{name} (+{definition.GetVisibilityRadiusBonus()} visibility)",
                HexBoonCategory.Rechargeable when definition.SupportsIgnoredObstaclePenalty()
                    => $"{name} ({activeBoon.CurrentCharges}/{activeBoon.MaxCharges} charges)",
                HexBoonCategory.MapModifying when definition.GetAdditionalPitstopCount() > 0
                    => $"{name} (+{definition.GetAdditionalPitstopCount()} pitstop this act)",
                HexBoonCategory.MapModifying when mapGenerationModifiers.HasTerrainModifiers
                                                        || mapGenerationModifiers.HasLandmarkRequests
                                                        || mapGenerationModifiers.HasMapObjectRequests
                    => $"{name} (modifies terrain generation)",
                _ => name
            };
            segments.Add(segment);
        }

        if (segments.Count == 0)
        {
            return string.Empty;
        }

        if (segments.Count == 1)
        {
            return $"Boon: {segments[0]}";
        }

        return $"Boons: {string.Join("; ", segments)}";
    }

    private HexActiveBoonState GetPrimaryRechargeableBoonState()
    {
        for (int index = 0; index < activeBoons.Count; index++)
        {
            HexActiveBoonState activeBoon = activeBoons[index];
            if (activeBoon.HasRechargeableCharges)
            {
                return activeBoon;
            }
        }

        return null;
    }
}
