using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class HexGameOverTransitionSettings : MonoBehaviour
{
    public const string BlackoutTargetKey = "blackout";
    public const string BackgroundTargetKey = "background";
    public const string ScrimTargetKey = "scrim";
    public const string TitleTargetKey = "title";
    public const string SeparatorTargetKey = "separator";
    public const string PanelTargetKey = "panel";
    public const string DescriptionTargetKey = "description";
    public const string RetryButtonTargetKey = "retry-button";
    public const string ReturnToTitleButtonTargetKey = "return-to-title-button";

    [Header("Blackout")]
    [Min(0f)] public float blackoutFadeStart = 0f;
    [Min(0f)] public float blackoutFadeDuration = 1f;

    [Header("Background Image")]
    [Min(0f)] public float backgroundFadeStart = 1f;
    [Min(0f)] public float backgroundFadeDuration = 0.55f;

    [Header("Overlay / Scrim")]
    [Min(0f)] public float scrimFadeStart = 1f;
    [Min(0f)] public float scrimFadeDuration = 0.55f;

    [Header("Title")]
    [Min(0f)] public float titleFadeStart = 1.35f;
    [Min(0f)] public float titleFadeDuration = 0.40f;

    [Header("Separator")]
    [Min(0f)] public float separatorFadeStart = 1.45f;
    [Min(0f)] public float separatorFadeDuration = 0.35f;

    [Header("Panel")]
    [Min(0f)] public float panelFadeStart = 1.60f;
    [Min(0f)] public float panelFadeDuration = 0.45f;

    [Header("Description")]
    [Min(0f)] public float descriptionFadeStart = 1.78f;
    [Min(0f)] public float descriptionFadeDuration = 0.30f;

    [Header("Buttons")]
    [Min(0f)] public float retryButtonFadeStart = 1.92f;
    [Min(0f)] public float retryButtonFadeDuration = 0.28f;
    [Min(0f)] public float returnToTitleButtonFadeStart = 2.02f;
    [Min(0f)] public float returnToTitleButtonFadeDuration = 0.28f;

    [Header("Easing")]
    public bool useEaseOut = true;

    public HexUiTransitionProfile CreateRuntimeProfile()
    {
        return CreateRuntimeProfile(
            blackoutFadeStart,
            blackoutFadeDuration,
            backgroundFadeStart,
            backgroundFadeDuration,
            scrimFadeStart,
            scrimFadeDuration,
            titleFadeStart,
            titleFadeDuration,
            separatorFadeStart,
            separatorFadeDuration,
            panelFadeStart,
            panelFadeDuration,
            descriptionFadeStart,
            descriptionFadeDuration,
            retryButtonFadeStart,
            retryButtonFadeDuration,
            returnToTitleButtonFadeStart,
            returnToTitleButtonFadeDuration,
            useEaseOut);
    }

    public static HexUiTransitionProfile CreateDefaultRuntimeProfile()
    {
        return CreateRuntimeProfile(
            blackoutStart: 0f,
            blackoutDuration: 1f,
            backgroundStart: 1f,
            backgroundDuration: 0.55f,
            scrimStart: 1f,
            scrimDuration: 0.55f,
            titleStart: 1.35f,
            titleDuration: 0.40f,
            separatorStart: 1.45f,
            separatorDuration: 0.35f,
            panelStart: 1.60f,
            panelDuration: 0.45f,
            descriptionStart: 1.78f,
            descriptionDuration: 0.30f,
            retryStart: 1.92f,
            retryDuration: 0.28f,
            returnStart: 2.02f,
            returnDuration: 0.28f,
            useEaseOut: true);
    }

    private static HexUiTransitionProfile CreateRuntimeProfile(
        float blackoutStart,
        float blackoutDuration,
        float backgroundStart,
        float backgroundDuration,
        float scrimStart,
        float scrimDuration,
        float titleStart,
        float titleDuration,
        float separatorStart,
        float separatorDuration,
        float panelStart,
        float panelDuration,
        float descriptionStart,
        float descriptionDuration,
        float retryStart,
        float retryDuration,
        float returnStart,
        float returnDuration,
        bool useEaseOut)
    {
        HexUiTransitionProfile profile = ScriptableObject.CreateInstance<HexUiTransitionProfile>();
        profile.hideFlags = HideFlags.DontSave;
        profile.profileId = "game-over-blackout-then-reveal";
        profile.easing = useEaseOut ? HexUiTransitionEasing.EaseOut : HexUiTransitionEasing.Linear;
        profile.overrideInteractionUnlockTime = false;
        profile.interactionUnlockTime = 0f;
        profile.fadeTracks = new List<HexUiTransitionFadeTrack>
        {
            CreateTrack(BlackoutTargetKey, blackoutStart, blackoutDuration),
            CreateTrack(BackgroundTargetKey, backgroundStart, backgroundDuration),
            CreateTrack(ScrimTargetKey, scrimStart, scrimDuration),
            CreateTrack(TitleTargetKey, titleStart, titleDuration),
            CreateTrack(SeparatorTargetKey, separatorStart, separatorDuration),
            CreateTrack(PanelTargetKey, panelStart, panelDuration),
            CreateTrack(DescriptionTargetKey, descriptionStart, descriptionDuration),
            CreateTrack(RetryButtonTargetKey, retryStart, retryDuration),
            CreateTrack(ReturnToTitleButtonTargetKey, returnStart, returnDuration)
        };
        return profile;
    }

    private static HexUiTransitionFadeTrack CreateTrack(string targetKey, float start, float duration)
    {
        return new HexUiTransitionFadeTrack
        {
            targetKey = targetKey,
            startTime = Mathf.Max(0f, start),
            duration = Mathf.Max(0f, duration),
            startOpacity = 0f,
            endOpacity = 1f,
            isRequired = true
        };
    }
}
