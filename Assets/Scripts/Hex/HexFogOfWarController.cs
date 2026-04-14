using System;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public sealed class HexFogOfWarSettings
{
    [Min(0)] public int visionRadius = 1;
    public Color fogColor = new(0.03f, 0.04f, 0.06f, 1f);
    [Range(0f, 1f)] public float rememberedOverlayAlpha = 0.42f;
    [Range(0f, 1f)] public float unseenOverlayAlpha = 0.92f;
    [Min(0f)] public float overlayHeight = 0.03f;
    [Min(0.1f)] public float overlayScale = 1.02f;

    public void Validate()
    {
        visionRadius = Mathf.Max(0, visionRadius);
        rememberedOverlayAlpha = Mathf.Clamp01(rememberedOverlayAlpha);
        unseenOverlayAlpha = Mathf.Clamp01(unseenOverlayAlpha);
        overlayHeight = Mathf.Max(0f, overlayHeight);
        overlayScale = Mathf.Max(0.1f, overlayScale);
    }
}

public sealed class HexFogOfWarController : MonoBehaviour
{
    [SerializeField] private HexFogOfWarSettings settings = new();

    private readonly HexFogOfWarState fogState = new();
    private readonly HexFogOfWarPresenter presenter = new();
    private MapGenerator mapGenerator;
    private int visionRadiusBonus;

    public event Action<HexFogUpdateResult> VisibilityUpdated;

    public HexFogUpdateResult LastUpdate { get; private set; } = HexFogUpdateResult.Empty;
    public HexFogOfWarSettings Settings => settings;
    public int EffectiveVisionRadius => Mathf.Max(0, settings.visionRadius + visionRadiusBonus);
    public bool IsInitialized => fogState.IsInitialized && mapGenerator != null;

    private void OnValidate()
    {
        settings ??= new HexFogOfWarSettings();
        settings.Validate();
    }

    public void Initialize(MapGenerator mapGenerator, IEnumerable<HexCoordinates> alwaysKnownCoordinates)
    {
        settings ??= new HexFogOfWarSettings();
        settings.Validate();
        this.mapGenerator = mapGenerator;
        fogState.Initialize(mapGenerator?.GridData, alwaysKnownCoordinates);
    }

    public HexFogUpdateResult RefreshVisibility(HexCoordinates caravanCoordinates, IEnumerable<HexCoordinates> alwaysKnownCoordinates)
    {
        if (!IsInitialized)
        {
            return HexFogUpdateResult.Empty;
        }

        fogState.SetAlwaysKnownTiles(alwaysKnownCoordinates);
        LastUpdate = fogState.UpdateVisibility(caravanCoordinates, EffectiveVisionRadius);
        presenter.Apply(mapGenerator, fogState, settings);
        VisibilityUpdated?.Invoke(LastUpdate);
        return LastUpdate;
    }

    public void SetVisionRadiusBonus(int bonus)
    {
        visionRadiusBonus = Mathf.Max(0, bonus);
    }

    public bool IsCurrentlyVisible(HexCoordinates coordinates)
    {
        return fogState.IsCurrentlyVisible(coordinates);
    }

    public bool HasBeenDiscovered(HexCoordinates coordinates)
    {
        return fogState.HasBeenDiscovered(coordinates);
    }

    public bool IsRemembered(HexCoordinates coordinates)
    {
        return fogState.IsRemembered(coordinates);
    }

    public bool IsAlwaysKnownSpecial(HexCoordinates coordinates)
    {
        return fogState.IsAlwaysKnownSpecial(coordinates);
    }

    public bool ShouldShowTerrain(HexCoordinates coordinates)
    {
        return fogState.ShouldShowTerrain(coordinates);
    }

    public bool ShouldShowSpecialTile(HexCoordinates coordinates)
    {
        return fogState.ShouldShowSpecialTile(coordinates);
    }

    public bool ShouldShowObstacle(HexCoordinates coordinates)
    {
        return fogState.ShouldShowObstacle(coordinates);
    }

    public HexFogKnowledgeState GetKnowledgeState(HexCoordinates coordinates)
    {
        return fogState.GetKnowledgeState(coordinates);
    }
}
