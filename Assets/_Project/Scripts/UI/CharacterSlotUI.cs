using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CharacterSlotUI : MonoBehaviour
{
    [Header("슬롯 UI")]
    [SerializeField] private Image portraitImage;
    [SerializeField] private TMP_Text characterNameText;
    [SerializeField] private Button button;

    private int characterId;
    private CharacterData characterData;
    private CharacterInfoUI characterInfoUI;
    private MyPlayerSlotUI myPlayerSlotUI;
    private Action<int> onCharacterSelected;
    private GameObject portraitFallback;

    private void Awake()
    {
        if (button == null)
        {
            button = GetComponent<Button>();
        }

        if (button != null)
        {
            button.onClick.AddListener(SelectCharacter);
        }
    }

    public void Initialize(
        int id,
        CharacterData data,
        CharacterInfoUI infoUI,
        MyPlayerSlotUI playerSlotUI,
        Action<int> selectedCallback)
    {
        characterId = id;
        characterData = data;
        characterInfoUI = infoUI;
        myPlayerSlotUI = playerSlotUI;
        onCharacterSelected = selectedCallback;

        if (characterData == null)
            return;

        if (portraitImage != null)
        {
            portraitImage.sprite = characterData.portrait;
            portraitImage.color = Color.white;
            portraitImage.preserveAspect = true;
            portraitImage.enabled = characterData.portrait != null;
            SetPortraitFallbackVisible(characterData.portrait == null);
        }

        if (characterNameText != null)
        {
            characterNameText.text = characterData.characterName;
        }
    }

    private void SetPortraitFallbackVisible(bool visible)
    {
        if (portraitImage == null || portraitImage.transform.parent == null)
        {
            return;
        }

        if (portraitFallback == null)
        {
            Transform existing = portraitImage.transform.parent.Find(
                "PortraitFallbackRuntime"
            );
            portraitFallback = existing != null
                ? existing.gameObject
                : CreatePortraitFallback();
        }

        if (portraitFallback != null)
        {
            portraitFallback.SetActive(visible);
        }
    }

    private GameObject CreatePortraitFallback()
    {
        GameObject fallback = new GameObject(
            "PortraitFallbackRuntime",
            typeof(RectTransform),
            typeof(Image),
            typeof(Outline)
        );
        fallback.transform.SetParent(portraitImage.transform.parent, false);

        RectTransform source = portraitImage.rectTransform;
        RectTransform rect = fallback.GetComponent<RectTransform>();
        rect.anchorMin = source.anchorMin;
        rect.anchorMax = source.anchorMax;
        rect.pivot = source.pivot;
        rect.anchoredPosition = source.anchoredPosition;
        rect.sizeDelta = source.sizeDelta;
        rect.localScale = Vector3.one;

        Image background = fallback.GetComponent<Image>();
        background.color = new Color32(9, 22, 42, 255);
        background.raycastTarget = false;

        Outline outline = fallback.GetComponent<Outline>();
        outline.effectColor = new Color32(24, 174, 211, 180);
        outline.effectDistance = new Vector2(2f, -2f);

        GameObject labelObject = new GameObject(
            "Label",
            typeof(RectTransform),
            typeof(TextMeshProUGUI)
        );
        labelObject.transform.SetParent(fallback.transform, false);
        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(12f, 12f);
        labelRect.offsetMax = new Vector2(-12f, -12f);

        TMP_Text label = labelObject.GetComponent<TMP_Text>();
        label.text = "초상화\n준비 중";
        label.alignment = TextAlignmentOptions.Center;
        label.fontStyle = FontStyles.Bold;
        label.fontSize = 24f;
        label.color = new Color32(158, 197, 219, 255);
        label.faceColor = label.color;
        label.raycastTarget = false;
        if (characterNameText != null && characterNameText.font != null)
        {
            label.font = characterNameText.font;
        }

        fallback.transform.SetSiblingIndex(
            portraitImage.transform.GetSiblingIndex() + 1
        );
        return fallback;
    }

    public void SelectCharacter()
    {
        if (characterData == null)
            return;

        characterInfoUI?.ShowCharacter(characterData);
        myPlayerSlotUI?.ShowCharacter(characterData);

        onCharacterSelected?.Invoke(characterId);
    }
}
