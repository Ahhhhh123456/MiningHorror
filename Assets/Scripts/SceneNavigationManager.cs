using UnityEngine;
using UnityEngine.SceneManagement; // Required for loading scenes

public class SceneNavigationManager : MonoBehaviour
{
    SteamLobbyManager steamLobbyManager;

    public void LoadScene(string sceneName)
    {
        // Load the scene
        SceneManager.LoadScene(sceneName);

        // Find the SteamLobbyManager in the scene
        SteamLobbyManager lobbyManager = FindObjectOfType<SteamLobbyManager>();
        if (lobbyManager != null)
        {
            lobbyManager.MakeLobby();
        }
        else
        {
            Debug.LogError("SteamLobbyManager not found in scene!");
        }
    }

    /**
     * This function will handle the "Exit" button.
     */
    public void QuitGame()
    {
        // This line only works in a built game (not in the Unity Editor)
        Application.Quit();

        // Use this line if you want to test in the editor
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #endif

        Debug.Log("Quit Game Requested");
    }
}