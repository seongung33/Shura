using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class UltimateCutInPresenter : MonoBehaviour
{
    private static UltimateCutInPresenter instance;
    private Canvas canvas;
    private Coroutine routine;

    public static void Show(GameObject owner)
    {
        if (owner == null)
        {
            return;
        }

        EnsureInstance();
        if (instance.routine != null)
        {
            instance.StopCoroutine(instance.routine);
        }

        instance.routine = instance.StartCoroutine(instance.Play(owner));
    }

    private static void EnsureInstance()
    {
        if (instance != null)
        {
            return;
        }

        instance = new GameObject("UltimateCutInRuntime")
            .AddComponent<UltimateCutInPresenter>();
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
        BuildCanvas();
    }

    private IEnumerator Play(GameObject owner)
    {
        CharacterData character = ResolveCharacter(owner);
        bool isCheok = character != null && character.CharacterId == 1;
        string heroName = isCheok ? "척준경" : "주몽";
        string quote = isCheok
            ? "비켜라.  길은 내가 낸다."
            : "한 발이면 충분하다.";
        Color accent = isCheok
            ? new Color(1f, 0.34f, 0.12f)
            : new Color(0.18f, 0.72f, 1f);
        Sprite portrait = character != null ? character.portrait : null;
        if (portrait == null)
        {
            SpriteRenderer renderer = owner.GetComponentInChildren<SpriteRenderer>(true);
            portrait = renderer != null ? renderer.sprite : null;
        }

        GameObject root = CreateRoot(accent, portrait, heroName, quote);
        CanvasGroup group = root.GetComponent<CanvasGroup>();
        RectTransform panel = root.transform.Find("CutInPanel") as RectTransform;
        Vector2 settled = panel.anchoredPosition;
        Vector2 entering = settled + Vector2.left * 220f;
        bool canSlowTime = !IsNetworked(owner);
        float originalTimeScale = Time.timeScale;
        if (canSlowTime)
        {
            Time.timeScale = Mathf.Min(originalTimeScale, 0.08f);
        }

        GameAudioController.PlayUltimateCue(isCheok);
        Shura.Camera.CameraFollow cameraFollow = FindFirstObjectByType<Shura.Camera.CameraFollow>();
        cameraFollow?.Shake(0.1f, 0.12f);

        float elapsed = 0f;
        const float enterDuration = 0.18f;
        while (elapsed < enterDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.SmoothStep(0f, 1f, elapsed / enterDuration);
            group.alpha = progress;
            panel.anchoredPosition = Vector2.Lerp(entering, settled, progress);
            yield return null;
        }

        if (canSlowTime)
        {
            Time.timeScale = originalTimeScale;
        }

        yield return new WaitForSecondsRealtime(0.5f);

        elapsed = 0f;
        const float exitDuration = 0.18f;
        while (elapsed < exitDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            group.alpha = 1f - Mathf.Clamp01(elapsed / exitDuration);
            yield return null;
        }

        Destroy(root);
        routine = null;
    }

    private GameObject CreateRoot(Color accent, Sprite portrait, string heroName, string quote)
    {
        GameObject root = new GameObject("UltimateCutIn", typeof(RectTransform), typeof(CanvasGroup));
        root.transform.SetParent(canvas.transform, false);
        Stretch(root.GetComponent<RectTransform>());
        root.GetComponent<CanvasGroup>().alpha = 0f;

        Image shade = CreateImage("Shade", root.transform, new Color(0.005f, 0.008f, 0.02f, 0.88f));
        Stretch(shade.rectTransform);

        Image topLine = CreateImage("TopAccent", root.transform, accent);
        SetAnchors(topLine.rectTransform, new Vector2(0f, 0.77f), new Vector2(1f, 0.79f));
        Image bottomLine = CreateImage("BottomAccent", root.transform, accent);
        SetAnchors(bottomLine.rectTransform, new Vector2(0f, 0.2f), new Vector2(1f, 0.22f));

        GameObject panel = new GameObject("CutInPanel", typeof(RectTransform));
        panel.transform.SetParent(root.transform, false);
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(1500f, 560f);

        if (portrait != null)
        {
            Image portraitImage = CreateImage("Portrait", panel.transform, Color.white);
            RectTransform portraitRect = portraitImage.rectTransform;
            portraitRect.anchorMin = new Vector2(0f, 0f);
            portraitRect.anchorMax = new Vector2(0.48f, 1f);
            portraitRect.offsetMin = Vector2.zero;
            portraitRect.offsetMax = Vector2.zero;
            portraitImage.sprite = portrait;
            portraitImage.preserveAspect = true;
        }

        TMP_Text nameText = CreateText("HeroName", panel.transform, heroName, 38f, accent);
        SetAnchors(nameText.rectTransform, new Vector2(0.45f, 0.6f), new Vector2(0.98f, 0.82f));
        nameText.alignment = TextAlignmentOptions.BottomLeft;

        TMP_Text quoteText = CreateText("Quote", panel.transform, quote, 64f, Color.white);
        SetAnchors(quoteText.rectTransform, new Vector2(0.45f, 0.18f), new Vector2(0.98f, 0.62f));
        quoteText.alignment = TextAlignmentOptions.MidlineLeft;
        quoteText.outlineWidth = 0.18f;
        quoteText.outlineColor = Color.black;

        return root;
    }

    private void BuildCanvas()
    {
        GameObject canvasObject = new GameObject(
            "UltimateCutInCanvas",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler)
        );
        canvasObject.transform.SetParent(transform, false);
        canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 950;
        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
    }

    private static CharacterData ResolveCharacter(GameObject owner)
    {
        NetworkPlayerCharacter networkCharacter = owner.GetComponent<NetworkPlayerCharacter>();
        if (networkCharacter != null && networkCharacter.Character != null)
        {
            return networkCharacter.Character;
        }

        return SinglePlayerSelection.Character;
    }

    private static bool IsNetworked(GameObject owner)
    {
        NetworkPlayerCharacter networkCharacter = owner.GetComponent<NetworkPlayerCharacter>();
        return networkCharacter != null && networkCharacter.IsSpawned;
    }

    private static Image CreateImage(string name, Transform parent, Color color)
    {
        Image image = new GameObject(name, typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        image.transform.SetParent(parent, false);
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private static TMP_Text CreateText(string name, Transform parent, string value, float size, Color color)
    {
        TMP_Text text = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TMP_Text>();
        text.transform.SetParent(parent, false);
        text.text = value;
        text.fontSize = size;
        text.fontStyle = FontStyles.Bold;
        text.color = color;
        text.faceColor = color;
        text.enableVertexGradient = false;
        text.raycastTarget = false;
        return text;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static void SetAnchors(RectTransform rect, Vector2 min, Vector2 max)
    {
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
