using UnityEngine;

[CreateAssetMenu(
    fileName = "BoonSelection",
    menuName = "Hex/Boons/Boon Selection")]
public sealed class HexBoonSelectionAsset : ScriptableObject
{
    public bool useSelectedBoonAtRunStart = true;
    public HexBoonDefinition selectedBoon;

    private void OnValidate()
    {
        selectedBoon?.Validate();
    }

    public HexBoonDefinition GetSelectedBoon()
    {
        if (!useSelectedBoonAtRunStart || selectedBoon == null)
        {
            return null;
        }

        selectedBoon.Validate();
        return selectedBoon.isEnabled ? selectedBoon : null;
    }
}
