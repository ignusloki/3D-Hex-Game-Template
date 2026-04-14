using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class HexNemesisController : MonoBehaviour
{
    [SerializeField] private HexNemesisSettings settings = new();

    private readonly HexNemesisPresenter presenter = new();
    private readonly HashSet<HexCoordinates> corruptedHexes = new();
    private MapGenerator mapGenerator;
    private PitstopSpawner pitstopSpawner;
    private HexObstacleController obstacleController;
    private HexCoordinates? actorCoordinates;
    private HexCoordinates? echoBlockedCoordinates;
    private HexNemesisPhase currentPhase = HexNemesisPhase.Inactive;
    private int caravanMovesSinceLastAction;
    private HexNemesisArchetypeProfile activeProfile;

    public bool IsInitialized => mapGenerator != null;
    public bool IsEnabled => settings != null
        && settings.enableNemesis
        && settings.activeArchetype != HexNemesisArchetype.None
        && settings.GetProfile(settings.activeArchetype) != null;
    public HexNemesisArchetype ActiveArchetype => IsEnabled ? settings.activeArchetype : HexNemesisArchetype.None;
    public HexNemesisPhase CurrentPhase => currentPhase;
    public IReadOnlyCollection<HexCoordinates> CorruptedHexes => corruptedHexes;

    private void OnValidate()
    {
        settings ??= new HexNemesisSettings();
        settings.Validate();
        activeProfile = settings.GetProfile(settings.activeArchetype);
    }

    private void OnDisable()
    {
        presenter.Clear();
    }

    public void Initialize(
        MapGenerator mapGenerator,
        PitstopSpawner pitstopSpawner,
        HexObstacleController obstacleController,
        HexCoordinates caravanStartCoordinates)
    {
        this.mapGenerator = mapGenerator;
        this.pitstopSpawner = pitstopSpawner;
        this.obstacleController = obstacleController;
        settings ??= new HexNemesisSettings();
        settings.Validate();

        corruptedHexes.Clear();
        actorCoordinates = null;
        echoBlockedCoordinates = null;
        caravanMovesSinceLastAction = 0;
        currentPhase = HexNemesisPhase.Inactive;
        activeProfile = settings.GetProfile(settings.activeArchetype);

        if (IsEnabled)
        {
            switch (settings.activeArchetype)
            {
                case HexNemesisArchetype.Hunter:
                    actorCoordinates = ResolveUnusedCornerCoordinates();
                    currentPhase = HexNemesisPhase.HunterPursuit;
                    break;

                case HexNemesisArchetype.Echo:
                    currentPhase = HexNemesisPhase.EchoSeal;
                    break;

                case HexNemesisArchetype.Corruptor:
                    actorCoordinates = ResolveUnusedCornerCoordinates();
                    currentPhase = HasUndestroyedPitstops()
                        ? HexNemesisPhase.CorruptorPitstopDestruction
                        : HexNemesisPhase.CorruptorPursuit;
                    break;
            }
        }

        ApplyObstaclePressureContext();
        RefreshPresentation();
        LogDebug($"Initialized Nemesis. Enabled: {IsEnabled}. Archetype: {ActiveArchetype}. Phase: {currentPhase}. Actor: {actorCoordinates?.ToString() ?? "none"}.");
    }

    public void ConfigureArchetype(HexNemesisArchetype archetype, bool enableNemesis)
    {
        settings.activeArchetype = archetype;
        settings.enableNemesis = enableNemesis;
        settings.Validate();
        activeProfile = settings.GetProfile(archetype);
    }

    public bool IsBlockedDestination(HexCoordinates coordinates)
    {
        return IsEnabled
            && settings.activeArchetype == HexNemesisArchetype.Echo
            && echoBlockedCoordinates.HasValue
            && echoBlockedCoordinates.Value.Equals(coordinates);
    }

    public string GetTileDetails(HexCoordinates coordinates)
    {
        if (!IsEnabled)
        {
            return string.Empty;
        }

        List<string> lines = new();
        if (actorCoordinates.HasValue && actorCoordinates.Value.Equals(coordinates))
        {
            lines.Add($"Nemesis: {FormatArchetype(settings.activeArchetype)}");
        }

        if (settings.activeArchetype == HexNemesisArchetype.Echo
            && echoBlockedCoordinates.HasValue
            && echoBlockedCoordinates.Value.Equals(coordinates))
        {
            lines.Add("Echo Seal: This hex is blocked.");
        }

        if (corruptedHexes.Contains(coordinates))
        {
            lines.Add("Corruption: Active.");
        }

        return string.Join("\n", lines);
    }

    public HexNemesisTurnResult ProcessCaravanMove(HexCoordinates previousCaravanCoordinates, HexCoordinates currentCaravanCoordinates)
    {
        HexNemesisTurnResult turnResult = new()
        {
            Active = IsEnabled,
            Archetype = ActiveArchetype,
            Phase = currentPhase,
            PreviousCoordinates = actorCoordinates,
            CurrentCoordinates = actorCoordinates,
            EchoBlockedCoordinates = echoBlockedCoordinates
        };

        if (!IsEnabled || !IsInitialized)
        {
            ApplyObstaclePressureContext();
            RefreshPresentation();
            return turnResult;
        }

        switch (settings.activeArchetype)
        {
            case HexNemesisArchetype.Hunter:
                ProcessHunter(currentCaravanCoordinates, turnResult);
                break;

            case HexNemesisArchetype.Echo:
                ProcessEcho(previousCaravanCoordinates, turnResult);
                break;

            case HexNemesisArchetype.Corruptor:
                ProcessCorruptor(currentCaravanCoordinates, turnResult);
                break;
        }

        turnResult.Phase = currentPhase;
        turnResult.CurrentCoordinates = actorCoordinates;
        turnResult.EchoBlockedCoordinates = echoBlockedCoordinates;

        ApplyObstaclePressureContext();
        RefreshPresentation();
        LogTurnResult(turnResult);
        return turnResult;
    }

    private void ProcessHunter(HexCoordinates caravanCoordinates, HexNemesisTurnResult turnResult)
    {
        if (!actorCoordinates.HasValue || activeProfile == null || !AdvanceActivationCadenceAndShouldAct(activeProfile.caravanMovesPerActivation))
        {
            return;
        }

        turnResult.Acted = true;
        HexCoordinates previousCoordinates = actorCoordinates.Value;
        turnResult.PreviousCoordinates = previousCoordinates;

        int stepCount = Mathf.Max(1, activeProfile.stepsPerActivation);
        for (int stepIndex = 0; stepIndex < stepCount; stepIndex++)
        {
            HexCoordinates nextCoordinates = GetNextStepTowards(actorCoordinates.Value, caravanCoordinates);
            actorCoordinates = nextCoordinates;

            if (nextCoordinates.Equals(caravanCoordinates))
            {
                turnResult.CausedDefeat = true;
                turnResult.DefeatReason = "The Hunter reached the caravan.";
                break;
            }
        }

        turnResult.Moved = !previousCoordinates.Equals(actorCoordinates.Value);
        turnResult.PreviousCoordinates = previousCoordinates;
        turnResult.CurrentCoordinates = actorCoordinates.Value;
    }

    private void ProcessEcho(HexCoordinates previousCaravanCoordinates, HexNemesisTurnResult turnResult)
    {
        turnResult.Acted = true;
        HexCoordinates? previousEchoCoordinates = echoBlockedCoordinates;
        echoBlockedCoordinates = previousCaravanCoordinates;
        actorCoordinates = echoBlockedCoordinates;
        turnResult.PreviousCoordinates = previousEchoCoordinates;
        turnResult.CurrentCoordinates = actorCoordinates;
        turnResult.EchoBlockedCoordinates = echoBlockedCoordinates;
        turnResult.Moved = !previousEchoCoordinates.HasValue || !previousEchoCoordinates.Value.Equals(previousCaravanCoordinates);
    }

    private void ProcessCorruptor(HexCoordinates caravanCoordinates, HexNemesisTurnResult turnResult)
    {
        if (!actorCoordinates.HasValue || activeProfile == null || !AdvanceActivationCadenceAndShouldAct(activeProfile.caravanMovesPerActivation))
        {
            return;
        }

        turnResult.Acted = true;
        HexCoordinates targetCoordinates = currentPhase switch
        {
            HexNemesisPhase.CorruptorPitstopDestruction when TryGetNearestUndestroyedPitstop(actorCoordinates.Value, out PitstopSite site) => site.Coordinates,
            _ => caravanCoordinates
        };

        HexCoordinates previousCoordinates = actorCoordinates.Value;
        turnResult.PreviousCoordinates = previousCoordinates;
        UpdateCorruptorPhase(turnResult);

        int stepCount = Mathf.Max(1, activeProfile.stepsPerActivation);
        for (int stepIndex = 0; stepIndex < stepCount; stepIndex++)
        {
            targetCoordinates = currentPhase switch
            {
                HexNemesisPhase.CorruptorPitstopDestruction when TryGetNearestUndestroyedPitstop(actorCoordinates.Value, out PitstopSite site) => site.Coordinates,
                _ => caravanCoordinates
            };

            HexCoordinates nextCoordinates = GetNextStepTowards(actorCoordinates.Value, targetCoordinates);
            actorCoordinates = nextCoordinates;
            MarkCorrupted(nextCoordinates, turnResult);

            if (currentPhase == HexNemesisPhase.CorruptorPitstopDestruction
                && pitstopSpawner != null
                && pitstopSpawner.TryGetPitstop(nextCoordinates, out PitstopSite siteAtDestination)
                && siteAtDestination != null
                && !siteAtDestination.IsDestroyed)
            {
                if (nextCoordinates.Equals(caravanCoordinates))
                {
                    turnResult.DeferredPitstopDestructions.Add(new HexNemesisPitstopDestructionInfo(nextCoordinates, siteAtDestination));
                    break;
                }

                DestroyPitstop(siteAtDestination, nextCoordinates, turnResult);
            }

            if (currentPhase == HexNemesisPhase.CorruptorPursuit && nextCoordinates.Equals(caravanCoordinates))
            {
                turnResult.CausedDefeat = true;
                turnResult.DefeatReason = "The Corruptor reached the caravan.";
                break;
            }
        }

        turnResult.Moved = !previousCoordinates.Equals(actorCoordinates.Value);
        turnResult.CurrentCoordinates = actorCoordinates.Value;
    }

    public void FinalizeDeferredPitstopDestructions(HexNemesisTurnResult turnResult)
    {
        if (turnResult == null || turnResult.DeferredPitstopDestructions.Count == 0)
        {
            return;
        }

        for (int index = 0; index < turnResult.DeferredPitstopDestructions.Count; index++)
        {
            HexNemesisPitstopDestructionInfo deferredInfo = turnResult.DeferredPitstopDestructions[index];
            if (deferredInfo.Site == null || deferredInfo.Site.IsDestroyed)
            {
                continue;
            }

            DestroyPitstop(deferredInfo.Site, deferredInfo.Coordinates, turnResult);
        }

        turnResult.DeferredPitstopDestructions.Clear();
        ApplyObstaclePressureContext();
        RefreshPresentation();
    }

    private void UpdateCorruptorPhase(HexNemesisTurnResult turnResult)
    {
        HexNemesisPhase resolvedPhase = HasUndestroyedPitstops()
            ? HexNemesisPhase.CorruptorPitstopDestruction
            : HexNemesisPhase.CorruptorPursuit;

        if (currentPhase != resolvedPhase)
        {
            currentPhase = resolvedPhase;
            turnResult.PhaseChanged = true;
        }
    }

    private bool AdvanceActivationCadenceAndShouldAct(int caravanMovesPerActivation)
    {
        caravanMovesSinceLastAction++;
        if (caravanMovesSinceLastAction < caravanMovesPerActivation)
        {
            return false;
        }

        caravanMovesSinceLastAction = 0;
        return true;
    }

    private void MarkCorrupted(HexCoordinates coordinates, HexNemesisTurnResult turnResult)
    {
        if (corruptedHexes.Add(coordinates))
        {
            mapGenerator?.TryApplyBiomeOverride(coordinates, Biome.desert);
            turnResult.NewlyCorruptedHexes.Add(coordinates);
        }
    }

    private void DestroyPitstop(PitstopSite site, HexCoordinates coordinates, HexNemesisTurnResult turnResult)
    {
        if (site == null || site.IsDestroyed || turnResult == null)
        {
            return;
        }

        site.MarkDestroyed(
            activeProfile.destroyedPitstopDescription,
            activeProfile.destroyedPitstopTint);
        turnResult.DestroyedPitstops.Add(new HexNemesisPitstopDestructionInfo(coordinates, site));
        UpdateCorruptorPhase(turnResult);
    }

    private bool HasUndestroyedPitstops()
    {
        if (pitstopSpawner?.SpawnedSites == null)
        {
            return false;
        }

        foreach (PitstopSite site in pitstopSpawner.SpawnedSites.Values)
        {
            if (site != null && !site.IsDestroyed)
            {
                return true;
            }
        }

        return false;
    }

    private bool TryGetNearestUndestroyedPitstop(HexCoordinates from, out PitstopSite nearestSite)
    {
        nearestSite = null;
        if (pitstopSpawner?.SpawnedSites == null)
        {
            return false;
        }

        int bestDistance = int.MaxValue;
        foreach (PitstopSite site in pitstopSpawner.SpawnedSites.Values)
        {
            if (site == null || site.IsDestroyed)
            {
                continue;
            }

            int distance = from.DistanceTo(site.Coordinates);
            if (distance < bestDistance
                || (distance == bestDistance && CompareCoordinates(site.Coordinates, nearestSite?.Coordinates ?? site.Coordinates) < 0))
            {
                bestDistance = distance;
                nearestSite = site;
            }
        }

        return nearestSite != null;
    }

    private HexCoordinates GetNextStepTowards(HexCoordinates origin, HexCoordinates target)
    {
        if (mapGenerator?.GridData == null)
        {
            return origin;
        }

        HexCoordinates bestCoordinates = origin;
        int bestDistance = origin.DistanceTo(target);
        foreach (HexCoordinates neighborCoordinates in mapGenerator.GridData.GetNeighborCoordinates(origin))
        {
            int distance = neighborCoordinates.DistanceTo(target);
            if (distance < bestDistance
                || (distance == bestDistance && CompareCoordinates(neighborCoordinates, bestCoordinates) < 0))
            {
                bestDistance = distance;
                bestCoordinates = neighborCoordinates;
            }
        }

        return bestCoordinates;
    }

    private HexCoordinates ResolveUnusedCornerCoordinates()
    {
        if (mapGenerator == null)
        {
            return default;
        }

        List<HexCoordinates> corners = new()
        {
            new HexCoordinates(0, 0),
            new HexCoordinates(0, mapGenerator.Columns - 1),
            new HexCoordinates(mapGenerator.Rows - 1, 0),
            new HexCoordinates(mapGenerator.Rows - 1, mapGenerator.Columns - 1)
        };

        corners.RemoveAll(coordinates =>
            coordinates.Equals(mapGenerator.StartCoordinates)
            || coordinates.Equals(mapGenerator.GoalCoordinates));
        corners.Sort(CompareCoordinates);

        if (corners.Count == 0)
        {
            return default;
        }

        int index = settings.cornerChoice == HexNemesisUnusedCornerChoice.SecondUnusedCorner && corners.Count > 1 ? 1 : 0;
        return corners[index];
    }

    private void ApplyObstaclePressureContext()
    {
        if (obstacleController == null)
        {
            return;
        }

        if (!IsEnabled)
        {
            obstacleController.SetPressureContext(HexObstaclePressureContext.Empty);
            return;
        }

        List<HexCoordinates> protectedCoordinates = new();
        if (actorCoordinates.HasValue)
        {
            protectedCoordinates.Add(actorCoordinates.Value);
        }

        HexObstaclePressureContext pressureContext = new()
        {
            MinimumMaxActiveObstacles = settings.activeArchetype == HexNemesisArchetype.Echo
                ? Mathf.Max(0, activeProfile != null ? activeProfile.minimumMaxActiveObstacles : 0)
                : 0,
            AdditionalProtectedHexes = protectedCoordinates
        };

        if (settings.activeArchetype == HexNemesisArchetype.Corruptor && activeProfile != null && corruptedHexes.Count > 0)
        {
            pressureContext.PressureHexes = new List<HexCoordinates>(corruptedHexes);
            pressureContext.PressureInfluenceRadius = activeProfile.corruptionInfluenceRadius;
            pressureContext.PressureSpawnChanceBonus = activeProfile.corruptionSpawnChanceBonus;
            pressureContext.PressureCandidateWeightBonus = activeProfile.corruptionCandidateWeightBonus;
        }

        obstacleController.SetPressureContext(pressureContext);
    }

    private void RefreshPresentation()
    {
        presenter.Apply(mapGenerator, ActiveArchetype, activeProfile, actorCoordinates, corruptedHexes);
    }

    private void LogTurnResult(HexNemesisTurnResult turnResult)
    {
        if (!settings.enableDebugLogging || turnResult == null || !turnResult.Active)
        {
            return;
        }

        Debug.Log(
            $"[Nemesis] Archetype={turnResult.Archetype}, Phase={turnResult.Phase}, Acted={turnResult.Acted}, " +
            $"Current={turnResult.CurrentCoordinates?.ToString() ?? "none"}, Corrupted+={turnResult.NewlyCorruptedHexes.Count}, " +
            $"DestroyedPitstops={turnResult.DestroyedPitstops.Count}, Defeat={turnResult.CausedDefeat}.",
            this);
    }

    private void LogDebug(string message)
    {
        if (!settings.enableDebugLogging)
        {
            return;
        }

        Debug.Log($"[Nemesis] {message}", this);
    }

    private static int CompareCoordinates(HexCoordinates left, HexCoordinates right)
    {
        int rowComparison = left.Row.CompareTo(right.Row);
        return rowComparison != 0 ? rowComparison : left.Column.CompareTo(right.Column);
    }

    private static string FormatArchetype(HexNemesisArchetype archetype)
    {
        string raw = archetype.ToString();
        return char.ToUpperInvariant(raw[0]) + raw[1..];
    }
}
