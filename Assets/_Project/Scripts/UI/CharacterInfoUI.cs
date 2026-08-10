using TMPro;
using UnityEngine;

public class CharacterInfoUI : MonoBehaviour
{
    [SerializeField] private TMP_Text characterNameText;
    [SerializeField] private TMP_Text characterRoleText;
    [SerializeField] private TMP_Text characterDescriptionText;

    public void ShowCharacter(CharacterData data)
    {
        if (data == null)
            return;

        characterNameText.text = data.characterName;
        characterRoleText.text = data.role;
        characterDescriptionText.text = data.description;
    }
}