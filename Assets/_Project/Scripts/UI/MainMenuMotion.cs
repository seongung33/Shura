using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class MainMenuButtonMotion : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler,
    IPointerDownHandler,
    IPointerUpHandler,
    ISelectHandler,
    IDeselectHandler
{
    private static readonly List<MainMenuButtonMotion> FocusGroup = new();

    private Vector3 targetScale = Vector3.one;
    private Image targetImage;
    private Color normalColor;
    private Color focusColor;
    private Color targetColor;
    private bool colorFocusEnabled;
    private bool defaultFocused;
    private bool pointerInside;

    public void ConfigureFocusColors(
        Color normal,
        Color focused,
        bool isDefaultFocused
    )
    {
        targetImage = GetComponent<Image>();
        normalColor = normal;
        focusColor = focused;
        defaultFocused = isDefaultFocused;
        colorFocusEnabled = targetImage != null;
        targetColor = defaultFocused ? focusColor : normalColor;

        if (targetImage != null)
        {
            targetImage.color = targetColor;
        }
    }

    private void OnEnable()
    {
        if (!FocusGroup.Contains(this))
        {
            FocusGroup.Add(this);
        }

        transform.localScale = Vector3.one;
        targetScale = Vector3.one;
        ApplyDefaultFocus();
    }

    private void OnDisable()
    {
        FocusGroup.Remove(this);
    }

    private void Update()
    {
        transform.localScale = Vector3.Lerp(
            transform.localScale,
            targetScale,
            1f - Mathf.Exp(-14f * Time.unscaledDeltaTime)
        );

        if (colorFocusEnabled && targetImage != null)
        {
            targetImage.color = Color.Lerp(
                targetImage.color,
                targetColor,
                1f - Mathf.Exp(-16f * Time.unscaledDeltaTime)
            );
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        pointerInside = true;
        targetScale = Vector3.one * 1.035f;
        SetFocused(this);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        pointerInside = false;
        targetScale = Vector3.one;
        RestoreDefaultFocus();
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

    public void OnSelect(BaseEventData eventData)
    {
        SetFocused(this);
    }

    public void OnDeselect(BaseEventData eventData)
    {
        if (!pointerInside)
        {
            RestoreDefaultFocus();
        }
    }

    private static void SetFocused(MainMenuButtonMotion focused)
    {
        foreach (MainMenuButtonMotion motion in FocusGroup)
        {
            if (motion == null || !motion.colorFocusEnabled)
            {
                continue;
            }

            motion.targetColor = motion == focused
                ? motion.focusColor
                : motion.normalColor;
        }
    }

    private static void RestoreDefaultFocus()
    {
        foreach (MainMenuButtonMotion motion in FocusGroup)
        {
            motion?.ApplyDefaultFocus();
        }
    }

    private void ApplyDefaultFocus()
    {
        if (colorFocusEnabled)
        {
            targetColor = defaultFocused ? focusColor : normalColor;
        }
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
