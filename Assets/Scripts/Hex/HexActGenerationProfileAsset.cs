using UnityEngine;

[CreateAssetMenu(
    fileName = "ActGenerationProfile",
    menuName = "Hex/Acts/Act Generation Profile")]
public sealed class HexActGenerationProfileAsset : ScriptableObject
{
    public string displayName = "Act Profile";
    [TextArea(2, 5)] public string description = string.Empty;
    public bool isEnabled = true;
    [Header("Biome Resolution")]
    public bool overrideFallbackBiome;
    public Biome fallbackBiome = Biome.grass;
    [Header("Generation")]
    public HexBiomeGenerationSettings biomeGenerationSettings = new();
    public HexSpecialTileSettings specialTileSettings = new();

    private void OnValidate()
    {
        Validate();
    }

    public void Validate()
    {
        displayName = string.IsNullOrWhiteSpace(displayName) ? name : displayName.Trim();
        description ??= string.Empty;
        biomeGenerationSettings ??= new HexBiomeGenerationSettings();
        biomeGenerationSettings.Validate();
        specialTileSettings ??= new HexSpecialTileSettings();
    }

    public HexBiomeGenerationSettings CreateGenerationSettings(HexBiomeGenerationSettings fallback)
    {
        Validate();
        if (!isEnabled)
        {
            return fallback?.Clone() ?? new HexBiomeGenerationSettings();
        }

        return biomeGenerationSettings?.Clone() ?? fallback?.Clone() ?? new HexBiomeGenerationSettings();
    }

    public HexSpecialTileSettings CreateSpecialTileSettings(HexSpecialTileSettings fallback)
    {
        Validate();
        if (!isEnabled)
        {
            return fallback?.Clone() ?? new HexSpecialTileSettings();
        }

        return specialTileSettings?.Clone() ?? fallback?.Clone() ?? new HexSpecialTileSettings();
    }

    public string GetResolvedDisplayName()
    {
        return string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    }

    public Biome GetResolvedFallbackBiome()
    {
        if (!overrideFallbackBiome)
        {
            return Biome.grass;
        }

        return fallbackBiome;
    }
}
