using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class FactorySoundController : MonoBehaviour
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

    [System.Serializable]
    private class AmbientLayer
    {
        [SerializeField] private AudioClip clip;
        [SerializeField, Min(1)] private int minimumBuildingCount = 1;
        [SerializeField, Range(0f, 1f)] private float volume = 0.25f;
        [SerializeField, Min(0.01f)] private float fadeInSpeed = 1.5f;
        [SerializeField, Min(0.01f)] private float fadeOutSpeed = 1.5f;

        public AudioClip Clip => clip;
        public int MinimumBuildingCount => minimumBuildingCount;
        public float Volume => volume;
        public float FadeInSpeed => fadeInSpeed;
        public float FadeOutSpeed => fadeOutSpeed;
    }

    private static FactorySoundController instance;

    [Header("Global Volume")]
    [SerializeField, Range(0f, 1f)] private float masterVolume = 1f;
    [SerializeField, Range(0f, 1f)] private float sfxVolume = 1f;
    [SerializeField, Range(0f, 1f)] private float ambientVolume = 1f;

    [Header("Audio Sources")]
    [SerializeField, Min(1)] private int initialSourcePoolSize = 6;
    [SerializeField, Min(1)] private int maxSimultaneousSfx = 20;

    [Header("Factory Sounds")]
    [SerializeField] private OneShotSoundEvent smallBuildingPlaced = new OneShotSoundEvent();
    [SerializeField] private OneShotSoundEvent mediumBuildingPlaced = new OneShotSoundEvent();
    [SerializeField] private OneShotSoundEvent largeBuildingPlaced = new OneShotSoundEvent();
    [SerializeField] private OneShotSoundEvent buildingRemoved = new OneShotSoundEvent();
    [SerializeField] private OneShotSoundEvent uiClick = new OneShotSoundEvent();
    [SerializeField] private OneShotSoundEvent ballCreated = new OneShotSoundEvent();

    [Header("Ambient Layers")]
    [SerializeField] private AmbientLayer[] ambientLayers;

    private readonly List<AudioSource> sourcePool = new List<AudioSource>();
    private readonly List<AudioSource> ambientSourcePool = new List<AudioSource>();

    public static FactorySoundController Instance
    {
        get
        {
            if (instance != null)
            {
                return instance;
            }

            instance = FindFirstObjectByType<FactorySoundController>();
            if (instance != null)
            {
                return instance;
            }

            GameObject go = new GameObject("FactorySoundController");
            instance = go.AddComponent<FactorySoundController>();
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

    public float AmbientVolume
    {
        get => ambientVolume;
        set => ambientVolume = Mathf.Clamp01(value);
    }

    public static void PlayBuildingPlacedSfx(BuildingDefinition.PlacementSoundSize placementSize)
    {
        switch (placementSize)
        {
            case BuildingDefinition.PlacementSoundSize.Small:
                Instance.PlayEvent(Instance.smallBuildingPlaced);
                break;
            case BuildingDefinition.PlacementSoundSize.Medium:
                Instance.PlayEvent(Instance.mediumBuildingPlaced);
                break;
            default:
                Instance.PlayEvent(Instance.largeBuildingPlaced);
                break;
        }
    }

    public static void PlayBuildingRemovedSfx()
    {
        Instance.PlayEvent(Instance.buildingRemoved);
    }

    public static void PlayUiClickSfx()
    {
        Instance.PlayEvent(Instance.uiClick);
    }

    public static void PlayBallCreatedSfx()
    {
        Instance.PlayEvent(Instance.ballCreated);
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
        EnsureAmbientSourcesInitialized();
        PreloadReferencedClips();
    }

    private void Update()
    {
        UpdateAmbientLayers();
    }

    private void PreloadReferencedClips()
    {
        HashSet<AudioClip> uniqueClips = new HashSet<AudioClip>();
        CollectClips(uniqueClips, smallBuildingPlaced);
        CollectClips(uniqueClips, mediumBuildingPlaced);
        CollectClips(uniqueClips, largeBuildingPlaced);
        CollectClips(uniqueClips, buildingRemoved);
        CollectClips(uniqueClips, uiClick);
        CollectClips(uniqueClips, ballCreated);
        CollectAmbientClips(uniqueClips);

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

    private void CollectAmbientClips(HashSet<AudioClip> uniqueClips)
    {
        if (uniqueClips == null || ambientLayers == null)
        {
            return;
        }

        for (int i = 0; i < ambientLayers.Length; i++)
        {
            AmbientLayer layer = ambientLayers[i];
            if (layer?.Clip != null)
            {
                uniqueClips.Add(layer.Clip);
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

    private void EnsureAmbientSourcesInitialized()
    {
        int desiredCount = ambientLayers != null ? ambientLayers.Length : 0;

        while (ambientSourcePool.Count < desiredCount)
        {
            ambientSourcePool.Add(CreateAmbientSource(ambientSourcePool.Count));
        }

        for (int i = 0; i < ambientSourcePool.Count; i++)
        {
            AudioSource source = ambientSourcePool[i];
            if (source == null)
            {
                continue;
            }

            bool shouldBeActive = i < desiredCount;
            if (source.gameObject.activeSelf != shouldBeActive)
            {
                source.gameObject.SetActive(shouldBeActive);
            }
        }
    }

    private AudioSource CreateAudioSource(int index)
    {
        GameObject child = new GameObject($"FactorySfxSource_{index}");
        child.transform.SetParent(transform, false);

        AudioSource source = child.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = false;
        source.spatialBlend = 0f;
        source.dopplerLevel = 0f;
        source.rolloffMode = AudioRolloffMode.Linear;
        return source;
    }

    private AudioSource CreateAmbientSource(int index)
    {
        GameObject child = new GameObject($"FactoryAmbientSource_{index}");
        child.transform.SetParent(transform, false);

        AudioSource source = child.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = true;
        source.spatialBlend = 0f;
        source.dopplerLevel = 0f;
        source.rolloffMode = AudioRolloffMode.Linear;
        source.volume = 0f;
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

    private void UpdateAmbientLayers()
    {
        EnsureAmbientSourcesInitialized();

        int buildingCount = GetActiveBuildingCount();
        float globalAmbientVolume = Mathf.Clamp01(masterVolume) * Mathf.Clamp01(ambientVolume);

        for (int i = 0; i < ambientSourcePool.Count; i++)
        {
            AudioSource source = ambientSourcePool[i];
            AmbientLayer layer = ambientLayers != null && i < ambientLayers.Length ? ambientLayers[i] : null;
            if (source == null || layer == null)
            {
                continue;
            }

            AudioClip clip = layer.Clip;
            if (clip != null && source.clip != clip)
            {
                source.clip = clip;
                source.time = Random.Range(0f, clip.length);
            }

            bool shouldBeAudible = clip != null && buildingCount >= layer.MinimumBuildingCount;
            float targetVolume = shouldBeAudible ? Mathf.Clamp01(layer.Volume) * globalAmbientVolume : 0f;
            float fadeSpeed = shouldBeAudible ? Mathf.Max(0.01f, layer.FadeInSpeed) : Mathf.Max(0.01f, layer.FadeOutSpeed);
            source.volume = Mathf.MoveTowards(source.volume, targetVolume, fadeSpeed * Time.deltaTime);

            if (clip != null && !source.isPlaying && (shouldBeAudible || source.volume > 0.001f))
            {
                source.Play();
            }
            else if (source.isPlaying && source.volume <= 0.0001f && !shouldBeAudible)
            {
                source.Stop();
            }
        }
    }

    private static int GetActiveBuildingCount()
    {
        BuildingInstance[] buildings = BuildingInstanceSceneQuery.GetBuildings();
        int count = 0;
        for (int i = 0; i < buildings.Length; i++)
        {
            if (buildings[i] != null)
            {
                count++;
            }
        }

        return count;
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
