using System.Collections.Generic;
using UnityEngine;

public sealed class PitstopEventController : MonoBehaviour
{
    [SerializeField] private List<PitstopEventDefinition> eventDefinitions = new();
    [Header("Encounter Pool")]
    [SerializeField] private List<PitstopEncounterAsset> encounterPool = new();
    [SerializeField] private string encounterResourcesPath = "PitstopEvents";

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
    }

    public PitstopEventResult ProcessArrival(HexCoordinates coordinates, CaravanResourceState resources)
    {
        if (!IsInitialized || resources == null || !pitstopSpawner.TryGetPitstop(coordinates, out PitstopSite site) || site == null)
        {
            return PitstopEventResult.Empty;
        }

        if (!definitionsByKind.TryGetValue(site.Kind, out PitstopEventDefinition definition) || definition == null)
        {
            return PitstopEventResult.Empty;
        }

        PitstopEventResult result = PitstopEventResolver.ResolveArrival(site, definition, resources);
        if (result.Triggered && TryChooseEncounter(site, result, resources.ToSnapshot(), out PitstopEncounterAsset encounter))
        {
            result.Encounter = encounter;
            result.RequiresChoice = encounter.options != null && encounter.options.Count > 0;
            seenEncounterIds.Add(encounter.eventId);
        }

        ApplyMetadata(site, definition);
        return result;
    }

    public void PresentChoice(PitstopEventResult eventResult, CaravanResourceState resources, System.Action<PitstopEventResult> onResolved)
    {
        if (eventResult == null || !eventResult.RequiresChoice || eventResult.Encounter == null)
        {
            onResolved?.Invoke(eventResult ?? PitstopEventResult.Empty);
            return;
        }

        EnsureModalPresenter();
        if (modalPresenter == null)
        {
            onResolved?.Invoke(eventResult);
            return;
        }

        CaravanResourceSnapshot resourceSnapshot = resources.ToSnapshot();
        modalPresenter.ShowChoice(eventResult, resourceSnapshot, optionIndex =>
        {
            if (!eventResult.Encounter.options[optionIndex].CanAfford(resources.ToSnapshot()))
            {
                return;
            }

            PitstopEventResult resolvedResult = ResolveChoice(eventResult, optionIndex, resources);
            onResolved?.Invoke(resolvedResult);

            if (modalPresenter == null || !modalPresenter.IsOpen)
            {
                return;
            }

            modalPresenter.ShowResolution(resolvedResult, null);
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
}
