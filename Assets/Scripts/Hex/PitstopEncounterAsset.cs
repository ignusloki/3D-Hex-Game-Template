using System.Collections.Generic;
using UnityEngine;

public enum PitstopEncounterRarity
{
    Common,
    Uncommon,
    Rare
}

public readonly struct PitstopEncounterSelectionContext
{
    private readonly IReadOnlyCollection<string> seenEncounterIds;

    public PitstopEncounterSelectionContext(
        PitstopSite site,
        bool isFirstVisit,
        int visitedPitstopCount,
        CaravanResourceSnapshot resources,
        IReadOnlyCollection<string> seenEncounterIds)
    {
        Site = site;
        IsFirstVisit = isFirstVisit;
        VisitedPitstopCount = Mathf.Max(0, visitedPitstopCount);
        Resources = resources;
        this.seenEncounterIds = seenEncounterIds;
    }

    public PitstopSite Site { get; }
    public bool IsFirstVisit { get; }
    public int VisitedPitstopCount { get; }
    public CaravanResourceSnapshot Resources { get; }

    public bool HasSeenEncounter(string eventId)
    {
        if (string.IsNullOrWhiteSpace(eventId) || seenEncounterIds == null)
        {
            return false;
        }

        foreach (string seenEventId in seenEncounterIds)
        {
            if (seenEventId == eventId)
            {
                return true;
            }
        }

        return false;
    }
}

[System.Serializable]
public sealed class PitstopEncounterSelectionRules
{
    public bool requireFirstVisit = true;
    public bool allowRepeatSelectionInRun;
    [Min(0)] public int minimumVisitedPitstops;
    public int maximumVisitedPitstops = -1;
    public int minimumFood = -1;
    public int maximumFood = -1;
    public int minimumMorale = -1;
    public int maximumMorale = -1;
    public int minimumGold = -1;
    public int maximumGold = -1;

    public void Validate()
    {
        minimumVisitedPitstops = Mathf.Max(0, minimumVisitedPitstops);
        maximumVisitedPitstops = NormalizeMaximum(maximumVisitedPitstops, minimumVisitedPitstops);
        maximumFood = NormalizeMaximum(maximumFood, minimumFood);
        maximumMorale = NormalizeMaximum(maximumMorale, minimumMorale);
        maximumGold = NormalizeMaximum(maximumGold, minimumGold);
    }

    public bool Matches(PitstopEncounterSelectionContext context, string eventId)
    {
        if (requireFirstVisit && !context.IsFirstVisit)
        {
            return false;
        }

        if (!allowRepeatSelectionInRun && context.HasSeenEncounter(eventId))
        {
            return false;
        }

        if (!MatchesRange(context.VisitedPitstopCount, minimumVisitedPitstops, maximumVisitedPitstops))
        {
            return false;
        }

        if (!MatchesRange(context.Resources.Food, minimumFood, maximumFood))
        {
            return false;
        }

        if (!MatchesRange(context.Resources.Morale, minimumMorale, maximumMorale))
        {
            return false;
        }

        if (!MatchesRange(context.Resources.Gold, minimumGold, maximumGold))
        {
            return false;
        }

        return true;
    }

    private static int NormalizeMaximum(int maximumValue, int minimumValue)
    {
        if (maximumValue < 0)
        {
            return -1;
        }

        if (minimumValue >= 0)
        {
            return Mathf.Max(maximumValue, minimumValue);
        }

        return maximumValue;
    }

    private static bool MatchesRange(int value, int minimumValue, int maximumValue)
    {
        if (minimumValue >= 0 && value < minimumValue)
        {
            return false;
        }

        if (maximumValue >= 0 && value > maximumValue)
        {
            return false;
        }

        return true;
    }
}

public static class PitstopEncounterSelector
{
    public static bool TryChooseEncounter(
        IReadOnlyList<PitstopEncounterAsset> encounters,
        PitstopEncounterSelectionContext context,
        out PitstopEncounterAsset chosenEncounter)
    {
        chosenEncounter = null;
        if (encounters == null || encounters.Count == 0)
        {
            return false;
        }

        List<PitstopEncounterAsset> eligibleEncounters = new();
        float totalWeight = 0f;
        for (int index = 0; index < encounters.Count; index++)
        {
            PitstopEncounterAsset encounter = encounters[index];
            if (encounter == null || !encounter.IsEligible(context))
            {
                continue;
            }

            eligibleEncounters.Add(encounter);
            totalWeight += Mathf.Max(0.1f, encounter.selectionWeight);
        }

        if (eligibleEncounters.Count == 0 || totalWeight <= 0f)
        {
            return false;
        }

        float roll = Random.Range(0f, totalWeight);
        for (int index = 0; index < eligibleEncounters.Count; index++)
        {
            PitstopEncounterAsset encounter = eligibleEncounters[index];
            roll -= Mathf.Max(0.1f, encounter.selectionWeight);
            if (roll <= 0f)
            {
                chosenEncounter = encounter;
                return true;
            }
        }

        chosenEncounter = eligibleEncounters[eligibleEncounters.Count - 1];
        return true;
    }
}

[System.Serializable]
public sealed class PitstopEncounterOption
{
    public string label = "Take the offer";
    [TextArea(2, 4)] public string outcomeText = "The caravan accepts the terms.";
    public List<PitstopResourceEffect> resourceEffects = new();

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(label))
        {
            label = "Continue";
        }

        if (string.IsNullOrWhiteSpace(outcomeText))
        {
            outcomeText = label;
        }

        resourceEffects ??= new List<PitstopResourceEffect>();
        for (int index = resourceEffects.Count - 1; index >= 0; index--)
        {
            if (resourceEffects[index] == null || resourceEffects[index].amount == 0)
            {
                resourceEffects.RemoveAt(index);
            }
        }
    }

    public bool CanAfford(CaravanResourceSnapshot resources)
    {
        return !TryGetUnavailableSummary(resources, out _);
    }

    public bool TryGetUnavailableSummary(CaravanResourceSnapshot resources, out string summary)
    {
        int requiredFood = 0;
        int requiredMorale = 0;
        int requiredGold = 0;

        for (int index = 0; index < resourceEffects.Count; index++)
        {
            PitstopResourceEffect effect = resourceEffects[index];
            if (effect == null || effect.amount >= 0)
            {
                continue;
            }

            int cost = Mathf.Abs(effect.amount);
            switch (effect.resourceType)
            {
                case CaravanResourceType.Food:
                    requiredFood += cost;
                    break;

                case CaravanResourceType.Morale:
                    requiredMorale += cost;
                    break;

                case CaravanResourceType.Gold:
                    requiredGold += cost;
                    break;
            }
        }

        List<string> missingResources = new();
        AppendMissingCost(resources.Food, requiredFood, CaravanResourceType.Food, missingResources);
        AppendMissingCost(resources.Morale, requiredMorale, CaravanResourceType.Morale, missingResources);
        AppendMissingCost(resources.Gold, requiredGold, CaravanResourceType.Gold, missingResources);

        if (missingResources.Count == 0)
        {
            summary = string.Empty;
            return false;
        }

        summary = $"Needs {string.Join(", ", missingResources)}";
        return true;
    }

    private static void AppendMissingCost(int currentAmount, int requiredAmount, CaravanResourceType resourceType, List<string> missingResources)
    {
        if (requiredAmount <= 0 || currentAmount >= requiredAmount)
        {
            return;
        }

        missingResources.Add($"{requiredAmount} {FormatResource(resourceType)}");
    }

    private static string FormatResource(CaravanResourceType resourceType)
    {
        return resourceType switch
        {
            CaravanResourceType.Food => "Food",
            CaravanResourceType.Morale => "Morale",
            CaravanResourceType.Gold => "Gold",
            _ => resourceType.ToString()
        };
    }
}

[CreateAssetMenu(menuName = "Hex/Pitstop Encounter", fileName = "PitstopEncounter_")]
public sealed class PitstopEncounterAsset : ScriptableObject
{
    [Header("Identity")]
    public string eventId = "pitstop-encounter";
    public PitstopKind pitstopKind = PitstopKind.Mill;
    public PitstopEncounterRarity rarity = PitstopEncounterRarity.Common;
    public List<string> tags = new();
    [TextArea(2, 5)] public string authoringNotes = string.Empty;

    [Header("Selection")]
    [Min(0.1f)] public float selectionWeight = 1f;
    public PitstopEncounterSelectionRules selectionRules = new();

    [Header("Presentation")]
    public string title = "Unexpected Offer";
    [TextArea(4, 8)] public string description = "A short roadside event interrupts the caravan's rest.";

    [Header("Choices")]
    public List<PitstopEncounterOption> options = new();

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(eventId))
        {
            eventId = name;
        }

        if (string.IsNullOrWhiteSpace(title))
        {
            title = pitstopKind.ToString();
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            description = title;
        }

        selectionWeight = Mathf.Max(0.1f, selectionWeight);
        selectionRules ??= new PitstopEncounterSelectionRules();
        selectionRules.Validate();
        tags ??= new List<string>();
        for (int index = tags.Count - 1; index >= 0; index--)
        {
            if (string.IsNullOrWhiteSpace(tags[index]))
            {
                tags.RemoveAt(index);
                continue;
            }

            tags[index] = tags[index].Trim();
        }

        options ??= new List<PitstopEncounterOption>();
        for (int index = options.Count - 1; index >= 0; index--)
        {
            if (options[index] == null)
            {
                options.RemoveAt(index);
                continue;
            }

            options[index].Validate();
        }
    }

    public bool IsEligible(PitstopEncounterSelectionContext context)
    {
        Validate();
        return selectionRules.Matches(context, eventId) && HasAffordableOption(context.Resources);
    }

    public bool HasAffordableOption(CaravanResourceSnapshot resources)
    {
        if (options == null || options.Count == 0)
        {
            return false;
        }

        for (int index = 0; index < options.Count; index++)
        {
            PitstopEncounterOption option = options[index];
            if (option != null && option.CanAfford(resources))
            {
                return true;
            }
        }

        return false;
    }
}
