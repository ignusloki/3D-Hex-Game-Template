using UnityEngine;

public sealed class HexTileInputService
{
    public bool TryGetClickedTile(Camera camera, Vector3 pointerPosition, out HexagonTile tile)
    {
        tile = null;

        if (!Input.GetMouseButtonDown(0) || camera == null)
        {
            return false;
        }

        return TryGetTileUnderPointer(camera, pointerPosition, out tile);
    }

    public bool TryGetTileUnderPointer(Camera camera, Vector3 pointerPosition, out HexagonTile tile)
    {
        tile = null;

        if (camera == null)
        {
            return false;
        }

        if (!Physics.Raycast(camera.ScreenPointToRay(pointerPosition), out RaycastHit hit))
        {
            return false;
        }

        tile = hit.collider.GetComponent<HexagonTile>();
        return tile != null;
    }
}
