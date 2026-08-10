using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MyPlayerSlotUI : MonoBehaviour
{
    [SerializeField] private Image portraitImage;
    [SerializeField] private TMP_Text characterNameText;

    private CharacterData selectedCharacter;

    public CharacterData SelectedCharacter
    {
        get { return selectedCharacter; }
    }

    private void Awake()
    {
        if (portraitImage != null && portraitImage.sprite == null)
        {
            portraitImage.enabled = false;
        }

        if (characterNameText != null)
        {
            characterNameText.text = "캐릭터 미선택";
        }
    }

    public void ShowCharacter(CharacterData data)
    {
        if (data == null)
            return;

        selectedCharacter = data;

        if (portraitImage != null)
        {
            portraitImage.sprite = data.portrait;
            portraitImage.color = Color.white;
            portraitImage.preserveAspect = true;
            portraitImage.enabled = data.portrait != null;
        }

        if (characterNameText != null)
        {
            characterNameText.text = data.characterName;
        }
    }
}