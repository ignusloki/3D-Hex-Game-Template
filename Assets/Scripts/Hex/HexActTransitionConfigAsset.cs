using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class HexActResourceGrant
{
    public int food;
    public int morale;
    public int gold;

    public CaravanResourceSnapshot ApplyTo(CaravanResourceSnapshot snapshot)
    {
        return new CaravanResourceSnapshot(
            snapshot.Food + food,
            snapshot.Morale + morale,
            snapshot.Gold + gold);
    }

    public bool HasAny => food != 0 || morale != 0 || gold != 0;
}

[Serializable]
public sealed class HexActTransitionStepDefinition
{
    public string title = "Act Complete";
    [TextArea(3, 8)] public string body =
        "The caravan presses on. A new stretch of the road lies ahead.";
    [TextArea(2, 5)] public string boonSelectionPrompt =
        "Choose one boon to carry into the next act.";
    public string continueButtonLabel = "Continue";
    public HexActResourceGrant betweenActGrant = new();
    public HexBoonDefinition[] boonOptions = Array.Empty<HexBoonDefinition>();

    public void Validate()
    {
        title = string.IsNullOrWhiteSpace(title) ? "Act Complete" : title.Trim();
        body ??= string.Empty;
        boonSelectionPrompt = string.IsNullOrWhiteSpace(boonSelectionPrompt)
            ? "Choose one boon to carry into the next act."
            : boonSelectionPrompt.Trim();
        continueButtonLabel = string.IsNullOrWhiteSpace(continueButtonLabel) ? "Continue" : continueButtonLabel.Trim();
        betweenActGrant ??= new HexActResourceGrant();
        boonOptions ??= Array.Empty<HexBoonDefinition>();

        for (int index = 0; index < boonOptions.Length; index++)
        {
            boonOptions[index]?.Validate();
        }
    }
}

[CreateAssetMenu(
    fileName = "ActTransitionConfig",
    menuName = "Hex/Acts/Act Transition Config")]
public sealed class HexActTransitionConfigAsset : ScriptableObject
{
    public bool enableActTransitions = true;
    [Min(1)] public int totalActs = 3;
    public bool disableNemesisInAct2 = true;
    [Header("Act Profiles")]
    public HexActGenerationProfileAsset act1Profile;
    public HexActGenerationProfileAsset act2Profile;
    public HexActGenerationProfileAsset act3Profile;
    [Header("Transitions")]
    public HexActTransitionStepDefinition act1ToAct2 = new()
    {
        title = "Act 1 Complete",
        body = "The caravan reaches the first goal. A harder road opens ahead.",
        continueButtonLabel = "Begin Act 2",
        betweenActGrant = new HexActResourceGrant
        {
            food = 6,
            morale = 1,
            gold = 2
        }
    };
    public HexActTransitionStepDefinition act2ToAct3 = new()
    {
        title = "Act 2 Complete",
        body = "The crossing continues. The last stretch of the journey is near.",
        continueButtonLabel = "Begin Act 3",
        betweenActGrant = new HexActResourceGrant
        {
            food = 8,
            morale = 1,
            gold = 3
        }
    };

    private void OnValidate()
    {
        ValidateInternal();
    }

    public void OnValidateFromSceneController()
    {
        ValidateInternal();
    }

    private void ValidateInternal()
    {
        totalActs = Mathf.Max(1, totalActs);
        act1ToAct2 ??= new HexActTransitionStepDefinition();
        act2ToAct3 ??= new HexActTransitionStepDefinition();
        act1Profile?.Validate();
        act2Profile?.Validate();
        act3Profile?.Validate();
        act1ToAct2.Validate();
        act2ToAct3.Validate();
    }

    public HexActTransitionStepDefinition GetTransitionForCompletedAct(int completedAct)
    {
        return completedAct switch
        {
            1 => act1ToAct2,
            2 => act2ToAct3,
            _ => null
        };
    }

    public HexActGenerationProfileAsset GetProfileForAct(int actNumber)
    {
        return actNumber switch
        {
            1 => act1Profile,
            2 => act2Profile,
            3 => act3Profile,
            _ => null
        };
    }

    public HexBoonDefinition[] GetBoonOptionsForCompletedAct(
        int completedAct,
        HexNemesisArchetype lockedFamily,
        IReadOnlyList<HexBoonDefinition> alreadySelectedBoons)
    {
        HexActTransitionStepDefinition step = GetTransitionForCompletedAct(completedAct);
        if (step == null)
        {
            return Array.Empty<HexBoonDefinition>();
        }

        return ResolveBoonOptions(step.boonOptions, completedAct, lockedFamily, alreadySelectedBoons);
    }

    private static HexBoonDefinition[] ResolveBoonOptions(
        HexBoonDefinition[] sourceOptions,
        int completedAct,
        HexNemesisArchetype lockedFamily,
        IReadOnlyList<HexBoonDefinition> alreadySelectedBoons)
    {
        List<HexBoonDefinition> sanitizedOptions = new();
        HashSet<string> seenIds = new(StringComparer.OrdinalIgnoreCase);

        if (sourceOptions != null)
        {
            for (int index = 0; index < sourceOptions.Length; index++)
            {
                HexBoonDefinition option = sourceOptions[index];
                if (option == null)
                {
                    continue;
                }

                option.Validate();
                if (!option.isEnabled)
                {
                    continue;
                }

                if (!seenIds.Add(option.id))
                {
                    continue;
                }

                sanitizedOptions.Add(option);
            }
        }

        if (sanitizedOptions.Count == 0)
        {
            return Array.Empty<HexBoonDefinition>();
        }

        bool requiresLockedFamily = completedAct >= 2 && lockedFamily != HexNemesisArchetype.None;
        if (requiresLockedFamily)
        {
            sanitizedOptions.RemoveAll(option => option == null || option.archetypeFamily != lockedFamily);
        }

        if (sanitizedOptions.Count == 0)
        {
            return Array.Empty<HexBoonDefinition>();
        }

        HashSet<string> selectedIds = new(StringComparer.OrdinalIgnoreCase);
        if (alreadySelectedBoons != null)
        {
            for (int index = 0; index < alreadySelectedBoons.Count; index++)
            {
                HexBoonDefinition selected = alreadySelectedBoons[index];
                if (selected == null)
                {
                    continue;
                }

                selected.Validate();
                if (!string.IsNullOrWhiteSpace(selected.id))
                {
                    selectedIds.Add(selected.id);
                }
            }
        }

        if (selectedIds.Count == 0)
        {
            return sanitizedOptions.ToArray();
        }

        List<HexBoonDefinition> unselectedOptions = new(sanitizedOptions.Count);
        for (int index = 0; index < sanitizedOptions.Count; index++)
        {
            HexBoonDefinition option = sanitizedOptions[index];
            if (option == null || selectedIds.Contains(option.id))
            {
                continue;
            }

            unselectedOptions.Add(option);
        }

        return unselectedOptions.Count > 0 ? unselectedOptions.ToArray() : sanitizedOptions.ToArray();
    }
}
