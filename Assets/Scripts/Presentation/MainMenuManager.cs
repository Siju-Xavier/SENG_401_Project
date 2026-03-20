using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    /// <summary>Flag checked by GameManager on scene load to restore save.</summary>
    public static bool ShouldLoadSave { get; private set; }

    // This method will be linked to the "Start New Game" button
    public void StartNewGame()
    {
        Debug.Log("Loading Game...");
        ShouldLoadSave = false;
        SceneManager.LoadScene("Game");
    }

    // This method will be linked to the "Continue" button
    public void ContinueGame()
    {
        if (Persistence.SaveManager.HasLocalSave())
        {
            Debug.Log("Loading saved game...");
            ShouldLoadSave = true;
            SceneManager.LoadScene("Game");
        }
        else
        {
            Debug.LogWarning("No save file found — cannot continue.");
        }
    }

    // This method will be linked to the "Settings" button
    public void OpenSettings()
    {
        Debug.Log("Settings opened - feature coming soon!");
    }
}

