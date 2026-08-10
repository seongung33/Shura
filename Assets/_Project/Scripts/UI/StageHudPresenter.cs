using System.Collections.Generic;
using Shura.Player;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class StageHudPresenter : MonoBehaviour
{
    private sealed class LoadoutSlot
    {
        public Image Icon;
        public Image[] LevelPips;
        public Image CooldownFill;
        public TMP_Text Cooldown;
    }

    private sealed class PlayerView
    {
        public GameObject Root;
        public Image Portrait;
        public Image HealthFill;
        public TMP_Text Name;
        public TMP_Text Health;
        public PlayerHealth Target;
        public CharacterData Character;
        public AutoSkillCaster SkillCaster;
        public PlayerRelicInventory Relics;
        public LoadoutSlot BasicWeapon;
        public LoadoutSlot[] Skills;
        public LoadoutSlot[] RelicSlots;
        public LoadoutSlot Ultimate;
    }

    private static readonly Color PanelColor = new Color(0.03f, 0.035f, 0.075f, 0.92f);
    private static readonly Color AccentColor = new Color(0.86f, 0.34f, 0.45f, 1f);
    private static readonly Color HealthColor = new Color(0.95f, 0.25f, 0.28f, 1f);
    private static readonly Color ExperienceColor = new Color(0.95f, 0.68f, 0.22f, 1f);
    private static readonly Color LevelPipActiveColor = new Color(0.95f, 0.68f, 0.22f, 1f);
    private static readonly Color LevelPipInactiveColor = new Color(0.12f, 0.16f, 0.24f, 1f);
    private static readonly Color PrimaryTextColor = new Color(0.96f, 0.98f, 1f, 1f);

    private readonly List<PlayerView> playerViews = new List<PlayerView>();
    private WaveManager waveManager;
    private PlayerExperience playerExperience;
    private RectTransform playerList;
    private TMP_Text timerText;
    private Image experienceFill;
    private TMP_Text experienceText;
    private GameObject bossPanel;
    private Image bossHealthFill;
    private TMP_Text bossHealthText;
    private EnemyHealth trackedBoss;
    private GameManager gameManager;
    private GameObject resultPanel;
    private TMP_Text resultTitle;
    private float nextPlayerRefresh;
    private float nextBossRefresh;

    private void Start()
    {
        waveManager ??= FindFirstObjectByType<WaveManager>();
        CreateView();
        RefreshPlayers();
        RefreshBoss();
    }

    public void Configure(WaveManager wave, EnemySpawner spawner)
    {
        waveManager = wave;
        if (timerText == null)
        {
            CreateView();
        }
        RefreshPlayers();
    }

    private void Update()
    {
        UpdateTimer();
        UpdatePlayerCards();
        UpdateExperience();
        UpdateBoss();
        UpdateOfflineResult();

        if (Time.unscaledTime >= nextPlayerRefresh)
        {
            nextPlayerRefresh = Time.unscaledTime + 0.5f;
            RefreshPlayers();
        }

        if (Time.unscaledTime >= nextBossRefresh)
        {
            nextBossRefresh = Time.unscaledTime + 0.25f;
            RefreshBoss();
        }
    }

    private void UpdateTimer()
    {
        if (waveManager == null)
        {
            waveManager = FindFirstObjectByType<WaveManager>();
        }

        if (waveManager == null || timerText == null)
        {
            return;
        }

        int remaining = Mathf.CeilToInt(Mathf.Max(0f, waveManager.Duration - waveManager.ElapsedTime));
        timerText.text = $"{remaining / 60:00}:{remaining % 60:00}";
    }

    private void RefreshPlayers()
    {
        List<GameObject> players = new List<GameObject>();
        NetworkManager manager = NetworkManager.Singleton;

        if (manager != null && manager.IsListening)
        {
            foreach (NetworkClient client in manager.ConnectedClientsList)
            {
                if (client.PlayerObject != null)
                {
                    players.Add(client.PlayerObject.gameObject);
                }
            }
        }
        else
        {
            GameObject[] offlinePlayers = GameObject.FindGameObjectsWithTag("Player");
            players.AddRange(offlinePlayers);
        }

        if (players.Count == playerViews.Count)
        {
            bool unchanged = true;
            for (int i = 0; i < players.Count; i++)
            {
                if (playerViews[i].Target != players[i].GetComponent<PlayerHealth>())
                {
                    unchanged = false;
                    break;
                }
            }
            if (unchanged)
            {
                return;
            }
        }

        ClearPlayerViews();
        for (int i = 0; i < players.Count; i++)
        {
            CreatePlayerCard(players[i], i);
        }

        ResolveLocalExperience(players);
    }

    private void ResolveLocalExperience(List<GameObject> players)
    {
        NetworkManager manager = NetworkManager.Singleton;
        if (manager != null && manager.IsListening)
        {
            playerExperience = manager.LocalClient?.PlayerObject?.GetComponent<PlayerExperience>();
            return;
        }
        playerExperience = players.Count > 0 ? players[0].GetComponent<PlayerExperience>() : null;
    }

    private void CreatePlayerCard(GameObject player, int index)
    {
        GameObject root = CreatePanel(playerList, $"PlayerStatus_{index + 1}", new Color(0.04f, 0.07f, 0.14f, 0.94f));
        RectTransform rect = root.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(820f, 104f);

        Image portrait = CreateImage(root.transform, "Portrait");
        RectTransform portraitRect = portrait.rectTransform;
        portraitRect.anchorMin = new Vector2(0f, 0.5f);
        portraitRect.anchorMax = new Vector2(0f, 0.5f);
        portraitRect.pivot = new Vector2(0f, 0.5f);
        portraitRect.anchoredPosition = new Vector2(12f, 0f);
        portraitRect.sizeDelta = new Vector2(76f, 76f);
        portrait.preserveAspect = true;

        CharacterData character = ResolveCharacter(player);
        portrait.sprite = character?.portrait;
        portrait.enabled = portrait.sprite != null;

        TMP_Text name = CreateText(root.transform, "PlayerName", TextAlignmentOptions.MidlineLeft, 27f);
        SetRect(name.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(100f, -12f), new Vector2(276f, 36f), new Vector2(0f, 1f));
        string characterName = character != null ? character.characterName : "주몽";
        name.text = $"플레이어 {index + 1}   {characterName}";

        GameObject healthBackground = CreatePanel(root.transform, "HealthBar", new Color(0.16f, 0.18f, 0.24f, 1f));
        SetRect(healthBackground.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(100f, 16f), new Vector2(276f, 25f), Vector2.zero);
        Image healthFill = CreateFill(healthBackground.transform, "Fill", HealthColor);

        TMP_Text health = CreateText(root.transform, "HealthText", TextAlignmentOptions.Center, 21f);
        SetRect(health.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(100f, 16f), new Vector2(276f, 25f), Vector2.zero);

        LoadoutSlot basicWeapon = CreateLoadoutSlot(root.transform, "BasicWeapon", 390f, 1);
        LoadoutSlot[] skills = new LoadoutSlot[3];
        LoadoutSlot[] relics = new LoadoutSlot[3];

        for (int slot = 0; slot < skills.Length; slot++)
        {
            skills[slot] = CreateLoadoutSlot(
                root.transform,
                $"Skill_{slot + 1}",
                444f + slot * 50f,
                5
            );
        }

        for (int slot = 0; slot < relics.Length; slot++)
        {
            relics[slot] = CreateLoadoutSlot(
                root.transform,
                $"Relic_{slot + 1}",
                600f + slot * 50f,
                1
            );
        }

        LoadoutSlot ultimate = CreateLoadoutSlot(
            root.transform,
            "Ultimate",
            756f,
            0
        );

        playerViews.Add(new PlayerView
        {
            Root = root,
            Portrait = portrait,
            HealthFill = healthFill,
            Name = name,
            Health = health,
            Target = player.GetComponent<PlayerHealth>(),
            Character = character,
            SkillCaster = player.GetComponent<AutoSkillCaster>(),
            Relics = player.GetComponent<PlayerRelicInventory>(),
            BasicWeapon = basicWeapon,
            Skills = skills,
            RelicSlots = relics,
            Ultimate = ultimate
        });
    }

    private static CharacterData ResolveCharacter(GameObject player)
    {
        NetworkPlayerCharacter networkCharacter = player.GetComponent<NetworkPlayerCharacter>();
        if (networkCharacter != null && networkCharacter.Character != null)
        {
            return networkCharacter.Character;
        }
        return SinglePlayerSelection.Character;
    }

    private void UpdatePlayerCards()
    {
        foreach (PlayerView view in playerViews)
        {
            if (view.Target == null)
            {
                continue;
            }
            float max = Mathf.Max(1f, view.Target.MaxHealth);
            float current = Mathf.Clamp(view.Target.CurrentHealth, 0f, max);
            SetBarValue(view.HealthFill, current / max);
            view.Health.text = $"{Mathf.CeilToInt(current)} / {Mathf.CeilToInt(max)}";
            UpdateLoadout(view);
        }
    }

    private static void UpdateLoadout(PlayerView view)
    {
        SetSlot(view.BasicWeapon, view.Character?.BasicSkill?.Icon, 1);

        IReadOnlyList<AutoSkillCaster.EquippedSkill> equipped =
            view.SkillCaster != null ? view.SkillCaster.EquippedSkills : null;

        for (int index = 0; index < view.Skills.Length; index++)
        {
            AutoSkillCaster.EquippedSkill skill =
                equipped != null && index < equipped.Count
                    ? equipped[index]
                    : null;
            SetSlot(
                view.Skills[index],
                skill?.data?.Icon,
                skill?.level ?? 0
            );
        }

        for (int index = 0; index < view.RelicSlots.Length; index++)
        {
            RelicData relic = view.Relics?.GetOwnedRelic(index);
            SetSlot(view.RelicSlots[index], relic?.Icon, relic != null ? 1 : 0);
        }

        UpdateUltimateSlot(view.Ultimate, view.SkillCaster);
    }

    private static void SetSlot(LoadoutSlot slot, Sprite sprite, int level)
    {
        bool occupied = sprite != null || level > 0;
        slot.Icon.sprite = sprite;
        slot.Icon.enabled = sprite != null;
        for (int index = 0; index < slot.LevelPips.Length; index++)
        {
            slot.LevelPips[index].color = occupied && index < level
                ? LevelPipActiveColor
                : LevelPipInactiveColor;
        }
    }

    private static void UpdateUltimateSlot(
        LoadoutSlot slot,
        AutoSkillCaster caster
    )
    {
        SkillData ultimate = caster?.UltimateSkill;
        slot.Icon.sprite = ultimate?.Icon;
        slot.Icon.enabled = slot.Icon.sprite != null;

        float remaining = caster != null
            ? caster.UltimateCooldownRemaining
            : 0f;
        float duration = caster != null
            ? caster.UltimateCooldownDuration
            : 0f;
        bool coolingDown = ultimate != null && remaining > 0f;

        slot.CooldownFill.gameObject.SetActive(coolingDown);
        slot.Cooldown.gameObject.SetActive(coolingDown);
        slot.CooldownFill.fillAmount = duration > 0f
            ? Mathf.Clamp01(remaining / duration)
            : 0f;
        slot.Cooldown.text = coolingDown
            ? Mathf.CeilToInt(remaining).ToString()
            : string.Empty;
    }

    private void UpdateExperience()
    {
        if (playerExperience == null)
        {
            return;
        }
        int required = Mathf.Max(1, playerExperience.ExperienceToNextLevel);
        SetBarValue(experienceFill, (float)playerExperience.CurrentExperience / required);
        experienceText.text = $"레벨 {playerExperience.CurrentLevel}     경험치 {playerExperience.CurrentExperience} / {required}";
    }

    private void RefreshBoss()
    {
        if (trackedBoss != null && trackedBoss.CurrentHealth > 0f)
        {
            return;
        }

        trackedBoss = null;
        EnemyHealth[] enemies = FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None);
        foreach (EnemyHealth enemy in enemies)
        {
            if (enemy.IsBoss && enemy.CurrentHealth > 0f)
            {
                trackedBoss = enemy;
                break;
            }
        }
        bossPanel.SetActive(trackedBoss != null);
    }

    private void UpdateBoss()
    {
        if (trackedBoss == null)
        {
            bossPanel?.SetActive(false);
            return;
        }
        float max = Mathf.Max(1f, trackedBoss.MaxHealth);
        float current = Mathf.Clamp(trackedBoss.CurrentHealth, 0f, max);
        bossPanel.SetActive(current > 0f);
        SetBarValue(bossHealthFill, current / max);
        bossHealthText.text = $"장산범     {Mathf.CeilToInt(current)} / {Mathf.CeilToInt(max)}";
    }

    private void UpdateOfflineResult()
    {
        NetworkManager manager = NetworkManager.Singleton;
        if (manager != null && manager.IsListening)
        {
            resultPanel?.SetActive(false);
            return;
        }

        gameManager ??= FindFirstObjectByType<GameManager>();
        bool finished = gameManager != null && gameManager.CurrentState == GameState.Result;
        if (resultPanel == null || resultPanel.activeSelf == finished)
        {
            return;
        }

        resultPanel.SetActive(finished);
        if (finished)
        {
            resultTitle.text = gameManager.IsVictory ? "승리" : "패배";
            resultTitle.color = gameManager.IsVictory
                ? new Color(1f, 0.8f, 0.18f, 1f)
                : new Color(1f, 0.3f, 0.32f, 1f);
        }
    }

    private void CreateView()
    {
        if (timerText != null)
        {
            return;
        }

        GameObject canvasObject = new GameObject(
            "StageHudCanvas",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster)
        );
        canvasObject.transform.SetParent(transform, false);
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 45;
        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        GameObject timerPanel = CreatePanel(canvasObject.transform, "RoundTimer", PanelColor);
        SetRect(timerPanel.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -22f), new Vector2(300f, 72f), new Vector2(0.5f, 1f));
        timerText = CreateText(timerPanel.transform, "Time", TextAlignmentOptions.Center, 46f);
        ApplyTextColor(timerText, PrimaryTextColor);
        Stretch(timerText.rectTransform, 8f);

        bossPanel = CreatePanel(canvasObject.transform, "BossPanel", PanelColor);
        SetRect(bossPanel.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -110f), new Vector2(760f, 76f), new Vector2(0.5f, 1f));
        GameObject bossBar = CreatePanel(bossPanel.transform, "BossHealthBar", new Color(0.15f, 0.08f, 0.09f, 1f));
        Stretch(bossBar.GetComponent<RectTransform>(), 12f);
        bossHealthFill = CreateFill(bossBar.transform, "Fill", new Color(0.75f, 0.08f, 0.12f, 1f));
        bossHealthText = CreateText(bossPanel.transform, "BossHealthText", TextAlignmentOptions.Center, 27f);
        Stretch(bossHealthText.rectTransform, 10f);
        bossPanel.SetActive(false);

        GameObject playerPanel = new GameObject("PlayerStatusList", typeof(RectTransform), typeof(VerticalLayoutGroup));
        playerPanel.transform.SetParent(canvasObject.transform, false);
        playerList = playerPanel.GetComponent<RectTransform>();
        SetRect(playerList, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(28f, -28f), new Vector2(820f, 226f), new Vector2(0f, 1f));
        VerticalLayoutGroup layout = playerPanel.GetComponent<VerticalLayoutGroup>();
        layout.spacing = 12f;
        layout.childControlHeight = false;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = true;

        GameObject experiencePanel = CreatePanel(canvasObject.transform, "ExperiencePanel", PanelColor);
        SetRect(experiencePanel.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 26f), new Vector2(1040f, 72f), new Vector2(0.5f, 0f));
        GameObject experienceBar = CreatePanel(experiencePanel.transform, "ExperienceBar", new Color(0.15f, 0.17f, 0.22f, 1f));
        Stretch(experienceBar.GetComponent<RectTransform>(), 12f);
        experienceFill = CreateFill(experienceBar.transform, "Fill", ExperienceColor);
        experienceText = CreateText(experiencePanel.transform, "ExperienceText", TextAlignmentOptions.Center, 27f);
        ApplyTextColor(experienceText, PrimaryTextColor);
        Stretch(experienceText.rectTransform, 10f);

        CreateResultView(canvasObject.transform);
    }

    private void CreateResultView(Transform parent)
    {
        resultPanel = CreatePanel(parent, "OfflineResultPanel", new Color(0.025f, 0.04f, 0.09f, 0.97f));
        SetRect(resultPanel.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(720f, 440f), new Vector2(0.5f, 0.5f));

        resultTitle = CreateText(resultPanel.transform, "ResultTitle", TextAlignmentOptions.Center, 92f);
        SetRect(resultTitle.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -64f), new Vector2(620f, 125f), new Vector2(0.5f, 1f));

        TMP_Text message = CreateText(resultPanel.transform, "ResultMessage", TextAlignmentOptions.Center, 30f);
        SetRect(message.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -195f), new Vector2(600f, 52f), new Vector2(0.5f, 1f));
        message.text = "다시 도전하거나 메인 메뉴로 돌아갈 수 있습니다.";

        CreateButton(resultPanel.transform, "RestartButton", "다시 시작", new Vector2(-170f, -120f), RestartOfflineGame);
        CreateButton(resultPanel.transform, "MainMenuButton", "메인 메뉴", new Vector2(170f, -120f), ReturnToMainMenu);
        resultPanel.SetActive(false);
    }

    private static void CreateButton(Transform parent, string name, string label, Vector2 position, UnityEngine.Events.UnityAction action)
    {
        GameObject buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button), typeof(Outline));
        buttonObject.transform.SetParent(parent, false);
        SetRect(buttonObject.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), position, new Vector2(280f, 84f), new Vector2(0.5f, 0.5f));
        buttonObject.GetComponent<Image>().color = new Color(0.16f, 0.55f, 0.72f, 1f);
        buttonObject.GetComponent<Button>().onClick.AddListener(action);

        TMP_Text labelText = CreateText(buttonObject.transform, "Label", TextAlignmentOptions.Center, 36f);
        labelText.text = label;
        Stretch(labelText.rectTransform, 10f);
    }

    private static void RestartOfflineGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("Main", LoadSceneMode.Single);
    }

    private static void ReturnToMainMenu()
    {
        Time.timeScale = 1f;
        SinglePlayerSelection.Clear();
        SceneManager.LoadScene("MainMenu", LoadSceneMode.Single);
    }

    private void ClearPlayerViews()
    {
        foreach (PlayerView view in playerViews)
        {
            if (view.Root != null)
            {
                Destroy(view.Root);
            }
        }
        playerViews.Clear();
    }

    private static GameObject CreatePanel(Transform parent, string name, Color color)
    {
        GameObject panel = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Outline));
        panel.transform.SetParent(parent, false);
        panel.GetComponent<Image>().color = color;
        Outline outline = panel.GetComponent<Outline>();
        outline.effectColor = new Color(0.72f, 0.26f, 0.38f, 0.72f);
        outline.effectDistance = new Vector2(2f, -2f);
        return panel;
    }

    private static Image CreateImage(Transform parent, string name)
    {
        GameObject imageObject = new GameObject(name, typeof(RectTransform), typeof(Image));
        imageObject.transform.SetParent(parent, false);
        return imageObject.GetComponent<Image>();
    }

    private static LoadoutSlot CreateLoadoutSlot(
        Transform parent,
        string name,
        float x,
        int levelPipCount
    )
    {
        GameObject root = CreatePanel(
            parent,
            name,
            new Color(0.025f, 0.035f, 0.07f, 0.98f)
        );
        SetRect(
            root.GetComponent<RectTransform>(),
            new Vector2(0f, 0.5f),
            new Vector2(0f, 0.5f),
            new Vector2(x, 0f),
            new Vector2(48f, 78f),
            new Vector2(0f, 0.5f)
        );

        Image icon = CreateImage(root.transform, "Icon");
        SetRect(
            icon.rectTransform,
            new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(0f, -2f),
            new Vector2(42f, 56f),
            new Vector2(0.5f, 1f)
        );
        icon.preserveAspect = true;
        icon.raycastTarget = false;
        icon.enabled = false;

        Image[] levelPips = new Image[Mathf.Max(0, levelPipCount)];
        float pipSize = levelPipCount <= 1 ? 10f : 6f;
        float pipSpacing = levelPipCount <= 1 ? 0f : 2f;
        float pipRowWidth = levelPipCount * pipSize +
            Mathf.Max(0, levelPipCount - 1) * pipSpacing;

        for (int index = 0; index < levelPips.Length; index++)
        {
            Image pip = CreateImage(root.transform, $"LevelPip_{index + 1}");
            SetRect(
                pip.rectTransform,
                new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f),
                new Vector2(
                    -pipRowWidth * 0.5f + pipSize * 0.5f +
                    index * (pipSize + pipSpacing),
                    4f
                ),
                new Vector2(pipSize, pipSize),
                new Vector2(0.5f, 0f)
            );
            pip.color = LevelPipInactiveColor;
            pip.raycastTarget = false;
            levelPips[index] = pip;
        }

        Image cooldownFill = CreateImage(root.transform, "CooldownFill");
        SetRect(
            cooldownFill.rectTransform,
            new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(0f, -2f),
            new Vector2(42f, 56f),
            new Vector2(0.5f, 1f)
        );
        cooldownFill.color = new Color(0f, 0f, 0f, 0.72f);
        cooldownFill.type = Image.Type.Filled;
        cooldownFill.fillMethod = Image.FillMethod.Radial360;
        cooldownFill.fillOrigin = (int)Image.Origin360.Top;
        cooldownFill.fillClockwise = false;
        cooldownFill.raycastTarget = false;

        TMP_Text cooldown = CreateText(
            root.transform,
            "Cooldown",
            TextAlignmentOptions.Center,
            25f
        );
        cooldown.fontSizeMin = 13f;
        SetRect(
            cooldown.rectTransform,
            new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(0f, -2f),
            new Vector2(42f, 56f),
            new Vector2(0.5f, 1f)
        );

        cooldownFill.gameObject.SetActive(false);
        cooldown.gameObject.SetActive(false);

        return new LoadoutSlot
        {
            Icon = icon,
            LevelPips = levelPips,
            CooldownFill = cooldownFill,
            Cooldown = cooldown
        };
    }

    private static Image CreateFill(Transform parent, string name, Color color)
    {
        Image image = CreateImage(parent, name);
        image.color = color;
        image.type = Image.Type.Simple;
        Stretch(image.rectTransform, 0f);
        return image;
    }

    private static void SetBarValue(Image image, float value)
    {
        if (image == null)
        {
            return;
        }

        RectTransform rect = image.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = new Vector2(Mathf.Clamp01(value), 1f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static TMP_Text CreateText(Transform parent, string name, TextAlignmentOptions alignment, float maxSize)
    {
        GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);
        TMP_Text text = textObject.GetComponent<TextMeshProUGUI>();
        text.alignment = alignment;
        text.enableAutoSizing = true;
        text.fontSizeMin = 16f;
        text.fontSizeMax = maxSize;
        text.fontStyle = FontStyles.Bold;
        ApplyTextColor(text, PrimaryTextColor);
        return text;
    }

    private static void ApplyTextColor(TMP_Text text, Color color)
    {
        text.color = color;
        text.faceColor = color;
        text.outlineColor = new Color(0f, 0f, 0f, 0.9f);
        text.outlineWidth = 0.18f;
    }

    private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 size, Vector2 pivot)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    private static void Stretch(RectTransform rect, float inset)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(inset, inset);
        rect.offsetMax = new Vector2(-inset, -inset);
    }
}
