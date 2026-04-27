using System;
using System.Collections.Generic;
using UnityEngine;
using UIE = UnityEngine.UIElements;

public sealed class HexUiTransitionTargetSet
{
    private readonly Dictionary<string, UIE.VisualElement> targets = new(StringComparer.Ordinal);

    public HexUiTransitionTargetSet Register(string targetKey, UIE.VisualElement target)
    {
        if (string.IsNullOrWhiteSpace(targetKey))
        {
            return this;
        }

        string resolvedKey = targetKey.Trim();
        if (target == null)
        {
            targets.Remove(resolvedKey);
            return this;
        }

        targets[resolvedKey] = target;
        return this;
    }

    public bool TryGetTarget(string targetKey, out UIE.VisualElement target)
    {
        target = null;
        return !string.IsNullOrWhiteSpace(targetKey)
            && targets.TryGetValue(targetKey.Trim(), out target)
            && target != null;
    }

    public void Clear()
    {
        targets.Clear();
    }
}

public sealed class HexUiTransitionPlayer
{
    private readonly UIE.VisualElement schedulerElement;

    private UIE.IVisualElementScheduledItem scheduledUpdate;
    private HexUiTransitionProfile activeProfile;
    private HexUiTransitionTargetSet activeTargets;
    private Action completionCallback;
    private Action<bool> interactionLockChanged;
    private Action<string> warningLogged;
    private float startTime;
    private float completeTime;
    private float interactionUnlockTime;
    private bool isPlaying;
    private bool isInteractionLocked;
    private bool completionInvoked;

    public HexUiTransitionPlayer(UIE.VisualElement schedulerElement)
    {
        this.schedulerElement = schedulerElement;
    }

    public bool IsPlaying => isPlaying;

    public bool Play(
        HexUiTransitionProfile profile,
        HexUiTransitionTargetSet targets,
        Action onCompleted = null,
        Action<bool> onInteractionLockChanged = null,
        Action<string> onWarningLogged = null)
    {
        if (isPlaying)
        {
            onWarningLogged?.Invoke($"UI transition '{activeProfile?.profileId ?? "unknown"}' is already playing.");
            return false;
        }

        if (schedulerElement == null)
        {
            onWarningLogged?.Invoke("Cannot play UI transition without a scheduler VisualElement.");
            return false;
        }

        activeProfile = profile;
        activeTargets = targets;
        completionCallback = onCompleted;
        interactionLockChanged = onInteractionLockChanged;
        warningLogged = onWarningLogged;
        completionInvoked = false;

        if (activeProfile == null)
        {
            InvokeInteractionLock(false);
            InvokeCompletion();
            ClearPlaybackState();
            return true;
        }

        completeTime = Mathf.Max(0f, activeProfile.ResolveCompleteTime());
        interactionUnlockTime = Mathf.Max(0f, activeProfile.ResolveInteractionUnlockTime());
        startTime = Time.unscaledTime;
        isPlaying = true;
        InvokeInteractionLock(true);
        ApplyInitialState();

        if (completeTime <= 0f)
        {
            Complete();
            return true;
        }

        scheduledUpdate = schedulerElement.schedule.Execute(Update).Every(16);
        Update();
        return true;
    }

    public void Cancel(bool applyEndState = false)
    {
        if (!isPlaying)
        {
            return;
        }

        if (applyEndState)
        {
            ApplyCompletedState();
        }

        StopScheduledUpdate();
        InvokeInteractionLock(false);
        ClearPlaybackState();
    }

    public void Complete()
    {
        if (!isPlaying)
        {
            return;
        }

        ApplyCompletedState();
        StopScheduledUpdate();
        InvokeInteractionLock(false);
        InvokeCompletion();
        ClearPlaybackState();
    }

    private void Update()
    {
        if (!isPlaying)
        {
            return;
        }

        float elapsed = Time.unscaledTime - startTime;
        ApplyStateAt(elapsed);

        if (elapsed >= interactionUnlockTime)
        {
            InvokeInteractionLock(false);
        }

        if (elapsed >= completeTime)
        {
            Complete();
        }
    }

    private void ApplyInitialState()
    {
        IReadOnlyList<HexUiTransitionFadeTrack> tracks = activeProfile.FadeTracks;
        for (int index = 0; index < tracks.Count; index++)
        {
            HexUiTransitionFadeTrack track = tracks[index];
            if (track == null || !track.HasTargetKey)
            {
                continue;
            }

            ApplyTrackOpacity(track, track.startOpacity, logMissingTarget: true);
        }
    }

    private void ApplyCompletedState()
    {
        IReadOnlyList<HexUiTransitionFadeTrack> tracks = activeProfile.FadeTracks;
        for (int index = 0; index < tracks.Count; index++)
        {
            HexUiTransitionFadeTrack track = tracks[index];
            if (track == null || !track.HasTargetKey)
            {
                continue;
            }

            ApplyTrackOpacity(track, track.endOpacity, logMissingTarget: false);
        }
    }

    private void ApplyStateAt(float elapsed)
    {
        IReadOnlyList<HexUiTransitionFadeTrack> tracks = activeProfile.FadeTracks;
        for (int index = 0; index < tracks.Count; index++)
        {
            HexUiTransitionFadeTrack track = tracks[index];
            if (track == null || !track.HasTargetKey)
            {
                continue;
            }

            float opacity = track.EvaluateOpacity(elapsed, activeProfile.easing);
            ApplyTrackOpacity(track, opacity, logMissingTarget: false);
        }
    }

    private void ApplyTrackOpacity(HexUiTransitionFadeTrack track, float opacity, bool logMissingTarget)
    {
        if (activeTargets == null || !activeTargets.TryGetTarget(track.targetKey, out UIE.VisualElement target))
        {
            if (logMissingTarget && track.isRequired)
            {
                warningLogged?.Invoke($"UI transition target '{track.targetKey}' is required but was not registered.");
            }

            return;
        }

        target.style.opacity = Mathf.Clamp01(opacity);
    }

    private void StopScheduledUpdate()
    {
        scheduledUpdate?.Pause();
        scheduledUpdate = null;
    }

    private void InvokeInteractionLock(bool locked)
    {
        if (isInteractionLocked == locked)
        {
            return;
        }

        isInteractionLocked = locked;
        interactionLockChanged?.Invoke(locked);
    }

    private void InvokeCompletion()
    {
        if (completionInvoked)
        {
            return;
        }

        completionInvoked = true;
        completionCallback?.Invoke();
    }

    private void ClearPlaybackState()
    {
        activeProfile = null;
        activeTargets = null;
        completionCallback = null;
        interactionLockChanged = null;
        warningLogged = null;
        startTime = 0f;
        completeTime = 0f;
        interactionUnlockTime = 0f;
        isPlaying = false;
        isInteractionLocked = false;
        completionInvoked = false;
    }
}
