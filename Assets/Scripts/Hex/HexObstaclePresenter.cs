using System.Collections.Generic;
using UnityEngine;

public sealed class HexObstaclePresenter
{
    private readonly Dictionary<HexCoordinates, Transform> visuals = new();
    private static bool didWarnMissingVisualPrefab;

    public void Apply(
        MapGenerator mapGenerator,
        IReadOnlyDictionary<HexCoordinates, HexObstacleInstance> activeObstacles,
        IReadOnlyCollection<HexCoordinates> visibleCoordinates,
        GameObject obstacleVisualPrefab,
        float visualHeight)
    {
        if (mapGenerator == null)
        {
            Clear();
            return;
        }

        HashSet<HexCoordinates> desiredVisibleCoordinates = visibleCoordinates != null
            ? new HashSet<HexCoordinates>(visibleCoordinates)
            : new HashSet<HexCoordinates>();

        List<HexCoordinates> coordinatesToRemove = new();
        foreach ((HexCoordinates coordinates, Transform visual) in visuals)
        {
            if (!desiredVisibleCoordinates.Contains(coordinates)
                || activeObstacles == null
                || !activeObstacles.TryGetValue(coordinates, out HexObstacleInstance obstacle)
                || obstacle == null)
            {
                DestroyVisual(visual);
                coordinatesToRemove.Add(coordinates);
            }
        }

        for (int index = 0; index < coordinatesToRemove.Count; index++)
        {
            visuals.Remove(coordinatesToRemove[index]);
        }

        if (activeObstacles == null)
        {
            return;
        }

        foreach ((HexCoordinates coordinates, HexObstacleInstance obstacle) in activeObstacles)
        {
            if (!desiredVisibleCoordinates.Contains(coordinates)
                || obstacle == null
                || obstacle.Definition == null
                || !mapGenerator.TryGetTileView(coordinates, out HexagonTile tileView)
                || tileView == null)
            {
                continue;
            }

            if (!visuals.TryGetValue(coordinates, out Transform visual) || visual == null)
            {
                visual = CreateVisual(obstacle, tileView.transform, obstacleVisualPrefab, visualHeight);
                if (visual == null)
                {
                    continue;
                }

                visuals[coordinates] = visual;
            }

            ApplyVisualStyle(visual, obstacle, visualHeight);
        }
    }

    public void Clear()
    {
        foreach (Transform visual in visuals.Values)
        {
            DestroyVisual(visual);
        }

        visuals.Clear();
    }

    private static Transform CreateVisual(HexObstacleInstance obstacle, Transform parent, GameObject obstacleVisualPrefab, float visualHeight)
    {
        GameObject visualObject;
        if (obstacleVisualPrefab != null)
        {
            visualObject = Object.Instantiate(obstacleVisualPrefab, parent, false);
        }
        else if (HexDevelopmentContentGate.CanUseGeneratedPlaceholderVisuals())
        {
            visualObject = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        }
        else
        {
            WarnMissingVisualPrefabOnce();
            return null;
        }

        visualObject.name = $"Obstacle_{obstacle.Definition.displayName}_{obstacle.Coordinates}";
        visualObject.transform.SetParent(parent, false);

        foreach (Collider collider in visualObject.GetComponentsInChildren<Collider>(true))
        {
            collider.enabled = false;
        }

        foreach (Animator animator in visualObject.GetComponentsInChildren<Animator>(true))
        {
            animator.enabled = false;
        }

        ApplyVisualStyle(visualObject.transform, obstacle, visualHeight);
        return visualObject.transform;
    }

    private static void ApplyVisualStyle(Transform visual, HexObstacleInstance obstacle, float visualHeight)
    {
        if (visual == null || obstacle == null || obstacle.Definition == null)
        {
            return;
        }

        visual.localPosition = new Vector3(0f, visualHeight, 0f);
        visual.localRotation = Quaternion.identity;
        visual.localScale = Vector3.one * obstacle.Definition.visualScale;

        foreach (Renderer renderer in visual.GetComponentsInChildren<Renderer>(true))
        {
            renderer.material.color = obstacle.Definition.visualColor;
        }
    }

    private static void DestroyVisual(Transform visual)
    {
        if (visual == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Object.Destroy(visual.gameObject);
        }
        else
        {
            Object.DestroyImmediate(visual.gameObject);
        }
    }

    private static void WarnMissingVisualPrefabOnce()
    {
        if (didWarnMissingVisualPrefab)
        {
            return;
        }

        didWarnMissingVisualPrefab = true;
        Debug.LogWarning("HexObstaclePresenter skipped generated obstacle placeholder outside a development build. Assign a real obstacle visual prefab.");
    }
}
