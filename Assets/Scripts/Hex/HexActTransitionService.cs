using System;
using System.Collections.Generic;
using UnityEngine;

public readonly struct HexActTransitionDisplayData
{
    public HexActTransitionDisplayData(
        string title,
        string body,
        string selectionPrompt,
        string continueButtonLabel,
        HexBoonDefinition[] boonOptions)
    {
        Title = title ?? string.Empty;
        Body = body ?? string.Empty;
        SelectionPrompt = selectionPrompt ?? string.Empty;
        ContinueButtonLabel = string.IsNullOrWhiteSpace(continueButtonLabel) ? "Continue" : continueButtonLabel.Trim();
        BoonOptions = boonOptions ?? Array.Empty<HexBoonDefinition>();
    }

    public string Title { get; }
    public string Body { get; }
    public string SelectionPrompt { get; }
    public string ContinueButtonLabel { get; }
    public HexBoonDefinition[] BoonOptions { get; }
    public bool RequiresBoonSelection => BoonOptions != null && BoonOptions.Length > 0;
}

public readonly struct HexActGenerationProfileSelection
{
    public HexActGenerationProfileSelection(int actNumber, HexActGenerationProfileAsset profile)
    {
        ActNumber = Mathf.Max(1, actNumber);
        Profile = profile;
    }

    public int ActNumber { get; }
    public HexActGenerationProfileAsset Profile { get; }
    public bool HasProfile => Profile != null && Profile.isEnabled;
}

internal sealed class HexActRunSessionState
{
    public int CurrentActNumber = 1;
    public CaravanResourceSnapshot CurrentResources;
    public HexNemesisArchetype LockedBoonFamily = HexNemesisArchetype.None;
    public readonly List<HexBoonDefinition> SelectedBoons = new();
}

public static class HexActTransitionService
{
    public const string DefaultConfigResourcePath = "Acts/DefaultActTransitionConfig";

    private static HexActRunSessionState currentSession;
    private static HexActTransitionConfigAsset cachedSceneConfig;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStaticState()
    {
        currentSession = null;
        cachedSceneConfig = null;
    }

    public static HexActTransitionController FindSceneController()
    {
        return UnityEngine.Object.FindAnyObjectByType<HexActTransitionController>();
    }

    public static HexActTransitionConfigAsset LoadConfig()
    {
        HexActTransitionController sceneController = FindSceneController();
        if (sceneController != null && sceneController.isActiveAndEnabled)
        {
            cachedSceneConfig = sceneController.GetTransitionConfig();
            if (cachedSceneConfig != null)
            {
                cachedSceneConfig.hideFlags &= ~HideFlags.DontUnloadUnusedAsset;
                return cachedSceneConfig;
            }
        }

        HexActTransitionConfigAsset config = Resources.Load<HexActTransitionConfigAsset>(DefaultConfigResourcePath);
        if (config != null)
        {
            config.hideFlags &= ~HideFlags.DontUnloadUnusedAsset;
        }

        return config;
    }

    public static void EnsureRunSession(CaravanResourceSnapshot defaultResources)
    {
        if (currentSession != null)
        {
            return;
        }

        currentSession = new HexActRunSessionState
        {
            CurrentActNumber = 1,
            CurrentResources = defaultResources
        };
    }

    public static void ResetRunSession()
    {
        currentSession = null;
    }

    public static bool UsesActTransitionBoonState()
    {
        HexActTransitionConfigAsset config = LoadConfig();
        return config != null && config.enableActTransitions;
    }

    public static int GetCurrentActNumber()
    {
        return currentSession?.CurrentActNumber ?? 1;
    }

    public static HexNemesisArchetype GetLockedBoonFamily()
    {
        return currentSession?.LockedBoonFamily ?? HexNemesisArchetype.None;
    }

    public static IReadOnlyList<HexBoonDefinition> GetSelectedBoons()
    {
        if (!UsesActTransitionBoonState())
        {
            return Array.Empty<HexBoonDefinition>();
        }

        if (currentSession == null || currentSession.SelectedBoons.Count == 0)
        {
            return Array.Empty<HexBoonDefinition>();
        }

        List<HexBoonDefinition> activeBoons = new(currentSession.SelectedBoons.Count);
        for (int index = 0; index < currentSession.SelectedBoons.Count; index++)
        {
            HexBoonDefinition boon = currentSession.SelectedBoons[index];
            if (boon == null)
            {
                continue;
            }

            boon.Validate();
            if (boon.isEnabled)
            {
                activeBoons.Add(boon);
            }
        }

        return activeBoons.Count == 0 ? Array.Empty<HexBoonDefinition>() : activeBoons;
    }

    public static CaravanResourceSnapshot GetStartingResources(CaravanResourceSnapshot defaultResources)
    {
        EnsureRunSession(defaultResources);
        return currentSession != null ? currentSession.CurrentResources : defaultResources;
    }

    public static bool CanAdvanceFromCurrentAct()
    {
        HexActTransitionConfigAsset config = LoadConfig();
        if (config == null || !config.enableActTransitions)
        {
            return false;
        }

        int currentAct = GetCurrentActNumber();
        return currentAct < Mathf.Max(1, config.totalActs);
    }

    public static bool ShouldDisableNemesisForCurrentAct()
    {
        HexActTransitionConfigAsset config = LoadConfig();
        return config != null
            && config.enableActTransitions
            && config.disableNemesisInAct2
            && GetCurrentActNumber() == 2;
    }

    public static HexNemesisArchetype ResolveNemesisArchetypeForCurrentAct(HexNemesisArchetype defaultArchetype)
    {
        if (!UsesActTransitionBoonState())
        {
            return defaultArchetype;
        }

        int currentAct = GetCurrentActNumber();
        if (currentAct >= 3 && GetLockedBoonFamily() != HexNemesisArchetype.None)
        {
            return GetLockedBoonFamily();
        }

        return defaultArchetype;
    }

    public static bool ResolveNemesisEnabledForCurrentAct(bool defaultEnabled)
    {
        if (!UsesActTransitionBoonState())
        {
            return defaultEnabled;
        }

        if (ShouldDisableNemesisForCurrentAct())
        {
            return false;
        }

        int currentAct = GetCurrentActNumber();
        if (currentAct >= 3 && GetLockedBoonFamily() != HexNemesisArchetype.None)
        {
            return true;
        }

        return defaultEnabled;
    }

    public static HexActGenerationProfileSelection GetCurrentActGenerationProfile()
    {
        int actNumber = GetCurrentActNumber();
        HexActTransitionConfigAsset config = LoadConfig();
        HexActGenerationProfileAsset profile = config != null && config.enableActTransitions
            ? config.GetProfileForAct(actNumber)
            : null;
        return new HexActGenerationProfileSelection(actNumber, profile);
    }

    public static HexActTransitionDisplayData BuildTransitionDisplayData(CaravanResourceSnapshot currentResources)
    {
        HexActTransitionConfigAsset config = LoadConfig();
        int currentAct = GetCurrentActNumber();
        HexActTransitionStepDefinition step = config != null
            ? config.GetTransitionForCompletedAct(currentAct)
            : null;

        string title = step != null && !string.IsNullOrWhiteSpace(step.title)
            ? step.title.Trim()
            : $"Act {currentAct} Complete";

        string body = step != null && !string.IsNullOrWhiteSpace(step.body)
            ? step.body.Trim()
            : "The caravan prepares for the next act.";

        if (step != null && step.betweenActGrant != null && step.betweenActGrant.HasAny)
        {
            body = $"{body}\n\n{FormatGrant(step.betweenActGrant)}";
        }

        HexBoonDefinition[] boonOptions = config != null
            ? config.GetBoonOptionsForCompletedAct(currentAct, GetLockedBoonFamily(), GetSelectedBoons())
            : Array.Empty<HexBoonDefinition>();

        string selectionPrompt = boonOptions.Length > 0
            ? BuildSelectionPrompt(step, currentAct)
            : string.Empty;

        string buttonLabel = step != null ? step.continueButtonLabel : "Continue";
        return new HexActTransitionDisplayData(title, body, selectionPrompt, buttonLabel, boonOptions);
    }

    public static bool TryAdvanceToNextAct(CaravanResourceSnapshot currentResources, HexBoonDefinition selectedBoon = null)
    {
        EnsureRunSession(currentResources);
        if (currentSession == null)
        {
            return false;
        }

        HexActTransitionConfigAsset config = LoadConfig();
        int completedAct = currentSession.CurrentActNumber;
        HexActTransitionStepDefinition step = config != null
            ? config.GetTransitionForCompletedAct(completedAct)
            : null;

        HexBoonDefinition[] availableBoons = config != null
            ? config.GetBoonOptionsForCompletedAct(completedAct, currentSession.LockedBoonFamily, currentSession.SelectedBoons)
            : Array.Empty<HexBoonDefinition>();

        if (availableBoons.Length > 0)
        {
            if (selectedBoon == null)
            {
                Debug.LogError("[ActTransition] Cannot advance without a selected boon.");
                return false;
            }

            selectedBoon.Validate();
            if (!selectedBoon.isEnabled)
            {
                Debug.LogError($"[ActTransition] Cannot advance with disabled boon '{selectedBoon.GetResolvedDisplayName()}'.");
                return false;
            }

            bool isAvailable = false;
            for (int index = 0; index < availableBoons.Length; index++)
            {
                HexBoonDefinition availableBoon = availableBoons[index];
                if (availableBoon == null)
                {
                    continue;
                }

                availableBoon.Validate();
                if (string.Equals(availableBoon.id, selectedBoon.id, StringComparison.OrdinalIgnoreCase))
                {
                    isAvailable = true;
                    break;
                }
            }

            if (!isAvailable)
            {
                Debug.LogError($"[ActTransition] Boon '{selectedBoon.GetResolvedDisplayName()}' is not valid for Act {completedAct}.");
                return false;
            }

            if (completedAct == 1 && currentSession.LockedBoonFamily == HexNemesisArchetype.None)
            {
                currentSession.LockedBoonFamily = selectedBoon.archetypeFamily;
            }

            if (currentSession.LockedBoonFamily != HexNemesisArchetype.None
                && completedAct >= 2
                && selectedBoon.archetypeFamily != currentSession.LockedBoonFamily)
            {
                Debug.LogError(
                    $"[ActTransition] Boon '{selectedBoon.GetResolvedDisplayName()}' does not match locked family {FormatArchetype(currentSession.LockedBoonFamily)}.");
                return false;
            }

            StoreSelectedBoon(selectedBoon);
        }

        CaravanResourceSnapshot nextResources = currentResources;
        if (step?.betweenActGrant != null)
        {
            nextResources = step.betweenActGrant.ApplyTo(nextResources);
        }

        if (selectedBoon != null)
        {
            nextResources = selectedBoon.ApplyActStartGrant(nextResources);
        }

        currentSession.CurrentActNumber++;
        currentSession.CurrentResources = nextResources;
        return true;
    }

    private static string FormatGrant(HexActResourceGrant grant)
    {
        System.Collections.Generic.List<string> parts = new();
        if (grant.food != 0)
        {
            parts.Add($"{FormatSigned(grant.food)} Food");
        }

        if (grant.morale != 0)
        {
            parts.Add($"{FormatSigned(grant.morale)} Morale");
        }

        if (grant.gold != 0)
        {
            parts.Add($"{FormatSigned(grant.gold)} Gold");
        }

        return parts.Count > 0
            ? $"Between-act grant: {string.Join(", ", parts)}."
            : "No between-act grant.";
    }

    private static string FormatSigned(int value)
    {
        return value > 0 ? $"+{value}" : value.ToString();
    }

    private static void StoreSelectedBoon(HexBoonDefinition selectedBoon)
    {
        if (currentSession == null || selectedBoon == null)
        {
            return;
        }

        for (int index = 0; index < currentSession.SelectedBoons.Count; index++)
        {
            HexBoonDefinition existingBoon = currentSession.SelectedBoons[index];
            if (existingBoon == null)
            {
                continue;
            }

            existingBoon.Validate();
            if (string.Equals(existingBoon.id, selectedBoon.id, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }
        }

        currentSession.SelectedBoons.Add(selectedBoon);
    }

    private static string BuildSelectionPrompt(HexActTransitionStepDefinition step, int completedAct)
    {
        string prompt = step != null && !string.IsNullOrWhiteSpace(step.boonSelectionPrompt)
            ? step.boonSelectionPrompt.Trim()
            : "Choose one boon to carry into the next act.";

        if (completedAct >= 2 && GetLockedBoonFamily() != HexNemesisArchetype.None)
        {
            prompt = $"{prompt}\n\nLocked family: {FormatArchetype(GetLockedBoonFamily())}.";
        }

        return prompt;
    }

    private static string FormatArchetype(HexNemesisArchetype archetype)
    {
        return archetype switch
        {
            HexNemesisArchetype.Hunter => "Hunter",
            HexNemesisArchetype.Echo => "Echo",
            HexNemesisArchetype.Corruptor => "Corruptor",
            _ => "None"
        };
    }
}
