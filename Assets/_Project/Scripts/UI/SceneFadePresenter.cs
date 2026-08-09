using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 씬이 바뀔 때 새 화면이 갑자기 나타나지 않도록 짧은 암전 해제를 제공한다.
/// 네트워크 씬 전환을 가로채지 않고 sceneLoaded 결과만 표현한다.
/// </summary>
public sealed class SceneFadePresenter : MonoBehaviour
{
    private const float FadeDuration = 0.32f;
    private static SceneFadePresenter instance;

    private CanvasGroup group;
    private Coroutine fadeRoutine;
    private bool firstScene = true;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void EnsureExists()
    {
        if (instance != null)
        {
            return;
        }

        new GameObject("SceneFadePresenter")
            .AddComponent<SceneFadePresenter>();
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
        BuildOverlay();
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void BuildOverlay()
    {
        GameObject canvasObject = new GameObject(
            "SceneFadeCanvas",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(CanvasGroup)
        );
        canvasObject.transform.SetParent(transform, false);

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        Image cover = new GameObject(
            "FadeCover",
            typeof(RectTransform),
            typeof(Image)
        ).GetComponent<Image>();
        cover.transform.SetParent(canvasObject.transform, false);
        RectTransform coverRect = cover.rectTransform;
        coverRect.anchorMin = Vector2.zero;
        coverRect.anchorMax = Vector2.one;
        coverRect.offsetMin = Vector2.zero;
        coverRect.offsetMax = Vector2.zero;
        cover.color = new Color(0.005f, 0.012f, 0.028f, 1f);

        group = canvasObject.GetComponent<CanvasGroup>();
        group.alpha = 0f;
        group.blocksRaycasts = false;
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (firstScene)
        {
            firstScene = false;
            if (scene.name == "MainMenu")
            {
                return;
            }
        }

        if (fadeRoutine != null)
        {
            StopCoroutine(fadeRoutine);
        }
        fadeRoutine = StartCoroutine(FadeFromBlack());
    }

    private IEnumerator FadeFromBlack()
    {
        group.alpha = 1f;
        group.blocksRaycasts = true;
        yield return null;

        float elapsed = 0f;
        while (elapsed < FadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / FadeDuration);
            group.alpha = 1f - t;
            yield return null;
        }

        group.alpha = 0f;
        group.blocksRaycasts = false;
        fadeRoutine = null;
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            instance = null;
        }
    }
}
