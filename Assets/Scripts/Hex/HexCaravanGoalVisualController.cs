using UnityEngine;

public sealed class HexCaravanGoalVisualController : MonoBehaviour
{
    private Transform caravanVisual;
    private Transform goalVisual;
    private float caravanHeight;
    private float caravanScale;
    private float goalHeight;
    private float goalScale;
    private Color caravanColor;
    private Color goalColor;

    public Transform CaravanVisual => caravanVisual;
    public Transform GoalVisual => goalVisual;

    public void Configure(
        Transform caravanVisual,
        Transform goalVisual,
        float caravanHeight,
        float caravanScale,
        float goalHeight,
        float goalScale,
        Color caravanColor,
        Color goalColor)
    {
        this.caravanVisual = caravanVisual != null ? caravanVisual : this.caravanVisual;
        this.goalVisual = goalVisual != null ? goalVisual : this.goalVisual;
        this.caravanHeight = Mathf.Max(0.1f, caravanHeight);
        this.caravanScale = Mathf.Max(0.1f, caravanScale);
        this.goalHeight = Mathf.Max(0.1f, goalHeight);
        this.goalScale = Mathf.Max(0.1f, goalScale);
        this.caravanColor = caravanColor;
        this.goalColor = goalColor;
    }

    public Transform EnsureCaravanVisual()
    {
        if (caravanVisual == null)
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = "Caravan Placeholder";
            cube.transform.localScale = Vector3.one * caravanScale;
            DisableCollider(cube);
            ApplyColor(cube, caravanColor);
            caravanVisual = cube.transform;
        }

        caravanVisual.localScale = Vector3.one * caravanScale;
        DisableCollider(caravanVisual.gameObject);
        return caravanVisual;
    }

    public Transform EnsureGoalVisual()
    {
        if (goalVisual == null)
        {
            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.name = "Goal Marker";
            sphere.transform.localScale = Vector3.one * goalScale;
            DisableCollider(sphere);
            ApplyColor(sphere, goalColor);
            goalVisual = sphere.transform;
        }

        goalVisual.localScale = Vector3.one * goalScale;
        DisableCollider(goalVisual.gameObject);
        return goalVisual;
    }

    public void AttachCaravanToTile(HexagonTile tile)
    {
        Transform visual = EnsureCaravanVisual();
        AttachToTile(visual, tile, caravanHeight, caravanScale);
    }

    public void AttachGoalToTile(HexagonTile tile)
    {
        Transform visual = EnsureGoalVisual();
        AttachToTile(visual, tile, goalHeight, goalScale);
    }

    private static void AttachToTile(Transform visual, HexagonTile tile, float height, float scale)
    {
        if (visual == null || tile == null)
        {
            return;
        }

        visual.SetParent(tile.transform, false);
        visual.localPosition = new Vector3(0f, height, 0f);
        visual.localRotation = Quaternion.identity;
        visual.localScale = Vector3.one * scale;
    }

    private static void DisableCollider(GameObject visual)
    {
        if (visual != null && visual.TryGetComponent(out Collider collider))
        {
            collider.enabled = false;
        }
    }

    private static void ApplyColor(GameObject visual, Color color)
    {
        if (visual != null && visual.TryGetComponent(out Renderer renderer))
        {
            renderer.material.color = color;
        }
    }
}
