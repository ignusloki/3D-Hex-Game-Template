/// Author: Mohammed Marzouq
/// Date: 19 Sep 2024
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;

public class TileSelector : MonoBehaviour
{
    // Public reference to the UI Text component to display travel time
    public Text travelTimeText;
    public Text selectionStatusText;
    public Text tileDetailsText;
    public Text hintText;

    private HexTileInputService inputService;
    private HexPathSelectionState selectionState;
    private HexPathHighlighter pathHighlighter;
    private HexTravelTimePresenter travelTimePresenter;
    private HexHudPresenter hudPresenter;
    private MapGenerator mapGenerator;

    private void Awake()
    {
        AutoAssignTextReferences();

        inputService = new HexTileInputService();
        selectionState = new HexPathSelectionState();
        pathHighlighter = new HexPathHighlighter();
        travelTimePresenter = new HexTravelTimePresenter(travelTimeText);
        hudPresenter = new HexHudPresenter(selectionStatusText, tileDetailsText, hintText);
        mapGenerator = FindAnyObjectByType<MapGenerator>();
        travelTimePresenter.Reset();
        hudPresenter.ShowAwaitingStart();
        hudPresenter.ResetTileDetails();
    }

    private void OnValidate()
    {
        AutoAssignTextReferences();
    }

    // Update is called once per frame
    void Update()
    {
        UpdateHoveredTileDetails();

        if (inputService.TryGetClickedTile(Camera.main, Input.mousePosition, out HexagonTile tile)
            && (tile.TileData?.IsPassable ?? tile.canTravelThrough))
        {
            HandleTileSelection(tile);
        }
    }

    // Handles the tile selection logic for both start and end tiles
    private void HandleTileSelection(HexagonTile tile)
    {
        if (selectionState.IsSelectingStart)
        {
            ResetHighlightedTiles();
            selectionState.BeginSelection(tile);
            pathHighlighter.HighlightEndpoint(tile);
            hudPresenter.ShowAwaitingDestination(tile);
        }
        else
        {
            pathHighlighter.Reset(null, selectionState.EndTile, null, mapGenerator);
            selectionState.CompleteSelection(tile, mapGenerator);
            pathHighlighter.HighlightEndpoint(tile);
            FindAndHighlightPath();
        }
    }

    // Finds and highlights the path between selected tiles
    public void FindAndHighlightPath()
    {
        if (mapGenerator == null || selectionState.StartTile == null || selectionState.EndTile == null)
        {
            Debug.Log("No path found or one of the required references is null.");
            return;
        }

        IReadOnlyList<HexTileData> path = selectionState.Path ?? selectionState.CompleteSelection(selectionState.EndTile, mapGenerator);
        if (path != null)
        {
            pathHighlighter.HighlightPath(path, selectionState.StartTile, selectionState.EndTile, mapGenerator);
            travelTimePresenter.ShowPath(path);
            hudPresenter.ShowPathPreview(path);
        }
        else
        {
            Debug.Log("No path found or one of the tiles is null.");
            travelTimePresenter.Reset();
            hudPresenter.ShowNoPath();
        }
    }

    // Resets the highlighted tiles and clears the path
    public void ResetHighlightedTiles()
    {
        pathHighlighter.Reset(selectionState.StartTile, selectionState.EndTile, selectionState.Path, mapGenerator);
        selectionState.Reset();
        travelTimePresenter.Reset();
        hudPresenter.ShowAwaitingStart();
    }

    private void UpdateHoveredTileDetails()
    {
        if (hudPresenter == null)
        {
            return;
        }

        if (inputService.TryGetTileUnderPointer(Camera.main, Input.mousePosition, out HexagonTile tile))
        {
            hudPresenter.ShowTileDetails(tile);
            return;
        }

        hudPresenter.ResetTileDetails();
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
