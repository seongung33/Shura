using UnityEngine;
using UnityEngine.SceneManagement;
public class StartMenuController : MonoBehaviour
{
    [SerializeField] private GameObject startMenuPanel;
    [SerializeField] private GameObject multiplayerPanel;

    public void OpenMultiplayer()
    {
        startMenuPanel.SetActive(false);
        multiplayerPanel.SetActive(true);
    }
    public void BackToStartMenu()
    {
        multiplayerPanel.SetActive(false);
        startMenuPanel.SetActive(true);
    }
    public void OpenMultiplayerScene()
    {
        SceneManager.LoadScene("MultiPlayerEntry");
    }
    public void OpenSinglePlayerScene()
    {
        SceneManager.LoadScene("CharacterSelect");
    }
    public void OpenMainMenuScene()
    {
        SceneManager.LoadScene("MainMenu");
    }
}
