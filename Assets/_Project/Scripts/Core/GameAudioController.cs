using System.Collections;
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
    private AudioClip strongHitSound;
    private AudioClip bossSound;
    private AudioClip logoRevealSound;
    private AudioClip archerUltimateSound;
    private AudioClip warriorUltimateSound;
    private Coroutine musicFadeRoutine;

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
        AudioClip clip = strong ? instance.strongHitSound : instance.hitSound;
        instance.effectsSource.PlayOneShot(clip, strong ? 0.7f : 0.32f);
        instance.effectsSource.pitch = 1f;
    }

    public static void PlayLogoReveal()
    {
        EnsureExists();
        instance.effectsSource.PlayOneShot(instance.logoRevealSound, 0.72f);
    }

    public static void PlayUltimateCue(bool warrior)
    {
        EnsureExists();
        AudioClip clip = warrior
            ? instance.warriorUltimateSound
            : instance.archerUltimateSound;
        instance.effectsSource.PlayOneShot(clip, 0.82f);
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
        clickSound = CreateUiClick();
        hitSound = CreateImpact("Hit", false);
        strongHitSound = CreateImpact("StrongHit", true);
        bossSound = CreateBossWarning();
        logoRevealSound = CreateLogoStinger();
        archerUltimateSound = CreateUltimateCue("ArcherUltimate", false);
        warriorUltimateSound = CreateUltimateCue("WarriorUltimate", true);

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
        musicSource.volume = 0f;
        musicSource.Play();

        if (musicFadeRoutine != null)
        {
            StopCoroutine(musicFadeRoutine);
        }

        musicFadeRoutine = StartCoroutine(FadeMusic(MusicVolume, 1.15f));
    }

    private IEnumerator FadeMusic(float targetVolume, float duration)
    {
        float elapsed = 0f;
        float startVolume = musicSource.volume;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            musicSource.volume = Mathf.Lerp(startVolume, targetVolume, progress);
            yield return null;
        }

        musicSource.volume = targetVolume;
        musicFadeRoutine = null;
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

    private static AudioClip CreateUiClick()
    {
        return CreateLayeredClip(
            "ButtonClick",
            0.085f,
            (progress, time) =>
            {
                float envelope = Mathf.Pow(1f - progress, 3f);
                float high = Mathf.Sin(2f * Mathf.PI * 880f * time);
                float low = Mathf.Sin(2f * Mathf.PI * 440f * time);
                return (high * 0.16f + low * 0.08f) * envelope;
            }
        );
    }

    private static AudioClip CreateImpact(string name, bool strong)
    {
        float duration = strong ? 0.18f : 0.1f;
        return CreateLayeredClip(
            name,
            duration,
            (progress, time) =>
            {
                float envelope = Mathf.Pow(1f - progress, strong ? 2f : 3f);
                float frequency = Mathf.Lerp(strong ? 92f : 150f, 48f, progress);
                float thump = Mathf.Sin(2f * Mathf.PI * frequency * time);
                float crack = Random.Range(-1f, 1f) * Mathf.Pow(1f - progress, 7f);
                return (thump * (strong ? 0.42f : 0.27f) + crack * 0.22f) * envelope;
            }
        );
    }

    private static AudioClip CreateBossWarning()
    {
        return CreateLayeredClip(
            "BossWarning",
            0.72f,
            (progress, time) =>
            {
                float pulse = 0.5f + 0.5f * Mathf.Sin(2f * Mathf.PI * 5f * time);
                float low = Mathf.Sin(2f * Mathf.PI * 58f * time);
                float sub = Mathf.Sin(2f * Mathf.PI * 29f * time);
                return (low * 0.28f + sub * 0.2f) * pulse * (1f - progress);
            }
        );
    }

    private static AudioClip CreateLogoStinger()
    {
        return CreateLayeredClip(
            "MugungLogoReveal",
            1.05f,
            (progress, time) =>
            {
                float swell = Mathf.SmoothStep(0f, 1f, Mathf.Min(1f, progress * 5f));
                float release = 1f - Mathf.SmoothStep(0.55f, 1f, progress);
                float root = Mathf.Sin(2f * Mathf.PI * 110f * time);
                float fifth = Mathf.Sin(2f * Mathf.PI * 164.81f * time);
                float octave = Mathf.Sin(2f * Mathf.PI * 220f * time);
                float shimmer = Mathf.Sin(2f * Mathf.PI * 880f * time) *
                    Mathf.Pow(1f - progress, 4f);
                return (root * 0.22f + fifth * 0.16f + octave * 0.12f + shimmer * 0.08f) *
                    swell * release;
            }
        );
    }

    private static AudioClip CreateUltimateCue(string name, bool warrior)
    {
        return CreateLayeredClip(
            name,
            warrior ? 0.52f : 0.44f,
            (progress, time) =>
            {
                float attack = Mathf.SmoothStep(0f, 1f, Mathf.Min(1f, progress * 16f));
                float release = 1f - Mathf.SmoothStep(0.45f, 1f, progress);
                float frequency = warrior
                    ? Mathf.Lerp(82f, 42f, progress)
                    : Mathf.Lerp(520f, 980f, progress);
                float core = Mathf.Sin(2f * Mathf.PI * frequency * time);
                float texture = warrior
                    ? Random.Range(-0.45f, 0.45f) * Mathf.Pow(1f - progress, 5f)
                    : Mathf.Sin(2f * Mathf.PI * frequency * 2f * time) * 0.22f;
                return (core * (warrior ? 0.42f : 0.28f) + texture) * attack * release;
            }
        );
    }

    private static AudioClip CreateLayeredClip(
        string clipName,
        float duration,
        System.Func<float, float, float> sampleGenerator
    )
    {
        const int sampleRate = 44100;
        int sampleCount = Mathf.CeilToInt(sampleRate * duration);
        float[] samples = new float[sampleCount];

        for (int index = 0; index < sampleCount; index++)
        {
            float progress = (float)index / sampleCount;
            float time = (float)index / sampleRate;
            samples[index] = Mathf.Clamp(sampleGenerator(progress, time), -1f, 1f);
        }

        AudioClip clip = AudioClip.Create(clipName, sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }
}
