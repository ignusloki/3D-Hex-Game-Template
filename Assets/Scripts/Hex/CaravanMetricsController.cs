using UnityEngine;

public sealed class CaravanMetricsController : MonoBehaviour
{
    [SerializeField] private PlayerController playerController;
    [Min(0)] [SerializeField] private int food = 20;
    [Min(0)] [SerializeField] private int morale = 3;
    [Min(0)] [SerializeField] private int gold = 3;
    [Min(0.05f)] [SerializeField] private float applyDelaySeconds = 0.3f;

    private bool hasPresentedValues;
    private bool hasPendingInspectorApply;
    private int lastPresentedFood;
    private int lastPresentedMorale;
    private int lastPresentedGold;
    private float lastInspectorEditTime;

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
            return;
        }

        ResolvePlayerController();
        if (playerController == null || !playerController.HasInitializedCaravanResources)
        {
            return;
        }

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
}
