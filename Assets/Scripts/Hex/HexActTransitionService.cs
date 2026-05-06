using System;
using System.Collections.Generic;
using UnityEngine;

public readonly struct HexActTransitionDisplayData
{
    public HexActTransitionDisplayData(
        string title,
        string body,
        string selectionPrompt,
        string selectionContextNote,
        string intermissionContinueButtonLabel,
        string continueButtonLabel,
        HexBoonDefinition[] boonOptions)
        : this(
            title,
            body,
            selectionPrompt,
            selectionContextNote,
            intermissionContinueButtonLabel,
            continueButtonLabel,
            boonOptions,
            0,
            0,
            new CaravanResourceSnapshot(0, 0, 0),
            0,
            0,
            0,
            null)
    {
    }

    public HexActTransitionDisplayData(
        string title,
        string body,
        string selectionPrompt,
        string selectionContextNote,
        string intermissionContinueButtonLabel,
        string continueButtonLabel,
        HexBoonDefinition[] boonOptions,
        int completedActNumber,
        int nextActNumber,
        CaravanResourceSnapshot currentResources,
        int betweenActFood,
        int betweenActMorale,
        int betweenActGold,
        Sprite intermissionIllustration)
    {
        Title = title ?? string.Empty;
        Body = body ?? string.Empty;
        SelectionPrompt = selectionPrompt ?? string.Empty;
        SelectionContextNote = selectionContextNote ?? string.Empty;
        IntermissionContinueButtonLabel = string.IsNullOrWhiteSpace(intermissionContinueButtonLabel)
            ? "Choose a Boon"
            : intermissionContinueButtonLabel.Trim();
        ContinueButtonLabel = string.IsNullOrWhiteSpace(continueButtonLabel) ? "Continue" : continueButtonLabel.Trim();
        BoonOptions = boonOptions ?? Array.Empty<HexBoonDefinition>();
        CompletedActNumber = Mathf.Max(0, completedActNumber);
        NextActNumber = Mathf.Max(0, nextActNumber);
        CurrentResources = currentResources;
        BetweenActFood = betweenActFood;
        BetweenActMorale = betweenActMorale;
        BetweenActGold = betweenActGold;
        IntermissionIllustration = intermissionIllustration;
    }

    public string Title { get; }
    public string Body { get; }
    public string SelectionPrompt { get; }
    public string SelectionContextNote { get; }
    public string IntermissionContinueButtonLabel { get; }
    public string ContinueButtonLabel { get; }
    public HexBoonDefinition[] BoonOptions { get; }
    public int CompletedActNumber { get; }
    public int NextActNumber { get; }
    public CaravanResourceSnapshot CurrentResources { get; }
    public int BetweenActFood { get; }
    public int BetweenActMorale { get; }
    public int BetweenActGold { get; }
    public Sprite IntermissionIllustration { get; }
    public bool HasBetweenActGrant => BetweenActFood != 0 || BetweenActMorale != 0 || BetweenActGold != 0;
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

public readonly struct HexActTransitionAdvanceResult
{
    public HexActTransitionAdvanceResult(
        bool succeeded,
        int completedActNumber,
        int nextActNumber,
        CaravanResourceSnapshot nextResources,
        HexNemesisArchetype lockedBoonFamily,
        IReadOnlyList<HexBoonDefinition> selectedBoons)
    {
        Succeeded = succeeded;
        CompletedActNumber = Mathf.Max(1, completedActNumber);
        NextActNumber = Mathf.Max(1, nextActNumber);
        NextResources = nextResources;
        LockedBoonFamily = lockedBoonFamily;
        SelectedBoons = selectedBoons ?? Array.Empty<HexBoonDefinition>();
    }

    public bool Succeeded { get; }
    public int CompletedActNumber { get; }
    public int NextActNumber { get; }
    public CaravanResourceSnapshot NextResources { get; }
    public HexNemesisArchetype LockedBoonFamily { get; }
    public IReadOnlyList<HexBoonDefinition> SelectedBoons { get; }
}

public static class HexActTransitionService
{
    public const string DefaultConfigResourcePath = "Acts/DefaultActTransitionConfig";

    private static HexActTransitionConfigAsset cachedSceneConfig;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStaticState()
    {
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
        HexRunSessionController.Resolve()?.GetStartingResources(defaultResources);
    }

    public static void ResetRunSession()
    {
        HexRunSessionController.ResetActiveSession();
    }

    public static bool HasActiveRunSession()
    {
        return HexRunSessionController.HasActiveSession();
    }

    public static void DebugConfigureRunSession(
        int currentActNumber,
        CaravanResourceSnapshot currentResources,
        HexNemesisArchetype lockedBoonFamily = HexNemesisArchetype.None,
        IReadOnlyList<HexBoonDefinition> selectedBoons = null)
    {
        List<HexBoonDefinition> validSelectedBoons = new();
        if (selectedBoons == null)
        {
            HexRunSessionController.Resolve()?.ConfigureRun(
                currentActNumber,
                currentResources,
                lockedBoonFamily,
                validSelectedBoons);
            return;
        }

        for (int index = 0; index < selectedBoons.Count; index++)
        {
            HexBoonDefinition boon = selectedBoons[index];
            if (boon == null)
            {
                continue;
            }

            boon.Validate();
            if (!boon.isEnabled)
            {
                continue;
            }

            validSelectedBoons.Add(boon);
        }

        HexRunSessionController.Resolve()?.ConfigureRun(
            currentActNumber,
            currentResources,
            lockedBoonFamily,
            validSelectedBoons);
    }

    public static bool UsesActTransitionBoonState()
    {
        HexActTransitionConfigAsset config = LoadConfig();
        return config != null && config.enableActTransitions;
    }

    public static int GetCurrentActNumber()
    {
        HexRunSessionController sessionController = HexRunSessionController.Resolve(createIfMissing: false);
        return sessionController != null && sessionController.HasActiveRunSession
            ? sessionController.RunState.CurrentActNumber
            : 1;
    }

    public static HexNemesisArchetype GetLockedBoonFamily()
    {
        HexRunSessionController sessionController = HexRunSessionController.Resolve(createIfMissing: false);
        return sessionController != null && sessionController.HasActiveRunSession
            ? sessionController.RunState.LockedBoonFamily
            : HexNemesisArchetype.None;
    }

    public static IReadOnlyList<HexBoonDefinition> GetSelectedBoons()
    {
        if (!UsesActTransitionBoonState())
        {
            return Array.Empty<HexBoonDefinition>();
        }

        HexRunSessionController sessionController = HexRunSessionController.Resolve(createIfMissing: false);
        IReadOnlyList<HexBoonDefinition> selectedBoons = sessionController != null && sessionController.HasActiveRunSession
            ? sessionController.RunState.SelectedBoons
            : Array.Empty<HexBoonDefinition>();
        if (selectedBoons.Count == 0)
        {
            return Array.Empty<HexBoonDefinition>();
        }

        List<HexBoonDefinition> activeBoons = new(selectedBoons.Count);
        for (int index = 0; index < selectedBoons.Count; index++)
        {
            HexBoonDefinition boon = selectedBoons[index];
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
        HexRunSessionController sessionController = HexRunSessionController.Resolve();
        return sessionController != null
            ? sessionController.GetStartingResources(defaultResources)
            : defaultResources;
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
        int nextAct = currentAct + 1;
        HexActTransitionStepDefinition step = config != null
            ? config.GetTransitionForCompletedAct(currentAct)
            : null;

        string title = step != null && !string.IsNullOrWhiteSpace(step.title)
            ? step.title.Trim()
            : $"Act {currentAct} Complete";

        string body = step != null && !string.IsNullOrWhiteSpace(step.body)
            ? step.body.Trim()
            : "The caravan prepares for the next act.";

        HexBoonDefinition[] boonOptions = config != null
            ? config.GetBoonOptionsForCompletedAct(currentAct, GetLockedBoonFamily(), GetSelectedBoons())
            : Array.Empty<HexBoonDefinition>();

        string selectionPrompt = boonOptions.Length > 0
            ? BuildSelectionPrompt(step)
            : string.Empty;
        string selectionContextNote = boonOptions.Length > 0
            ? BuildSelectionContextNote(currentAct)
            : string.Empty;

        string buttonLabel = step != null ? step.continueButtonLabel : "Continue";
        string intermissionContinueButtonLabel = boonOptions.Length > 0
            ? ResolveIntermissionContinueButtonLabel(step)
            : buttonLabel;
        HexActResourceGrant grant = step?.betweenActGrant;
        return new HexActTransitionDisplayData(
            title,
            body,
            selectionPrompt,
            selectionContextNote,
            intermissionContinueButtonLabel,
            buttonLabel,
            boonOptions,
            currentAct,
            nextAct,
            currentResources,
            grant?.food ?? 0,
            grant?.morale ?? 0,
            grant?.gold ?? 0,
            step?.intermissionIllustration);
    }

    public static bool TryAdvanceToNextAct(CaravanResourceSnapshot currentResources, HexBoonDefinition selectedBoon = null)
    {
        HexRunSessionController sessionController = HexRunSessionController.Resolve();
        return sessionController != null && sessionController.TryAdvanceToNextAct(currentResources, selectedBoon);
    }

    public static bool TryBuildAdvanceResult(
        HexRunStateSnapshot currentRunState,
        CaravanResourceSnapshot currentResources,
        HexBoonDefinition selectedBoon,
        out HexActTransitionAdvanceResult advanceResult)
    {
        advanceResult = default;
        HexActTransitionConfigAsset config = LoadConfig();
        int completedAct = Mathf.Max(1, currentRunState.CurrentActNumber);
        HexActTransitionStepDefinition step = config != null
            ? config.GetTransitionForCompletedAct(completedAct)
            : null;

        HexBoonDefinition[] availableBoons = config != null
            ? config.GetBoonOptionsForCompletedAct(completedAct, currentRunState.LockedBoonFamily, currentRunState.SelectedBoons)
            : Array.Empty<HexBoonDefinition>();

        HexNemesisArchetype nextLockedFamily = currentRunState.LockedBoonFamily;
        List<HexBoonDefinition> nextSelectedBoons = BuildSelectedBoonList(currentRunState.SelectedBoons);

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

            if (completedAct == 1 && nextLockedFamily == HexNemesisArchetype.None)
            {
                nextLockedFamily = selectedBoon.archetypeFamily;
            }

            if (nextLockedFamily != HexNemesisArchetype.None
                && completedAct >= 2
                && selectedBoon.archetypeFamily != nextLockedFamily)
            {
                Debug.LogError(
                    $"[ActTransition] Boon '{selectedBoon.GetResolvedDisplayName()}' does not match locked family {FormatArchetype(nextLockedFamily)}.");
                return false;
            }

            AddSelectedBoonIfMissing(nextSelectedBoons, selectedBoon);
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

        advanceResult = new HexActTransitionAdvanceResult(
            true,
            completedAct,
            completedAct + 1,
            nextResources,
            nextLockedFamily,
            nextSelectedBoons);
        return true;
    }

    private static List<HexBoonDefinition> BuildSelectedBoonList(IReadOnlyList<HexBoonDefinition> selectedBoons)
    {
        List<HexBoonDefinition> result = new();
        if (selectedBoons == null)
        {
            return result;
        }

        for (int index = 0; index < selectedBoons.Count; index++)
        {
            AddSelectedBoonIfMissing(result, selectedBoons[index]);
        }

        return result;
    }

    private static void AddSelectedBoonIfMissing(List<HexBoonDefinition> selectedBoons, HexBoonDefinition selectedBoon)
    {
        if (selectedBoons == null || selectedBoon == null)
        {
            return;
        }

        for (int index = 0; index < selectedBoons.Count; index++)
        {
            HexBoonDefinition existingBoon = selectedBoons[index];
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

        selectedBoons.Add(selectedBoon);
    }

    private static string BuildSelectionPrompt(HexActTransitionStepDefinition step)
    {
        return step != null && !string.IsNullOrWhiteSpace(step.boonSelectionPrompt)
            ? step.boonSelectionPrompt.Trim()
            : "Choose one boon to carry into the next act.";
    }

    private static string BuildSelectionContextNote(int completedAct)
    {
        if (completedAct <= 1)
        {
            return "This choice determines the family for Act 3.";
        }

        if (GetLockedBoonFamily() != HexNemesisArchetype.None)
        {
            string lockedFamilyLabel = FormatArchetype(GetLockedBoonFamily());
            return $"The {lockedFamilyLabel} family is already locked for Act 3.";
        }

        return string.Empty;
    }

    private static string ResolveIntermissionContinueButtonLabel(HexActTransitionStepDefinition step)
    {
        return step != null && !string.IsNullOrWhiteSpace(step.intermissionContinueButtonLabel)
            ? step.intermissionContinueButtonLabel.Trim()
            : "Choose a Boon";
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
