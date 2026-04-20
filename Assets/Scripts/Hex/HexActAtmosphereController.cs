using UnityEngine;
using UnityEngine.Rendering;

[ExecuteAlways]
[AddComponentMenu("Hex/Visual/Act Atmosphere Controller")]
[DisallowMultipleComponent]
public sealed class HexActAtmosphereController : MonoBehaviour
{
    [SerializeField] private bool applyActAtmosphere = true;
    [SerializeField] private bool previewAtmosphereInEditor;
    [Range(1, 3)] [SerializeField] private int editorPreviewAct = 1;
    [SerializeField] private HexActAtmosphereConfigAsset atmosphereConfig;

    private Camera cachedMainCamera;
    private Light cachedDirectionalLight;
    private int lastAppliedAct = -1;
    private HexActAtmosphereConfigAsset lastAppliedConfig;
    private int lastAppliedConfigRevision = -1;
    private bool originalRenderSettingsCaptured;
    private bool originalCameraStateCaptured;
    private bool originalDirectionalLightStateCaptured;
    private bool originalDirectionalLightCookieCaptured;
    private bool originalFog;
    private FogMode originalFogMode;
    private Color originalFogColor;
    private float originalFogDensity;
    private float originalFogStartDistance;
    private float originalFogEndDistance;
    private AmbientMode originalAmbientMode;
    private Color originalAmbientSkyColor;
    private Color originalAmbientEquatorColor;
    private Color originalAmbientGroundColor;
    private float originalAmbientIntensity;
    private Material originalSkyboxMaterial;
    private CameraClearFlags originalCameraClearFlags;
    private Color originalCameraBackgroundColor;
    private Color originalDirectionalLightColor;
    private float originalDirectionalLightIntensity;
    private LightShadows originalDirectionalLightShadows;
    private float originalDirectionalLightShadowStrength;
    private float originalDirectionalLightShadowBias;
    private float originalDirectionalLightShadowNormalBias;
    private Quaternion originalDirectionalLightRotation;
    private Texture originalDirectionalLightCookie;
    private Vector2 originalDirectionalLightCookieSize2D;
    private Texture2D generatedCloudShadowCookie;
    private Color[] generatedCloudShadowPixels;
    private HexCloudShadowSettings activeCloudShadowSettings;
    private Vector2 cloudShadowOffset;
    private float cloudShadowRefreshTimer;

    private void Awake()
    {
        ApplyAtmosphereForResolvedAct(true);
    }

    private void OnEnable()
    {
        ApplyAtmosphereForResolvedAct(true);
    }

    private void OnDisable()
    {
        RestoreOriginalSceneState();
    }

    private void OnDestroy()
    {
        RestoreOriginalSceneState();
        ReleaseGeneratedCloudShadowCookie();
    }

    private void LateUpdate()
    {
        if (Application.isPlaying)
        {
            if (!applyActAtmosphere)
            {
                return;
            }

            HexActAtmosphereConfigAsset runtimeConfig = GetResolvedConfig();
            int currentAct = Mathf.Clamp(HexActTransitionService.GetCurrentActNumber(), 1, 3);
            if (ShouldApplyAtmosphere(currentAct, runtimeConfig))
            {
                ApplyAtmosphere(currentAct, runtimeConfig);
            }

            UpdateCloudShadows(Time.deltaTime);
            return;
        }

        if (!applyActAtmosphere || !previewAtmosphereInEditor)
        {
            RestoreOriginalSceneState();
            return;
        }

        HexActAtmosphereConfigAsset previewConfig = GetResolvedConfig();
        if (ShouldApplyAtmosphere(editorPreviewAct, previewConfig))
        {
            ApplyAtmosphere(editorPreviewAct, previewConfig);
        }
    }

    private void OnValidate()
    {
        editorPreviewAct = Mathf.Clamp(editorPreviewAct, 1, 3);
        atmosphereConfig?.Validate();

        if (Application.isPlaying)
        {
            return;
        }

        if (applyActAtmosphere && previewAtmosphereInEditor)
        {
            ApplyAtmosphere(editorPreviewAct, GetResolvedConfig());
        }
        else
        {
            RestoreOriginalSceneState();
        }
    }

    private void ApplyAtmosphereForResolvedAct(bool allowEditorPreview)
    {
        if (!applyActAtmosphere)
        {
            RestoreOriginalSceneState();
            return;
        }

        HexActAtmosphereConfigAsset config = GetResolvedConfig();
        if (Application.isPlaying)
        {
            ApplyAtmosphere(Mathf.Clamp(HexActTransitionService.GetCurrentActNumber(), 1, 3), config);
            return;
        }

        if (allowEditorPreview && previewAtmosphereInEditor)
        {
            ApplyAtmosphere(editorPreviewAct, config);
        }
    }

    private bool ShouldApplyAtmosphere(int actNumber, HexActAtmosphereConfigAsset config)
    {
        int resolvedAct = Mathf.Clamp(actNumber, 1, 3);
        int configRevision = config != null ? config.EditorRevision : -1;
        return resolvedAct != lastAppliedAct
            || lastAppliedConfig != config
            || configRevision != lastAppliedConfigRevision;
    }

    private HexActAtmosphereConfigAsset GetResolvedConfig()
    {
        atmosphereConfig?.Validate();
        return atmosphereConfig;
    }

    private void ApplyAtmosphere(int actNumber, HexActAtmosphereConfigAsset config)
    {
        if (config == null)
        {
            RestoreOriginalSceneState();
            return;
        }

        HexActAtmosphereSettings settings = config.GetAtmosphereForAct(actNumber);
        if (settings == null)
        {
            RestoreOriginalSceneState();
            return;
        }

        CaptureOriginalSceneState();
        settings.Validate();
        ApplyRenderSettings(settings);
        ApplyDirectionalLight(settings.directionalLight);
        ApplyCloudShadows(settings.cloudShadows);
        ApplyCameraSettings(settings);
        lastAppliedAct = Mathf.Clamp(actNumber, 1, 3);
        lastAppliedConfig = config;
        lastAppliedConfigRevision = config.EditorRevision;
    }

    private void CaptureOriginalSceneState()
    {
        if (!originalRenderSettingsCaptured)
        {
            originalFog = RenderSettings.fog;
            originalFogMode = RenderSettings.fogMode;
            originalFogColor = RenderSettings.fogColor;
            originalFogDensity = RenderSettings.fogDensity;
            originalFogStartDistance = RenderSettings.fogStartDistance;
            originalFogEndDistance = RenderSettings.fogEndDistance;
            originalAmbientMode = RenderSettings.ambientMode;
            originalAmbientSkyColor = RenderSettings.ambientSkyColor;
            originalAmbientEquatorColor = RenderSettings.ambientEquatorColor;
            originalAmbientGroundColor = RenderSettings.ambientGroundColor;
            originalAmbientIntensity = RenderSettings.ambientIntensity;
            originalSkyboxMaterial = RenderSettings.skybox;
            originalRenderSettingsCaptured = true;
        }

        Camera mainCamera = ResolveMainCamera();
        if (!originalCameraStateCaptured && mainCamera != null)
        {
            originalCameraClearFlags = mainCamera.clearFlags;
            originalCameraBackgroundColor = mainCamera.backgroundColor;
            originalCameraStateCaptured = true;
        }

        Light directionalLight = ResolveDirectionalLight();
        if (!originalDirectionalLightStateCaptured && directionalLight != null)
        {
            originalDirectionalLightColor = directionalLight.color;
            originalDirectionalLightIntensity = directionalLight.intensity;
            originalDirectionalLightShadows = directionalLight.shadows;
            originalDirectionalLightShadowStrength = directionalLight.shadowStrength;
            originalDirectionalLightShadowBias = directionalLight.shadowBias;
            originalDirectionalLightShadowNormalBias = directionalLight.shadowNormalBias;
            originalDirectionalLightRotation = directionalLight.transform.rotation;
            originalDirectionalLightStateCaptured = true;
            CacheOriginalDirectionalLightCookie(directionalLight);
        }
    }

    private void RestoreOriginalSceneState()
    {
        if (originalRenderSettingsCaptured)
        {
            RenderSettings.fog = originalFog;
            RenderSettings.fogMode = originalFogMode;
            RenderSettings.fogColor = originalFogColor;
            RenderSettings.fogDensity = originalFogDensity;
            RenderSettings.fogStartDistance = originalFogStartDistance;
            RenderSettings.fogEndDistance = originalFogEndDistance;
            RenderSettings.ambientMode = originalAmbientMode;
            RenderSettings.ambientSkyColor = originalAmbientSkyColor;
            RenderSettings.ambientEquatorColor = originalAmbientEquatorColor;
            RenderSettings.ambientGroundColor = originalAmbientGroundColor;
            RenderSettings.ambientIntensity = originalAmbientIntensity;
            RenderSettings.skybox = originalSkyboxMaterial;
        }

        Camera mainCamera = ResolveMainCamera();
        if (originalCameraStateCaptured && mainCamera != null)
        {
            mainCamera.clearFlags = originalCameraClearFlags;
            mainCamera.backgroundColor = originalCameraBackgroundColor;
        }

        Light directionalLight = ResolveDirectionalLight();
        if (originalDirectionalLightStateCaptured && directionalLight != null)
        {
            directionalLight.color = originalDirectionalLightColor;
            directionalLight.intensity = originalDirectionalLightIntensity;
            directionalLight.shadows = originalDirectionalLightShadows;
            directionalLight.shadowStrength = originalDirectionalLightShadowStrength;
            directionalLight.shadowBias = originalDirectionalLightShadowBias;
            directionalLight.shadowNormalBias = originalDirectionalLightShadowNormalBias;
            directionalLight.transform.rotation = originalDirectionalLightRotation;
        }

        RestoreOriginalDirectionalLightCookie();
        ReleaseGeneratedCloudShadowCookie();
        activeCloudShadowSettings = null;
        cloudShadowOffset = Vector2.zero;
        cloudShadowRefreshTimer = 0f;
        ResetApplyTracking();
    }

    private void ResetApplyTracking()
    {
        lastAppliedAct = -1;
        lastAppliedConfig = null;
        lastAppliedConfigRevision = -1;
    }

    private void ApplyRenderSettings(HexActAtmosphereSettings settings)
    {
        RenderSettings.fog = settings.enableFog;
        RenderSettings.fogMode = settings.fogMode;
        RenderSettings.fogColor = settings.fogColor;
        RenderSettings.fogDensity = settings.fogDensity;
        RenderSettings.fogStartDistance = settings.linearFogStart;
        RenderSettings.fogEndDistance = settings.linearFogEnd;
        RenderSettings.ambientMode = settings.ambientMode;
        RenderSettings.ambientSkyColor = settings.ambientSkyColor;
        RenderSettings.ambientEquatorColor = settings.ambientEquatorColor;
        RenderSettings.ambientGroundColor = settings.ambientGroundColor;
        RenderSettings.ambientIntensity = settings.ambientIntensity;
        RenderSettings.skybox = settings.skyboxMaterial != null ? settings.skyboxMaterial : originalSkyboxMaterial;
    }

    private void ApplyDirectionalLight(HexDirectionalAtmosphereSettings settings)
    {
        Light directionalLight = ResolveDirectionalLight();
        if (directionalLight == null || settings == null)
        {
            return;
        }

        directionalLight.color = settings.color;
        directionalLight.intensity = settings.intensity;
        directionalLight.shadows = settings.shadows;
        directionalLight.shadowStrength = settings.shadowStrength;
        directionalLight.shadowBias = settings.shadowBias;
        directionalLight.shadowNormalBias = settings.shadowNormalBias;

        if (settings.overrideRotation)
        {
            directionalLight.transform.rotation = Quaternion.Euler(settings.eulerAngles);
        }

        if (RenderSettings.sun == null)
        {
            RenderSettings.sun = directionalLight;
        }
    }

    private void ApplyCameraSettings(HexActAtmosphereSettings settings)
    {
        Camera mainCamera = ResolveMainCamera();
        if (mainCamera == null || settings == null)
        {
            return;
        }

        mainCamera.clearFlags = settings.cameraClearFlags;
        mainCamera.backgroundColor = settings.cameraBackgroundColor;
    }

    private void ApplyCloudShadows(HexCloudShadowSettings settings)
    {
        Light directionalLight = ResolveDirectionalLight();
        if (directionalLight == null || settings == null)
        {
            return;
        }

        CacheOriginalDirectionalLightCookie(directionalLight);
        settings.Validate();

        if (!settings.enabled)
        {
            activeCloudShadowSettings = null;
            RestoreOriginalDirectionalLightCookie();
            ReleaseGeneratedCloudShadowCookie();
            return;
        }

        activeCloudShadowSettings = settings;
        cloudShadowRefreshTimer = 0f;
        EnsureGeneratedCloudShadowCookie(settings.textureSize);
        RegenerateCloudShadowCookie(settings);
        directionalLight.cookie = generatedCloudShadowCookie;
        directionalLight.cookieSize2D = new Vector2(settings.cookieWorldSize, settings.cookieWorldSize);
    }

    private void UpdateCloudShadows(float deltaTime)
    {
        if (activeCloudShadowSettings == null)
        {
            return;
        }

        Light directionalLight = ResolveDirectionalLight();
        if (directionalLight == null)
        {
            return;
        }

        cloudShadowRefreshTimer -= deltaTime;
        if (cloudShadowRefreshTimer > 0f)
        {
            return;
        }

        float elapsed = Mathf.Max(activeCloudShadowSettings.refreshInterval, deltaTime);
        Vector2 windStep = activeCloudShadowSettings.windDirection.normalized * (activeCloudShadowSettings.windSpeed * elapsed);
        cloudShadowOffset += windStep;
        cloudShadowOffset.x = Mathf.Repeat(cloudShadowOffset.x, 1f);
        cloudShadowOffset.y = Mathf.Repeat(cloudShadowOffset.y, 1f);
        RegenerateCloudShadowCookie(activeCloudShadowSettings);
        directionalLight.cookie = generatedCloudShadowCookie;
        directionalLight.cookieSize2D = new Vector2(activeCloudShadowSettings.cookieWorldSize, activeCloudShadowSettings.cookieWorldSize);
        cloudShadowRefreshTimer = activeCloudShadowSettings.refreshInterval;
    }

    private void CacheOriginalDirectionalLightCookie(Light directionalLight)
    {
        if (originalDirectionalLightCookieCaptured || directionalLight == null)
        {
            return;
        }

        originalDirectionalLightCookie = directionalLight.cookie;
        originalDirectionalLightCookieSize2D = directionalLight.cookieSize2D;
        originalDirectionalLightCookieCaptured = true;
    }

    private void RestoreOriginalDirectionalLightCookie()
    {
        if (!originalDirectionalLightCookieCaptured)
        {
            return;
        }

        Light directionalLight = ResolveDirectionalLight();
        if (directionalLight == null)
        {
            return;
        }

        directionalLight.cookie = originalDirectionalLightCookie;
        directionalLight.cookieSize2D = originalDirectionalLightCookieSize2D;
    }

    private void EnsureGeneratedCloudShadowCookie(int textureSize)
    {
        if (generatedCloudShadowCookie != null && generatedCloudShadowCookie.width == textureSize && generatedCloudShadowCookie.height == textureSize)
        {
            return;
        }

        ReleaseGeneratedCloudShadowCookie();
        generatedCloudShadowCookie = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, mipChain: false, linear: true)
        {
            name = "HexCloudShadowCookie",
            wrapMode = TextureWrapMode.Repeat,
            filterMode = FilterMode.Bilinear,
            hideFlags = HideFlags.HideAndDontSave
        };
        generatedCloudShadowPixels = new Color[textureSize * textureSize];
    }

    private void ReleaseGeneratedCloudShadowCookie()
    {
        generatedCloudShadowPixels = null;
        if (generatedCloudShadowCookie == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Destroy(generatedCloudShadowCookie);
        }
        else
        {
            DestroyImmediate(generatedCloudShadowCookie);
        }

        generatedCloudShadowCookie = null;
    }

    private void RegenerateCloudShadowCookie(HexCloudShadowSettings settings)
    {
        if (generatedCloudShadowCookie == null || generatedCloudShadowPixels == null)
        {
            return;
        }

        int textureSize = generatedCloudShadowCookie.width;
        float shadowFloor = 1f - settings.darkness;
        float thresholdStart = Mathf.Clamp01(settings.coverage - (settings.softness * 0.5f));
        float thresholdEnd = Mathf.Clamp01(settings.coverage + (settings.softness * 0.5f));
        int pixelIndex = 0;

        for (int y = 0; y < textureSize; y++)
        {
            float v = y / (float)textureSize;
            for (int x = 0; x < textureSize; x++)
            {
                float u = x / (float)textureSize;
                float primary = SampleTileablePerlin(u, v, settings.primaryScale, cloudShadowOffset);
                Vector2 detailOffset = (cloudShadowOffset * 1.73f) + new Vector2(0.31f, 0.57f);
                float detail = SampleTileablePerlin(u, v, settings.detailScale, detailOffset);
                float combined = Mathf.Lerp(primary, (primary * 0.7f) + (detail * 0.3f), settings.detailBlend);
                float cloudMask = Mathf.InverseLerp(thresholdStart, thresholdEnd, combined);
                cloudMask = cloudMask * cloudMask * (3f - (2f * cloudMask));
                float lightValue = Mathf.Lerp(1f, shadowFloor, cloudMask);
                generatedCloudShadowPixels[pixelIndex++] = new Color(lightValue, lightValue, lightValue, lightValue);
            }
        }

        generatedCloudShadowCookie.SetPixels(generatedCloudShadowPixels);
        generatedCloudShadowCookie.Apply(updateMipmaps: false, makeNoLongerReadable: false);
    }

    private static float SampleTileablePerlin(float u, float v, float scale, Vector2 offset)
    {
        float wrappedU = Mathf.Repeat(u + offset.x, 1f);
        float wrappedV = Mathf.Repeat(v + offset.y, 1f);
        float sample00 = Mathf.PerlinNoise(wrappedU * scale, wrappedV * scale);
        float sample10 = Mathf.PerlinNoise((wrappedU - 1f) * scale, wrappedV * scale);
        float sample01 = Mathf.PerlinNoise(wrappedU * scale, (wrappedV - 1f) * scale);
        float sample11 = Mathf.PerlinNoise((wrappedU - 1f) * scale, (wrappedV - 1f) * scale);

        float blendX = wrappedU;
        float blendY = wrappedV;
        float top = Mathf.Lerp(sample00, sample10, blendX);
        float bottom = Mathf.Lerp(sample01, sample11, blendX);
        return Mathf.Lerp(top, bottom, blendY);
    }

    private Camera ResolveMainCamera()
    {
        if (cachedMainCamera != null)
        {
            return cachedMainCamera;
        }

        cachedMainCamera = Camera.main;
        if (cachedMainCamera == null)
        {
            cachedMainCamera = FindAnyObjectByType<Camera>();
        }

        return cachedMainCamera;
    }

    private Light ResolveDirectionalLight()
    {
        if (cachedDirectionalLight != null)
        {
            return cachedDirectionalLight;
        }

        if (RenderSettings.sun != null)
        {
            cachedDirectionalLight = RenderSettings.sun;
            return cachedDirectionalLight;
        }

        Light[] sceneLights = FindObjectsByType<Light>(FindObjectsInactive.Exclude);
        for (int index = 0; index < sceneLights.Length; index++)
        {
            if (sceneLights[index] != null && sceneLights[index].type == LightType.Directional)
            {
                cachedDirectionalLight = sceneLights[index];
                return cachedDirectionalLight;
            }
        }

        return null;
    }
}
