using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuManager : MonoBehaviour
{
    [Header("UI Panelleri")]
    public GameObject settingsPanel;

    [Header("Sahne Ýsimleri")]
    public string practiceSceneName = "PracticeScene";
    public string multiplayerSceneName = "MultiplayerScene";
    void Start()
    {
        if (settingsPanel != null)
            settingsPanel.SetActive(false);
    }

    public void OpenSettings()
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(true);
        }
    }

    public void CloseSettings()
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }
    }

    public void PlayPracticeMode()
    {
        SceneManager.LoadScene(practiceSceneName);
    }

    public void PlayMultiplayerMode()
    {
        SceneManager.LoadScene(multiplayerSceneName);
    }
}