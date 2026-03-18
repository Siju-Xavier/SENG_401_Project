using UnityEngine;
using UnityEngine.SceneManagement; // This is required to load new scenes!

public class MainMenuManager : MonoBehaviour
{
    // This method will be linked to the "Start New Game" button
    public void StartNewGame()
    {
        Debug.Log("Loading Game...");
        // "Game" must be the exact name of your saved game scene file!
        SceneManager.LoadScene("Game"); 
    }

    // This method will be linked to the "Continue" button
    public void ContinueGame()
    {
        Debug.Log("Continue Game clicked - feature coming soon!");
        // We will add save loading logic here later
    }

    // This method will be linked to the "Settings" button
    public void OpenSettings()
    {
        Debug.Log("Settings opened - feature coming soon!");
         // We will open a settings menu panel here later
    }
}
