using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CharacterSlotUI : MonoBehaviour
{
    [SerializeField] private CharacterData characterData;
    [SerializeField] private CharacterInfoUI characterInfoUI;

    [Header("슬롯 UI")]
    [SerializeField] private Image portraitImage;
    [SerializeField] private TMP_Text characterNameText;

    private void Start()
    {
        RefreshSlot();
    }

    private void RefreshSlot()
    {
        if (characterData == null)
            return;

        portraitImage.sprite = characterData.portrait;
        characterNameText.text = characterData.characterName;
    }

    public void SelectCharacter()
    {
        if (characterData == null || characterInfoUI == null)
            return;

        characterInfoUI.ShowCharacter(characterData);
    }
}