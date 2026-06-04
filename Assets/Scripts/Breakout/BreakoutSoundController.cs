using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class BreakoutSoundController : MonoBehaviour
{
    [System.Serializable]
    private class OneShotSoundEvent
    {
        [SerializeField] private AudioClip[] clips;
        [SerializeField] private GameSettings.AudioBus volumeBus = GameSettings.AudioBus.Sfx;
        [SerializeField, Range(0f, 1f)] private float volume = 1f;
        [SerializeField, Min(0.01f)] private float pitchMin = 0.95f;
        [SerializeField, Min(0.01f)] private float pitchMax = 1.05f;

        public AudioClip[] Clips => clips;
        public GameSettings.AudioBus VolumeBus => volumeBus;
        public float Volume => volume;
        public float PitchMin => pitchMin;
        public float PitchMax => pitchMax;

    }

    private static BreakoutSoundController instance;

    [Header("Audio Sources")]
    [SerializeField, Min(1)] private int initialSourcePoolSize = 6;
    [SerializeField, Min(1)] private int maxSimultaneousSfx = 20;

    [Header("Hit Sounds")]
    [SerializeField] private OneShotSoundEvent basicBrickHit = new OneShotSoundEvent();
    [SerializeField] private OneShotSoundEvent superEffectiveBrickHit = new OneShotSoundEvent();
    [SerializeField] private OneShotSoundEvent brickDestroyed = new OneShotSoundEvent();
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
    [SerializeField] private OneShotSoundEvent pauseMenuOpen = new OneShotSoundEvent();
    [SerializeField] private OneShotSoundEvent pauseResumeCountdownTick = new OneShotSoundEvent();
    [SerializeField] private OneShotSoundEvent itemSold = new OneShotSoundEvent();
    [SerializeField] private OneShotSoundEvent itemBought = new OneShotSoundEvent();
    [SerializeField] private OneShotSoundEvent shopReroll = new OneShotSoundEvent();
    [SerializeField] private OneShotSoundEvent lifeLost = new OneShotSoundEvent();

    [Header("Pause Muffle")]
    [SerializeField, Range(0f, 1f)] private float pausedSfxVolumeMultiplier = 0.45f;
    [SerializeField, Min(10f)] private float pausedLowPassCutoff = 900f;
    [SerializeField, Min(10f)] private float normalLowPassCutoff = 22000f;

    private readonly List<AudioSource> sourcePool = new List<AudioSource>();
    private bool isPauseMuffled;

    public static BreakoutSoundController Instance => TryGetExistingInstance();

    public static void PlayBasicBrickHitSfx()
    {
        PlayConfiguredEvent(controller => controller.basicBrickHit);
    }

    public static void PlaySuperEffectiveBrickHitSfx()
    {
        PlayConfiguredEvent(controller => controller.superEffectiveBrickHit);
    }

    public static void PlayBrickDestroyedSfx()
    {
        PlayConfiguredEvent(controller => controller.brickDestroyed);
    }

    public static void PlayWallHitSfx()
    {
        PlayConfiguredEvent(controller => controller.wallHit);
    }

    public static void PlayPaddleHitSfx()
    {
        PlayConfiguredEvent(controller => controller.paddleHit);
    }

    public static void PlayItemPickupSfx()
    {
        PlayConfiguredEvent(controller => controller.itemPickup);
    }

    public static void PlayScrapPickupSfx()
    {
        PlayConfiguredEvent(controller => controller.scrapPickup);
    }

    public static void PlayBallDispenseSfx()
    {
        PlayConfiguredEvent(controller => controller.ballDispense);
    }

    public static void PlayLevelWinSfx()
    {
        PlayConfiguredEvent(controller => controller.levelWin);
    }

    public static void PlayDamageTakenSfx()
    {
        PlayConfiguredEvent(controller => controller.damageTaken);
    }

    public static void PlayHealedSfx()
    {
        PlayConfiguredEvent(controller => controller.healed);
    }

    public static void PlayPauseMenuOpenSfx()
    {
        PlayConfiguredEvent(controller => controller.pauseMenuOpen);
    }

    public static void PlayPauseResumeCountdownTickSfx()
    {
        PlayConfiguredEvent(controller => controller.pauseResumeCountdownTick);
    }

    public static void PlayItemSoldSfx()
    {
        PlayConfiguredEvent(controller => controller.itemSold);
    }

    public static void PlayItemBoughtSfx()
    {
        PlayConfiguredEvent(controller => controller.itemBought);
    }

    public static void PlayShopRerollSfx()
    {
        PlayConfiguredEvent(controller => controller.shopReroll);
    }

    public static void PlayLifeLostSfx()
    {
        PlayConfiguredEvent(controller => controller.lifeLost);
    }

    public static void SetPauseMuffled(bool isMuffled)
    {
        BreakoutSoundController existingInstance = TryGetExistingInstance();
        if (existingInstance == null)
        {
            return;
        }

        existingInstance.isPauseMuffled = isMuffled;
        existingInstance.ApplyPauseMuffleToAllSources();
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

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }

    private void PreloadReferencedClips()
    {
        HashSet<AudioClip> uniqueClips = new HashSet<AudioClip>();
        CollectClips(uniqueClips, basicBrickHit);
        CollectClips(uniqueClips, superEffectiveBrickHit);
        CollectClips(uniqueClips, brickDestroyed);
        CollectClips(uniqueClips, wallHit);
        CollectClips(uniqueClips, paddleHit);
        CollectClips(uniqueClips, itemPickup);
        CollectClips(uniqueClips, scrapPickup);
        CollectClips(uniqueClips, ballDispense);
        CollectClips(uniqueClips, levelWin);
        CollectClips(uniqueClips, damageTaken);
        CollectClips(uniqueClips, healed);
        CollectClips(uniqueClips, pauseMenuOpen);
        CollectClips(uniqueClips, pauseResumeCountdownTick);
        CollectClips(uniqueClips, itemSold);
        CollectClips(uniqueClips, itemBought);
        CollectClips(uniqueClips, shopReroll);
        CollectClips(uniqueClips, lifeLost);

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

        AudioLowPassFilter lowPassFilter = child.AddComponent<AudioLowPassFilter>();
        lowPassFilter.cutoffFrequency = isPauseMuffled ? pausedLowPassCutoff : normalLowPassCutoff;
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
        float pauseVolumeMultiplier = isPauseMuffled ? Mathf.Clamp01(pausedSfxVolumeMultiplier) : 1f;
        float globalVolumeMultiplier = GameSettings.GetCombinedVolumeMultiplier(soundEvent.VolumeBus);
        source.volume = Mathf.Clamp01(soundEvent.Volume) * globalVolumeMultiplier * pauseVolumeMultiplier;
        source.clip = clip;
        source.Play();
    }

    private static BreakoutSoundController TryGetExistingInstance()
    {
        if (instance != null)
        {
            return instance;
        }

        instance = FindFirstObjectByType<BreakoutSoundController>();
        return instance;
    }

    private static void PlayConfiguredEvent(System.Func<BreakoutSoundController, OneShotSoundEvent> eventSelector)
    {
        BreakoutSoundController existingInstance = TryGetExistingInstance();
        if (existingInstance == null || eventSelector == null)
        {
            return;
        }

        existingInstance.PlayEvent(eventSelector(existingInstance));
    }

    private void ApplyPauseMuffleToAllSources()
    {
        float cutoff = isPauseMuffled ? pausedLowPassCutoff : normalLowPassCutoff;
        for (int i = 0; i < sourcePool.Count; i++)
        {
            AudioSource source = sourcePool[i];
            if (source == null)
            {
                continue;
            }

            AudioLowPassFilter lowPassFilter = source.GetComponent<AudioLowPassFilter>();
            if (lowPassFilter != null)
            {
                lowPassFilter.cutoffFrequency = cutoff;
            }
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
