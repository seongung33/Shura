using UnityEngine;
using UnityEngine.EventSystems;

public sealed class MainMenuButtonMotion : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler,
    IPointerDownHandler,
    IPointerUpHandler
{
    private Vector3 targetScale = Vector3.one;

    private void OnEnable()
    {
        transform.localScale = Vector3.one;
        targetScale = Vector3.one;
    }

    private void Update()
    {
        transform.localScale = Vector3.Lerp(
            transform.localScale,
            targetScale,
            1f - Mathf.Exp(-14f * Time.unscaledDeltaTime)
        );
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        targetScale = Vector3.one * 1.035f;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        targetScale = Vector3.one;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        targetScale = Vector3.one * 0.975f;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        GameObject hovered = eventData.pointerEnter;
        bool pointerInside = hovered != null &&
            (hovered == gameObject || hovered.transform.IsChildOf(transform));
        targetScale = pointerInside
            ? Vector3.one * 1.035f
            : Vector3.one;
    }
}

public sealed class MainMenuBackdropMotion : MonoBehaviour
{
    private RectTransform rect;
    private Vector2 origin;

    private void Awake()
    {
        rect = (RectTransform)transform;
        origin = rect.anchoredPosition;
    }

    private void Update()
    {
        float phase = Time.unscaledTime * 0.12f;
        float zoom = 1.025f + Mathf.Sin(phase) * 0.008f;
        rect.localScale = new Vector3(zoom, zoom, 1f);
        rect.anchoredPosition = origin + new Vector2(
            Mathf.Sin(phase * 0.73f) * 7f,
            Mathf.Cos(phase * 0.61f) * 4f
        );
    }
}
