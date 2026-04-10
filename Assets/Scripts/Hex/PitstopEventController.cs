using System.Collections.Generic;
using UnityEngine;

public sealed class PitstopEventController : MonoBehaviour
{
    [SerializeField] private List<PitstopEventDefinition> eventDefinitions = new();

    private readonly Dictionary<PitstopKind, PitstopEventDefinition> definitionsByKind = new();
    private PitstopSpawner pitstopSpawner;

    public bool IsInitialized => pitstopSpawner != null;

    private void OnValidate()
    {
        EnsureDefinitions();
    }

    public void Initialize(PitstopSpawner pitstopSpawner)
    {
        this.pitstopSpawner = pitstopSpawner;
        EnsureDefinitions();
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
        ApplyMetadata(site, definition);
        return result;
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
}
