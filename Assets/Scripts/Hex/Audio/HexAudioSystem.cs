using System.Collections;
using UnityEngine;
using UnityEngine.Audio;

[DefaultExecutionOrder(-9500)]
[DisallowMultipleComponent]
public sealed class HexAudioSystem : MonoBehaviour
{
    private const string AudioLibraryResourcePath = "Audio/HexAudioLibrary";
    private const string MasterVolumeParameter = "MasterVolume";
    private const string MusicVolumeParameter = "MusicVolume";
    private const string SfxVolumeParameter = "SfxVolume";
    private const string MasterVolumePrefKey = "audio.masterVolume";
    private const string MusicVolumePrefKey = "audio.musicVolume";
    private const string SfxVolumePrefKey = "audio.sfxVolume";
    private const float MutedDecibels = -80f;

    [Header("Library")]
    [SerializeField] private HexAudioLibrary audioLibrary;

    [Header("Mixer")]
    [SerializeField] private AudioMixer audioMixer;
    [SerializeField] private AudioMixerGroup musicMixerGroup;
    [SerializeField] private AudioMixerGroup sfxMixerGroup;

    [Header("Sources")]
    [SerializeField] private AudioSource musicSourceA;
    [SerializeField] private AudioSource musicSourceB;
    [SerializeField] private AudioSource uiSfxSource;
    [SerializeField] private AudioSource gameplaySfxSource;

    [Header("Volume")]
    [SerializeField, Range(0f, 1f)] private float defaultMasterVolume = 1f;
    [SerializeField, Range(0f, 1f)] private float defaultMusicVolume = 1f;
    [SerializeField, Range(0f, 1f)] private float defaultSfxVolume = 1f;
    [SerializeField] private bool saveVolumesToPlayerPrefs = true;

    [Header("Music")]
    [SerializeField, Min(0f)] private float defaultMusicFadeSeconds = 0.75f;

    [Header("Debug")]
    [SerializeField] private HexMusicStage debugMusicStage = HexMusicStage.MainMenu;
    [SerializeField] private HexSfxId debugSfxId = HexSfxId.UiClick;
    [SerializeField] private bool logMissingClips = true;

    private static HexAudioSystem sharedInstance;
    private AudioSource activeMusicSource;
    private AudioSource inactiveMusicSource;
    private Coroutine musicFadeRoutine;
    private HexMusicStage currentMusicStage = HexMusicStage.None;
    private float masterVolume = 1f;
    private float musicVolume = 1f;
    private float sfxVolume = 1f;
    private bool isInitialized;

    public float MasterVolume => masterVolume;
    public float MusicVolume => musicVolume;
    public float SfxVolume => sfxVolume;
    public HexMusicStage CurrentMusicStage => currentMusicStage;

    public static HexAudioSystem ResolveShared(MonoBehaviour owner = null, bool createIfMissing = true)
    {
        if (sharedInstance != null)
        {
            return sharedInstance;
        }

        sharedInstance = FindAnyObjectByType<HexAudioSystem>();
        if (sharedInstance != null || !createIfMissing)
        {
            return sharedInstance;
        }

        GameObject audioObject = new("Audio System");
        if (owner != null)
        {
            audioObject.transform.SetParent(owner.transform.parent != null ? owner.transform.parent : owner.transform, false);
        }

        sharedInstance = audioObject.AddComponent<HexAudioSystem>();
        return sharedInstance;
    }

    public static HexMusicStage ResolveCurrentActMusicStage()
    {
        return HexActTransitionService.GetCurrentActNumber() switch
        {
            1 => HexMusicStage.Act1,
            2 => HexMusicStage.Act2,
            3 => HexMusicStage.Act3,
            _ => HexMusicStage.Act1
        };
    }

    private void Awake()
    {
        if (sharedInstance != null && sharedInstance != this)
        {
            Destroy(gameObject);
            return;
        }

        sharedInstance = this;
        EnsureInitialized();
    }

    private void OnDestroy()
    {
        if (sharedInstance == this)
        {
            sharedInstance = null;
        }
    }

    private void OnValidate()
    {
        defaultMasterVolume = Mathf.Clamp01(defaultMasterVolume);
        defaultMusicVolume = Mathf.Clamp01(defaultMusicVolume);
        defaultSfxVolume = Mathf.Clamp01(defaultSfxVolume);
        defaultMusicFadeSeconds = Mathf.Max(0f, defaultMusicFadeSeconds);
    }

    public void EnsureInitialized()
    {
        if (isInitialized)
        {
            return;
        }

        if (audioLibrary == null)
        {
            audioLibrary = Resources.Load<HexAudioLibrary>(AudioLibraryResourcePath);
        }
        musicSourceA = ResolveSource(musicSourceA, "Music Source A", true, musicMixerGroup);
        musicSourceB = ResolveSource(musicSourceB, "Music Source B", true, musicMixerGroup);
        uiSfxSource = ResolveSource(uiSfxSource, "UI SFX Source", false, sfxMixerGroup);
        gameplaySfxSource = ResolveSource(gameplaySfxSource, "Gameplay SFX Source", false, sfxMixerGroup);
        activeMusicSource = musicSourceA;
        inactiveMusicSource = musicSourceB;
        LoadVolumes();
        ApplyVolumes();
        isInitialized = true;
    }

    public void PlayMusicForCurrentAct(float? fadeSeconds = null)
    {
        PlayMusic(ResolveCurrentActMusicStage(), fadeSeconds);
    }

    public void PlayMusic(HexMusicStage stage, float? fadeSeconds = null)
    {
        EnsureInitialized();

        if (stage == HexMusicStage.None)
        {
            StopMusic(fadeSeconds);
            return;
        }

        if (!TryGetMusicClip(stage, out HexMusicEntry entry) || entry.clip == null)
        {
            currentMusicStage = stage;
            StopMusic(fadeSeconds);
            LogMissing($"No music clip configured for stage '{stage}'.");
            return;
        }

        if (currentMusicStage == stage && activeMusicSource != null && activeMusicSource.clip == entry.clip && activeMusicSource.isPlaying)
        {
            return;
        }

        currentMusicStage = stage;
        float resolvedFade = ResolveFadeSeconds(fadeSeconds, entry.fadeInSeconds);
        StartMusicTransition(entry, resolvedFade);
    }

    public void StopMusic(float? fadeSeconds = null)
    {
        EnsureInitialized();
        currentMusicStage = HexMusicStage.None;
        float resolvedFade = Mathf.Max(0f, fadeSeconds ?? defaultMusicFadeSeconds);

        if (musicFadeRoutine != null)
        {
            StopCoroutine(musicFadeRoutine);
            musicFadeRoutine = null;
        }

        if (resolvedFade <= 0f)
        {
            StopMusicSource(musicSourceA);
            StopMusicSource(musicSourceB);
            return;
        }

        musicFadeRoutine = StartCoroutine(FadeOutMusic(resolvedFade));
    }

    public void PlaySfx(HexSfxId id)
    {
        PlaySfx(id, 1f, useGameplaySource: false);
    }

    public void PlaySfx(HexSfxId id, float volumeScale)
    {
        PlaySfx(id, volumeScale, useGameplaySource: false);
    }

    public void PlayGameplaySfx(HexSfxId id, float volumeScale = 1f)
    {
        PlaySfx(id, volumeScale, useGameplaySource: true);
    }

    public void SetMasterVolume(float normalized)
    {
        masterVolume = Mathf.Clamp01(normalized);
        PersistVolume(MasterVolumePrefKey, masterVolume);
        ApplyVolumes();
    }

    public void SetMusicVolume(float normalized)
    {
        musicVolume = Mathf.Clamp01(normalized);
        PersistVolume(MusicVolumePrefKey, musicVolume);
        ApplyVolumes();
    }

    public void SetSfxVolume(float normalized)
    {
        sfxVolume = Mathf.Clamp01(normalized);
        PersistVolume(SfxVolumePrefKey, sfxVolume);
        ApplyVolumes();
    }

    [ContextMenu("Debug/Play Debug Music Stage")]
    private void DebugPlayMusicStage()
    {
        PlayMusic(debugMusicStage);
    }

    [ContextMenu("Debug/Stop Music")]
    private void DebugStopMusic()
    {
        StopMusic();
    }

    [ContextMenu("Debug/Play Debug SFX")]
    private void DebugPlaySfx()
    {
        PlaySfx(debugSfxId);
    }

    [ContextMenu("Debug/Reset Volumes")]
    private void DebugResetVolumes()
    {
        SetMasterVolume(defaultMasterVolume);
        SetMusicVolume(defaultMusicVolume);
        SetSfxVolume(defaultSfxVolume);
    }

    private void PlaySfx(HexSfxId id, float volumeScale, bool useGameplaySource)
    {
        EnsureInitialized();

        if (!TryGetSfxClip(id, out HexSfxEntry entry) || entry.clip == null)
        {
            LogMissing($"No SFX clip configured for id '{id}'.");
            return;
        }

        AudioSource source = useGameplaySource ? gameplaySfxSource : uiSfxSource;
        if (source == null)
        {
            return;
        }

        if (entry.interruptSameId)
        {
            source.Stop();
        }

        source.pitch = ResolvePitch(entry.pitchRange);
        source.PlayOneShot(entry.clip, Mathf.Clamp01(volumeScale) * Mathf.Clamp01(entry.volume) * ResolveSfxSourceVolumeMultiplier());
        source.pitch = 1f;
    }

    private void StartMusicTransition(HexMusicEntry entry, float fadeSeconds)
    {
        if (musicFadeRoutine != null)
        {
            StopCoroutine(musicFadeRoutine);
            musicFadeRoutine = null;
        }

        if (activeMusicSource == null || inactiveMusicSource == null)
        {
            activeMusicSource = musicSourceA;
            inactiveMusicSource = musicSourceB;
        }

        AudioSource from = activeMusicSource;
        AudioSource to = inactiveMusicSource;
        to.clip = entry.clip;
        to.loop = entry.loop;
        to.volume = 0f;
        to.Play();

        musicFadeRoutine = StartCoroutine(CrossfadeMusic(from, to, entry, fadeSeconds));
    }

    private IEnumerator CrossfadeMusic(AudioSource from, AudioSource to, HexMusicEntry entry, float fadeSeconds)
    {
        float elapsed = 0f;
        float fromStartVolume = from != null ? from.volume : 0f;
        float targetVolume = ResolveMusicSourceVolume(entry.volume);

        if (fadeSeconds <= 0f)
        {
            if (from != null)
            {
                StopMusicSource(from);
            }

            to.volume = targetVolume;
            SwapMusicSources(to);
            musicFadeRoutine = null;
            yield break;
        }

        while (elapsed < fadeSeconds)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / fadeSeconds);
            if (from != null)
            {
                from.volume = Mathf.Lerp(fromStartVolume, 0f, t);
            }

            to.volume = Mathf.Lerp(0f, targetVolume, t);
            yield return null;
        }

        if (from != null)
        {
            StopMusicSource(from);
        }

        to.volume = targetVolume;
        SwapMusicSources(to);
        musicFadeRoutine = null;
    }

    private IEnumerator FadeOutMusic(float fadeSeconds)
    {
        float elapsed = 0f;
        float aStart = musicSourceA != null ? musicSourceA.volume : 0f;
        float bStart = musicSourceB != null ? musicSourceB.volume : 0f;

        while (elapsed < fadeSeconds)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / fadeSeconds);
            if (musicSourceA != null)
            {
                musicSourceA.volume = Mathf.Lerp(aStart, 0f, t);
            }

            if (musicSourceB != null)
            {
                musicSourceB.volume = Mathf.Lerp(bStart, 0f, t);
            }

            yield return null;
        }

        StopMusicSource(musicSourceA);
        StopMusicSource(musicSourceB);
        musicFadeRoutine = null;
    }

    private AudioSource ResolveSource(AudioSource source, string childName, bool loop, AudioMixerGroup mixerGroup)
    {
        if (source == null)
        {
            Transform child = transform.Find(childName);
            GameObject sourceObject = child != null ? child.gameObject : new GameObject(childName);
            sourceObject.transform.SetParent(transform, false);
            source = sourceObject.GetComponent<AudioSource>();
            if (source == null)
            {
                source = sourceObject.AddComponent<AudioSource>();
            }
        }

        source.playOnAwake = false;
        source.loop = loop;
        source.spatialBlend = 0f;
        source.outputAudioMixerGroup = mixerGroup;
        return source;
    }

    private bool TryGetMusicClip(HexMusicStage stage, out HexMusicEntry entry)
    {
        if (audioLibrary != null && audioLibrary.TryGetMusic(stage, out entry))
        {
            return true;
        }

        entry = null;
        return false;
    }

    private bool TryGetSfxClip(HexSfxId id, out HexSfxEntry entry)
    {
        if (audioLibrary != null && audioLibrary.TryGetSfx(id, out entry))
        {
            return true;
        }

        entry = null;
        return false;
    }

    private void ApplyVolumes()
    {
        if (audioMixer != null)
        {
            audioMixer.SetFloat(MasterVolumeParameter, ToDecibels(masterVolume));
            audioMixer.SetFloat(MusicVolumeParameter, ToDecibels(musicVolume));
            audioMixer.SetFloat(SfxVolumeParameter, ToDecibels(sfxVolume));
        }

        if (activeMusicSource != null && activeMusicSource.isPlaying && audioLibrary != null && audioLibrary.TryGetMusic(currentMusicStage, out HexMusicEntry entry))
        {
            activeMusicSource.volume = ResolveMusicSourceVolume(entry.volume);
        }
    }

    private void LoadVolumes()
    {
        masterVolume = LoadVolume(MasterVolumePrefKey, defaultMasterVolume);
        musicVolume = LoadVolume(MusicVolumePrefKey, defaultMusicVolume);
        sfxVolume = LoadVolume(SfxVolumePrefKey, defaultSfxVolume);
    }

    private float LoadVolume(string key, float defaultValue)
    {
        return saveVolumesToPlayerPrefs && PlayerPrefs.HasKey(key)
            ? Mathf.Clamp01(PlayerPrefs.GetFloat(key, defaultValue))
            : Mathf.Clamp01(defaultValue);
    }

    private void PersistVolume(string key, float value)
    {
        if (!saveVolumesToPlayerPrefs)
        {
            return;
        }

        PlayerPrefs.SetFloat(key, Mathf.Clamp01(value));
        PlayerPrefs.Save();
    }

    private float ResolveMusicSourceVolume(float entryVolume)
    {
        float clampedEntryVolume = Mathf.Clamp01(entryVolume);
        return musicMixerGroup != null ? clampedEntryVolume : clampedEntryVolume * masterVolume * musicVolume;
    }

    private float ResolveSfxSourceVolumeMultiplier()
    {
        return sfxMixerGroup != null ? 1f : masterVolume * sfxVolume;
    }

    private float ResolveFadeSeconds(float? fadeSeconds, float entryFadeSeconds)
    {
        return Mathf.Max(0f, fadeSeconds ?? (entryFadeSeconds > 0f ? entryFadeSeconds : defaultMusicFadeSeconds));
    }

    private static float ResolvePitch(Vector2 pitchRange)
    {
        float min = Mathf.Max(0.01f, Mathf.Min(pitchRange.x, pitchRange.y));
        float max = Mathf.Max(min, Mathf.Max(pitchRange.x, pitchRange.y));
        return Mathf.Approximately(min, max) ? min : Random.Range(min, max);
    }

    private static float ToDecibels(float normalized)
    {
        float clamped = Mathf.Clamp01(normalized);
        return clamped <= 0.0001f ? MutedDecibels : Mathf.Log10(clamped) * 20f;
    }

    private static void StopMusicSource(AudioSource source)
    {
        if (source == null)
        {
            return;
        }

        source.Stop();
        source.clip = null;
        source.volume = 0f;
    }

    private void SwapMusicSources(AudioSource newActiveSource)
    {
        activeMusicSource = newActiveSource;
        inactiveMusicSource = activeMusicSource == musicSourceA ? musicSourceB : musicSourceA;
    }

    private void LogMissing(string message)
    {
        if (logMissingClips)
        {
            Debug.Log($"[Audio] {message}", this);
        }
    }
}
