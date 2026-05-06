using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public sealed class PitstopEventModalPresenter : MonoBehaviour
{
    private HexPitstopEventModalDocumentController documentController;
    private HexGameplayUiRootController gameplayUiRootController;

    public bool IsOpen => documentController != null && documentController.IsOpen;

    public void ShowChoice(PitstopEventResult eventResult, CaravanResourceSnapshot resources, Action<int> onOptionSelected)
    {
        if (eventResult?.Encounter == null)
        {
            return;
        }

        LogDebug(
            $"ShowChoice title='{eventResult.Title}' encounter='{eventResult.Encounter.eventId}' optionCount={eventResult.Encounter.options?.Count ?? 0} food={resources.Food} morale={resources.Morale} gold={resources.Gold}.");

        EnsureView();
        if (documentController == null)
        {
            return;
        }

        documentController.ShowChoice(
            BuildMetaLabel(eventResult),
            eventResult.Title,
            eventResult.Description,
            BuildEffectChips(eventResult.EntryEffects, "No arrival bonus"),
            BuildResourceSummaryData(resources),
            BuildOptionViewData(eventResult.Encounter.options, resources),
            onOptionSelected);
    }

    public void ShowResolution(PitstopEventResult eventResult, CaravanResourceSnapshot resources, Action onCloseRequested)
    {
        if (eventResult == null)
        {
            return;
        }

        LogDebug(
            $"ShowResolution title='{eventResult.Title}' selectedOption='{eventResult.SelectedOption?.label ?? "none"}' food={resources.Food} morale={resources.Morale} gold={resources.Gold}.");

        EnsureView();
        if (documentController == null)
        {
            return;
        }

        string description = string.IsNullOrWhiteSpace(eventResult.OutcomeText)
            ? eventResult.Description
            : eventResult.OutcomeText;

        documentController.ShowResolution(
            BuildMetaLabel(eventResult),
            eventResult.Title,
            description,
            BuildEffectChips(eventResult.EntryEffects, "No arrival bonus"),
            BuildResourceSummaryData(resources),
            BuildResolutionChoiceText(eventResult),
            BuildEffectChips(eventResult.ChoiceEffects, "No resource change"),
            "Map",
            onCloseRequested);
    }

    public void Hide()
    {
        LogDebug("Hide requested.");
        documentController?.Hide();
    }

    private void EnsureView()
    {
        documentController ??= new HexPitstopEventModalDocumentController(this);
        documentController.EnsureInitialized();
    }

    internal void LogDebug(string message, bool verbose = false)
    {
        gameplayUiRootController ??= HexGameplayUiRootController.ResolveShared(this, createIfMissing: false);
        gameplayUiRootController?.EnsureInitialized();
        if (gameplayUiRootController != null)
        {
            gameplayUiRootController.LogDiagnostic("PitstopModal", message, this, verbose);
            return;
        }

        Debug.Log($"[PitstopEventModal] {message}", this);
    }

    private static List<HexPitstopOptionViewData> BuildOptionViewData(
        IReadOnlyList<PitstopEncounterOption> options,
        CaravanResourceSnapshot resources)
    {
        List<HexPitstopOptionViewData> viewData = new();
        if (options == null)
        {
            return viewData;
        }

        for (int index = 0; index < options.Count; index++)
        {
            PitstopEncounterOption option = options[index];
            if (option == null)
            {
                continue;
            }

            List<HexPitstopEffectChipData> chips = BuildEffectChips(option.resourceEffects, "No resource change");
            bool isEnabled = !option.TryGetUnavailableSummary(resources, out string unavailableSummary);
            if (!string.IsNullOrWhiteSpace(unavailableSummary))
            {
                chips.Add(new HexPitstopEffectChipData(unavailableSummary, HexPitstopEffectChipTone.Warning));
            }

            viewData.Add(new HexPitstopOptionViewData(index, option.label, chips, isEnabled));
        }

        return viewData;
    }

    private static List<HexPitstopEffectChipData> BuildEffectChips(
        IReadOnlyList<PitstopResourceEffectResult> effects,
        string fallbackText)
    {
        List<HexPitstopEffectChipData> chips = new();
        if (effects != null)
        {
            for (int index = 0; index < effects.Count; index++)
            {
                PitstopResourceEffectResult effect = effects[index];
                if (effect.Amount == 0)
                {
                    continue;
                }

                chips.Add(CreateChipData(effect.ResourceType, effect.Amount));
            }
        }

        if (chips.Count == 0 && !string.IsNullOrWhiteSpace(fallbackText))
        {
            chips.Add(new HexPitstopEffectChipData(fallbackText, HexPitstopEffectChipTone.Neutral));
        }

        return chips;
    }

    private static List<HexPitstopEffectChipData> BuildEffectChips(
        IReadOnlyList<PitstopResourceEffect> effects,
        string fallbackText)
    {
        List<HexPitstopEffectChipData> chips = new();
        if (effects != null)
        {
            for (int index = 0; index < effects.Count; index++)
            {
                PitstopResourceEffect effect = effects[index];
                if (effect == null || effect.amount == 0)
                {
                    continue;
                }

                chips.Add(CreateChipData(effect.resourceType, effect.amount));
            }
        }

        if (chips.Count == 0 && !string.IsNullOrWhiteSpace(fallbackText))
        {
            chips.Add(new HexPitstopEffectChipData(fallbackText, HexPitstopEffectChipTone.Neutral));
        }

        return chips;
    }

    private static HexPitstopEffectChipData CreateChipData(CaravanResourceType resourceType, int amount)
    {
        bool isNegative = amount < 0;
        string prefix = isNegative ? "-" : "+";
        string text = $"{prefix}{Mathf.Abs(amount)} {FormatResource(resourceType)}";

        HexPitstopEffectChipTone tone = resourceType switch
        {
            CaravanResourceType.Food => HexPitstopEffectChipTone.Food,
            CaravanResourceType.Morale => HexPitstopEffectChipTone.Morale,
            CaravanResourceType.Gold => HexPitstopEffectChipTone.Gold,
            _ => HexPitstopEffectChipTone.Neutral
        };

        return new HexPitstopEffectChipData(text, tone, isNegative);
    }

    private static List<HexPitstopResourceViewData> BuildResourceSummaryData(CaravanResourceSnapshot resources)
    {
        return new List<HexPitstopResourceViewData>(3)
        {
            new("F", Mathf.Max(resources.Food, 0).ToString(), HexPitstopEffectChipTone.Food),
            new("M", Mathf.Max(resources.Morale, 0).ToString(), HexPitstopEffectChipTone.Morale),
            new("G", Mathf.Max(resources.Gold, 0).ToString(), HexPitstopEffectChipTone.Gold)
        };
    }

    private static string BuildResolutionChoiceText(PitstopEventResult eventResult)
    {
        if (eventResult?.SelectedOption == null)
        {
            return "Outcome";
        }

        return eventResult.SelectedOption.label;
    }

    private static string BuildMetaLabel(PitstopEventResult eventResult)
    {
        PitstopSite site = eventResult?.Site;
        if (site == null)
        {
            return "Pitstop";
        }

        return $"{FormatPitstopKind(site.Kind)} • {site.Coordinates.Row},{site.Coordinates.Column}";
    }

    private static string FormatPitstopKind(PitstopKind kind)
    {
        string raw = kind.ToString();
        System.Text.StringBuilder builder = new(raw.Length + 8);
        for (int index = 0; index < raw.Length; index++)
        {
            char character = raw[index];
            if (index > 0 && char.IsUpper(character))
            {
                builder.Append(' ');
            }

            builder.Append(character);
        }

        builder.Append(" Pitstop");
        return builder.ToString().ToUpperInvariant();
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
