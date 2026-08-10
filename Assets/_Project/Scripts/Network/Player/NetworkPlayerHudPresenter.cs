using Shura.Player;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[RequireComponent(typeof(PlayerHealth))]
[RequireComponent(typeof(PlayerExperience))]
public class NetworkPlayerHudPresenter : NetworkBehaviour
{
    [SerializeField]
    private string gameplaySceneName = "Main";

    private PlayerHealth playerHealth;
    private PlayerExperience playerExperience;
    private GameObject canvasObject;
    private GameObject hudPanel;
    private TMP_Text healthText;
    private TMP_Text progressText;

    private void Awake()
    {
        playerHealth = GetComponent<PlayerHealth>();
        playerExperience = GetComponent<PlayerExperience>();
    }

    public override void OnNetworkSpawn()
    {
        if (!IsOwner)
        {
            return;
        }

        CreateHud();
        SceneManager.sceneLoaded += HandleSceneLoaded;
        RefreshSceneVisibility();
    }

    public override void OnNetworkDespawn()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;

        if (canvasObject != null)
        {
            Destroy(canvasObject);
        }
    }

    private void Update()
    {
        if (FindFirstObjectByType<StageHudPresenter>() != null)
        {
            if (hudPanel != null && hudPanel.activeSelf)
            {
                hudPanel.SetActive(false);
            }
            return;
        }

        if (!IsOwner || hudPanel == null || !hudPanel.activeSelf)
        {
            return;
        }

        healthText.text =
            $"HP  {playerHealth.CurrentHealth:0} / {playerHealth.MaxHealth:0}";

        progressText.text =
            $"Lv.{playerExperience.CurrentLevel}    " +
            $"EXP  {playerExperience.CurrentExperience} / " +
            $"{playerExperience.ExperienceToNextLevel}";
    }

    private void CreateHud()
    {
        canvasObject = new GameObject(
            "NetworkPlayerHudCanvas",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler)
        );
        DontDestroyOnLoad(canvasObject);

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        hudPanel = new GameObject(
            "PlayerStatusPanel",
            typeof(RectTransform),
            typeof(Image)
        );
        hudPanel.transform.SetParent(canvasObject.transform, false);

        RectTransform panelRect = hudPanel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0f, 1f);
        panelRect.anchorMax = new Vector2(0f, 1f);
        panelRect.pivot = new Vector2(0f, 1f);
        panelRect.anchoredPosition = new Vector2(32f, -32f);
        panelRect.sizeDelta = new Vector2(500f, 150f);

        Image panelImage = hudPanel.GetComponent<Image>();
        panelImage.color = new Color(0.04f, 0.06f, 0.1f, 0.82f);

        healthText = CreateText(
            "HealthText",
            new Vector2(0.05f, 0.52f),
            new Vector2(0.95f, 0.92f),
            new Color(0.95f, 0.35f, 0.35f, 1f)
        );

        progressText = CreateText(
            "ProgressText",
            new Vector2(0.05f, 0.08f),
            new Vector2(0.95f, 0.48f),
            new Color(0.9f, 0.92f, 1f, 1f)
        );
    }

    private TMP_Text CreateText(
        string objectName,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Color color
    )
    {
        GameObject textObject = new GameObject(
            objectName,
            typeof(RectTransform),
            typeof(TextMeshProUGUI)
        );
        textObject.transform.SetParent(hudPanel.transform, false);

        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = anchorMin;
        textRect.anchorMax = anchorMax;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        TMP_Text text = textObject.GetComponent<TextMeshProUGUI>();
        text.alignment = TextAlignmentOptions.MidlineLeft;
        text.enableAutoSizing = true;
        text.fontSizeMin = 22f;
        text.fontSizeMax = 38f;
        text.fontStyle = FontStyles.Bold;
        text.color = color;
        return text;
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        RefreshSceneVisibility();
    }

    private void RefreshSceneVisibility()
    {
        if (hudPanel == null)
        {
            return;
        }

        hudPanel.SetActive(
            SceneManager.GetActiveScene().name == gameplaySceneName &&
            FindFirstObjectByType<StageHudPresenter>() == null
        );
    }
}
