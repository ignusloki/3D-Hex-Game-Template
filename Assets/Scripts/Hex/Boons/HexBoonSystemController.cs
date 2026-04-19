using UnityEngine;

[AddComponentMenu("Hex/Boons/Boon System Controller")]
[DisallowMultipleComponent]
public sealed class HexBoonSystemController : MonoBehaviour
{
    [SerializeField] private bool applySelectedBoonAtRunStart = true;
    [SerializeField] private HexBoonDefinition selectedBoon;

    public bool ApplySelectedBoonAtRunStart => applySelectedBoonAtRunStart;
    public HexBoonDefinition SelectedBoon => selectedBoon;

    private void Reset()
    {
        gameObject.name = "Boon System";
    }

    private void OnValidate()
    {
        selectedBoon?.Validate();
    }

    public HexBoonDefinition GetSelectedBoonDefinition()
    {
        if (!applySelectedBoonAtRunStart || selectedBoon == null)
        {
            return null;
        }

        selectedBoon.Validate();
        return selectedBoon.isEnabled ? selectedBoon : null;
    }

    public string GetSelectionSummary()
    {
        if (HexActTransitionService.UsesActTransitionBoonState())
        {
            return "Act transitions are enabled. Run-start boon selection is ignored until a transition boon is chosen.";
        }

        if (!applySelectedBoonAtRunStart)
        {
            return "Boons are disabled for this run.";
        }

        if (selectedBoon == null)
        {
            return "No boon selected.";
        }

        selectedBoon.Validate();
        string name = selectedBoon.GetResolvedDisplayName();
        return selectedBoon.category switch
        {
            HexBoonCategory.Passive => $"{name}: passive boon.",
            HexBoonCategory.Rechargeable => $"{name}: rechargeable boon.",
            HexBoonCategory.MapModifying => $"{name}: map-modifying boon.",
            _ => name
        };
    }
}
