using System.Collections.Generic;
using UnityEngine;

public sealed class PitstopEventController : MonoBehaviour
{
    [SerializeField] private List<PitstopEventDefinition> eventDefinitions = new();
    [Header("Encounter Pool")]
    [SerializeField] private List<PitstopEncounterAsset> encounterPool = new();
    [SerializeField] private string encounterResourcesPath = "PitstopEvents";
    [Header("Debug")]
    [SerializeField] private bool enableDebugLogging = true;

    private readonly Dictionary<PitstopKind, PitstopEventDefinition> definitionsByKind = new();
    private readonly Dictionary<PitstopKind, List<PitstopEncounterAsset>> encountersByKind = new();
    private readonly HashSet<string> seenEncounterIds = new();
    private PitstopSpawner pitstopSpawner;
    private PitstopEventModalPresenter modalPresenter;

    public bool IsInitialized => pitstopSpawner != null;
    public bool IsChoiceModalOpen => modalPresenter != null && modalPresenter.IsOpen;

    private void OnValidate()
    {
        EnsureDefinitions();
        EnsureEncounters();
    }

    public void Initialize(PitstopSpawner pitstopSpawner)
    {
        this.pitstopSpawner = pitstopSpawner;
        seenEncounterIds.Clear();
        EnsureDefinitions();
        EnsureEncounters();
        EnsureModalPresenter();
        SyncSiteMetadata();
        LogDebug($"Initialized controller. Definitions={definitionsByKind.Count}, encounterAssets={encounterPool?.Count ?? 0}, kindsWithChoicePools={encountersByKind.Count}.");
    }

    public PitstopEventResult ProcessArrival(HexCoordinates coordinates, CaravanResourceState resources)
    {
        if (!IsInitialized || resources == null || !pitstopSpawner.TryGetPitstop(coordinates, out PitstopSite site) || site == null)
        {
            return PitstopEventResult.Empty;
        }

        if (site.IsDestroyed)
        {
            LogDebug($"Skipped arrival at {coordinates}: site is destroyed.");
            return PitstopEventResult.Empty;
        }

        if (!definitionsByKind.TryGetValue(site.Kind, out PitstopEventDefinition definition) || definition == null)
        {
            Debug.LogWarning($"[PitstopEvent] No event definition configured for pitstop kind '{site.Kind}' at {coordinates}.", this);
            return PitstopEventResult.Empty;
        }

        PitstopEventResult result = PitstopEventResolver.ResolveArrival(site, definition, resources);
        if (!result.Triggered)
        {
            LogDebug($"No pitstop event triggered at {coordinates} ({site.Kind}). Reason: revisit of non-repeatable definition '{definition.title}'.");
        }

        if (result.Triggered && TryChooseEncounter(site, result, resources.ToSnapshot(), out PitstopEncounterAsset encounter))
        {
            result.Encounter = encounter;
            result.RequiresChoice = encounter.options != null && encounter.options.Count > 0;
            seenEncounterIds.Add(encounter.eventId);
            LogDebug($"Selected encounter '{encounter.eventId}' for {site.Kind} at {coordinates}. RequiresChoice={result.RequiresChoice}, FirstVisit={result.IsFirstVisit}.");
        }
        else if (result.Triggered)
        {
            LogDebug($"No encounter modal for {site.Kind} at {coordinates}. {BuildEncounterSelectionDiagnostics(site, result, resources.ToSnapshot())}");
        }

        ApplyMetadata(site, definition);
        return result;
    }

    public void PresentChoice(PitstopEventResult eventResult, CaravanResourceState resources, System.Action<PitstopEventResult> onResolved)
    {
        if (eventResult == null || !eventResult.RequiresChoice || eventResult.Encounter == null)
        {
            if (eventResult != null)
            {
                LogDebug($"Skipped modal presentation for pitstop '{eventResult.Title}' at {eventResult.Site?.Coordinates.ToString() ?? "unknown"} because no choice encounter is active.");
            }
            onResolved?.Invoke(eventResult ?? PitstopEventResult.Empty);
            return;
        }

        EnsureModalPresenter();
        if (modalPresenter == null)
        {
            Debug.LogError($"[PitstopEvent] Could not present pitstop event modal for '{eventResult.Encounter.eventId}' because no modal presenter is available.", this);
            onResolved?.Invoke(eventResult);
            return;
        }

        CaravanResourceSnapshot resourceSnapshot = resources.ToSnapshot();
        LogDebug($"Presenting encounter '{eventResult.Encounter.eventId}' at {eventResult.Site?.Coordinates.ToString() ?? "unknown"} with {eventResult.Encounter.options.Count} option(s).");
        modalPresenter.ShowChoice(eventResult, resourceSnapshot, optionIndex =>
        {
            LogDebug($"Modal callback invoked for encounter '{eventResult.Encounter.eventId}' with optionIndex={optionIndex}.");

            if (optionIndex < 0 || optionIndex >= eventResult.Encounter.options.Count)
            {
                Debug.LogError(
                    $"[PitstopEvent] Received invalid option index {optionIndex} for encounter '{eventResult.Encounter.eventId}' with {eventResult.Encounter.options.Count} option(s).",
                    this);
                return;
            }

            if (!eventResult.Encounter.options[optionIndex].CanAfford(resources.ToSnapshot()))
            {
                LogDebug($"Rejected option {optionIndex} for encounter '{eventResult.Encounter.eventId}' because the caravan cannot afford it.");
                return;
            }

            PitstopEventResult resolvedResult = ResolveChoice(eventResult, optionIndex, resources);
            LogDebug($"Resolved encounter '{eventResult.Encounter.eventId}' with option '{resolvedResult.SelectedOption?.label ?? "unknown"}'.");
            onResolved?.Invoke(resolvedResult);

            if (modalPresenter == null || !modalPresenter.IsOpen)
            {
                LogDebug($"Skipping resolution modal for encounter '{eventResult.Encounter.eventId}' because the presenter is not open anymore.");
                return;
            }

            LogDebug($"Showing resolution modal for encounter '{eventResult.Encounter.eventId}'.");
            modalPresenter.ShowResolution(resolvedResult, resources.ToSnapshot(), null);
        });
    }

    public void HideActiveModal()
    {
        modalPresenter?.Hide();
    }

    private void SyncSiteMetadata()
    {
        if (pitstopSpawner?.SpawnedSites == null)
        {
            return;
        }

        foreach (PitstopSite site in pitstopSpawner.SpawnedSites.Values)
        {
            if (site == null || !definitionsByKind.TryGetValue(site.Kind, out PitstopEventDefinition definition) || definition == null)
            {
                continue;
            }

            ApplyMetadata(site, definition);
        }
    }

    private void ApplyMetadata(PitstopSite site, PitstopEventDefinition definition)
    {
        site.ConfigureEventMetadata(definition.title, definition.description, definition.hasRefuelPoint, definition.repeatable);
    }

    private void EnsureDefinitions()
    {
        if (eventDefinitions == null || eventDefinitions.Count == 0)
        {
            eventDefinitions = PitstopEventDefinition.CreateDefaultSet();
        }

        definitionsByKind.Clear();
        for (int index = 0; index < eventDefinitions.Count; index++)
        {
            PitstopEventDefinition definition = eventDefinitions[index];
            if (definition == null)
            {
                continue;
            }

            definition.Validate();
            definitionsByKind[definition.kind] = definition;
        }
    }

    private void EnsureEncounters()
    {
        if (encounterPool == null || encounterPool.Count == 0)
        {
            encounterPool = new List<PitstopEncounterAsset>(Resources.LoadAll<PitstopEncounterAsset>(encounterResourcesPath));
        }

        encountersByKind.Clear();
        if (encounterPool == null)
        {
            return;
        }

        for (int index = 0; index < encounterPool.Count; index++)
        {
            PitstopEncounterAsset encounter = encounterPool[index];
            if (encounter == null)
            {
                continue;
            }

            encounter.Validate();
            if (!encountersByKind.TryGetValue(encounter.pitstopKind, out List<PitstopEncounterAsset> encounters))
            {
                encounters = new List<PitstopEncounterAsset>();
                encountersByKind[encounter.pitstopKind] = encounters;
            }

            if (encounter.options != null && encounter.options.Count > 0)
            {
                encounters.Add(encounter);
            }
        }
    }

    private bool TryChooseEncounter(PitstopSite site, PitstopEventResult eventResult, CaravanResourceSnapshot resources, out PitstopEncounterAsset chosenEncounter)
    {
        chosenEncounter = null;
        if (site == null || eventResult == null)
        {
            return false;
        }

        PitstopKind kind = site.Kind;
        if (!encountersByKind.TryGetValue(kind, out List<PitstopEncounterAsset> encounters) || encounters == null || encounters.Count == 0)
        {
            return false;
        }

        PitstopEncounterSelectionContext context = new(
            site,
            eventResult.IsFirstVisit,
            GetVisitedPitstopCount(),
            resources,
            seenEncounterIds);

        return PitstopEncounterSelector.TryChooseEncounter(encounters, context, out chosenEncounter);
    }

    private PitstopEventResult ResolveChoice(PitstopEventResult eventResult, int optionIndex, CaravanResourceState resources)
    {
        if (eventResult?.Encounter == null || eventResult.Encounter.options == null || optionIndex < 0 || optionIndex >= eventResult.Encounter.options.Count)
        {
            return eventResult ?? PitstopEventResult.Empty;
        }

        return PitstopEventResolver.ResolveChoice(eventResult, eventResult.Encounter.options[optionIndex], resources);
    }

    private void EnsureModalPresenter()
    {
        modalPresenter ??= GetComponent<PitstopEventModalPresenter>() ?? gameObject.AddComponent<PitstopEventModalPresenter>();
    }

    private int GetVisitedPitstopCount()
    {
        if (pitstopSpawner?.SpawnedSites == null)
        {
            return 0;
        }

        int visitedCount = 0;
        foreach (PitstopSite site in pitstopSpawner.SpawnedSites.Values)
        {
            if (site != null && site.Visited)
            {
                visitedCount++;
            }
        }

        return visitedCount;
    }

    private string BuildEncounterSelectionDiagnostics(PitstopSite site, PitstopEventResult eventResult, CaravanResourceSnapshot resources)
    {
        if (site == null || eventResult == null)
        {
            return "Encounter diagnostics unavailable.";
        }

        int totalForKind = 0;
        int withChoices = 0;
        int eligible = 0;
        int blockedRepeatVisit = 0;
        int blockedSeen = 0;
        int blockedVisitedCount = 0;
        int blockedResourceBounds = 0;
        int blockedNoAffordableOptions = 0;
        List<string> exampleReasons = new();

        PitstopEncounterSelectionContext context = new(
            site,
            eventResult.IsFirstVisit,
            GetVisitedPitstopCount(),
            resources,
            seenEncounterIds);

        if (encounterPool != null)
        {
            for (int index = 0; index < encounterPool.Count; index++)
            {
                PitstopEncounterAsset encounter = encounterPool[index];
                if (encounter == null || encounter.pitstopKind != site.Kind)
                {
                    continue;
                }

                totalForKind++;
                encounter.Validate();
                if (encounter.options == null || encounter.options.Count == 0)
                {
                    continue;
                }

                withChoices++;
                if (!TryGetEncounterRejectionReason(encounter, context, out string rejectionReason))
                {
                    eligible++;
                    continue;
                }

                switch (rejectionReason)
                {
                    case "repeatVisit":
                        blockedRepeatVisit++;
                        break;

                    case "alreadySeen":
                        blockedSeen++;
                        break;

                    case "visitedCount":
                        blockedVisitedCount++;
                        break;

                    case "resourceBounds":
                        blockedResourceBounds++;
                        break;

                    case "noAffordableOptions":
                        blockedNoAffordableOptions++;
                        break;
                }

                if (exampleReasons.Count < 3)
                {
                    exampleReasons.Add($"{encounter.eventId}:{rejectionReason}");
                }
            }
        }

        System.Text.StringBuilder summary = new();
        summary.Append($"Encounter diagnostics: totalForKind={totalForKind}, withChoices={withChoices}, eligible={eligible}");
        summary.Append($", repeatVisit={blockedRepeatVisit}, alreadySeen={blockedSeen}, visitedCount={blockedVisitedCount}, resourceBounds={blockedResourceBounds}, noAffordableOptions={blockedNoAffordableOptions}");
        summary.Append($", firstVisit={eventResult.IsFirstVisit}, visitedPitstops={context.VisitedPitstopCount}, seenInRun={seenEncounterIds.Count}");
        if (exampleReasons.Count > 0)
        {
            summary.Append($", examples=[{string.Join(", ", exampleReasons)}]");
        }

        return summary.ToString();
    }

    private static bool TryGetEncounterRejectionReason(
        PitstopEncounterAsset encounter,
        PitstopEncounterSelectionContext context,
        out string rejectionReason)
    {
        rejectionReason = string.Empty;
        if (encounter == null)
        {
            rejectionReason = "missingEncounter";
            return true;
        }

        PitstopEncounterSelectionRules rules = encounter.selectionRules ?? new PitstopEncounterSelectionRules();
        rules.Validate();

        if (rules.requireFirstVisit && !context.IsFirstVisit)
        {
            rejectionReason = "repeatVisit";
            return true;
        }

        if (!rules.allowRepeatSelectionInRun && context.HasSeenEncounter(encounter.eventId))
        {
            rejectionReason = "alreadySeen";
            return true;
        }

        if (!MatchesRange(context.VisitedPitstopCount, rules.minimumVisitedPitstops, rules.maximumVisitedPitstops))
        {
            rejectionReason = "visitedCount";
            return true;
        }

        if (!MatchesRange(context.Resources.Food, rules.minimumFood, rules.maximumFood)
            || !MatchesRange(context.Resources.Morale, rules.minimumMorale, rules.maximumMorale)
            || !MatchesRange(context.Resources.Gold, rules.minimumGold, rules.maximumGold))
        {
            rejectionReason = "resourceBounds";
            return true;
        }

        if (!encounter.HasAffordableOption(context.Resources))
        {
            rejectionReason = "noAffordableOptions";
            return true;
        }

        return false;
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

    private void LogDebug(string message)
    {
        if (!enableDebugLogging || string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        Debug.Log($"[PitstopEvent] {message}", this);
    }
}
