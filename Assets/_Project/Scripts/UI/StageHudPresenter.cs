using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public sealed class StageHudPresenter : MonoBehaviour
{
    private WaveManager waveManager;
    private EnemySpawner enemySpawner;
    private PlayerExperience playerExperience;
    private TMP_Text stageText;
    private TMP_Text levelText;

    private void Start()
    {
        if (waveManager == null)
        {
            waveManager = FindFirstObjectByType<WaveManager>();
        }

        if (enemySpawner == null)
        {
            enemySpawner = FindFirstObjectByType<EnemySpawner>();
        }

        if (stageText == null)
        {
            CreateView();
        }

        ResolvePlayer();
    }

    public void Configure(WaveManager wave, EnemySpawner spawner)
    {
        waveManager = wave;
        enemySpawner = spawner;
        if (stageText == null)
        {
            CreateView();
        }
        ResolvePlayer();
    }

    private void Update()
    {
        if (playerExperience == null)
        {
            ResolvePlayer();
        }

        if (waveManager != null && stageText != null)
        {
            int elapsed = Mathf.FloorToInt(waveManager.ElapsedTime);
            int duration = Mathf.FloorToInt(waveManager.Duration);
            string phase = waveManager.RoundFinished
                ? "보스"
                : waveManager.CleanupStarted
                    ? "정리"
                    : $"웨이브 {Mathf.Max(1, waveManager.CurrentWave)}";
            stageText.text =
                $"{elapsed / 60:00}:{elapsed % 60:00} / " +
                $"{duration / 60:00}:{duration % 60:00}   {phase}   " +
                $"적 {enemySpawner?.AliveCount ?? 0}/{enemySpawner?.TargetAlive ?? 0}";
        }

        if (playerExperience != null && levelText != null)
        {
            NetworkPlayerExperience networkExperience =
                playerExperience.GetComponent<NetworkPlayerExperience>();
            int pendingChoices = networkExperience != null
                ? networkExperience.PendingChoiceCount
                : 0;
            string choiceText = pendingChoices > 0
                ? $"   선택 대기 {pendingChoices}"
                : string.Empty;
            levelText.text =
                $"팀 Lv.{playerExperience.CurrentLevel}   " +
                $"EXP {playerExperience.CurrentExperience}/" +
                $"{playerExperience.ExperienceToNextLevel}{choiceText}";
        }
    }

    private void ResolvePlayer()
    {
        if (NetworkManager.Singleton != null &&
            NetworkManager.Singleton.IsListening)
        {
            NetworkObject localPlayer = NetworkManager.Singleton.LocalClient?.PlayerObject;
            playerExperience = localPlayer?.GetComponent<PlayerExperience>();
            return;
        }

        GameObject localPlayerObject = GameObject.FindGameObjectWithTag("Player");
        playerExperience = localPlayerObject?.GetComponent<PlayerExperience>();
    }

    private void CreateView()
    {
        GameObject canvasObject = new GameObject(
            "StageHudCanvas",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler)
        );
        canvasObject.transform.SetParent(transform, false);

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 45;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        stageText = CreateText(
            canvasObject.transform,
            "StageProgress",
            new Vector2(0.5f, 1f),
            new Vector2(0f, -38f),
            new Vector2(900f, 58f)
        );

        levelText = CreateText(
            canvasObject.transform,
            "TeamLevel",
            new Vector2(0.5f, 1f),
            new Vector2(0f, -96f),
            new Vector2(900f, 52f)
        );
    }

    private static TMP_Text CreateText(
        Transform parent,
        string objectName,
        Vector2 anchor,
        Vector2 position,
        Vector2 size
    )
    {
        GameObject textObject = new GameObject(
            objectName,
            typeof(RectTransform),
            typeof(TextMeshProUGUI)
        );
        textObject.transform.SetParent(parent, false);

        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = anchor;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        TMP_Text text = textObject.GetComponent<TextMeshProUGUI>();
        text.alignment = TextAlignmentOptions.Center;
        text.enableAutoSizing = true;
        text.fontSizeMin = 20f;
        text.fontSizeMax = 34f;
        text.fontStyle = FontStyles.Bold;
        text.color = Color.white;
        text.outlineWidth = 0.2f;
        return text;
    }
}
