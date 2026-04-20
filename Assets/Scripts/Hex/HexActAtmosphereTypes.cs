using UnityEngine;
using UnityEngine.Rendering;

[System.Serializable]
public sealed class HexDirectionalAtmosphereSettings
{
    public bool overrideRotation = true;
    public Vector3 eulerAngles = new(50f, -30f, 0f);
    public Color color = new(1f, 0.95f, 0.84f, 1f);
    [Min(0f)] public float intensity = 1.2f;
    public LightShadows shadows = LightShadows.Soft;
    [Range(0f, 1f)] public float shadowStrength = 0.95f;
    [Min(0f)] public float shadowBias = 0.05f;
    [Min(0f)] public float shadowNormalBias = 0.4f;

    public void Validate()
    {
        intensity = Mathf.Max(0f, intensity);
        shadowStrength = Mathf.Clamp01(shadowStrength);
        shadowBias = Mathf.Max(0f, shadowBias);
        shadowNormalBias = Mathf.Max(0f, shadowNormalBias);
    }
}

[System.Serializable]
public sealed class HexCloudShadowSettings
{
    public bool enabled = true;
    [Range(0f, 0.75f)] public float darkness = 0.18f;
    [Range(0.05f, 0.95f)] public float softness = 0.32f;
    [Range(0.05f, 0.95f)] public float coverage = 0.62f;
    [Min(0.1f)] public float primaryScale = 1.4f;
    [Min(0.1f)] public float detailScale = 3.1f;
    [Range(0f, 1f)] public float detailBlend = 0.35f;
    [Range(32, 256)] public int textureSize = 96;
    [Min(1f)] public float cookieWorldSize = 52f;
    public Vector2 windDirection = new(1f, 0.2f);
    [Min(0f)] public float windSpeed = 0.05f;
    [Min(0.02f)] public float refreshInterval = 0.16f;

    public void Validate()
    {
        darkness = Mathf.Clamp(darkness, 0f, 0.75f);
        softness = Mathf.Clamp(softness, 0.05f, 0.95f);
        coverage = Mathf.Clamp(coverage, 0.05f, 0.95f);
        primaryScale = Mathf.Max(0.1f, primaryScale);
        detailScale = Mathf.Max(0.1f, detailScale);
        detailBlend = Mathf.Clamp01(detailBlend);
        textureSize = Mathf.Clamp(textureSize, 32, 256);
        cookieWorldSize = Mathf.Max(1f, cookieWorldSize);
        windSpeed = Mathf.Max(0f, windSpeed);
        refreshInterval = Mathf.Max(0.02f, refreshInterval);

        if (windDirection.sqrMagnitude <= 0.0001f)
        {
            windDirection = new Vector2(1f, 0.2f);
        }
    }
}

[System.Serializable]
public sealed class HexActAtmosphereSettings
{
    public CameraClearFlags cameraClearFlags = CameraClearFlags.SolidColor;
    public Color cameraBackgroundColor = new(0.55f, 0.68f, 0.79f, 1f);
    public bool enableFog = true;
    public FogMode fogMode = FogMode.Linear;
    public Color fogColor = new(0.68f, 0.74f, 0.78f, 1f);
    [Min(0f)] public float fogDensity = 0.01f;
    public float linearFogStart = 18f;
    public float linearFogEnd = 55f;
    public AmbientMode ambientMode = AmbientMode.Trilight;
    public Color ambientSkyColor = new(0.55f, 0.61f, 0.66f, 1f);
    public Color ambientEquatorColor = new(0.35f, 0.38f, 0.39f, 1f);
    public Color ambientGroundColor = new(0.16f, 0.15f, 0.13f, 1f);
    [Min(0f)] public float ambientIntensity = 1.05f;
    public Material skyboxMaterial;
    public HexDirectionalAtmosphereSettings directionalLight = new();
    public HexCloudShadowSettings cloudShadows = new();

    public void Validate()
    {
        fogDensity = Mathf.Max(0f, fogDensity);
        linearFogEnd = Mathf.Max(linearFogStart + 1f, linearFogEnd);
        ambientIntensity = Mathf.Max(0f, ambientIntensity);
        directionalLight ??= new HexDirectionalAtmosphereSettings();
        cloudShadows ??= new HexCloudShadowSettings();
        directionalLight.Validate();
        cloudShadows.Validate();
    }

    public static HexActAtmosphereSettings CreateAct1Defaults()
    {
        return new HexActAtmosphereSettings
        {
            cameraClearFlags = CameraClearFlags.SolidColor,
            cameraBackgroundColor = new Color(0.59f, 0.69f, 0.78f, 1f),
            enableFog = true,
            fogMode = FogMode.Linear,
            fogColor = new Color(0.73f, 0.79f, 0.82f, 1f),
            fogDensity = 0.006f,
            linearFogStart = 24f,
            linearFogEnd = 70f,
            ambientMode = AmbientMode.Trilight,
            ambientSkyColor = new Color(0.58f, 0.64f, 0.68f, 1f),
            ambientEquatorColor = new Color(0.34f, 0.37f, 0.38f, 1f),
            ambientGroundColor = new Color(0.17f, 0.15f, 0.13f, 1f),
            ambientIntensity = 1.1f,
            directionalLight = new HexDirectionalAtmosphereSettings
            {
                overrideRotation = true,
                eulerAngles = new Vector3(48f, -26f, 0f),
                color = new Color(1f, 0.95f, 0.84f, 1f),
                intensity = 1.22f,
                shadows = LightShadows.Soft,
                shadowStrength = 0.92f,
                shadowBias = 0.05f,
                shadowNormalBias = 0.35f
            },
            cloudShadows = new HexCloudShadowSettings
            {
                enabled = true,
                darkness = 0.12f,
                softness = 0.34f,
                coverage = 0.66f,
                primaryScale = 1.2f,
                detailScale = 2.4f,
                detailBlend = 0.28f,
                textureSize = 96,
                cookieWorldSize = 60f,
                windDirection = new Vector2(1f, 0.15f),
                windSpeed = 0.035f,
                refreshInterval = 0.18f
            }
        };
    }

    public static HexActAtmosphereSettings CreateAct2Defaults()
    {
        return new HexActAtmosphereSettings
        {
            cameraClearFlags = CameraClearFlags.SolidColor,
            cameraBackgroundColor = new Color(0.79f, 0.72f, 0.57f, 1f),
            enableFog = true,
            fogMode = FogMode.Linear,
            fogColor = new Color(0.82f, 0.73f, 0.58f, 1f),
            fogDensity = 0.01f,
            linearFogStart = 16f,
            linearFogEnd = 42f,
            ambientMode = AmbientMode.Trilight,
            ambientSkyColor = new Color(0.74f, 0.64f, 0.49f, 1f),
            ambientEquatorColor = new Color(0.48f, 0.39f, 0.28f, 1f),
            ambientGroundColor = new Color(0.23f, 0.18f, 0.12f, 1f),
            ambientIntensity = 1f,
            directionalLight = new HexDirectionalAtmosphereSettings
            {
                overrideRotation = true,
                eulerAngles = new Vector3(58f, -18f, 0f),
                color = new Color(1f, 0.89f, 0.69f, 1f),
                intensity = 1.38f,
                shadows = LightShadows.Soft,
                shadowStrength = 0.98f,
                shadowBias = 0.06f,
                shadowNormalBias = 0.4f
            },
            cloudShadows = new HexCloudShadowSettings
            {
                enabled = true,
                darkness = 0.2f,
                softness = 0.3f,
                coverage = 0.58f,
                primaryScale = 1.45f,
                detailScale = 3f,
                detailBlend = 0.4f,
                textureSize = 96,
                cookieWorldSize = 48f,
                windDirection = new Vector2(0.9f, 0.28f),
                windSpeed = 0.06f,
                refreshInterval = 0.14f
            }
        };
    }

    public static HexActAtmosphereSettings CreateAct3Defaults()
    {
        return new HexActAtmosphereSettings
        {
            cameraClearFlags = CameraClearFlags.SolidColor,
            cameraBackgroundColor = new Color(0.33f, 0.42f, 0.47f, 1f),
            enableFog = true,
            fogMode = FogMode.Linear,
            fogColor = new Color(0.38f, 0.47f, 0.49f, 1f),
            fogDensity = 0.012f,
            linearFogStart = 14f,
            linearFogEnd = 36f,
            ambientMode = AmbientMode.Trilight,
            ambientSkyColor = new Color(0.28f, 0.36f, 0.39f, 1f),
            ambientEquatorColor = new Color(0.19f, 0.24f, 0.25f, 1f),
            ambientGroundColor = new Color(0.08f, 0.08f, 0.09f, 1f),
            ambientIntensity = 0.95f,
            directionalLight = new HexDirectionalAtmosphereSettings
            {
                overrideRotation = true,
                eulerAngles = new Vector3(38f, -34f, 0f),
                color = new Color(0.76f, 0.86f, 0.91f, 1f),
                intensity = 0.98f,
                shadows = LightShadows.Soft,
                shadowStrength = 1f,
                shadowBias = 0.05f,
                shadowNormalBias = 0.45f
            },
            cloudShadows = new HexCloudShadowSettings
            {
                enabled = true,
                darkness = 0.26f,
                softness = 0.38f,
                coverage = 0.56f,
                primaryScale = 1.7f,
                detailScale = 3.3f,
                detailBlend = 0.45f,
                textureSize = 128,
                cookieWorldSize = 42f,
                windDirection = new Vector2(0.7f, 0.35f),
                windSpeed = 0.07f,
                refreshInterval = 0.12f
            }
        };
    }
}
