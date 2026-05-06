using System;
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

        RaycastHit[] hits = Physics.RaycastAll(camera.ScreenPointToRay(pointerPosition));
        if (hits.Length == 0)
        {
            return false;
        }

        Array.Sort(hits, static (left, right) => left.distance.CompareTo(right.distance));
        for (int index = 0; index < hits.Length; index++)
        {
            if (TryResolveTile(hits[index].collider, out tile))
            {
                return true;
            }
        }

        return false;
    }

    private static bool TryResolveTile(Collider collider, out HexagonTile tile)
    {
        tile = null;
        if (collider == null)
        {
            return false;
        }

        tile = collider.GetComponent<HexagonTile>() ?? collider.GetComponentInParent<HexagonTile>();
        return tile != null;
    }
}
