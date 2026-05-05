using System;
using System.Collections.Generic;
using UnityEngine;
using UIE = UnityEngine.UIElements;

internal sealed partial class HexActTransitionModalDocumentController
{
    private void ApplyIntermissionTypographyTheme()
    {
        ApplyRimouskiFont(intermissionTitleLabel);
        ApplyRimouskiFont(intermissionContinueButton);
    }

    private void UpdateOverlayForScreen(bool showSelection)
    {
        if (modalOverlay == null)
        {
            return;
        }

        UIE.VisualElement scrim = modalScrim ?? modalOverlay;
        scrim.style.backgroundColor = showSelection
            ? new UIE.StyleColor(new Color(10f / 255f, 12f / 255f, 16f / 255f, 0.72f))
            : new UIE.StyleColor(new Color(8f / 255f, 10f / 255f, 16f / 255f, 0.58f));
    }

    private void ApplySelectionTypographyTheme()
    {
        ApplyRimouskiFont(selectionPromptLabel);
        ApplyRimouskiFont(detailTitleLabel);
        ApplyRimouskiFont(selectionContinueButton);
    }

    private void ApplyRimouskiFont(UIE.VisualElement element)
    {
        if (element == null || rimouskiFont == null)
        {
            return;
        }

        element.style.unityFont = new UIE.StyleFont(rimouskiFont);
        element.style.unityFontDefinition = UIE.FontDefinition.FromFont(rimouskiFont);
    }

    private static Font LoadRimouskiFont()
    {
#if UNITY_EDITOR
        return UnityEditor.AssetDatabase.LoadAssetAtPath<Font>(RimouskiFontEditorAssetPath);
#else
        return null;
#endif
    }

    private void SetModalVisibility(bool visible)
    {
        hudDocumentController ??= owner.GetComponent<HexHudDocumentController>() ?? UnityEngine.Object.FindAnyObjectByType<HexHudDocumentController>();
        hudDocumentController?.SetGameplayModalState(visible);
        gameplayUiRootController?.SetLayerVisible(HexGameplayUiLayerId.Modal, visible);
        gameplayUiRootController?.SetLayerInteractive(HexGameplayUiLayerId.Modal, visible);

        if (modalRoot != null)
        {
            modalRoot.style.display = visible ? UIE.DisplayStyle.Flex : UIE.DisplayStyle.None;
        }

        isOpen = visible;
        LogDebug($"SetModalVisibility visible={visible} isOpen={isOpen}.");
    }

    private void PlayRevealTransition(UIE.VisualElement activeShell, bool includeScrimFade)
    {
        StopRevealTransition();
        if (revealTransitionPlayer == null || activeShell == null)
        {
            SetElementOpacity(modalScrim, 1f);
            SetElementOpacity(activeShell, 1f);
            SetTransitionInteractionLock(false);
            return;
        }

        activeRevealProfile = CreateRevealRuntimeProfile(includeScrimFade);
        HexUiTransitionTargetSet targets = new HexUiTransitionTargetSet()
            .Register(ScrimTransitionTargetKey, modalScrim)
            .Register(PageTransitionTargetKey, activeShell);

        bool started = revealTransitionPlayer.Play(
            activeRevealProfile,
            targets,
            () => CompleteRevealTransition(activeShell),
            SetTransitionInteractionLock,
            message => LogDebug(message, true));
        if (!started)
        {
            CompleteRevealTransition(activeShell);
        }
    }

    private void StopRevealTransition()
    {
        revealTransitionPlayer?.Cancel(applyEndState: false);
        SetTransitionInteractionLock(false);
        ReleaseRevealProfile();
    }

    private void CompleteRevealTransition(UIE.VisualElement activeShell)
    {
        SetElementOpacity(modalScrim, 1f);
        SetElementOpacity(activeShell, 1f);
        SetTransitionInteractionLock(false);
        ReleaseRevealProfile();
    }

    private HexUiTransitionProfile CreateRevealRuntimeProfile(bool includeScrimFade)
    {
        return HexUiTransitionProfile.CreateRuntimeProfile(
            chapterPageRevealProfile,
            "act-transition-chapter-page-reveal",
            HexUiTransitionEasing.EaseOut,
            CreateDefaultRevealTracks(),
            track => includeScrimFade || !string.Equals(track.targetKey, ScrimTransitionTargetKey, StringComparison.Ordinal));
    }

    private static IReadOnlyList<HexUiTransitionFadeTrack> CreateDefaultRevealTracks()
    {
        return new[]
        {
            new HexUiTransitionFadeTrack
            {
                targetKey = ScrimTransitionTargetKey,
                startTime = 0f,
                duration = 0.18f,
                startOpacity = 0f,
                endOpacity = 1f,
                isRequired = true
            },
            new HexUiTransitionFadeTrack
            {
                targetKey = PageTransitionTargetKey,
                startTime = 0.08f,
                duration = 0.24f,
                startOpacity = 0f,
                endOpacity = 1f,
                isRequired = true
            }
        };
    }

    private void SetTransitionInteractionLock(bool locked)
    {
        areTransitionControlsLocked = locked;
        SetButtonInteractionEnabled(intermissionContinueButton, !locked);
        RefreshSelectionActionState();
    }

    private static void SetButtonInteractionEnabled(UIE.Button button, bool enabled)
    {
        if (button == null)
        {
            return;
        }

        button.SetEnabled(enabled);
        button.pickingMode = enabled ? UIE.PickingMode.Position : UIE.PickingMode.Ignore;
        button.focusable = enabled;
    }

    private static void SetElementOpacity(UIE.VisualElement element, float opacity)
    {
        if (element != null)
        {
            element.style.opacity = Mathf.Clamp01(opacity);
        }
    }

    private void ReleaseRevealProfile()
    {
        if (activeRevealProfile != null)
        {
            UnityEngine.Object.Destroy(activeRevealProfile);
            activeRevealProfile = null;
        }
    }

    private void LogDebug(string message, bool verbose = false)
    {
        if (owner is HexActTransitionModalPresenter presenter)
        {
            presenter.LogDebug(message, verbose);
            return;
        }

        Debug.Log($"[GameplayUI:TransitionModal] {message}", owner);
    }
}
