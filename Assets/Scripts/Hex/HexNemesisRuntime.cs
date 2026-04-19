using System.Collections.Generic;
using UnityEngine;

public readonly struct HexNemesisVisibilityRule
{
    public HexNemesisVisibilityRule(HexBoonNemesisVisibilityMode visibilityMode, Biome[] affectedBiomes)
    {
        VisibilityMode = visibilityMode;
        AffectedBiomes = SanitizeBiomes(affectedBiomes);
    }

    public HexBoonNemesisVisibilityMode VisibilityMode { get; }
    public Biome[] AffectedBiomes { get; }

    public bool HasAny =>
        VisibilityMode != HexBoonNemesisVisibilityMode.None
        && AffectedBiomes != null
        && AffectedBiomes.Length > 0;

    public bool Matches(Biome biome)
    {
        if (!HasAny)
        {
            return false;
        }

        for (int index = 0; index < AffectedBiomes.Length; index++)
        {
            if (AffectedBiomes[index] == biome)
            {
                return true;
            }
        }

        return false;
    }

    private static Biome[] SanitizeBiomes(Biome[] source)
    {
        if (source == null || source.Length == 0)
        {
            return System.Array.Empty<Biome>();
        }

        HashSet<Biome> uniqueBiomes = new();
        for (int index = 0; index < source.Length; index++)
        {
            uniqueBiomes.Add(source[index]);
        }

        Biome[] sanitized = new Biome[uniqueBiomes.Count];
        uniqueBiomes.CopyTo(sanitized);
        return sanitized;
    }
}

public readonly struct HexNemesisRuntimeModifiers
{
    public static HexNemesisRuntimeModifiers None { get; } = new(
        HexNemesisArchetype.None,
        0,
        0,
        HexBoonNemesisStartLocationOverride.None,
        0,
        null);

    public HexNemesisRuntimeModifiers(
        HexNemesisArchetype archetype,
        int caravanMovesPerActivationOverride,
        int stepsPerActivationOverride,
        HexBoonNemesisStartLocationOverride startLocationOverride,
        int startStepsTowardCaravanSpawn,
        HexNemesisVisibilityRule[] visibilityRules)
    {
        Archetype = archetype;
        CaravanMovesPerActivationOverride = Mathf.Max(0, caravanMovesPerActivationOverride);
        StepsPerActivationOverride = Mathf.Max(0, stepsPerActivationOverride);
        StartLocationOverride = startLocationOverride;
        StartStepsTowardCaravanSpawn = Mathf.Max(0, startStepsTowardCaravanSpawn);
        VisibilityRules = SanitizeRules(visibilityRules);
    }

    public HexNemesisArchetype Archetype { get; }
    public int CaravanMovesPerActivationOverride { get; }
    public int StepsPerActivationOverride { get; }
    public HexBoonNemesisStartLocationOverride StartLocationOverride { get; }
    public int StartStepsTowardCaravanSpawn { get; }
    public HexNemesisVisibilityRule[] VisibilityRules { get; }

    public bool HasAny =>
        Archetype != HexNemesisArchetype.None
        && (CaravanMovesPerActivationOverride > 0
            || StepsPerActivationOverride > 0
            || StartLocationOverride != HexBoonNemesisStartLocationOverride.None
            || StartStepsTowardCaravanSpawn > 0
            || (VisibilityRules != null && VisibilityRules.Length > 0));

    public HexNemesisRuntimeModifiers Combine(HexNemesisRuntimeModifiers other)
    {
        if (!HasAny)
        {
            return other;
        }

        if (!other.HasAny)
        {
            return this;
        }

        HexNemesisArchetype resolvedArchetype = other.Archetype != HexNemesisArchetype.None ? other.Archetype : Archetype;
        int caravanMovesOverride = other.CaravanMovesPerActivationOverride > 0
            ? other.CaravanMovesPerActivationOverride
            : CaravanMovesPerActivationOverride;
        int stepsOverride = other.StepsPerActivationOverride > 0
            ? other.StepsPerActivationOverride
            : StepsPerActivationOverride;
        HexBoonNemesisStartLocationOverride startLocationOverride =
            other.StartLocationOverride != HexBoonNemesisStartLocationOverride.None
                ? other.StartLocationOverride
                : StartLocationOverride;
        int startStepsTowardCaravanSpawn = Mathf.Max(StartStepsTowardCaravanSpawn, other.StartStepsTowardCaravanSpawn);

        return new HexNemesisRuntimeModifiers(
            resolvedArchetype,
            caravanMovesOverride,
            stepsOverride,
            startLocationOverride,
            startStepsTowardCaravanSpawn,
            CombineRules(VisibilityRules, other.VisibilityRules));
    }

    public int ResolveCaravanMovesPerActivation(int baseValue)
    {
        return CaravanMovesPerActivationOverride > 0
            ? CaravanMovesPerActivationOverride
            : Mathf.Max(1, baseValue);
    }

    public int ResolveStepsPerActivation(int baseValue)
    {
        return StepsPerActivationOverride > 0
            ? StepsPerActivationOverride
            : Mathf.Max(1, baseValue);
    }

    public int ResolveStartStepsTowardCaravanSpawn()
    {
        return Mathf.Max(0, StartStepsTowardCaravanSpawn);
    }

    public HexBoonNemesisVisibilityMode GetVisibilityModeForBiome(Biome biome)
    {
        if (VisibilityRules == null || VisibilityRules.Length == 0)
        {
            return HexBoonNemesisVisibilityMode.None;
        }

        HexBoonNemesisVisibilityMode resolvedMode = HexBoonNemesisVisibilityMode.None;
        for (int index = 0; index < VisibilityRules.Length; index++)
        {
            HexNemesisVisibilityRule rule = VisibilityRules[index];
            if (!rule.HasAny || !rule.Matches(biome))
            {
                continue;
            }

            if (rule.VisibilityMode == HexBoonNemesisVisibilityMode.Hidden)
            {
                return HexBoonNemesisVisibilityMode.Hidden;
            }

            if (rule.VisibilityMode == HexBoonNemesisVisibilityMode.Obscured)
            {
                resolvedMode = HexBoonNemesisVisibilityMode.Obscured;
            }
        }

        return resolvedMode;
    }

    private static HexNemesisVisibilityRule[] SanitizeRules(HexNemesisVisibilityRule[] source)
    {
        if (source == null || source.Length == 0)
        {
            return System.Array.Empty<HexNemesisVisibilityRule>();
        }

        List<HexNemesisVisibilityRule> sanitized = new(source.Length);
        for (int index = 0; index < source.Length; index++)
        {
            HexNemesisVisibilityRule rule = source[index];
            if (rule.HasAny)
            {
                sanitized.Add(rule);
            }
        }

        return sanitized.Count == 0 ? System.Array.Empty<HexNemesisVisibilityRule>() : sanitized.ToArray();
    }

    private static HexNemesisVisibilityRule[] CombineRules(HexNemesisVisibilityRule[] left, HexNemesisVisibilityRule[] right)
    {
        if ((left == null || left.Length == 0) && (right == null || right.Length == 0))
        {
            return System.Array.Empty<HexNemesisVisibilityRule>();
        }

        List<HexNemesisVisibilityRule> combined = new();
        if (left != null)
        {
            for (int index = 0; index < left.Length; index++)
            {
                if (left[index].HasAny)
                {
                    combined.Add(left[index]);
                }
            }
        }

        if (right != null)
        {
            for (int index = 0; index < right.Length; index++)
            {
                if (right[index].HasAny)
                {
                    combined.Add(right[index]);
                }
            }
        }

        return combined.Count == 0 ? System.Array.Empty<HexNemesisVisibilityRule>() : combined.ToArray();
    }
}

public readonly struct HexNemesisPitstopDestructionInfo
{
    public HexNemesisPitstopDestructionInfo(HexCoordinates coordinates, PitstopSite site)
    {
        Coordinates = coordinates;
        Site = site;
    }

    public HexCoordinates Coordinates { get; }
    public PitstopSite Site { get; }
}

public sealed class HexNemesisTurnResult
{
    public static HexNemesisTurnResult Empty { get; } = new();

    public bool Active { get; set; }
    public HexNemesisArchetype Archetype { get; set; } = HexNemesisArchetype.None;
    public HexNemesisPhase Phase { get; set; } = HexNemesisPhase.Inactive;
    public bool Acted { get; set; }
    public bool Moved { get; set; }
    public bool PhaseChanged { get; set; }
    public bool CausedDefeat { get; set; }
    public string DefeatReason { get; set; } = string.Empty;
    public HexCoordinates? PreviousCoordinates { get; set; }
    public HexCoordinates? CurrentCoordinates { get; set; }
    public HexBoonNemesisVisibilityMode CurrentCoordinatesVisibilityMode { get; set; } = HexBoonNemesisVisibilityMode.None;
    public HexCoordinates? EchoBlockedCoordinates { get; set; }
    public List<HexCoordinates> NewlyCorruptedHexes { get; } = new();
    public List<HexNemesisPitstopDestructionInfo> DestroyedPitstops { get; } = new();
    public List<HexNemesisPitstopDestructionInfo> DeferredPitstopDestructions { get; } = new();
}
