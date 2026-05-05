using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public partial class PlayerController
{
    private void ClearSelection()
    {
        ClearHighlights();

        selectedTile = null;
        previewPath = null;
        caravanSelectionActive = false;

        travelTimePresenter.Reset();
        RefreshTileDetails(currentTile);
        hudPresenter.ShowCaravanIdle(currentTile);
        RefreshHighlights();
    }

    private void RefreshHighlights()
    {
        pathHighlighter.Reset(currentTile, selectedTile, previewPath, mapGenerator);
        RefreshSelectionMarker();

        if (caravanSelectionActive && previewPath != null && currentTile != null && selectedTile != null)
        {
            pathHighlighter.HighlightPath(previewPath, currentTile, selectedTile, mapGenerator);
        }
    }

    private void ClearHighlights()
    {
        pathHighlighter.Reset(currentTile, selectedTile, previewPath, mapGenerator);
        selectionMarker?.Hide();
        HideCurrentTileSelectionInstance();
    }

    private void RefreshSelectionMarker()
    {
        bool shouldShowCurrentTileSelection = caravanSelectionActive && currentTile != null;
        if (shouldShowCurrentTileSelection)
        {
            ShowCurrentTileSelectionInstance(currentTile);
        }
        else
        {
            HideCurrentTileSelectionInstance();
        }

        if (selectedTile == null)
        {
            selectionMarker?.Hide();
            return;
        }

        if (currentTile != null && selectedTile == currentTile)
        {
            selectionMarker?.Hide();
            return;
        }

        selectionMarker ??= new HexSelectionMarker();
        selectionMarker.Show(
            selectedTile,
            ResolveSelectionMarkerColor(selectedTile),
            selectedHexMarkerRadius,
            selectedHexMarkerScale,
            selectedHexMarkerYOffset);
    }

    private void ShowCurrentTileSelectionInstance(HexagonTile tile)
    {
        if (tile == null)
        {
            HideCurrentTileSelectionInstance();
            return;
        }

        if (currentTileSelectionPrefab == null)
        {
            WarnMissingCurrentTileSelectionPrefabOnce();
            return;
        }

        if (currentTileSelectionInstance == null)
        {
            currentTileSelectionInstance = InstantiateCurrentTileSelectionPrefab();
            if (currentTileSelectionInstance == null)
            {
                return;
            }

            currentTileSelectionInstance.name = "Current Tile Selection Marker";
            DisableSelectionMarkerColliders(currentTileSelectionInstance);
        }

        Transform markerTransform = currentTileSelectionInstance.transform;
        markerTransform.SetParent(tile.transform, false);
        markerTransform.localPosition = new Vector3(0f, currentTileSelectionYOffset, 0f);
        markerTransform.localRotation = Quaternion.identity;
        markerTransform.localScale = Vector3.one * Mathf.Max(0.01f, currentTileSelectionScale);
        currentTileSelectionInstance.SetActive(true);
    }

    private GameObject InstantiateCurrentTileSelectionPrefab()
    {
#if UNITY_EDITOR
        GameObject editorPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CurrentTileSelectionPrefabPath);
        if (editorPrefab != null)
        {
            UnityEngine.Object editorInstance = PrefabUtility.InstantiatePrefab(editorPrefab);
            GameObject editorInstanceGameObject = ResolveInstantiatedSelectionGameObject(editorInstance);
            if (editorInstanceGameObject != null)
            {
                return editorInstanceGameObject;
            }
        }
#endif

        if (currentTileSelectionPrefab == null)
        {
            WarnMissingCurrentTileSelectionPrefabOnce();
            return null;
        }

        UnityEngine.Object instance = UnityEngine.Object.Instantiate((UnityEngine.Object)currentTileSelectionPrefab);
        GameObject instanceGameObject = ResolveInstantiatedSelectionGameObject(instance);
        if (instanceGameObject != null)
        {
            return instanceGameObject;
        }

        Debug.LogWarning(
            $"PlayerController could not instantiate '{CurrentTileSelectionPrefabPath}' as a GameObject. Falling back to no caravan tile selection marker.",
            this);

        if (instance != null)
        {
            Destroy(instance);
        }

        return null;
    }

    private static GameObject ResolveInstantiatedSelectionGameObject(UnityEngine.Object instance)
    {
        return instance switch
        {
            GameObject gameObject => gameObject,
            Component component => component.gameObject,
            _ => null
        };
    }

    private void HideCurrentTileSelectionInstance()
    {
        if (currentTileSelectionInstance != null)
        {
            currentTileSelectionInstance.SetActive(false);
        }
    }

    private void DestroyCurrentTileSelectionInstance()
    {
        if (currentTileSelectionInstance == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Destroy(currentTileSelectionInstance);
        }
        else
        {
            DestroyImmediate(currentTileSelectionInstance);
        }

        currentTileSelectionInstance = null;
    }

    private void WarnMissingCurrentTileSelectionPrefabOnce()
    {
        if (didWarnMissingCurrentTileSelectionPrefab)
        {
            return;
        }

        didWarnMissingCurrentTileSelectionPrefab = true;
        Debug.LogWarning(
            $"PlayerController cannot show the caravan tile selection marker because '{CurrentTileSelectionPrefabPath}' is not assigned.",
            this);
    }

    private static void DisableSelectionMarkerColliders(GameObject markerRoot)
    {
        if (markerRoot == null)
        {
            return;
        }

        foreach (Collider collider in markerRoot.GetComponentsInChildren<Collider>(true))
        {
            collider.enabled = false;
        }
    }

    private Color ResolveSelectionMarkerColor(HexagonTile tile)
    {
        if (!caravanSelectionActive || tile == null || currentTile == null)
        {
            return selectedHexColor;
        }

        return IsWithinImmediateMovementRange(tile)
            ? selectedHexColor
            : outOfRangeSelectionColor;
    }

}
