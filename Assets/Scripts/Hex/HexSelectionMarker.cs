using UnityEngine;

public sealed class HexSelectionMarker
{
    private const string MarkerObjectName = "Selected Hex Marker";
    private const string GlowShaderResourcePath = "Shaders/HexSelectionGlow";

    private GameObject markerObject;
    private GameObject fillObject;
    private GameObject rimObject;
    private GameObject auraObject;
    private MeshRenderer fillRenderer;
    private MeshRenderer rimRenderer;
    private MeshRenderer auraRenderer;
    private MeshFilter fillMeshFilter;
    private MeshFilter rimMeshFilter;
    private MeshFilter auraMeshFilter;
    private Material fillMaterial;
    private Material rimMaterial;
    private Material auraMaterial;

    public void Show(HexagonTile tile, Color color, float radius, float scale, float yOffset)
    {
        if (tile == null)
        {
            Hide();
            return;
        }

        EnsureCreated(radius);
        if (markerObject == null)
        {
            return;
        }

        Color resolvedColor = color;
        resolvedColor.a = Mathf.Clamp01(resolvedColor.a);
        ApplyMaterialColor(resolvedColor);

        Transform markerTransform = markerObject.transform;
        markerTransform.SetParent(tile.transform, false);
        markerTransform.localPosition = new Vector3(0f, Mathf.Max(0f, yOffset), 0f);
        markerTransform.localRotation = Quaternion.identity;
        markerTransform.localScale = Vector3.one * Mathf.Max(0.01f, scale);
        markerObject.SetActive(true);
    }

    public void Hide()
    {
        if (markerObject != null)
        {
            markerObject.SetActive(false);
        }
    }

    public void Destroy()
    {
        if (markerObject == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Object.Destroy(markerObject);
        }
        else
        {
            Object.DestroyImmediate(markerObject);
        }

        markerObject = null;
        fillObject = null;
        rimObject = null;
        auraObject = null;
        fillRenderer = null;
        rimRenderer = null;
        auraRenderer = null;
        fillMeshFilter = null;
        rimMeshFilter = null;
        auraMeshFilter = null;
        fillMaterial = null;
        rimMaterial = null;
        auraMaterial = null;
    }

    private void EnsureCreated(float radius)
    {
        if (markerObject != null)
        {
            return;
        }

        markerObject = new GameObject(MarkerObjectName);
        markerObject.SetActive(false);
        float resolvedRadius = Mathf.Max(0.01f, radius);

        fillObject = CreateLayer("Selection Fill", markerObject.transform, 0.006f, out fillMeshFilter, out fillRenderer);
        fillMeshFilter.sharedMesh = CreateHexMesh(resolvedRadius);
        fillMaterial = CreateSelectionMaterial();
        fillRenderer.sharedMaterial = fillMaterial;
        ConfigureRenderer(fillRenderer);

        rimObject = CreateLayer("Selection Cyan Rim", markerObject.transform, 0.011f, out rimMeshFilter, out rimRenderer);
        rimMeshFilter.sharedMesh = CreateHexRingMesh(resolvedRadius * 0.94f, resolvedRadius * 1.12f);
        rimMaterial = CreateTransparentColorMaterial("SelectionRimMaterial", 3100);
        rimRenderer.sharedMaterial = rimMaterial;
        ConfigureRenderer(rimRenderer);

        auraObject = CreateLayer("Selection Outer Aura", markerObject.transform, 0.004f, out auraMeshFilter, out auraRenderer);
        auraMeshFilter.sharedMesh = CreateHexRingMesh(resolvedRadius * 1.02f, resolvedRadius * 1.22f);
        auraMaterial = CreateTransparentColorMaterial("SelectionAuraMaterial", 3090);
        auraRenderer.sharedMaterial = auraMaterial;
        ConfigureRenderer(auraRenderer);
    }

    private static GameObject CreateLayer(
        string name,
        Transform parent,
        float localYOffset,
        out MeshFilter meshFilter,
        out MeshRenderer meshRenderer)
    {
        GameObject layer = new(name);
        layer.transform.SetParent(parent, false);
        layer.transform.localPosition = new Vector3(0f, localYOffset, 0f);
        layer.transform.localRotation = Quaternion.identity;
        layer.transform.localScale = Vector3.one;
        meshFilter = layer.AddComponent<MeshFilter>();
        meshRenderer = layer.AddComponent<MeshRenderer>();
        return layer;
    }

    private static void ConfigureRenderer(MeshRenderer renderer)
    {
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
    }

    private static Mesh CreateHexMesh(float radius)
    {
        Vector3[] vertices = new Vector3[7];
        vertices[0] = Vector3.zero;
        for (int index = 0; index < 6; index++)
        {
            float angle = Mathf.Deg2Rad * ((60f * index) + 30f);
            vertices[index + 1] = new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
        }

        Vector2[] uvs = new Vector2[vertices.Length];
        for (int index = 0; index < vertices.Length; index++)
        {
            uvs[index] = new Vector2(
                (vertices[index].x / (radius * 2f)) + 0.5f,
                (vertices[index].z / (radius * 2f)) + 0.5f);
        }

        int[] triangles = new int[18];
        for (int index = 0; index < 6; index++)
        {
            int triangleIndex = index * 3;
            triangles[triangleIndex] = 0;
            triangles[triangleIndex + 1] = index == 5 ? 1 : index + 2;
            triangles[triangleIndex + 2] = index + 1;
        }

        Mesh mesh = new()
        {
            name = "SelectionHexMesh",
            vertices = vertices,
            uv = uvs,
            triangles = triangles
        };
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    private static Mesh CreateHexRingMesh(float innerRadius, float outerRadius)
    {
        const int HexSides = 6;
        Vector3[] vertices = new Vector3[HexSides * 2];
        Vector2[] uvs = new Vector2[vertices.Length];
        for (int index = 0; index < HexSides; index++)
        {
            float angle = Mathf.Deg2Rad * ((60f * index) + 30f);
            float x = Mathf.Cos(angle);
            float z = Mathf.Sin(angle);
            int outerIndex = index * 2;
            int innerIndex = outerIndex + 1;
            vertices[outerIndex] = new Vector3(x * outerRadius, 0f, z * outerRadius);
            vertices[innerIndex] = new Vector3(x * innerRadius, 0f, z * innerRadius);
            uvs[outerIndex] = new Vector2((x * 0.5f) + 0.5f, (z * 0.5f) + 0.5f);
            uvs[innerIndex] = uvs[outerIndex];
        }

        int[] triangles = new int[HexSides * 6];
        for (int index = 0; index < HexSides; index++)
        {
            int next = (index + 1) % HexSides;
            int outerCurrent = index * 2;
            int innerCurrent = outerCurrent + 1;
            int outerNext = next * 2;
            int innerNext = outerNext + 1;
            int triangleIndex = index * 6;

            triangles[triangleIndex] = outerCurrent;
            triangles[triangleIndex + 1] = outerNext;
            triangles[triangleIndex + 2] = innerNext;
            triangles[triangleIndex + 3] = outerCurrent;
            triangles[triangleIndex + 4] = innerNext;
            triangles[triangleIndex + 5] = innerCurrent;
        }

        Mesh mesh = new()
        {
            name = "SelectionHexRingMesh",
            vertices = vertices,
            uv = uvs,
            triangles = triangles
        };
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    private static Material CreateSelectionMaterial()
    {
        Shader shader = Resources.Load<Shader>(GlowShaderResourcePath)
            ?? Shader.Find("Greenroad/HexSelectionGlow")
            ?? Shader.Find("Universal Render Pipeline/Unlit")
            ?? Shader.Find("Unlit/Color")
            ?? Shader.Find("Sprites/Default")
            ?? Shader.Find("Standard");
        Material material = new(shader)
        {
            name = "SelectionMarkerMaterial",
            hideFlags = HideFlags.DontSave,
            renderQueue = 3000
        };

        material.SetOverrideTag("RenderType", "Transparent");
        SetFloatIfPresent(material, "_Surface", 1f);
        SetFloatIfPresent(material, "_Blend", 0f);
        SetFloatIfPresent(material, "_AlphaClip", 0f);
        SetFloatIfPresent(material, "_Cull", 0f);
        material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        material.SetInt("_ZWrite", 0);
        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.DisableKeyword("_ALPHATEST_ON");
        material.EnableKeyword("_ALPHABLEND_ON");
        material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        SetFloatIfPresent(material, "_CoreAlpha", 0.34f);
        SetFloatIfPresent(material, "_RimAlpha", 1f);
        SetFloatIfPresent(material, "_PulseSpeed", 2.2f);
        SetFloatIfPresent(material, "_ParticleAlpha", 0.8f);
        SetFloatIfPresent(material, "_ParticleScale", 7.5f);
        SetFloatIfPresent(material, "_ParticleSpeed", 0.75f);
        SetFloatIfPresent(material, "_ParticleDensity", 0.52f);
        SetFloatIfPresent(material, "_RimWidth", 0.2f);
        return material;
    }

    private static Material CreateTransparentColorMaterial(string materialName, int renderQueue)
    {
        Shader shader = Shader.Find("Sprites/Default")
            ?? Shader.Find("Unlit/Transparent")
            ?? Shader.Find("Unlit/Color")
            ?? Shader.Find("Standard");

        Material material = new(shader)
        {
            name = materialName,
            hideFlags = HideFlags.DontSave,
            renderQueue = renderQueue
        };

        if (material.HasProperty("_MainTex"))
        {
            material.SetTexture("_MainTex", Texture2D.whiteTexture);
        }

        material.SetOverrideTag("RenderType", "Transparent");
        SetFloatIfPresent(material, "_Surface", 1f);
        SetFloatIfPresent(material, "_Blend", 0f);
        SetFloatIfPresent(material, "_AlphaClip", 0f);
        SetFloatIfPresent(material, "_Cull", 0f);
        material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        material.SetInt("_ZWrite", 0);
        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.DisableKeyword("_ALPHATEST_ON");
        material.EnableKeyword("_ALPHABLEND_ON");
        material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        return material;
    }

    private static void SetFloatIfPresent(Material material, string propertyName, float value)
    {
        if (material != null && material.HasProperty(propertyName))
        {
            material.SetFloat(propertyName, value);
        }
    }

    private void ApplyMaterialColor(Color color)
    {
        if (fillMaterial == null)
        {
            return;
        }

        Color fillColor = new(
            Mathf.Lerp(color.r, 0.05f, 0.25f),
            Mathf.Lerp(color.g, 0.88f, 0.25f),
            1f,
            Mathf.Max(color.a, 0.56f));
        Color rimColor = Color.Lerp(fillColor, Color.white, 0.58f);
        rimColor.a = 0.92f;
        Color auraColor = Color.Lerp(fillColor, Color.white, 0.25f);
        auraColor.a = 0.34f;

        if (fillMaterial.HasProperty("_BaseColor"))
        {
            fillMaterial.SetColor("_BaseColor", fillColor);
        }

        if (fillMaterial.HasProperty("_Color"))
        {
            fillMaterial.SetColor("_Color", fillColor);
        }

        if (fillMaterial.HasProperty("_RimColor"))
        {
            fillMaterial.SetColor("_RimColor", rimColor);
        }

        if (fillMaterial.HasProperty("_ParticleColor"))
        {
            Color particleColor = Color.Lerp(fillColor, Color.white, 0.82f);
            particleColor.a = 1f;
            fillMaterial.SetColor("_ParticleColor", particleColor);
        }

        ApplySimpleMaterialColor(rimMaterial, rimColor);
        ApplySimpleMaterialColor(auraMaterial, auraColor);
    }

    private static void ApplySimpleMaterialColor(Material material, Color color)
    {
        if (material == null)
        {
            return;
        }

        if (material.HasProperty("_Color"))
        {
            material.SetColor("_Color", color);
        }

        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", color);
        }
    }
}
