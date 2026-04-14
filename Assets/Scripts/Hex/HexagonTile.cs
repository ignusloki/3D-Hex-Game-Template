/// Author: Mohammed Marzouq
/// Date: 19 Sep 2024
using UnityEngine;
using Pathing;
using System.Collections.Generic;
using System;
using UnityEngine.Rendering;

[RequireComponent(typeof(Renderer))]
public class HexagonTile : MonoBehaviour, IAStarNode {
    public int travelCost; // Cost to travel through this tile
    public bool canTravelThrough = true; // Whether the tile can be traversed

    // Materials used for different states of the tiles
    private Renderer tileRenderer;
    private Renderer[] baseTerrainRenderers;
    private Renderer[] runtimeTerrainRenderers = Array.Empty<Renderer>();
    private GameObject fogOverlayObject;
    private MeshRenderer fogOverlayRenderer;
    private Material fogOverlayMaterial;
    private HexFogKnowledgeState currentFogState = HexFogKnowledgeState.Visible;
    private HexFogOfWarSettings currentFogSettings;
    private bool hasSelectionHighlight;
    private Color selectionHighlightColor;
    private GameObject runtimeTerrainVisual;
    private readonly Dictionary<Renderer, Material[]> originalHighlightMaterials = new();

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
        baseTerrainRenderers = GetComponentsInChildren<Renderer>(true);
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
        ApplySelectionHighlight();
        ApplyHighlightToFogOverlay();
    }

    // Resets the tile's material to its original state
    public void ResetMaterial()
    {
        hasSelectionHighlight = false;
        RestoreRendererMaterials();
        RestoreFogOverlayState();
    }

    public void ApplyRuntimeBiomeVisual(GameObject biomePrefab, HexScriptableObject biomeProperties)
    {
        properties = biomeProperties;
        ApplyProperties();
        DestroyRuntimeTerrainVisual();

        if (biomePrefab != null)
        {
            runtimeTerrainVisual = Instantiate(biomePrefab, transform);
            runtimeTerrainVisual.name = $"RuntimeTerrain_{biomePrefab.name}";
            runtimeTerrainVisual.transform.localPosition = Vector3.zero;
            runtimeTerrainVisual.transform.localRotation = Quaternion.identity;
            runtimeTerrainVisual.transform.localScale = Vector3.one;

            foreach (HexagonTile extraTileComponent in runtimeTerrainVisual.GetComponentsInChildren<HexagonTile>(true))
            {
                if (extraTileComponent != null)
                {
                    if (Application.isPlaying)
                    {
                        Destroy(extraTileComponent);
                    }
                    else
                    {
                        DestroyImmediate(extraTileComponent);
                    }
                }
            }

            foreach (Collider collider in runtimeTerrainVisual.GetComponentsInChildren<Collider>(true))
            {
                collider.enabled = false;
            }

            runtimeTerrainRenderers = runtimeTerrainVisual.GetComponentsInChildren<Renderer>(true);
        }
        else
        {
            runtimeTerrainRenderers = Array.Empty<Renderer>();
        }

        RestoreRendererMaterials();
        ApplyFogState(currentFogState, currentFogSettings);

        if (hasSelectionHighlight)
        {
            ApplySelectionHighlight();
        }
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
        if (baseTerrainRenderers != null)
        {
            bool showBaseRenderers = enabled && runtimeTerrainVisual == null;
            for (int index = 0; index < baseTerrainRenderers.Length; index++)
            {
                if (baseTerrainRenderers[index] != null)
                {
                    baseTerrainRenderers[index].enabled = showBaseRenderers;
                }
            }
        }

        if (runtimeTerrainRenderers == null)
        {
            return;
        }

        bool showRuntimeRenderers = enabled && runtimeTerrainVisual != null;
        for (int index = 0; index < runtimeTerrainRenderers.Length; index++)
        {
            if (runtimeTerrainRenderers[index] != null)
            {
                runtimeTerrainRenderers[index].enabled = showRuntimeRenderers;
            }
        }
    }

    private void ApplySelectionHighlight()
    {
        RestoreRendererMaterials();

        foreach (Renderer renderer in GetHighlightRenderers())
        {
            if (renderer == null)
            {
                continue;
            }

            Material[] sourceMaterials = renderer.materials;
            originalHighlightMaterials[renderer] = sourceMaterials;
            Material[] highlightedMaterials = new Material[sourceMaterials.Length];

            for (int materialIndex = 0; materialIndex < sourceMaterials.Length; materialIndex++)
            {
                Material sourceMaterial = sourceMaterials[materialIndex];
                Material highlightedMaterial = sourceMaterial != null ? new Material(sourceMaterial) : null;
                if (highlightedMaterial != null)
                {
                    if (highlightedMaterial.HasProperty("_EmissionColor"))
                    {
                        highlightedMaterial.SetColor("_EmissionColor", selectionHighlightColor);
                    }

                    highlightedMaterial.EnableKeyword("_EMISSION");
                }

                highlightedMaterials[materialIndex] = highlightedMaterial;
            }

            renderer.materials = highlightedMaterials;
        }
    }

    private void RestoreRendererMaterials()
    {
        foreach ((Renderer renderer, Material[] materials) in originalHighlightMaterials)
        {
            if (renderer != null)
            {
                renderer.materials = materials;
            }
        }

        originalHighlightMaterials.Clear();
    }

    private Renderer[] GetHighlightRenderers()
    {
        return runtimeTerrainVisual != null && runtimeTerrainRenderers.Length > 0
            ? runtimeTerrainRenderers
            : baseTerrainRenderers ?? Array.Empty<Renderer>();
    }

    private void DestroyRuntimeTerrainVisual()
    {
        if (runtimeTerrainVisual == null)
        {
            runtimeTerrainRenderers = Array.Empty<Renderer>();
            return;
        }

        if (Application.isPlaying)
        {
            Destroy(runtimeTerrainVisual);
        }
        else
        {
            DestroyImmediate(runtimeTerrainVisual);
        }

        runtimeTerrainVisual = null;
        runtimeTerrainRenderers = Array.Empty<Renderer>();
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
