using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class NetworkTeammateOffscreenIndicator : MonoBehaviour
{
    private const string GameplaySceneName = "Main";
    private const float EdgePadding = 54f;
    private const float PlayerRefreshInterval = 0.5f;

    private sealed class IndicatorView
    {
        public GameObject Root;
        public RectTransform Rect;
        public Image Arrow;
        public TMP_Text StateText;
    }

    private static Sprite arrowSprite;
    private readonly Dictionary<NetworkPlayerHealth, IndicatorView> views = new();
    private readonly List<NetworkPlayerHealth> stalePlayers = new();

    private NetworkPlayerHealth localPlayer;
    private GameObject canvasObject;
    private RectTransform canvasRect;
    private Camera gameplayCamera;
    private float nextRefreshTime;

    private void Awake()
    {
        localPlayer = GetComponent<NetworkPlayerHealth>();
    }

    private void OnEnable()
    {
        CreateCanvas();
        RefreshPlayers();
    }

    private void Update()
    {
        bool inGameplay = SceneManager.GetActiveScene().name == GameplaySceneName;

        if (canvasObject != null && canvasObject.activeSelf != inGameplay)
        {
            canvasObject.SetActive(inGameplay);
        }

        if (!inGameplay)
        {
            return;
        }

        if (Time.unscaledTime >= nextRefreshTime)
        {
            RefreshPlayers();
        }

        if (gameplayCamera == null)
        {
            gameplayCamera = Camera.main;
        }

        if (gameplayCamera == null)
        {
            SetAllVisible(false);
            return;
        }

        foreach (KeyValuePair<NetworkPlayerHealth, IndicatorView> pair in views)
        {
            UpdateIndicator(pair.Key, pair.Value);
        }
    }

    private void OnDisable()
    {
        DestroyCanvas();
    }

    private void OnDestroy()
    {
        DestroyCanvas();
    }

    private void RefreshPlayers()
    {
        nextRefreshTime = Time.unscaledTime + PlayerRefreshInterval;
        NetworkPlayerHealth[] players = FindObjectsByType<NetworkPlayerHealth>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None
        );

        foreach (NetworkPlayerHealth player in players)
        {
            if (player == null || player == localPlayer || !player.IsSpawned)
            {
                continue;
            }

            if (!views.ContainsKey(player))
            {
                views[player] = CreateIndicator();
            }
        }

        stalePlayers.Clear();

        foreach (NetworkPlayerHealth player in views.Keys)
        {
            if (player == null || !player.IsSpawned)
            {
                stalePlayers.Add(player);
            }
        }

        foreach (NetworkPlayerHealth player in stalePlayers)
        {
            if (views.TryGetValue(player, out IndicatorView view) &&
                view.Root != null)
            {
                Destroy(view.Root);
            }

            views.Remove(player);
        }
    }

    private void UpdateIndicator(
        NetworkPlayerHealth teammate,
        IndicatorView view
    )
    {
        if (teammate == null || !teammate.IsSpawned || view.Root == null)
        {
            return;
        }

        Vector3 viewportPoint = gameplayCamera.WorldToViewportPoint(
            teammate.transform.position
        );
        bool behindCamera = viewportPoint.z <= 0f;

        bool isOnScreen = !behindCamera &&
            viewportPoint.x >= 0f && viewportPoint.x <= 1f &&
            viewportPoint.y >= 0f && viewportPoint.y <= 1f;

        if (isOnScreen)
        {
            view.Root.SetActive(false);
            return;
        }

        view.Root.SetActive(true);

        Vector2 direction = new(
            viewportPoint.x - 0.5f,
            viewportPoint.y - 0.5f
        );

        if (behindCamera)
        {
            direction = -direction;
        }

        if (direction.sqrMagnitude < 0.001f)
        {
            direction = Vector2.up;
        }
        else
        {
            direction.Normalize();
        }

        Rect canvasBounds = canvasRect.rect;
        float horizontalLimit = Mathf.Max(
            1f,
            canvasBounds.width * 0.5f - EdgePadding
        );
        float verticalLimit = Mathf.Max(
            1f,
            canvasBounds.height * 0.5f - EdgePadding
        );
        float horizontalScale = Mathf.Abs(direction.x) > 0.001f
            ? horizontalLimit / Mathf.Abs(direction.x)
            : float.MaxValue;
        float verticalScale = Mathf.Abs(direction.y) > 0.001f
            ? verticalLimit / Mathf.Abs(direction.y)
            : float.MaxValue;
        float edgeScale = Mathf.Min(horizontalScale, verticalScale);

        view.Rect.anchoredPosition = direction * edgeScale;
        view.Rect.localEulerAngles = new Vector3(
            0f,
            0f,
            Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f
        );

        bool isDead = teammate.IsDead;
        view.Arrow.color = isDead
            ? new Color(1f, 0.42f, 0.24f, 0.68f)
            : new Color(0.38f, 0.88f, 0.82f, 0.42f);
        view.Rect.sizeDelta = isDead
            ? new Vector2(38f, 38f)
            : new Vector2(30f, 30f);
        view.StateText.gameObject.SetActive(isDead);
        view.StateText.rectTransform.localEulerAngles = -view.Rect.localEulerAngles;
    }

    private void CreateCanvas()
    {
        if (canvasObject != null)
        {
            return;
        }

        canvasObject = new GameObject(
            "TeammateOffscreenIndicators",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler)
        );
        DontDestroyOnLoad(canvasObject);

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 45;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        canvasRect = canvasObject.GetComponent<RectTransform>();
    }

    private IndicatorView CreateIndicator()
    {
        GameObject root = new GameObject(
            "TeammateDirection",
            typeof(RectTransform),
            typeof(Image)
        );
        root.transform.SetParent(canvasRect, false);

        RectTransform rect = root.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(30f, 30f);

        Image arrow = root.GetComponent<Image>();
        arrow.sprite = GetArrowSprite();
        arrow.preserveAspect = true;
        arrow.raycastTarget = false;

        GameObject stateObject = new GameObject(
            "DownState",
            typeof(RectTransform),
            typeof(TextMeshProUGUI)
        );
        stateObject.transform.SetParent(root.transform, false);

        RectTransform stateRect = stateObject.GetComponent<RectTransform>();
        stateRect.anchorMin = new Vector2(0.5f, 0f);
        stateRect.anchorMax = new Vector2(0.5f, 0f);
        stateRect.pivot = new Vector2(0.5f, 1f);
        stateRect.anchoredPosition = new Vector2(0f, -3f);
        stateRect.sizeDelta = new Vector2(58f, 18f);

        TMP_Text stateText = stateObject.GetComponent<TMP_Text>();
        stateText.text = "DOWN";
        stateText.alignment = TextAlignmentOptions.Center;
        stateText.fontSize = 11f;
        stateText.fontStyle = FontStyles.Bold;
        stateText.color = new Color(1f, 0.66f, 0.48f, 0.75f);
        stateText.raycastTarget = false;

        root.SetActive(false);
        return new IndicatorView
        {
            Root = root,
            Rect = rect,
            Arrow = arrow,
            StateText = stateText
        };
    }

    private void SetAllVisible(bool visible)
    {
        foreach (IndicatorView view in views.Values)
        {
            if (view.Root != null)
            {
                view.Root.SetActive(visible);
            }
        }
    }

    private void DestroyCanvas()
    {
        views.Clear();
        stalePlayers.Clear();

        if (canvasObject != null)
        {
            Destroy(canvasObject);
            canvasObject = null;
            canvasRect = null;
        }
    }

    private static Sprite GetArrowSprite()
    {
        if (arrowSprite != null)
        {
            return arrowSprite;
        }

        const int size = 32;
        Texture2D texture = new(size, size, TextureFormat.RGBA32, false)
        {
            name = "TeammateArrowTexture",
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };
        Color[] pixels = new Color[size * size];

        for (int y = 3; y < 29; y++)
        {
            float progress = (y - 3f) / 26f;
            float halfWidth = Mathf.Lerp(10f, 0.5f, progress);

            for (int x = 0; x < size; x++)
            {
                if (Mathf.Abs(x - 15.5f) <= halfWidth)
                {
                    pixels[y * size + x] = Color.white;
                }
            }
        }

        texture.SetPixels(pixels);
        texture.Apply(false, true);
        arrowSprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, size, size),
            new Vector2(0.5f, 0.5f),
            size
        );
        arrowSprite.name = "TeammateArrowSprite";
        return arrowSprite;
    }
}
