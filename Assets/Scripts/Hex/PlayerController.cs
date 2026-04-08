using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerController : MonoBehaviour
{
    public Text travelTimeText;
    public Text selectionStatusText;
    public Text tileDetailsText;
    public Text hintText;
    public Transform caravanVisual;
    [Min(0.1f)] public float caravanHeight = 0.65f;
    [Min(0.1f)] public float caravanScale = 0.45f;
    public Color caravanColor = new(0.95f, 0.76f, 0.29f, 1f);

    private HexTileInputService inputService;
    private HexPathHighlighter pathHighlighter;
    private HexTravelTimePresenter travelTimePresenter;
    private HexHudPresenter hudPresenter;
    private MapGenerator mapGenerator;

    private HexagonTile currentTile;
    private HexagonTile selectedTile;
    private IReadOnlyList<HexTileData> previewPath;
    private bool caravanSelectionActive;
    private bool isReady;

    private void Awake()
    {
        AutoAssignTextReferences();

        inputService = new HexTileInputService();
        pathHighlighter = new HexPathHighlighter();
        travelTimePresenter = new HexTravelTimePresenter(travelTimeText);
        hudPresenter = new HexHudPresenter(selectionStatusText, tileDetailsText, hintText);
        mapGenerator = FindAnyObjectByType<MapGenerator>();
        travelTimePresenter.Reset();
        hudPresenter.ResetTileDetails();
    }

    private void OnValidate()
    {
        AutoAssignTextReferences();
        caravanHeight = Mathf.Max(0.1f, caravanHeight);
        caravanScale = Mathf.Max(0.1f, caravanScale);
    }

    private IEnumerator Start()
    {
        while (mapGenerator == null || mapGenerator.GridData == null)
        {
            mapGenerator = FindAnyObjectByType<MapGenerator>();
            yield return null;
        }

        if (!TrySpawnCaravan())
        {
            yield break;
        }

        isReady = true;
        hudPresenter.ShowCaravanIdle(currentTile);
    }

    private void Update()
    {
        if (!isReady)
        {
            return;
        }

        if (inputService.TryGetClickedTile(Camera.main, Input.mousePosition, out HexagonTile tile))
        {
            HandleTileClick(tile);
        }
    }

    private void HandleTileClick(HexagonTile clickedTile)
    {
        if (clickedTile == null || !(clickedTile.TileData?.IsPassable ?? clickedTile.canTravelThrough))
        {
            return;
        }

        if (selectedTile == clickedTile)
        {
            HandleRepeatedSelection(clickedTile);
            return;
        }

        ClearHighlights();
        selectedTile = clickedTile;
        hudPresenter.ShowTileDetails(clickedTile);

        if (clickedTile == currentTile)
        {
            caravanSelectionActive = true;
            previewPath = null;
            travelTimePresenter.Reset();
            hudPresenter.ShowCaravanSelected(currentTile);
            RefreshHighlights();
            return;
        }

        if (!caravanSelectionActive)
        {
            previewPath = null;
            travelTimePresenter.Reset();
            hudPresenter.ShowInspectingTile(clickedTile);
            RefreshHighlights();
            return;
        }

        previewPath = mapGenerator.FindPath(currentTile.Coordinates, clickedTile.Coordinates);
        if (previewPath == null)
        {
            travelTimePresenter.Reset();
            hudPresenter.ShowUnreachableDestination(clickedTile);
        }
        else
        {
            travelTimePresenter.ShowPath(previewPath);
            hudPresenter.ShowDestinationPreview(clickedTile, previewPath);
        }

        RefreshHighlights();
    }

    private void HandleRepeatedSelection(HexagonTile clickedTile)
    {
        if (clickedTile == currentTile)
        {
            ClearSelection();
            return;
        }

        if (!caravanSelectionActive || previewPath == null)
        {
            hudPresenter.ShowInspectingTile(clickedTile);
            return;
        }

        CommitMove(clickedTile);
    }

    private void CommitMove(HexagonTile destinationTile)
    {
        int moveCost = HexPathMetrics.GetTravelCost(previewPath);
        ClearHighlights();

        currentTile.TileData?.SetOccupied(false);
        currentTile = destinationTile;
        currentTile.TileData?.SetOccupied(true);

        AttachCaravanToTile(currentTile);

        selectedTile = null;
        previewPath = null;
        caravanSelectionActive = false;

        travelTimePresenter.Reset();
        hudPresenter.ShowTileDetails(currentTile);
        hudPresenter.ShowMoveComplete(currentTile, moveCost);
        RefreshHighlights();
    }

    private void ClearSelection()
    {
        ClearHighlights();

        selectedTile = null;
        previewPath = null;
        caravanSelectionActive = false;

        travelTimePresenter.Reset();
        hudPresenter.ResetTileDetails();
        hudPresenter.ShowCaravanIdle(currentTile);
        RefreshHighlights();
    }

    private void RefreshHighlights()
    {
        pathHighlighter.Reset(currentTile, selectedTile, previewPath, mapGenerator);

        if (caravanSelectionActive && currentTile != null)
        {
            pathHighlighter.HighlightEndpoint(currentTile);
        }

        if (selectedTile != null && selectedTile != currentTile)
        {
            pathHighlighter.HighlightEndpoint(selectedTile);
        }

        if (caravanSelectionActive && previewPath != null && currentTile != null && selectedTile != null)
        {
            pathHighlighter.HighlightPath(previewPath, currentTile, selectedTile, mapGenerator);
        }
    }

    private void ClearHighlights()
    {
        pathHighlighter.Reset(currentTile, selectedTile, previewPath, mapGenerator);
    }

    private bool TrySpawnCaravan()
    {
        List<HexTileData> spawnCandidates = new();
        foreach (HexTileData tileData in mapGenerator.GridData.Tiles)
        {
            if (tileData.IsAvailable)
            {
                spawnCandidates.Add(tileData);
            }
        }

        if (spawnCandidates.Count == 0)
        {
            Debug.LogError("PlayerController could not find a passable tile to spawn the caravan.", this);
            return false;
        }

        HexTileData spawnTileData = spawnCandidates[Random.Range(0, spawnCandidates.Count)];
        if (!mapGenerator.TryGetTileView(spawnTileData.Coordinates, out currentTile))
        {
            Debug.LogError($"PlayerController could not resolve the tile view for caravan spawn at {spawnTileData.Coordinates}.", this);
            return false;
        }

        currentTile.TileData?.SetOccupied(true);
        EnsureCaravanVisual();
        AttachCaravanToTile(currentTile);
        hudPresenter.ShowTileDetails(currentTile);
        return true;
    }

    private void EnsureCaravanVisual()
    {
        if (caravanVisual == null)
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = "Caravan Placeholder";
            cube.transform.localScale = Vector3.one * caravanScale;

            Collider cubeCollider = cube.GetComponent<Collider>();
            if (cubeCollider != null)
            {
                Destroy(cubeCollider);
            }

            Renderer cubeRenderer = cube.GetComponent<Renderer>();
            if (cubeRenderer != null)
            {
                cubeRenderer.material.color = caravanColor;
            }

            caravanVisual = cube.transform;
        }

        caravanVisual.localScale = Vector3.one * caravanScale;

        if (caravanVisual.TryGetComponent<Collider>(out Collider collider))
        {
            collider.enabled = false;
        }
    }

    private void AttachCaravanToTile(HexagonTile tile)
    {
        if (caravanVisual == null || tile == null)
        {
            return;
        }

        caravanVisual.SetParent(tile.transform, false);
        caravanVisual.localPosition = new Vector3(0f, caravanHeight, 0f);
        caravanVisual.localRotation = Quaternion.identity;
        caravanVisual.localScale = Vector3.one * caravanScale;
    }

    private void AutoAssignTextReferences()
    {
        travelTimeText = FindTextReference(travelTimeText, "Travel Time Text");
        selectionStatusText = FindTextReference(selectionStatusText, "Selection Status Text");
        tileDetailsText = FindTextReference(tileDetailsText, "Tile Details Text");
        hintText = FindTextReference(hintText, "Hint Text");
    }

    private static Text FindTextReference(Text currentValue, string objectName)
    {
        if (currentValue != null)
        {
            return currentValue;
        }

        GameObject textObject = GameObject.Find(objectName);
        return textObject != null ? textObject.GetComponent<Text>() : null;
    }
}
