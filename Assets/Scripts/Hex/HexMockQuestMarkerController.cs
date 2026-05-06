using System.Collections.Generic;
using UnityEngine;

public sealed class HexMockQuestMarkerController : MonoBehaviour
{
    [Header("Mock Content")]
    [SerializeField] private string fallbackTitle = "Quest Marker";
    [TextArea(3, 6)] [SerializeField] private string placeholderBody =
        "This is a mock quest marker.\n\nReplace this placeholder with a real quest flow later.";

    private readonly Dictionary<HexCoordinates, HexMapObjectPlacement> questMarkersByCoordinate = new();
    private readonly HashSet<HexCoordinates> resolvedQuestMarkers = new();

    private MapGenerator mapGenerator;
    private HexMockQuestMarkerModalPresenter modalPresenter;
    private HexCoordinates? activeQuestMarker;

    public bool IsModalOpen => modalPresenter != null && modalPresenter.IsOpen;

    public void Initialize(MapGenerator generator)
    {
        mapGenerator = generator;
        modalPresenter ??= GetComponent<HexMockQuestMarkerModalPresenter>() ?? gameObject.AddComponent<HexMockQuestMarkerModalPresenter>();
        RebuildQuestMarkers();
    }

    public bool TryProcessArrival(HexCoordinates coordinates)
    {
        if (IsModalOpen
            || resolvedQuestMarkers.Contains(coordinates)
            || !questMarkersByCoordinate.TryGetValue(coordinates, out HexMapObjectPlacement placement))
        {
            return false;
        }

        activeQuestMarker = coordinates;
        modalPresenter.Show(
            ResolveTitle(placement),
            ResolveBody(placement),
            HandleDialogClosed);
        return true;
    }

    public void HideActiveModal()
    {
        activeQuestMarker = null;
        modalPresenter?.Hide();
    }

    private void HandleDialogClosed()
    {
        if (activeQuestMarker.HasValue)
        {
            resolvedQuestMarkers.Add(activeQuestMarker.Value);
        }

        activeQuestMarker = null;
    }

    private void RebuildQuestMarkers()
    {
        questMarkersByCoordinate.Clear();
        if (mapGenerator?.MapObjectPlacements == null)
        {
            return;
        }

        List<HexMapObjectPlacement> questMarkers = mapGenerator.MapObjectPlacements.GetByType(HexMapObjectType.QuestMarker);
        for (int index = 0; index < questMarkers.Count; index++)
        {
            HexMapObjectPlacement placement = questMarkers[index];
            questMarkersByCoordinate[placement.Coordinates] = placement;
        }
    }

    private string ResolveTitle(HexMapObjectPlacement placement)
    {
        return string.IsNullOrWhiteSpace(placement.DisplayName) ? fallbackTitle : placement.DisplayName;
    }

    private string ResolveBody(HexMapObjectPlacement placement)
    {
        string baseBody = string.IsNullOrWhiteSpace(placeholderBody)
            ? "This is a mock quest marker placeholder."
            : placeholderBody.Trim();

        return $"{baseBody}\n\nLocation: {placement.Coordinates}";
    }
}
