using UnityEngine;

public readonly struct HexActTransitionDisplayData
{
    public HexActTransitionDisplayData(string title, string body, string continueButtonLabel)
    {
        Title = title ?? string.Empty;
        Body = body ?? string.Empty;
        ContinueButtonLabel = string.IsNullOrWhiteSpace(continueButtonLabel) ? "Continue" : continueButtonLabel.Trim();
    }

    public string Title { get; }
    public string Body { get; }
    public string ContinueButtonLabel { get; }
}

internal sealed class HexActRunSessionState
{
    public int CurrentActNumber = 1;
    public CaravanResourceSnapshot CurrentResources;
}

public static class HexActTransitionService
{
    public const string DefaultConfigResourcePath = "Acts/DefaultActTransitionConfig";

    private static HexActRunSessionState currentSession;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStaticState()
    {
        currentSession = null;
    }

    public static HexActTransitionConfigAsset LoadConfig()
    {
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

    public static int GetCurrentActNumber()
    {
        return currentSession?.CurrentActNumber ?? 1;
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

        string buttonLabel = step != null ? step.continueButtonLabel : "Continue";
        return new HexActTransitionDisplayData(title, body, buttonLabel);
    }

    public static void AdvanceToNextAct(CaravanResourceSnapshot currentResources)
    {
        EnsureRunSession(currentResources);
        if (currentSession == null)
        {
            return;
        }

        HexActTransitionConfigAsset config = LoadConfig();
        HexActTransitionStepDefinition step = config != null
            ? config.GetTransitionForCompletedAct(currentSession.CurrentActNumber)
            : null;

        CaravanResourceSnapshot nextResources = currentResources;
        if (step?.betweenActGrant != null)
        {
            nextResources = step.betweenActGrant.ApplyTo(nextResources);
        }

        currentSession.CurrentActNumber++;
        currentSession.CurrentResources = nextResources;
        Debug.Log($"[ActTransition] Advanced to Act {currentSession.CurrentActNumber}. Resources: Food={nextResources.Food}, Morale={nextResources.Morale}, Gold={nextResources.Gold}.");
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
}
