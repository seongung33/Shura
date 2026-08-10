using System.Collections.Generic;
using TMPro;
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
    private TMP_Text targetLabel;
    private Color normalTextColor;
    private Color focusTextColor;
    private Color targetTextColor;
    private bool colorFocusEnabled;
    private bool defaultFocused;
    private bool pointerInside;
    private bool selected;

    public void ConfigureFocusColors(
        Color normal,
        Color focused,
        bool isDefaultFocused,
        TMP_Text label = null,
        Color? normalLabelColor = null,
        Color? focusedLabelColor = null
    )
    {
        targetImage = GetComponent<Image>();
        normalColor = normal;
        focusColor = focused;
        defaultFocused = isDefaultFocused;
        colorFocusEnabled = targetImage != null;
        targetColor = defaultFocused ? focusColor : normalColor;
        targetLabel = label;
        normalTextColor = normalLabelColor ?? Color.white;
        focusTextColor = focusedLabelColor ?? normalTextColor;
        targetTextColor = defaultFocused ? focusTextColor : normalTextColor;

        if (targetImage != null)
        {
            targetImage.color = targetColor;
        }

        if (targetLabel != null)
        {
            targetLabel.color = targetTextColor;
            targetLabel.faceColor = targetTextColor;
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
        selected = false;
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

        if (targetLabel != null)
        {
            Color labelColor = Color.Lerp(
                targetLabel.color,
                targetTextColor,
                1f - Mathf.Exp(-16f * Time.unscaledDeltaTime)
            );
            targetLabel.color = labelColor;
            targetLabel.faceColor = labelColor;
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
        if (!selected)
        {
            RestoreDefaultFocus();
        }
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
        selected = true;
        SetFocused(this);
    }

    public void OnDeselect(BaseEventData eventData)
    {
        selected = false;
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
            motion.targetTextColor = motion == focused
                ? motion.focusTextColor
                : motion.normalTextColor;
        }
    }

    private static void RestoreDefaultFocus()
    {
        foreach (MainMenuButtonMotion motion in FocusGroup)
        {
            if (motion != null && motion.selected && motion.colorFocusEnabled)
            {
                SetFocused(motion);
                return;
            }
        }

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
        if (targetLabel != null)
        {
            targetTextColor = defaultFocused ? focusTextColor : normalTextColor;
        }
    }
}

/// <summary>
/// A package-free, soft center vignette used to separate the menu from busy artwork.
/// </summary>
public sealed class MainMenuCenterShade : MaskableGraphic
{
    private const int Columns = 16;
    private const int Rows = 12;

    private float strength = 0.24f;
    private Vector2 radius = new(0.58f, 0.96f);

    public void Configure(float alpha, Vector2 normalizedRadius)
    {
        strength = Mathf.Clamp01(alpha);
        radius = new Vector2(
            Mathf.Max(0.01f, normalizedRadius.x),
            Mathf.Max(0.01f, normalizedRadius.y)
        );
        color = Color.black;
        raycastTarget = false;
        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper vertexHelper)
    {
        vertexHelper.Clear();
        Rect bounds = GetPixelAdjustedRect();

        for (int row = 0; row <= Rows; row++)
        {
            float v = row / (float)Rows;
            for (int column = 0; column <= Columns; column++)
            {
                float u = column / (float)Columns;
                float normalizedX = (u * 2f - 1f) / radius.x;
                float normalizedY = (v * 2f - 1f) / radius.y;
                float distance = Mathf.Sqrt(
                    normalizedX * normalizedX + normalizedY * normalizedY
                );
                float falloff = Mathf.SmoothStep(1f, 0f, distance);
                Color vertexColor = new(0f, 0f, 0f, strength * falloff);

                vertexHelper.AddVert(
                    new Vector3(
                        Mathf.Lerp(bounds.xMin, bounds.xMax, u),
                        Mathf.Lerp(bounds.yMin, bounds.yMax, v)
                    ),
                    vertexColor,
                    new Vector2(u, v)
                );
            }
        }

        int stride = Columns + 1;
        for (int row = 0; row < Rows; row++)
        {
            for (int column = 0; column < Columns; column++)
            {
                int bottomLeft = row * stride + column;
                int bottomRight = bottomLeft + 1;
                int topLeft = bottomLeft + stride;
                int topRight = topLeft + 1;
                vertexHelper.AddTriangle(bottomLeft, topLeft, topRight);
                vertexHelper.AddTriangle(bottomLeft, topRight, bottomRight);
            }
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
