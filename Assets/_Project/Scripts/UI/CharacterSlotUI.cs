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
            portraitImage.preserveAspect = true;
        }

        if (characterNameText != null)
        {
            characterNameText.text = characterData.characterName;
        }
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