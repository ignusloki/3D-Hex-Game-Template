using UnityEngine;

public static class HexBoonSelectionService
{
    public const string DefaultSelectionResourcePath = "Boons/DefaultBoonSelection";

    public static HexBoonSystemController FindSceneController()
    {
        return Object.FindAnyObjectByType<HexBoonSystemController>();
    }

    public static HexBoonSelectionAsset LoadSelectionAsset()
    {
        return Resources.Load<HexBoonSelectionAsset>(DefaultSelectionResourcePath);
    }

    public static HexBoonDefinition GetSelectedBoonDefinition()
    {
        System.Collections.Generic.IReadOnlyList<HexBoonDefinition> definitions = GetSelectedBoonDefinitions();
        return definitions.Count > 0 ? definitions[0] : null;
    }

    public static System.Collections.Generic.IReadOnlyList<HexBoonDefinition> GetSelectedBoonDefinitions()
    {
        if (HexActTransitionService.UsesActTransitionBoonState())
        {
            return HexActTransitionService.GetSelectedBoons();
        }

        HexBoonSystemController sceneController = FindSceneController();
        if (sceneController != null && sceneController.isActiveAndEnabled)
        {
            HexBoonDefinition selectedBoon = sceneController.GetSelectedBoonDefinition();
            return selectedBoon != null
                ? new[] { selectedBoon }
                : System.Array.Empty<HexBoonDefinition>();
        }

        HexBoonSelectionAsset selectionAsset = LoadSelectionAsset();
        HexBoonDefinition fallbackSelection = selectionAsset != null ? selectionAsset.GetSelectedBoon() : null;
        return fallbackSelection != null
            ? new[] { fallbackSelection }
            : System.Array.Empty<HexBoonDefinition>();
    }

    public static HexMapGenerationModifiers GetMapGenerationModifiers()
    {
        HexMapGenerationModifiers combinedModifiers = HexMapGenerationModifiers.None;
        System.Collections.Generic.IReadOnlyList<HexBoonDefinition> definitions = GetSelectedBoonDefinitions();
        for (int index = 0; index < definitions.Count; index++)
        {
            HexBoonDefinition definition = definitions[index];
            if (definition == null)
            {
                continue;
            }

            combinedModifiers = combinedModifiers.Combine(definition.GetMapGenerationModifiers());
        }

        return combinedModifiers;
    }
}
