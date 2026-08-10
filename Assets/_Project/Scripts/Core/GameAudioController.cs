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
    private AudioClip playerHurtSound;
    private AudioClip bossSound;
    private AudioClip logoRevealSound;
    private AudioClip archerUltimateSound;
    private AudioClip warriorUltimateSound;
    private AudioClip levelUpSound;
    private AudioClip upgradeConfirmSound;
    private AudioClip experiencePickupSound;
    private AudioClip itemPickupSound;
    private AudioClip navigationSound;
    private AudioClip enemyDefeatedSound;
    private AudioClip victorySound;
    private AudioClip defeatSound;
    private float nextExperienceSoundTime;
    private float nextEnemyDefeatedSoundTime;
    private float nextPlayerHurtSoundTime;
    private Coroutine musicFadeRoutine;
    private bool initialized;

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

        instance.Initialize();
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

    public static void PlayPlayerHurt(bool defeated)
    {
        EnsureExists();

        if (Time.unscaledTime < instance.nextPlayerHurtSoundTime)
        {
            return;
        }

        instance.nextPlayerHurtSoundTime = Time.unscaledTime + 0.14f;
        instance.effectsSource.pitch = defeated
            ? 0.82f
            : Random.Range(0.94f, 1.02f);
        instance.effectsSource.PlayOneShot(
            instance.playerHurtSound,
            defeated ? 0.46f : 0.32f
        );
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

    public static void PlayLevelUp()
    {
        EnsureExists();
        instance.effectsSource.PlayOneShot(instance.levelUpSound, 0.68f);
    }

    public static void PlayUpgradeConfirm()
    {
        EnsureExists();
        instance.effectsSource.PlayOneShot(instance.upgradeConfirmSound, 0.52f);
    }

    public static void PlayExperiencePickup()
    {
        EnsureExists();
        if (Time.unscaledTime < instance.nextExperienceSoundTime)
        {
            return;
        }

        instance.nextExperienceSoundTime = Time.unscaledTime + 0.055f;
        instance.effectsSource.pitch = Random.Range(0.96f, 1.08f);
        instance.effectsSource.PlayOneShot(instance.experiencePickupSound, 0.28f);
        instance.effectsSource.pitch = 1f;
    }

    public static void PlayItemPickup(FieldItemType itemType)
    {
        EnsureExists();
        instance.effectsSource.pitch = itemType switch
        {
            FieldItemType.Health => 0.92f,
            FieldItemType.Magnet => 1.08f,
            FieldItemType.EnemyFreeze => 0.78f,
            FieldItemType.SkillCooldownReset => 1.18f,
            _ => 1f
        };
        instance.effectsSource.PlayOneShot(instance.itemPickupSound, 0.58f);
        instance.effectsSource.pitch = 1f;
    }

    public static void PlayBossWarning()
    {
        EnsureExists();
        instance.effectsSource.PlayOneShot(instance.bossSound, 0.55f);
    }

    public static void PlayCardNavigation()
    {
        EnsureExists();
        instance.effectsSource.PlayOneShot(instance.navigationSound, 0.34f);
    }

    public static void PlayEnemyDefeated()
    {
        EnsureExists();
        if (Time.unscaledTime < instance.nextEnemyDefeatedSoundTime)
        {
            return;
        }

        instance.nextEnemyDefeatedSoundTime = Time.unscaledTime + 0.045f;
        instance.effectsSource.pitch = Random.Range(0.92f, 1.08f);
        instance.effectsSource.PlayOneShot(instance.enemyDefeatedSound, 0.38f);
        instance.effectsSource.pitch = 1f;
    }

    public static void PlayGameResult(bool victory)
    {
        EnsureExists();
        AudioClip clip = victory ? instance.victorySound : instance.defeatSound;
        instance.effectsSource.PlayOneShot(clip, victory ? 0.78f : 0.7f);

        if (instance.musicFadeRoutine != null)
        {
            instance.StopCoroutine(instance.musicFadeRoutine);
        }

        instance.musicFadeRoutine = instance.StartCoroutine(
            instance.FadeMusic(MusicVolume * 0.38f, 0.35f)
        );
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
        Initialize();
    }

    private void Initialize()
    {
        if (initialized)
        {
            return;
        }

        initialized = true;

        if (Application.isPlaying)
        {
            DontDestroyOnLoad(gameObject);
        }

        musicSource = gameObject.AddComponent<AudioSource>();
        effectsSource = gameObject.AddComponent<AudioSource>();
        musicSource.loop = true;
        musicSource.playOnAwake = false;
        musicSource.volume = MusicVolume;
        effectsSource.playOnAwake = false;
        effectsSource.volume = EffectsVolume;

        menuMusic = CreateMugungMenuMusic() ??
                    Resources.Load<AudioClip>("Audio/Music/EmptyCity");
        battleMusic = CreateMugungBattleMusic() ??
                      Resources.Load<AudioClip>("Audio/Music/CyberBattle");
        clickSound = CreateUiClick();
        hitSound = CreateImpact("Hit", false);
        strongHitSound = CreateImpact("StrongHit", true);
        playerHurtSound = CreatePlayerHurtCue();
        bossSound = CreateBossWarning();
        logoRevealSound = CreateLogoStinger();
        archerUltimateSound = CreateUltimateCue("ArcherUltimate", false);
        warriorUltimateSound = CreateUltimateCue("WarriorUltimate", true);
        levelUpSound = CreateLevelUpCue();
        upgradeConfirmSound = CreateUpgradeConfirmCue();
        experiencePickupSound = CreateExperiencePickupCue();
        itemPickupSound = CreateItemPickupCue();
        navigationSound = CreateNavigationCue();
        enemyDefeatedSound = CreateEnemyDefeatedCue();
        victorySound = CreateResultCue("Victory", true);
        defeatSound = CreateResultCue("Defeat", false);

        if (Application.isPlaying)
        {
            SceneManager.sceneLoaded += HandleSceneLoaded;
            PlayMusicForScene(SceneManager.GetActiveScene().name);
        }
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

    private static AudioClip CreatePlayerHurtCue()
    {
        return CreateLayeredClip(
            "PlayerHurt",
            0.16f,
            (progress, time) =>
            {
                float envelope = Mathf.Pow(1f - progress, 2.4f);
                float bodyFrequency = Mathf.Lerp(145f, 92f, progress);
                float body = Mathf.Sin(
                    2f * Mathf.PI * bodyFrequency * time
                );
                float breath = Random.Range(-1f, 1f) *
                    Mathf.Pow(1f - progress, 5f);
                float cloth = Mathf.Sin(
                    2f * Mathf.PI * 310f * time
                ) * Mathf.Pow(1f - progress, 4f);
                return (body * 0.28f + breath * 0.12f + cloth * 0.06f) *
                    envelope;
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

    private static AudioClip CreateLevelUpCue()
    {
        return CreateLayeredClip(
            "LevelUp",
            0.72f,
            (progress, time) =>
            {
                float envelope = Mathf.Sin(Mathf.PI * progress);
                int step = Mathf.Min(3, Mathf.FloorToInt(progress * 4f));
                float[] notes = { 261.63f, 329.63f, 392f, 523.25f };
                float note = Mathf.Sin(2f * Mathf.PI * notes[step] * time);
                float shimmer = Mathf.Sin(2f * Mathf.PI * notes[step] * 2f * time) * 0.18f;
                return (note * 0.28f + shimmer) * envelope;
            }
        );
    }

    private static AudioClip CreateUpgradeConfirmCue()
    {
        return CreateLayeredClip(
            "UpgradeConfirm",
            0.2f,
            (progress, time) =>
            {
                float envelope = Mathf.Pow(1f - progress, 2f);
                float low = Mathf.Sin(2f * Mathf.PI * 392f * time);
                float high = Mathf.Sin(2f * Mathf.PI * 784f * time);
                return (low * 0.18f + high * 0.2f) * envelope;
            }
        );
    }

    private static AudioClip CreateExperiencePickupCue()
    {
        return CreateLayeredClip(
            "ExperiencePickup",
            0.12f,
            (progress, time) =>
            {
                float envelope = Mathf.Pow(1f - progress, 2f);
                float frequency = Mathf.Lerp(620f, 980f, progress);
                return Mathf.Sin(2f * Mathf.PI * frequency * time) * 0.22f * envelope;
            }
        );
    }

    private static AudioClip CreateItemPickupCue()
    {
        return CreateLayeredClip(
            "FieldItemPickup",
            0.38f,
            (progress, time) =>
            {
                float envelope = 1f - Mathf.SmoothStep(0.45f, 1f, progress);
                float low = Mathf.Sin(2f * Mathf.PI * 330f * time);
                float high = Mathf.Sin(2f * Mathf.PI * 660f * time);
                float sparkle = Mathf.Sin(2f * Mathf.PI * 1320f * time) *
                    Mathf.Pow(1f - progress, 3f);
                return (low * 0.16f + high * 0.2f + sparkle * 0.1f) * envelope;
            }
        );
    }

    private static AudioClip CreateNavigationCue()
    {
        return CreateLayeredClip(
            "CardNavigation",
            0.075f,
            (progress, time) =>
            {
                float envelope = Mathf.Pow(1f - progress, 3f);
                float tone = Mathf.Sin(2f * Mathf.PI * 560f * time);
                float shimmer = Mathf.Sin(2f * Mathf.PI * 1120f * time);
                return (tone * 0.16f + shimmer * 0.07f) * envelope;
            }
        );
    }

    private static AudioClip CreateEnemyDefeatedCue()
    {
        return CreateLayeredClip(
            "EnemyDefeated",
            0.16f,
            (progress, time) =>
            {
                float envelope = Mathf.Pow(1f - progress, 2f);
                float fall = Mathf.Lerp(220f, 72f, progress);
                float tone = Mathf.Sin(2f * Mathf.PI * fall * time);
                float dust = Random.Range(-0.5f, 0.5f) * Mathf.Pow(1f - progress, 5f);
                return (tone * 0.22f + dust * 0.12f) * envelope;
            }
        );
    }

    private static AudioClip CreateResultCue(string name, bool victory)
    {
        return CreateLayeredClip(
            name,
            victory ? 1.25f : 1.05f,
            (progress, time) =>
            {
                float envelope = Mathf.Sin(Mathf.PI * Mathf.Clamp01(progress * 1.1f));
                float[] victoryNotes = { 196f, 246.94f, 293.66f, 392f };
                float[] defeatNotes = { 196f, 164.81f, 146.83f, 98f };
                float[] notes = victory ? victoryNotes : defeatNotes;
                int step = Mathf.Min(notes.Length - 1, Mathf.FloorToInt(progress * notes.Length));
                float core = Mathf.Sin(2f * Mathf.PI * notes[step] * time);
                float overtone = Mathf.Sin(2f * Mathf.PI * notes[step] * 2f * time);
                float pulse = victory ? 1f : 0.78f + Mathf.Sin(2f * Mathf.PI * 4f * time) * 0.22f;
                return (core * 0.3f + overtone * 0.1f) * envelope * pulse;
            }
        );
    }

    private static AudioClip CreateMugungMenuMusic()
    {
        const float duration = 12f;
        const int sampleRate = 22050;
        float[] notes = { 146.83f, 174.61f, 196f, 220f, 196f, 174.61f };
        return CreateMusicClip(
            "MugungMenuTheme",
            duration,
            sampleRate,
            (index, time) =>
            {
                float phrase = time / 2f;
                int noteIndex = Mathf.FloorToInt(phrase) % notes.Length;
                float noteTime = phrase - Mathf.Floor(phrase);
                float pluck = Mathf.Exp(-noteTime * 5.5f);
                float note = Mathf.Sin(2f * Mathf.PI * notes[noteIndex] * time);
                float overtone = Mathf.Sin(2f * Mathf.PI * notes[noteIndex] * 2f * time);
                float drone = Mathf.Sin(2f * Mathf.PI * 73.415f * time) * 0.11f +
                              Mathf.Sin(2f * Mathf.PI * 110f * time) * 0.06f;
                float breath = PseudoNoise(index / 22) * 0.018f;
                float slowSwell = 0.72f + Mathf.Sin(2f * Mathf.PI * time / duration) * 0.16f;
                return (drone + (note * 0.12f + overtone * 0.035f) * pluck + breath) *
                       slowSwell;
            }
        );
    }

    private static AudioClip CreateMugungBattleMusic()
    {
        const float duration = 8f;
        const int sampleRate = 22050;
        const float beatDuration = 0.5f;
        float[] bassNotes = { 73.415f, 73.415f, 98f, 110f, 73.415f, 130.81f, 110f, 98f };
        float[] leadNotes = { 293.66f, 349.23f, 392f, 440f, 392f, 349.23f, 293.66f, 261.63f };
        return CreateMusicClip(
            "MugungBattleTheme",
            duration,
            sampleRate,
            (index, time) =>
            {
                float beat = time / beatDuration;
                int beatIndex = Mathf.FloorToInt(beat) % bassNotes.Length;
                float beatTime = (beat - Mathf.Floor(beat)) * beatDuration;
                float kickEnvelope = Mathf.Exp(-beatTime * 15f);
                float kickFrequency = Mathf.Lerp(92f, 48f, Mathf.Clamp01(beatTime * 8f));
                float drum = Mathf.Sin(2f * Mathf.PI * kickFrequency * time) *
                             kickEnvelope * (beatIndex % 2 == 0 ? 0.28f : 0.18f);
                float snap = PseudoNoise(index) * Mathf.Exp(-beatTime * 34f) *
                             (beatIndex % 2 == 1 ? 0.11f : 0.035f);
                float bass = Mathf.Sin(2f * Mathf.PI * bassNotes[beatIndex] * time) * 0.13f;
                float leadEnvelope = Mathf.Exp(-beatTime * 5.5f);
                float lead = Mathf.Sin(2f * Mathf.PI * leadNotes[beatIndex] * time) *
                             leadEnvelope * 0.105f;
                float metal = Mathf.Sin(2f * Mathf.PI * 1174.66f * time) *
                              Mathf.Exp(-beatTime * 26f) *
                              (beatIndex % 4 == 3 ? 0.055f : 0.018f);
                return drum + snap + bass + lead + metal;
            }
        );
    }

    private static AudioClip CreateMusicClip(
        string name,
        float duration,
        int sampleRate,
        System.Func<int, float, float> generator
    )
    {
        int sampleCount = Mathf.CeilToInt(duration * sampleRate);
        float[] samples = new float[sampleCount];
        const int fadeSamples = 256;

        for (int index = 0; index < sampleCount; index++)
        {
            float time = (float)index / sampleRate;
            float edgeFade = Mathf.Min(
                1f,
                Mathf.Min(index, sampleCount - 1 - index) / (float)fadeSamples
            );
            samples[index] = Mathf.Clamp(generator(index, time) * edgeFade, -0.82f, 0.82f);
        }

        AudioClip clip = AudioClip.Create(name, sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private static float PseudoNoise(int value)
    {
        uint hash = (uint)value;
        hash ^= hash << 13;
        hash ^= hash >> 17;
        hash ^= hash << 5;
        return (hash & 0xffff) / 32767.5f - 1f;
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
