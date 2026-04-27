using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public sealed class HexGlobalUiTransitionController : MonoBehaviour
{
    private const string GlobalFadeToBlackProfileResourcePath = "UI/Transitions/GlobalFadeToBlack";
    private const string GlobalTransitionMountName = "global-transition-mount";
    private const string GlobalBlackoutElementName = "global-transition-blackout";
    private const string BlackoutTransitionTargetKey = "blackout";

    [SerializeField] private HexUiTransitionProfile globalFadeToBlackProfile;

    private static HexGlobalUiTransitionController sharedInstance;
    private HexGameplayUiRootController gameplayUiRootController;
    private VisualElement transitionMount;
    private VisualElement blackoutElement;
    private HexUiTransitionPlayer transitionPlayer;
    private HexUiTransitionProfile activeRuntimeProfile;
    private IVisualElementScheduledItem scheduledFadeOut;
    private bool isInitialized;
    private bool isTransitioning;

    public bool IsReady => isInitialized;
    public bool IsTransitioning => isTransitioning;

    public static HexGlobalUiTransitionController ResolveShared(MonoBehaviour owner, bool createIfMissing = true)
    {
        if (sharedInstance != null)
        {
            return sharedInstance;
        }

        sharedInstance = UnityEngine.Object.FindAnyObjectByType<HexGlobalUiTransitionController>();
        if (sharedInstance != null)
        {
            return sharedInstance;
        }

        if (!createIfMissing || owner == null)
        {
            return null;
        }

        HexGameplayUiRootController rootController = HexGameplayUiRootController.ResolveShared(owner, createIfMissing);
        if (rootController == null)
        {
            return null;
        }

        sharedInstance = rootController.GetComponent<HexGlobalUiTransitionController>()
            ?? rootController.gameObject.AddComponent<HexGlobalUiTransitionController>();
        return sharedInstance;
    }

    private void OnEnable()
    {
        if (sharedInstance == null)
        {
            sharedInstance = this;
        }
    }

    private void OnDestroy()
    {
        if (sharedInstance == this)
        {
            sharedInstance = null;
        }
    }

    public void EnsureInitialized()
    {
        if (isInitialized)
        {
            return;
        }

        gameplayUiRootController ??= HexGameplayUiRootController.ResolveShared(this);
        if (gameplayUiRootController == null)
        {
            Debug.LogError("HexGlobalUiTransitionController could not resolve the shared gameplay UI root.", this);
            return;
        }

        gameplayUiRootController.EnsureInitialized();
        transitionMount = gameplayUiRootController.RequestLayerMount(
            HexGameplayUiLayerId.GlobalTransition,
            GlobalTransitionMountName,
            nameof(HexGlobalUiTransitionController),
            false,
            this);
        if (transitionMount == null)
        {
            Debug.LogError("HexGlobalUiTransitionController could not bind to the global transition layer.", this);
            return;
        }

        blackoutElement = transitionMount.Q<VisualElement>(GlobalBlackoutElementName);
        if (blackoutElement == null)
        {
            blackoutElement = new VisualElement
            {
                name = GlobalBlackoutElementName,
                pickingMode = PickingMode.Position
            };
            blackoutElement.AddToClassList("global-transition-blackout");
            transitionMount.Add(blackoutElement);
        }

        transitionPlayer = new HexUiTransitionPlayer(transitionMount);
        SetElementOpacity(blackoutElement, 0f);
        SetGlobalTransitionLayerActive(false);
        globalFadeToBlackProfile ??= Resources.Load<HexUiTransitionProfile>(GlobalFadeToBlackProfileResourcePath);
        isInitialized = true;
    }

    public bool PlayFadeToBlack(Action whileBlack = null, Action onCompleted = null, bool fadeBackOut = true)
    {
        EnsureInitialized();
        if (!isInitialized)
        {
            RunCallback(whileBlack);
            RunCallback(onCompleted);
            return false;
        }

        if (isTransitioning || (transitionPlayer != null && transitionPlayer.IsPlaying))
        {
            LogDebug("Ignored global fade request because a global transition is already running.", true);
            return false;
        }

        isTransitioning = true;
        SetGlobalTransitionLayerActive(true);
        SetElementOpacity(blackoutElement, 0f);

        return PlayRuntimeProfile(
            CreateRuntimeProfile(reverse: false),
            () =>
            {
                ReleaseRuntimeProfile();
                RunCallback(whileBlack);

                if (fadeBackOut)
                {
                    ScheduleFadeFromBlack(onCompleted);
                    return;
                }

                SetElementOpacity(blackoutElement, 1f);
                isTransitioning = false;
                RunCallback(onCompleted);
            });
    }

    public bool PlayFadeFromBlack(Action onCompleted = null)
    {
        EnsureInitialized();
        if (!isInitialized)
        {
            RunCallback(onCompleted);
            return false;
        }

        if (transitionPlayer != null && transitionPlayer.IsPlaying)
        {
            LogDebug("Ignored global fade-out request because a global transition is already running.", true);
            return false;
        }

        isTransitioning = true;
        SetGlobalTransitionLayerActive(true);
        SetElementOpacity(blackoutElement, 1f);

        return PlayRuntimeProfile(
            CreateRuntimeProfile(reverse: true),
            () =>
            {
                ReleaseRuntimeProfile();
                SetElementOpacity(blackoutElement, 0f);
                SetGlobalTransitionLayerActive(false);
                isTransitioning = false;
                RunCallback(onCompleted);
            });
    }

    public void Cancel(bool hideLayer = true)
    {
        transitionPlayer?.Cancel(applyEndState: false);
        scheduledFadeOut?.Pause();
        scheduledFadeOut = null;
        ReleaseRuntimeProfile();
        isTransitioning = false;
        if (hideLayer)
        {
            SetElementOpacity(blackoutElement, 0f);
            SetGlobalTransitionLayerActive(false);
        }
    }

    private bool PlayRuntimeProfile(HexUiTransitionProfile profile, Action onCompleted)
    {
        if (transitionPlayer == null || blackoutElement == null)
        {
            RunCallback(onCompleted);
            return false;
        }

        ReleaseRuntimeProfile();
        activeRuntimeProfile = profile;
        HexUiTransitionTargetSet targets = new HexUiTransitionTargetSet()
            .Register(BlackoutTransitionTargetKey, blackoutElement);

        bool started = transitionPlayer.Play(
            activeRuntimeProfile,
            targets,
            onCompleted,
            null,
            message => LogDebug(message, true));
        if (!started)
        {
            RunCallback(onCompleted);
        }

        return started;
    }

    private void ScheduleFadeFromBlack(Action onCompleted)
    {
        if (transitionMount == null)
        {
            PlayFadeFromBlack(onCompleted);
            return;
        }

        scheduledFadeOut?.Pause();
        scheduledFadeOut = transitionMount.schedule.Execute(() =>
        {
            scheduledFadeOut = null;
            PlayFadeFromBlack(onCompleted);
        }).StartingIn(0);
    }

    private HexUiTransitionProfile CreateRuntimeProfile(bool reverse)
    {
        HexUiTransitionProfile source = globalFadeToBlackProfile;
        HexUiTransitionProfile runtimeProfile = ScriptableObject.CreateInstance<HexUiTransitionProfile>();
        runtimeProfile.hideFlags = HideFlags.DontSave;
        runtimeProfile.profileId = source != null ? source.profileId : "global-fade-to-black";
        runtimeProfile.easing = source != null ? source.easing : HexUiTransitionEasing.EaseOut;
        runtimeProfile.overrideInteractionUnlockTime = source != null && source.overrideInteractionUnlockTime;
        runtimeProfile.interactionUnlockTime = source != null ? source.interactionUnlockTime : 0f;
        runtimeProfile.fadeTracks = new List<HexUiTransitionFadeTrack>();

        IReadOnlyList<HexUiTransitionFadeTrack> tracks = source != null
            ? source.FadeTracks
            : CreateDefaultFadeToBlackTracks();
        for (int index = 0; index < tracks.Count; index++)
        {
            HexUiTransitionFadeTrack track = tracks[index];
            if (track == null || !track.HasTargetKey)
            {
                continue;
            }

            runtimeProfile.fadeTracks.Add(CopyFadeTrack(track, reverse));
        }

        return runtimeProfile;
    }

    private static IReadOnlyList<HexUiTransitionFadeTrack> CreateDefaultFadeToBlackTracks()
    {
        return new[]
        {
            new HexUiTransitionFadeTrack
            {
                targetKey = BlackoutTransitionTargetKey,
                startTime = 0f,
                duration = 0.35f,
                startOpacity = 0f,
                endOpacity = 1f,
                isRequired = true
            }
        };
    }

    private static HexUiTransitionFadeTrack CopyFadeTrack(HexUiTransitionFadeTrack source, bool reverse)
    {
        return new HexUiTransitionFadeTrack
        {
            targetKey = source.targetKey,
            startTime = source.startTime,
            duration = source.duration,
            startOpacity = reverse ? source.endOpacity : source.startOpacity,
            endOpacity = reverse ? source.startOpacity : source.endOpacity,
            isRequired = source.isRequired
        };
    }

    private void SetGlobalTransitionLayerActive(bool active)
    {
        if (gameplayUiRootController == null)
        {
            return;
        }

        gameplayUiRootController.SetLayerVisible(HexGameplayUiLayerId.GlobalTransition, active);
        gameplayUiRootController.SetLayerInteractive(HexGameplayUiLayerId.GlobalTransition, active);
    }

    private static void SetElementOpacity(VisualElement element, float opacity)
    {
        if (element != null)
        {
            element.style.opacity = Mathf.Clamp01(opacity);
        }
    }

    private static void RunCallback(Action callback)
    {
        callback?.Invoke();
    }

    private void ReleaseRuntimeProfile()
    {
        if (activeRuntimeProfile != null)
        {
            Destroy(activeRuntimeProfile);
            activeRuntimeProfile = null;
        }
    }

    private void LogDebug(string message, bool verbose = false)
    {
        gameplayUiRootController ??= HexGameplayUiRootController.ResolveShared(this, createIfMissing: false);
        if (gameplayUiRootController != null)
        {
            gameplayUiRootController.LogDiagnostic("GlobalTransition", message, this, verbose);
            return;
        }

        Debug.Log($"[GlobalTransition] {message}", this);
    }
}
