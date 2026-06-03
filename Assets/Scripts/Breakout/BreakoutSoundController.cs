using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class BreakoutSoundController : MonoBehaviour
{
    [System.Serializable]
    private class OneShotSoundEvent
    {
        [SerializeField] private AudioClip[] clips;
        [SerializeField, Range(0f, 1f)] private float volume = 1f;
        [SerializeField, Min(0.01f)] private float pitchMin = 0.95f;
        [SerializeField, Min(0.01f)] private float pitchMax = 1.05f;

        public AudioClip[] Clips => clips;
        public float Volume => volume;
        public float PitchMin => pitchMin;
        public float PitchMax => pitchMax;
    }

    private static BreakoutSoundController instance;

    [Header("Global Volume")]
    [SerializeField, Range(0f, 1f)] private float masterVolume = 1f;
    [SerializeField, Range(0f, 1f)] private float sfxVolume = 1f;

    [Header("Audio Sources")]
    [SerializeField, Min(1)] private int initialSourcePoolSize = 6;
    [SerializeField, Min(1)] private int maxSimultaneousSfx = 20;

    [Header("Hit Sounds")]
    [SerializeField] private OneShotSoundEvent basicBrickHit = new OneShotSoundEvent();
    [SerializeField] private OneShotSoundEvent superEffectiveBrickHit = new OneShotSoundEvent();
    [SerializeField] private OneShotSoundEvent wallHit = new OneShotSoundEvent();
    [SerializeField] private OneShotSoundEvent paddleHit = new OneShotSoundEvent();

    [Header("Pickup Sounds")]
    [SerializeField] private OneShotSoundEvent itemPickup = new OneShotSoundEvent();
    [SerializeField] private OneShotSoundEvent scrapPickup = new OneShotSoundEvent();

    [Header("Gameplay Sounds")]
    [SerializeField] private OneShotSoundEvent ballDispense = new OneShotSoundEvent();
    [SerializeField] private OneShotSoundEvent levelWin = new OneShotSoundEvent();
    [SerializeField] private OneShotSoundEvent damageTaken = new OneShotSoundEvent();
    [SerializeField] private OneShotSoundEvent healed = new OneShotSoundEvent();

    private readonly List<AudioSource> sourcePool = new List<AudioSource>();

    public static BreakoutSoundController Instance
    {
        get
        {
            if (instance != null)
            {
                return instance;
            }

            instance = FindFirstObjectByType<BreakoutSoundController>();
            if (instance != null)
            {
                return instance;
            }

            GameObject go = new GameObject("BreakoutSoundController");
            instance = go.AddComponent<BreakoutSoundController>();
            return instance;
        }
    }

    public float MasterVolume
    {
        get => masterVolume;
        set => masterVolume = Mathf.Clamp01(value);
    }

    public float SfxVolume
    {
        get => sfxVolume;
        set => sfxVolume = Mathf.Clamp01(value);
    }

    public static void PlayBasicBrickHitSfx()
    {
        Instance.PlayEvent(Instance.basicBrickHit);
    }

    public static void PlaySuperEffectiveBrickHitSfx()
    {
        Instance.PlayEvent(Instance.superEffectiveBrickHit);
    }

    public static void PlayWallHitSfx()
    {
        Instance.PlayEvent(Instance.wallHit);
    }

    public static void PlayPaddleHitSfx()
    {
        Instance.PlayEvent(Instance.paddleHit);
    }

    public static void PlayItemPickupSfx()
    {
        Instance.PlayEvent(Instance.itemPickup);
    }

    public static void PlayScrapPickupSfx()
    {
        Instance.PlayEvent(Instance.scrapPickup);
    }

    public static void PlayBallDispenseSfx()
    {
        Instance.PlayEvent(Instance.ballDispense);
    }

    public static void PlayLevelWinSfx()
    {
        Instance.PlayEvent(Instance.levelWin);
    }

    public static void PlayDamageTakenSfx()
    {
        Instance.PlayEvent(Instance.damageTaken);
    }

    public static void PlayHealedSfx()
    {
        Instance.PlayEvent(Instance.healed);
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        EnsurePoolInitialized();
        PreloadReferencedClips();
    }

    private void PreloadReferencedClips()
    {
        HashSet<AudioClip> uniqueClips = new HashSet<AudioClip>();
        CollectClips(uniqueClips, basicBrickHit);
        CollectClips(uniqueClips, superEffectiveBrickHit);
        CollectClips(uniqueClips, wallHit);
        CollectClips(uniqueClips, paddleHit);
        CollectClips(uniqueClips, itemPickup);
        CollectClips(uniqueClips, scrapPickup);
        CollectClips(uniqueClips, ballDispense);
        CollectClips(uniqueClips, levelWin);
        CollectClips(uniqueClips, damageTaken);
        CollectClips(uniqueClips, healed);

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
        GameObject child = new GameObject($"SfxSource_{index}");
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
        source.volume = Mathf.Clamp01(masterVolume) * Mathf.Clamp01(sfxVolume) * Mathf.Clamp01(soundEvent.Volume);
        source.clip = clip;
        source.Play();
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
