using UnityEngine;
using TMPro;

public class HUDController : MonoBehaviour
{
    [SerializeField]
    private PlayerHealth playerHealth;

    [SerializeField]
    private PlayerExperience playerExperience;

    [SerializeField]
    private TMP_Text healthText;

    [SerializeField]
    private TMP_Text levelText;

    [SerializeField]
    private GameObject resultPanel;

    private void Update()
    {
        if (playerHealth == null)
        {
            if (resultPanel != null && !resultPanel.activeSelf)
            {
                resultPanel.SetActive(true);
            }
            return;
        }

        if (healthText != null)
        {
            healthText.text = $"HP {playerHealth.CurrentHealth:0} / {playerHealth.MaxHealth:0}";
        }

        if (levelText != null && playerExperience != null)
        {
            levelText.text = $"Lv.{playerExperience.CurrentLevel}  EXP {playerExperience.CurrentExperience}/{playerExperience.ExperienceToNextLevel}";
        }
    }
}
