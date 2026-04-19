using UnityEngine;

public sealed class CaravanMetricsController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerController playerController;
    [Header("Resources")]
    [Min(0)] [SerializeField] private int food = 20;
    [Min(0)] [SerializeField] private int morale = 3;
    [Min(0)] [SerializeField] private int gold = 3;
    [Min(0.05f)] [SerializeField] private float applyDelaySeconds = 0.3f;
    [Header("Runtime Selection")]
    [TextArea(3, 8)] [SerializeField] private string selectedBoonsSummary = "Boons: None";

    private bool hasPresentedValues;
    private bool hasPendingInspectorApply;
    private int lastPresentedFood;
    private int lastPresentedMorale;
    private int lastPresentedGold;
    private float lastInspectorEditTime;
    private string lastPresentedSelectedBoonsSummary = string.Empty;

    public CaravanResourceSnapshot GetConfiguredSnapshot()
    {
        return new CaravanResourceSnapshot(
            Mathf.Max(0, food),
            Mathf.Max(0, morale),
            Mathf.Max(0, gold));
    }

    public void InitializeRuntimeSnapshot(CaravanResourceSnapshot snapshot)
    {
        ApplySnapshot(snapshot);
        RefreshSelectedBoonsSummary();
    }

    private void Awake()
    {
        ResolvePlayerController();
    }

    private void OnEnable()
    {
        ResolvePlayerController();
    }

    private void OnValidate()
    {
        ResolvePlayerController();
        food = Mathf.Max(0, food);
        morale = Mathf.Max(0, morale);
        gold = Mathf.Max(0, gold);
        applyDelaySeconds = Mathf.Max(0.05f, applyDelaySeconds);
        RefreshSelectedBoonsSummary();

        if (Application.isPlaying && playerController != null && playerController.HasInitializedCaravanResources)
        {
            hasPendingInspectorApply = true;
            lastInspectorEditTime = Time.realtimeSinceStartup;
        }
    }

    private void LateUpdate()
    {
        if (!Application.isPlaying)
        {
            RefreshSelectedBoonsSummary();
            return;
        }

        ResolvePlayerController();
        if (playerController == null || !playerController.HasInitializedCaravanResources)
        {
            RefreshSelectedBoonsSummary();
            return;
        }

        RefreshSelectedBoonsSummary();

        CaravanResourceSnapshot currentSnapshot = playerController.GetCurrentResources();
        if (!hasPresentedValues)
        {
            ApplySnapshot(currentSnapshot);
            return;
        }

        if (hasPendingInspectorApply)
        {
            if (Time.realtimeSinceStartup - lastInspectorEditTime >= applyDelaySeconds)
            {
                hasPendingInspectorApply = false;
                playerController.OverrideCaravanResources(food, morale, gold);
                ApplySnapshot(playerController.GetCurrentResources());
            }

            return;
        }

        if (!SnapshotsMatch(currentSnapshot))
        {
            ApplySnapshot(currentSnapshot);
        }
    }

    private void ResolvePlayerController()
    {
        playerController ??= GetComponentInParent<PlayerController>();
    }

    private bool SnapshotsMatch(CaravanResourceSnapshot snapshot)
    {
        return snapshot.Food == lastPresentedFood
            && snapshot.Morale == lastPresentedMorale
            && snapshot.Gold == lastPresentedGold;
    }

    private void ApplySnapshot(CaravanResourceSnapshot snapshot)
    {
        food = Mathf.Max(0, snapshot.Food);
        morale = Mathf.Max(0, snapshot.Morale);
        gold = Mathf.Max(0, snapshot.Gold);
        lastPresentedFood = food;
        lastPresentedMorale = morale;
        lastPresentedGold = gold;
        hasPresentedValues = true;
    }

    private void RefreshSelectedBoonsSummary()
    {
        string nextSummary = BuildSelectedBoonsSummary();
        if (string.Equals(nextSummary, lastPresentedSelectedBoonsSummary, System.StringComparison.Ordinal))
        {
            return;
        }

        selectedBoonsSummary = nextSummary;
        lastPresentedSelectedBoonsSummary = nextSummary;
    }

    private static string BuildSelectedBoonsSummary()
    {
        System.Collections.Generic.IReadOnlyList<HexBoonDefinition> selectedBoons = HexBoonSelectionService.GetSelectedBoonDefinitions();
        int currentAct = HexActTransitionService.GetCurrentActNumber();
        HexNemesisArchetype lockedFamily = HexActTransitionService.GetLockedBoonFamily();

        System.Text.StringBuilder summary = new();
        summary.Append($"Act: {currentAct}");
        if (lockedFamily != HexNemesisArchetype.None)
        {
            summary.Append($"\nLocked Family: {lockedFamily}");
        }

        summary.Append("\nBoons:");
        if (selectedBoons == null || selectedBoons.Count == 0)
        {
            summary.Append("\n- None");
            return summary.ToString();
        }

        for (int index = 0; index < selectedBoons.Count; index++)
        {
            HexBoonDefinition boon = selectedBoons[index];
            if (boon == null)
            {
                continue;
            }

            boon.Validate();
            summary.Append($"\n- {boon.GetResolvedDisplayName()} [{boon.archetypeFamily}]");
        }

        return summary.ToString();
    }
}
