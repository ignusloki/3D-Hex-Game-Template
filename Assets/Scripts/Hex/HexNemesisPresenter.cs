using System.Collections.Generic;
using UnityEngine;

public sealed class HexNemesisPresenter
{
    private readonly Dictionary<HexCoordinates, Transform> corruptionVisuals = new();
    private Transform actorVisual;
    private HexCoordinates? actorVisualCoordinates;
    private GameObject actorVisualSourcePrefab;

    public void Apply(
        MapGenerator mapGenerator,
        HexNemesisArchetype archetype,
        HexNemesisArchetypeProfile profile,
        HexCoordinates? actorCoordinates,
        IReadOnlyCollection<HexCoordinates> corruptedHexes)
    {
        if (mapGenerator == null || profile == null || archetype == HexNemesisArchetype.None)
        {
            Clear();
            return;
        }

        ApplyActor(mapGenerator, archetype, profile, actorCoordinates);
        ApplyCorruption(mapGenerator, profile, corruptedHexes);
    }

    public void Clear()
    {
        if (actorVisual != null)
        {
            DestroyVisual(actorVisual);
            actorVisual = null;
            actorVisualCoordinates = null;
            actorVisualSourcePrefab = null;
        }

        foreach (Transform visual in corruptionVisuals.Values)
        {
            DestroyVisual(visual);
        }

        corruptionVisuals.Clear();
    }

    private void ApplyActor(MapGenerator mapGenerator, HexNemesisArchetype archetype, HexNemesisArchetypeProfile profile, HexCoordinates? actorCoordinates)
    {
        if (!actorCoordinates.HasValue || !mapGenerator.TryGetTileView(actorCoordinates.Value, out HexagonTile tileView) || tileView == null)
        {
            if (actorVisual != null)
            {
                DestroyVisual(actorVisual);
                actorVisual = null;
                actorVisualCoordinates = null;
                actorVisualSourcePrefab = null;
            }

            return;
        }

        if (actorVisual == null
            || !actorVisualCoordinates.HasValue
            || !actorVisualCoordinates.Value.Equals(actorCoordinates.Value)
            || actorVisualSourcePrefab != profile.actorPrefab)
        {
            if (actorVisual != null)
            {
                DestroyVisual(actorVisual);
            }

            actorVisual = CreateActorVisual(archetype, profile, tileView.transform);
            actorVisualCoordinates = actorCoordinates;
            actorVisualSourcePrefab = profile.actorPrefab;
        }

        ApplyActorStyle(actorVisual, profile);
    }

    private void ApplyCorruption(MapGenerator mapGenerator, HexNemesisArchetypeProfile profile, IReadOnlyCollection<HexCoordinates> corruptedHexes)
    {
        HashSet<HexCoordinates> desired = corruptedHexes != null ? new HashSet<HexCoordinates>(corruptedHexes) : new HashSet<HexCoordinates>();
        List<HexCoordinates> toRemove = new();
        foreach ((HexCoordinates coordinates, Transform visual) in corruptionVisuals)
        {
            if (!desired.Contains(coordinates) || !mapGenerator.TryGetTileView(coordinates, out _) || visual == null)
            {
                DestroyVisual(visual);
                toRemove.Add(coordinates);
            }
        }

        for (int index = 0; index < toRemove.Count; index++)
        {
            corruptionVisuals.Remove(toRemove[index]);
        }

        foreach (HexCoordinates coordinates in desired)
        {
            if (!mapGenerator.TryGetTileView(coordinates, out HexagonTile tileView) || tileView == null)
            {
                continue;
            }

            if (!corruptionVisuals.TryGetValue(coordinates, out Transform visual) || visual == null)
            {
                visual = CreateCorruptionVisual(tileView.transform);
                corruptionVisuals[coordinates] = visual;
            }

            ApplyCorruptionStyle(visual, profile);
        }
    }

    private static Transform CreateActorVisual(HexNemesisArchetype archetype, HexNemesisArchetypeProfile profile, Transform parent)
    {
        GameObject visualObject;
        if (profile != null && profile.actorPrefab != null)
        {
            visualObject = Object.Instantiate(profile.actorPrefab, parent, false);
        }
        else
        {
            PrimitiveType primitiveType = archetype switch
            {
                HexNemesisArchetype.Hunter => PrimitiveType.Capsule,
                HexNemesisArchetype.Echo => PrimitiveType.Cube,
                HexNemesisArchetype.Corruptor => PrimitiveType.Sphere,
                _ => PrimitiveType.Sphere
            };

            visualObject = GameObject.CreatePrimitive(primitiveType);
            visualObject.transform.SetParent(parent, false);
        }

        visualObject.name = $"Nemesis_{archetype}";
        foreach (Collider collider in visualObject.GetComponentsInChildren<Collider>(true))
        {
            collider.enabled = false;
        }

        return visualObject.transform;
    }

    private static void ApplyActorStyle(Transform visual, HexNemesisArchetypeProfile profile)
    {
        if (visual == null || profile == null)
        {
            return;
        }

        visual.localPosition = new Vector3(0f, profile.actorHeight, 0f);
        visual.localRotation = Quaternion.identity;
        visual.localScale = Vector3.one * profile.actorScale;

        foreach (Renderer renderer in visual.GetComponentsInChildren<Renderer>(true))
        {
            renderer.material.color = profile.actorColor;
        }
    }

    private static Transform CreateCorruptionVisual(Transform parent)
    {
        GameObject visualObject = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        visualObject.name = "Corruption Marker";
        visualObject.transform.SetParent(parent, false);
        foreach (Collider collider in visualObject.GetComponentsInChildren<Collider>(true))
        {
            collider.enabled = false;
        }

        return visualObject.transform;
    }

    private static void ApplyCorruptionStyle(Transform visual, HexNemesisArchetypeProfile profile)
    {
        if (visual == null || profile == null)
        {
            return;
        }

        visual.localPosition = new Vector3(0f, profile.corruptionHeight, 0f);
        visual.localRotation = Quaternion.identity;
        visual.localScale = new Vector3(profile.corruptionScale, 0.025f, profile.corruptionScale);

        foreach (Renderer renderer in visual.GetComponentsInChildren<Renderer>(true))
        {
            renderer.material.color = profile.corruptionColor;
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
}
