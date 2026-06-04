using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class UISoundController : MonoBehaviour
{
    [System.Serializable]
    private class OneShotSoundEvent
    {
        [SerializeField] private AudioClip[] clips;
        [SerializeField] private GameSettings.AudioBus volumeBus = GameSettings.AudioBus.Sfx;
        [SerializeField, Range(0f, 1f)] private float volume = 1f;
        [SerializeField, Min(0.01f)] private float pitchMin = 0.98f;
        [SerializeField, Min(0.01f)] private float pitchMax = 1.02f;

        public AudioClip[] Clips => clips;
        public GameSettings.AudioBus VolumeBus => volumeBus;
        public float Volume => volume;
        public float PitchMin => pitchMin;
        public float PitchMax => pitchMax;

        public OneShotSoundEvent Clone()
        {
            return new OneShotSoundEvent
            {
                clips = clips != null ? (AudioClip[])clips.Clone() : null,
                volume = volume,
                pitchMin = pitchMin,
                pitchMax = pitchMax
            };
        }
    }

    private static UISoundController instance;

    [Header("Audio Sources")]
    [SerializeField, Min(1)] private int initialSourcePoolSize = 4;
    [SerializeField, Min(1)] private int maxSimultaneousSfx = 12;

    [Header("UI Sounds")]
    [SerializeField] private OneShotSoundEvent buttonClick = new OneShotSoundEvent();

    private readonly List<AudioSource> sourcePool = new List<AudioSource>();

    public static UISoundController Instance => TryGetExistingInstance();

    public static void PlayButtonClickSfx()
    {
        UISoundController existingInstance = TryGetExistingInstance();
        if (existingInstance == null)
        {
            return;
        }

        existingInstance.PlayEvent(existingInstance.buttonClick);
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(this);
            return;
        }

        instance = this;
        EnsurePoolInitialized();
        PreloadReferencedClips();
    }

    private void PreloadReferencedClips()
    {
        HashSet<AudioClip> uniqueClips = new HashSet<AudioClip>();
        CollectClips(uniqueClips, buttonClick);

        foreach (AudioClip clip in uniqueClips)
        {
            if (clip == null || clip.loadState == AudioDataLoadState.Loaded || clip.loadState == AudioDataLoadState.Loading)
            {
                continue;
            }

            clip.LoadAudioData();
        }
    }

    private static void CollectClips(HashSet<AudioClip> uniqueClips, OneShotSoundEvent soundEvent)
    {
        if (uniqueClips == null || soundEvent == null || soundEvent.Clips == null)
        {
            return;
        }

        for (int i = 0; i < soundEvent.Clips.Length; i++)
        {
            AudioClip clip = soundEvent.Clips[i];
            if (clip != null)
            {
                uniqueClips.Add(clip);
            }
        }
    }

    private void EnsurePoolInitialized()
    {
        if (sourcePool.Count > 0)
        {
            return;
        }

        int sourceCount = Mathf.Clamp(initialSourcePoolSize, 1, Mathf.Max(1, maxSimultaneousSfx));
        for (int i = 0; i < sourceCount; i++)
        {
            sourcePool.Add(CreateAudioSource(i));
        }
    }

    private AudioSource CreateAudioSource(int index)
    {
        GameObject child = new GameObject($"UiSfxSource_{index}");
        child.transform.SetParent(transform, false);

        AudioSource source = child.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = false;
        source.spatialBlend = 0f;
        source.dopplerLevel = 0f;
        source.rolloffMode = AudioRolloffMode.Linear;
        return source;
    }

    private AudioSource GetAvailableSource()
    {
        EnsurePoolInitialized();

        for (int i = 0; i < sourcePool.Count; i++)
        {
            AudioSource source = sourcePool[i];
            if (source != null && !source.isPlaying)
            {
                return source;
            }
        }

        if (sourcePool.Count < Mathf.Max(1, maxSimultaneousSfx))
        {
            AudioSource source = CreateAudioSource(sourcePool.Count);
            sourcePool.Add(source);
            return source;
        }

        return null;
    }

    private void PlayEvent(OneShotSoundEvent soundEvent)
    {
        if (soundEvent == null)
        {
            return;
        }

        AudioClip clip = PickRandomClip(soundEvent.Clips);
        if (clip == null)
        {
            return;
        }

        AudioSource source = GetAvailableSource();
        if (source == null)
        {
            return;
        }

        float minPitch = Mathf.Max(0.01f, soundEvent.PitchMin);
        float maxPitch = Mathf.Max(minPitch, soundEvent.PitchMax);

        source.pitch = Random.Range(minPitch, maxPitch);
        float globalVolumeMultiplier = GameSettings.GetCombinedVolumeMultiplier(soundEvent.VolumeBus);
        source.volume = Mathf.Clamp01(soundEvent.Volume) * globalVolumeMultiplier;
        source.clip = clip;
        source.Play();
    }

    private static UISoundController TryGetExistingInstance()
    {
        if (instance != null)
        {
            return instance;
        }

        instance = FindFirstObjectByType<UISoundController>();
        return instance;
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }

    private static AudioClip PickRandomClip(AudioClip[] clips)
    {
        if (clips == null || clips.Length == 0)
        {
            return null;
        }

        int nonNullCount = 0;
        for (int i = 0; i < clips.Length; i++)
        {
            if (clips[i] != null)
            {
                nonNullCount++;
            }
        }

        if (nonNullCount == 0)
        {
            return null;
        }

        int selected = Random.Range(0, nonNullCount);
        for (int i = 0; i < clips.Length; i++)
        {
            if (clips[i] == null)
            {
                continue;
            }

            if (selected == 0)
            {
                return clips[i];
            }

            selected--;
        }

        return null;
    }
}