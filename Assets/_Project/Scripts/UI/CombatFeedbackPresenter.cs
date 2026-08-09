using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class CombatFeedbackPresenter : MonoBehaviour
{
    private static CombatFeedbackPresenter instance;

    private Canvas canvas;
    private Image[] dangerEdges;
    private GameObject bossWarning;
    private TMP_Text bossWarningText;
    private Coroutine bossRoutine;
    private float nextHealthRefresh;

    private void Awake()
    {
        instance = this;
        CreateOverlay();
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }

    private void Update()
    {
        if (Time.unscaledTime < nextHealthRefresh)
        {
            return;
        }

        nextHealthRefresh = Time.unscaledTime + 0.15f;
        Shura.Player.PlayerHealth player = FindFirstObjectByType<Shura.Player.PlayerHealth>();
        bool danger = player != null && !player.IsDead &&
            player.CurrentHealth / Mathf.Max(1f, player.MaxHealth) <= 0.25f;
        float alpha = danger ? 0.12f + Mathf.PingPong(Time.unscaledTime * 0.08f, 0.08f) : 0f;
        foreach (Image edge in dangerEdges)
        {
            edge.color = new Color(0.65f, 0.02f, 0.04f, alpha);
        }
    }

    public static void PlayHit(EnemyHealth target, float damage, bool defeated)
    {
        if (target == null)
        {
            return;
        }

        EnsureInstance();
        instance.StartCoroutine(instance.FlashSprite(target));
        instance.CreateDamageNumber(target.transform.position, damage, target.IsBoss, defeated);

        Shura.Camera.CameraFollow cameraFollow = FindFirstObjectByType<Shura.Camera.CameraFollow>();
        cameraFollow?.Shake(defeated || target.IsBoss ? 0.12f : 0.05f, defeated ? 0.18f : 0.08f);
        GameAudioController.PlayHit(defeated || target.IsBoss);
    }

    public static void ShowBossWarning(string bossName)
    {
        EnsureInstance();
        if (instance.bossRoutine != null)
        {
            instance.StopCoroutine(instance.bossRoutine);
        }
        instance.bossRoutine = instance.StartCoroutine(instance.PlayBossWarning(bossName));
        GameAudioController.PlayBossWarning();
    }

    private static void EnsureInstance()
    {
        if (instance != null)
        {
            return;
        }

        instance = FindFirstObjectByType<CombatFeedbackPresenter>();
        if (instance == null)
        {
            GameObject runtime = new GameObject("CombatFeedbackRuntime");
            instance = runtime.AddComponent<CombatFeedbackPresenter>();
        }
    }

    private IEnumerator FlashSprite(EnemyHealth target)
    {
        SpriteRenderer[] renderers = target.GetComponentsInChildren<SpriteRenderer>(true);
        Color[] colors = new Color[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
        {
            colors[i] = renderers[i].color;
            renderers[i].color = Color.white;
        }

        yield return new WaitForSecondsRealtime(0.06f);

        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
            {
                renderers[i].color = colors[i];
            }
        }
    }

    private void CreateDamageNumber(Vector3 worldPosition, float damage, bool boss, bool defeated)
    {
        GameObject numberObject = new GameObject("DamageNumber", typeof(RectTransform), typeof(TextMeshProUGUI));
        numberObject.transform.SetParent(canvas.transform, false);
        RectTransform rect = numberObject.GetComponent<RectTransform>();
        Vector2 screen = Camera.main != null
            ? Camera.main.WorldToScreenPoint(worldPosition + Vector3.up * 0.7f)
            : new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
        rect.position = screen;
        rect.sizeDelta = new Vector2(180f, 70f);

        TMP_Text text = numberObject.GetComponent<TMP_Text>();
        text.text = defeated ? $"{Mathf.CeilToInt(damage)}  처치!" : Mathf.CeilToInt(damage).ToString();
        text.alignment = TextAlignmentOptions.Center;
        text.fontStyle = FontStyles.Bold;
        text.fontSize = defeated ? 42f : boss ? 34f : 28f;
        Color numberColor = defeated
            ? new Color(1f, 0.82f, 0.18f)
            : boss
                ? new Color(1f, 0.48f, 0.32f)
                : new Color(1f, 0.94f, 0.9f);
        ApplyReadableColor(text, numberColor);
        text.outlineWidth = 0.2f;
        text.outlineColor = Color.black;
        StartCoroutine(AnimateNumber(rect, text));
    }

    private static IEnumerator AnimateNumber(RectTransform rect, TMP_Text text)
    {
        float elapsed = 0f;
        const float duration = 0.55f;
        Vector2 start = rect.anchoredPosition;
        while (elapsed < duration && rect != null)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            rect.anchoredPosition = start + Vector2.up * (55f * progress);
            Color color = text.color;
            color.a = 1f - progress;
            text.color = color;
            yield return null;
        }

        if (rect != null)
        {
            Destroy(rect.gameObject);
        }
    }

    private IEnumerator PlayBossWarning(string bossName)
    {
        bossWarningText.text = "경고\n" + bossName + " 출현";
        bossWarning.SetActive(true);
        yield return new WaitForSecondsRealtime(1.35f);
        bossWarning.SetActive(false);
        bossRoutine = null;
    }

    private void CreateOverlay()
    {
        GameObject canvasObject = new GameObject("CombatFeedbackCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        canvasObject.transform.SetParent(transform, false);
        canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 70;
        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        dangerEdges = new Image[4];
        dangerEdges[0] = CreateDangerEdge("DangerTop", canvas.transform, new Vector2(0f, 0.94f), Vector2.one);
        dangerEdges[1] = CreateDangerEdge("DangerBottom", canvas.transform, Vector2.zero, new Vector2(1f, 0.06f));
        dangerEdges[2] = CreateDangerEdge("DangerLeft", canvas.transform, Vector2.zero, new Vector2(0.035f, 1f));
        dangerEdges[3] = CreateDangerEdge("DangerRight", canvas.transform, new Vector2(0.965f, 0f), Vector2.one);

        bossWarning = new GameObject("BossWarning", typeof(RectTransform), typeof(Image));
        bossWarning.transform.SetParent(canvas.transform, false);
        RectTransform warningRect = bossWarning.GetComponent<RectTransform>();
        warningRect.anchorMin = new Vector2(0.5f, 0.5f);
        warningRect.anchorMax = new Vector2(0.5f, 0.5f);
        warningRect.pivot = new Vector2(0.5f, 0.5f);
        warningRect.sizeDelta = new Vector2(820f, 210f);
        bossWarning.GetComponent<Image>().color = new Color(0.03f, 0.01f, 0.02f, 0.86f);

        bossWarningText = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TMP_Text>();
        bossWarningText.transform.SetParent(bossWarning.transform, false);
        Stretch(bossWarningText.rectTransform);
        bossWarningText.alignment = TextAlignmentOptions.Center;
        bossWarningText.fontStyle = FontStyles.Bold;
        bossWarningText.fontSize = 52f;
        ApplyReadableColor(
            bossWarningText,
            new Color(1f, 0.22f, 0.22f)
        );
        bossWarningText.outlineWidth = 0.2f;
        bossWarning.SetActive(false);
    }

    private static void ApplyReadableColor(TMP_Text text, Color color)
    {
        text.color = color;
        text.faceColor = color;
        text.enableVertexGradient = false;
        text.colorGradient = new VertexGradient(color);
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static Image CreateDangerEdge(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax)
    {
        Image edge = new GameObject(name, typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        edge.transform.SetParent(parent, false);
        RectTransform rect = edge.rectTransform;
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        edge.raycastTarget = false;
        edge.color = Color.clear;
        return edge;
    }
}
