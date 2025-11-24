using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

public class PauseMenu : NetworkBehaviour
{
    [Header("UI References")]
    public GameObject pausePanel;
    public Button resumeButton;
    public Button optionsButton;
    public Button exitButton;

    [Header("Settings")]
    public KeyCode pauseKey = KeyCode.Escape;

    private bool isPaused = false;

    private void Start()
    {
        // Initialize UI
        if (pausePanel != null)
            pausePanel.SetActive(false);

        // Add button listeners
        if (resumeButton != null)
            resumeButton.onClick.AddListener(ResumeGame);
        
        if (optionsButton != null)
            optionsButton.onClick.AddListener(OpenOptions);
        
        if (exitButton != null)
            exitButton.onClick.AddListener(ExitGame);
    }

    private void Update()
    {
        // Check for pause key press
        if (Input.GetKeyDown(pauseKey))
        {
            if (isPaused)
                ResumeGame();
            else
                PauseGame();
        }
    }

    public void PauseGame()
    {
        if (isPaused) return;

        isPaused = true;
        
        // Show pause panel
        if (pausePanel != null)
            pausePanel.SetActive(true);

        // Pause time
        Time.timeScale = 0f;
        
        // Lock cursor for UI interaction
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Pause player movement
        PausePlayerMovement();

        Debug.Log("Game Paused");
    }

    public void ResumeGame()
    {
        if (!isPaused) return;

        isPaused = false;
        
        // Hide pause panel
        if (pausePanel != null)
            pausePanel.SetActive(false);

        // Resume time
        Time.timeScale = 1f;
        
        // Lock cursor for game play
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Resume player movement
        ResumePlayerMovement();

        Debug.Log("Game Resumed");
    }

    public void OpenOptions()
    {
        Debug.Log("Options button clicked - Options menu not implemented yet");
        // TODO: Implement options menu
    }

    public void ExitGame()
    {
        Debug.Log("Exit button clicked");
        
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }

    private void PausePlayerMovement()
    {
        // Find and disable player movement scripts
        PlayerMovement playerMovement = FindObjectOfType<PlayerMovement>();
        if (playerMovement != null)
        {
            playerMovement.enabled = false;
        }

        // Disable camera look
        LookAndClickInteraction lookAndClick = FindObjectOfType<LookAndClickInteraction>();
        if (lookAndClick != null)
        {
            lookAndClick.enabled = false;
        }
    }

    private void ResumePlayerMovement()
    {
        // Re-enable player movement scripts
        PlayerMovement playerMovement = FindObjectOfType<PlayerMovement>();
        if (playerMovement != null)
        {
            playerMovement.enabled = true;
        }

        // Re-enable camera look
        LookAndClickInteraction lookAndClick = FindObjectOfType<LookAndClickInteraction>();
        if (lookAndClick != null)
        {
            lookAndClick.enabled = true;
        }
    }

    private void OnDestroy()
    {
        // Clean up button listeners
        if (resumeButton != null)
            resumeButton.onClick.RemoveListener(ResumeGame);
        
        if (optionsButton != null)
            optionsButton.onClick.RemoveListener(OpenOptions);
        
        if (exitButton != null)
            exitButton.onClick.RemoveListener(ExitGame);
    }
}
