using UnityEngine;
using UnityEngine.SceneManagement; // Required for loading scenes

public class SceneNavigationManager : MonoBehaviour
{
    /**
     * This one public function can handle your "Play", "Create Lobby", 
     * and "Options" buttons. We give it a parameter (sceneName)
     * so it knows which scene to load.
     */
    public void LoadScene(string sceneName)
    {
        // Make sure the sceneName you provided exists in your Build Settings
        SceneManager.LoadScene(sceneName);
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