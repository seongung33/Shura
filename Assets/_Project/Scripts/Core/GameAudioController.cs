using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class GameAudioController : MonoBehaviour
{
    private const string MusicVolumeKey = "settings.musicVolume";
    private const string EffectsVolumeKey = "settings.effectsVolume";
    private const float DefaultMusicVolume = 0.32f;
    private const float DefaultEffectsVolume = 1f;

    private static GameAudioController instance;

    private AudioSource musicSource;
    private AudioSource effectsSource;
    private AudioClip menuMusic;
    private AudioClip battleMusic;
    private AudioClip clickSound;
    private AudioClip hitSound;
    private AudioClip bossSound;

    public static float MusicVolume =>
        PlayerPrefs.GetFloat(MusicVolumeKey, DefaultMusicVolume);

    public static float EffectsVolume =>
        PlayerPrefs.GetFloat(EffectsVolumeKey, DefaultEffectsVolume);

    public static void EnsureExists()
    {
        if (instance != null)
        {
            return;
        }

        instance = FindFirstObjectByType<GameAudioController>();
        if (instance == null)
        {
            instance = new GameObject("GameAudioController").AddComponent<GameAudioController>();
        }
    }

    public static void PlayButtonClick()
    {
        EnsureExists();
        instance.effectsSource.PlayOneShot(instance.clickSound, 0.28f);
    }

    public static void PlayHit(bool strong)
    {
        EnsureExists();
        instance.effectsSource.pitch = strong ? 0.82f : Random.Range(0.96f, 1.05f);
        instance.effectsSource.PlayOneShot(instance.hitSound, strong ? 0.5f : 0.2f);
        instance.effectsSource.pitch = 1f;
    }

    public static void PlayBossWarning()
    {
        EnsureExists();
        instance.effectsSource.PlayOneShot(instance.bossSound, 0.55f);
    }

    public static void SetMusicVolume(float value)
    {
        EnsureExists();
        float volume = Mathf.Clamp01(value);
        instance.musicSource.volume = volume;
        PlayerPrefs.SetFloat(MusicVolumeKey, volume);
    }

    public static void SetEffectsVolume(float value)
    {
        EnsureExists();
        float volume = Mathf.Clamp01(value);
        instance.effectsSource.volume = volume;
        PlayerPrefs.SetFloat(EffectsVolumeKey, volume);
    }

    public static void SaveSettings()
    {
        PlayerPrefs.Save();
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
        musicSource = gameObject.AddComponent<AudioSource>();
        effectsSource = gameObject.AddComponent<AudioSource>();
        musicSource.loop = true;
        musicSource.playOnAwake = false;
        musicSource.volume = MusicVolume;
        effectsSource.playOnAwake = false;
        effectsSource.volume = EffectsVolume;

        menuMusic = Resources.Load<AudioClip>("Audio/Music/EmptyCity");
        battleMusic = Resources.Load<AudioClip>("Audio/Music/CyberBattle");
        clickSound = CreateTone("ButtonClick", 720f, 0.055f, 0.18f, false);
        hitSound = CreateTone("Hit", 125f, 0.07f, 0.3f, true);
        bossSound = CreateTone("BossWarning", 72f, 0.48f, 0.5f, true);

        SceneManager.sceneLoaded += HandleSceneLoaded;
        PlayMusicForScene(SceneManager.GetActiveScene().name);
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            instance = null;
        }
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        PlayMusicForScene(scene.name);
    }

    private void PlayMusicForScene(string sceneName)
    {
        AudioClip target = sceneName == "Main" ? battleMusic : menuMusic;
        if (target == null || musicSource.clip == target)
        {
            return;
        }

        musicSource.Stop();
        musicSource.clip = target;
        musicSource.Play();
    }

    private static AudioClip CreateTone(string clipName, float frequency, float duration, float volume, bool noise)
    {
        const int sampleRate = 44100;
        int sampleCount = Mathf.CeilToInt(sampleRate * duration);
        float[] samples = new float[sampleCount];
        for (int i = 0; i < sampleCount; i++)
        {
            float progress = (float)i / sampleCount;
            float envelope = 1f - progress;
            float wave = Mathf.Sin(2f * Mathf.PI * frequency * i / sampleRate);
            float texture = noise ? Random.Range(-0.35f, 0.35f) : 0f;
            samples[i] = (wave + texture) * envelope * volume;
        }

        AudioClip clip = AudioClip.Create(clipName, sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }
}
