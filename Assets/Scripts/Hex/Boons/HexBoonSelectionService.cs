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
        HexBoonSystemController sceneController = FindSceneController();
        if (sceneController != null && sceneController.isActiveAndEnabled)
        {
            return sceneController.GetSelectedBoonDefinition();
        }

        HexBoonSelectionAsset selectionAsset = LoadSelectionAsset();
        return selectionAsset != null ? selectionAsset.GetSelectedBoon() : null;
    }

    public static HexActMapModifiers GetActMapModifiers()
    {
        HexBoonDefinition definition = GetSelectedBoonDefinition();
        if (definition == null)
        {
            return HexActMapModifiers.None;
        }

        return new HexActMapModifiers(
            definition.GetAdditionalPitstopCount(),
            definition.GetAdditionalPitstopPlacementBand());
    }
}
