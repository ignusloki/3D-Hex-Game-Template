/// Author: Mohammed Marzouq
/// Date: 19 Sep 2024
using UnityEngine;
using Pathing;
using System.Collections.Generic;
using UnityEngine.Rendering;

[RequireComponent(typeof(Renderer))]
public class HexagonTile : MonoBehaviour, IAStarNode {
    public int travelCost; // Cost to travel through this tile
    public bool canTravelThrough = true; // Whether the tile can be traversed

    // Materials used for different states of the tiles
    private Material originalMaterial; // Original material of the tile
    private Material highlightMaterial; // Material used for highlighting tiles
    private Renderer tileRenderer;
    private Renderer[] terrainRenderers;
    private GameObject fogOverlayObject;
    private MeshRenderer fogOverlayRenderer;
    private Material fogOverlayMaterial;
    private HexFogKnowledgeState currentFogState = HexFogKnowledgeState.Visible;
    private HexFogOfWarSettings currentFogSettings;
    private bool hasSelectionHighlight;
    private Color selectionHighlightColor;

    // List of neighboring tiles
    public List<HexagonTile> neighbors = new();

    // Provides a collection of neighboring tiles cast to IAStarNode
    public IEnumerable<IAStarNode> Neighbours => neighbors;
    [SerializeField] private HexScriptableObject properties;
    [SerializeField] private int row;
    [SerializeField] private int column;

    public HexCoordinates Coordinates => new(row, column);
    public HexTileData TileData { get; private set; }

    private void Awake() {
        tileRenderer = GetComponent<Renderer>();
        originalMaterial = tileRenderer.material; // Fetch and store the original material on creation
        terrainRenderers = GetComponentsInChildren<Renderer>(true);
        ApplyProperties();
    }

    private void OnValidate()
    {
        neighbors ??= new List<HexagonTile>();

        if (properties != null)
        {
            travelCost = properties.travelCost;
            canTravelThrough = properties.passable;
        }
    }

    public void Initialize(HexTileData tileData)
    {
        TileData = tileData;
        properties = tileData?.Properties;
        row = tileData?.Coordinates.Row ?? 0;
        column = tileData?.Coordinates.Column ?? 0;
        ApplyProperties();
    }

    public void ClearNeighbors()
    {
        neighbors.Clear();
    }

    private void ApplyProperties()
    {
        if (properties == null)
        {
            travelCost = TileData?.TravelCost ?? 0;
            canTravelThrough = TileData?.IsPassable ?? true;
            return;
        }

        travelCost = TileData?.TravelCost ?? properties.travelCost;
        canTravelThrough = TileData?.IsPassable ?? properties.passable;
    }

    // Method to highlight the road on the path
    public void HighlightedRoad()
    {
        SetTileMaterial(new Color(1f, 0.25f, 0f, 1f));
    }


    // Mark this tile as start or end
    public void SelectAsStartOrEnd()
    {
        SetTileMaterial(new Color(0f, 1f, 0f, 1f));
    }

    public void HighlightSelection(Color color)
    {
        SetTileMaterial(color);
    }

    // Helper method to set material properties based on the provided color
    private void SetTileMaterial(Color color)
    {
        hasSelectionHighlight = true;
        selectionHighlightColor = color;
        highlightMaterial = Instantiate(originalMaterial);
        highlightMaterial.SetColor("_EmissionColor", color);
        highlightMaterial.EnableKeyword("_EMISSION");
        tileRenderer.material = highlightMaterial;
        ApplyHighlightToFogOverlay();
    }

    // Resets the tile's material to its original state
    public void ResetMaterial()
    {
        hasSelectionHighlight = false;
        tileRenderer.material = originalMaterial;
        RestoreFogOverlayState();
    }

    public void ApplyFogState(HexFogKnowledgeState fogState, HexFogOfWarSettings settings)
    {
        currentFogState = fogState;
        currentFogSettings = settings;

        if (settings == null)
        {
            SetTerrainRenderersEnabled(true);
            SetFogOverlayVisible(false, settings, 0f);
            return;
        }

        EnsureFogOverlay(settings);

        switch (fogState)
        {
            case HexFogKnowledgeState.Visible:
                SetTerrainRenderersEnabled(true);
                SetFogOverlayVisible(false, settings, 0f);
                break;

            case HexFogKnowledgeState.Remembered:
                SetTerrainRenderersEnabled(true);
                SetFogOverlayVisible(true, settings, settings.rememberedOverlayAlpha);
                break;

            default:
                SetTerrainRenderersEnabled(false);
                SetFogOverlayVisible(true, settings, settings.unseenOverlayAlpha);
                break;
        }

        if (hasSelectionHighlight)
        {
            ApplyHighlightToFogOverlay();
        }
    }

    public float EstimatedCostTo(IAStarNode other)
    {
        return Coordinates.DistanceTo(((HexagonTile)other).Coordinates);
    }

    public float CostTo(IAStarNode next)
    {
        HexagonTile nextTile = (HexagonTile)next;
        return nextTile.TileData?.TravelCost ?? nextTile.travelCost;
    }

    private void EnsureFogOverlay(HexFogOfWarSettings settings)
    {
        if (fogOverlayRenderer != null)
        {
            UpdateFogOverlayTransform(settings);
            return;
        }

        MeshFilter sourceMeshFilter = GetComponent<MeshFilter>();
        if (sourceMeshFilter == null || sourceMeshFilter.sharedMesh == null)
        {
            return;
        }

        fogOverlayObject = new GameObject("Fog Overlay");
        fogOverlayObject.transform.SetParent(transform, false);
        UpdateFogOverlayTransform(settings);
        fogOverlayObject.layer = gameObject.layer;

        MeshFilter overlayMeshFilter = fogOverlayObject.AddComponent<MeshFilter>();
        overlayMeshFilter.sharedMesh = sourceMeshFilter.sharedMesh;

        fogOverlayRenderer = fogOverlayObject.AddComponent<MeshRenderer>();
        fogOverlayRenderer.shadowCastingMode = ShadowCastingMode.Off;
        fogOverlayRenderer.receiveShadows = false;

        fogOverlayMaterial = CreateFogOverlayMaterial();
        fogOverlayRenderer.sharedMaterial = fogOverlayMaterial;
        fogOverlayObject.SetActive(false);
    }

    private void UpdateFogOverlayTransform(HexFogOfWarSettings settings)
    {
        if (fogOverlayObject == null || settings == null)
        {
            return;
        }

        fogOverlayObject.transform.localPosition = new Vector3(0f, settings.overlayHeight, 0f);
        fogOverlayObject.transform.localRotation = Quaternion.identity;
        fogOverlayObject.transform.localScale = Vector3.one * settings.overlayScale;
    }

    private void SetTerrainRenderersEnabled(bool enabled)
    {
        if (terrainRenderers == null)
        {
            return;
        }

        for (int index = 0; index < terrainRenderers.Length; index++)
        {
            if (terrainRenderers[index] != null)
            {
                terrainRenderers[index].enabled = enabled;
            }
        }
    }

    private void SetFogOverlayVisible(bool isVisible, HexFogOfWarSettings settings, float alpha)
    {
        if (fogOverlayRenderer == null || fogOverlayObject == null || settings == null)
        {
            return;
        }

        Color fogColor = settings.fogColor;
        fogColor.a = alpha;

        if (fogOverlayMaterial != null)
        {
            if (fogOverlayMaterial.HasProperty("_Color"))
            {
                fogOverlayMaterial.SetColor("_Color", fogColor);
            }

            if (fogOverlayMaterial.HasProperty("_BaseColor"))
            {
                fogOverlayMaterial.SetColor("_BaseColor", fogColor);
            }
        }

        fogOverlayObject.SetActive(isVisible);
    }

    private void RestoreFogOverlayState()
    {
        if (currentFogSettings == null)
        {
            return;
        }

        switch (currentFogState)
        {
            case HexFogKnowledgeState.Visible:
                SetFogOverlayVisible(false, currentFogSettings, 0f);
                break;

            case HexFogKnowledgeState.Remembered:
                SetFogOverlayVisible(true, currentFogSettings, currentFogSettings.rememberedOverlayAlpha);
                break;

            default:
                SetFogOverlayVisible(true, currentFogSettings, currentFogSettings.unseenOverlayAlpha);
                break;
        }
    }

    private void ApplyHighlightToFogOverlay()
    {
        if (!hasSelectionHighlight || fogOverlayRenderer == null || fogOverlayObject == null || currentFogSettings == null)
        {
            return;
        }

        if (currentFogState == HexFogKnowledgeState.Visible)
        {
            SetFogOverlayVisible(false, currentFogSettings, 0f);
            return;
        }

        Color highlightOverlayColor = selectionHighlightColor;
        highlightOverlayColor.a = currentFogState == HexFogKnowledgeState.Remembered ? 0.55f : 0.82f;

        if (fogOverlayMaterial.HasProperty("_Color"))
        {
            fogOverlayMaterial.SetColor("_Color", highlightOverlayColor);
        }

        if (fogOverlayMaterial.HasProperty("_BaseColor"))
        {
            fogOverlayMaterial.SetColor("_BaseColor", highlightOverlayColor);
        }

        fogOverlayObject.SetActive(true);
    }

    private static Material CreateFogOverlayMaterial()
    {
        Shader shader = Shader.Find("Standard");
        if (shader == null)
        {
            shader = Shader.Find("Universal Render Pipeline/Lit");
        }

        if (shader == null)
        {
            shader = Shader.Find("Unlit/Color");
        }

        Material material = new(shader)
        {
            name = "Hex Fog Overlay"
        };

        if (material.shader != null && material.shader.name == "Standard")
        {
            material.SetFloat("_Mode", 3f);
            material.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            material.SetInt("_ZWrite", 0);
            material.DisableKeyword("_ALPHATEST_ON");
            material.EnableKeyword("_ALPHABLEND_ON");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.renderQueue = 3000;
        }

        material.color = Color.black;
        return material;
    }
}
