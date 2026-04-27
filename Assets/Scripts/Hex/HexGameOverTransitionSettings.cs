using UnityEngine;

[DisallowMultipleComponent]
public sealed class HexGameOverTransitionSettings : MonoBehaviour
{
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

    public float ResolveCompleteTime()
    {
        float completeTime = blackoutFadeStart + blackoutFadeDuration;
        completeTime = Mathf.Max(completeTime, backgroundFadeStart + backgroundFadeDuration);
        completeTime = Mathf.Max(completeTime, scrimFadeStart + scrimFadeDuration);
        completeTime = Mathf.Max(completeTime, titleFadeStart + titleFadeDuration);
        completeTime = Mathf.Max(completeTime, separatorFadeStart + separatorFadeDuration);
        completeTime = Mathf.Max(completeTime, panelFadeStart + panelFadeDuration);
        completeTime = Mathf.Max(completeTime, descriptionFadeStart + descriptionFadeDuration);
        completeTime = Mathf.Max(completeTime, retryButtonFadeStart + retryButtonFadeDuration);
        completeTime = Mathf.Max(completeTime, returnToTitleButtonFadeStart + returnToTitleButtonFadeDuration);
        return completeTime;
    }
}
