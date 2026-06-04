using System;
using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class MusicController : MonoBehaviour
{
    private const float UnmuffledLowPassCutoff = 22000f;

    private static MusicController instance;

    [Header("Music")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioClip musicClip;
    [SerializeField] private bool playOnStart = true;
    [SerializeField] private bool loop = true;

    [Header("Volume")]
    [SerializeField, Range(0f, 1f)] private float targetVolume = 1f;
    [SerializeField, Min(0f)] private float fadeInDuration = 1.5f;
    [SerializeField, Min(0f)] private float fadeOutDuration = 1f;

    [Header("Lifecycle")]
    [SerializeField] private bool persistAcrossScenes = true;

    [Header("Pause Muffle")]
    [SerializeField] private bool enablePauseMuffle = true;
    [SerializeField, Range(300f, 22000f)] private float pauseMuffleLowPassCutoff = 1200f;

    private Coroutine fadeRoutine;
    private AudioLowPassFilter musicLowPassFilter;
    private bool isPauseMuffled;

    public static MusicController Instance => instance;

    public static void SetPauseMuffled(bool muffled)
    {
        if (instance == null)
        {
            return;
        }

        instance.ApplyPauseMuffle(muffled);
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            if (!instance.persistAcrossScenes)
            {
                instance = this;
                EnsureAudioSource();
                ConfigureSource();
                ApplyPauseMuffle(isPauseMuffled);

                if (persistAcrossScenes)
                {
                    DontDestroyOnLoad(gameObject);
                }

                return;
            }

            instance.CopyConfigurationFrom(this);
            Destroy(this);
            return;
        }

        instance = this;

        if (persistAcrossScenes)
        {
            DontDestroyOnLoad(gameObject);
        }

        EnsureAudioSource();
        ConfigureSource();
        ApplyPauseMuffle(isPauseMuffled);
    }

    private void CopyConfigurationFrom(MusicController other)
    {
        if (other == null)
        {
            return;
        }

        musicClip = other.musicClip;
        playOnStart = other.playOnStart;
        loop = other.loop;
        targetVolume = other.targetVolume;
        fadeInDuration = other.fadeInDuration;
        fadeOutDuration = other.fadeOutDuration;
        enablePauseMuffle = other.enablePauseMuffle;
        pauseMuffleLowPassCutoff = other.pauseMuffleLowPassCutoff;

        EnsureAudioSource();
        ConfigureSource();
        ApplyPauseMuffle(isPauseMuffled);

        if (playOnStart && musicClip != null)
        {
            PlayWithFadeIn();
            return;
        }

        if (fadeRoutine != null)
        {
            StopCoroutine(fadeRoutine);
            fadeRoutine = null;
        }

        if (musicSource != null)
        {
            musicSource.Stop();
            musicSource.clip = musicClip;
            musicSource.volume = 0f;
        }
    }

    private void Start()
    {
        if (!playOnStart)
        {
            return;
        }

        PlayWithFadeIn();
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }

    public void PlayWithFadeIn()
    {
        if (musicSource == null)
        {
            return;
        }

        if (musicClip != null && musicSource.clip != musicClip)
        {
            musicSource.clip = musicClip;
        }

        musicSource.loop = loop;

        if (fadeRoutine != null)
        {
            StopCoroutine(fadeRoutine);
            fadeRoutine = null;
        }

        float clampedTargetVolume = Mathf.Clamp01(targetVolume);
        if (fadeInDuration <= 0f)
        {
            musicSource.volume = clampedTargetVolume;
            if (!musicSource.isPlaying)
            {
                musicSource.Play();
            }

            return;
        }

        fadeRoutine = StartCoroutine(FadeInCoroutine(clampedTargetVolume, fadeInDuration));
    }

    public static void FadeOutBeforeSceneChange(Action onComplete)
    {
        if (instance == null || instance.musicSource == null)
        {
            onComplete?.Invoke();
            return;
        }

        instance.BeginFadeOut(onComplete);
    }

    private void BeginFadeOut(Action onComplete)
    {
        if (fadeRoutine != null)
        {
            StopCoroutine(fadeRoutine);
            fadeRoutine = null;
        }

        if (fadeOutDuration <= 0f || !musicSource.isPlaying)
        {
            musicSource.volume = 0f;
            musicSource.Stop();
            onComplete?.Invoke();
            return;
        }

        fadeRoutine = StartCoroutine(FadeOutCoroutine(fadeOutDuration, onComplete));
    }

    private IEnumerator FadeInCoroutine(float destinationVolume, float duration)
    {
        if (!musicSource.isPlaying)
        {
            musicSource.volume = 0f;
            musicSource.Play();
        }

        float startVolume = musicSource.volume;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            musicSource.volume = Mathf.Lerp(startVolume, destinationVolume, t);
            yield return null;
        }

        musicSource.volume = destinationVolume;
        fadeRoutine = null;
    }

    private IEnumerator FadeOutCoroutine(float duration, Action onComplete)
    {
        float startVolume = musicSource.volume;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            musicSource.volume = Mathf.Lerp(startVolume, 0f, t);
            yield return null;
        }

        musicSource.volume = 0f;
        musicSource.Stop();
        fadeRoutine = null;
        onComplete?.Invoke();
    }

    private void EnsureAudioSource()
    {
        if (musicSource != null)
        {
            return;
        }

        musicSource = GetComponent<AudioSource>();
        if (musicSource == null)
        {
            musicSource = gameObject.AddComponent<AudioSource>();
        }
    }

    private void ConfigureSource()
    {
        if (musicSource == null)
        {
            return;
        }

        if (musicClip != null)
        {
            musicSource.clip = musicClip;
        }

        musicSource.loop = loop;
        musicSource.playOnAwake = false;
        musicSource.spatialBlend = 0f;
        musicSource.dopplerLevel = 0f;
        musicSource.rolloffMode = AudioRolloffMode.Linear;
        musicSource.volume = 0f;

        musicLowPassFilter = musicSource.GetComponent<AudioLowPassFilter>();
        if (musicLowPassFilter == null)
        {
            musicLowPassFilter = musicSource.gameObject.AddComponent<AudioLowPassFilter>();
        }

        musicLowPassFilter.enabled = false;
        musicLowPassFilter.cutoffFrequency = UnmuffledLowPassCutoff;
    }

    private void ApplyPauseMuffle(bool muffled)
    {
        isPauseMuffled = muffled;
        if (!enablePauseMuffle || musicLowPassFilter == null)
        {
            return;
        }

        musicLowPassFilter.enabled = muffled;
        musicLowPassFilter.cutoffFrequency = muffled
            ? Mathf.Clamp(pauseMuffleLowPassCutoff, 300f, UnmuffledLowPassCutoff)
            : UnmuffledLowPassCutoff;
    }
}