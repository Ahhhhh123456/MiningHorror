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
    public GameObject optionsPanel;
    public Button optionsBackButton;

    [Header("Options Tabs")]
    public Button generalButton;
    public Button videoButton;
    public Button audioButton;
    public GameObject generalPanel;
    public GameObject videoPanel;
    public GameObject audioPanel;

    [Header("Settings")]
    public KeyCode pauseKey = KeyCode.Escape;

    private bool isPaused = false;

    private void Start()
    {
        // Initialize UI
        if (pausePanel != null)
            pausePanel.SetActive(false);
        if (optionsPanel != null)
            optionsPanel.SetActive(false);
        if (generalPanel != null)
            generalPanel.SetActive(false);
        if (videoPanel != null)
            videoPanel.SetActive(false);
        if (audioPanel != null)
            audioPanel.SetActive(false);

        // Add button listeners
        if (resumeButton != null)
            resumeButton.onClick.AddListener(ResumeGame);
        
        if (optionsButton != null)
            optionsButton.onClick.AddListener(OpenOptions);
        if (optionsBackButton != null)
            optionsBackButton.onClick.AddListener(CloseOptions);
        if (generalButton != null)
            generalButton.onClick.AddListener(OpenGeneralTab);
        if (videoButton != null)
            videoButton.onClick.AddListener(OpenVideoTab);
        if (audioButton != null)
            audioButton.onClick.AddListener(OpenAudioTab);
        
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
        if (optionsPanel != null)
            optionsPanel.SetActive(false);

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

        // Lock cursor for game play
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Resume player movement
        ResumePlayerMovement();

        Debug.Log("Game Resumed");
    }

    public void OpenOptions()
    {
        if (!isPaused) return;
        if (pausePanel != null)
            pausePanel.SetActive(false);
        if (optionsPanel != null)
            optionsPanel.SetActive(true);
        // Default to General tab when opening Options
        OpenGeneralTab();
    }

    public void CloseOptions()
    {
        if (!isPaused) return;
        if (optionsPanel != null)
            optionsPanel.SetActive(false);
        if (pausePanel != null)
            pausePanel.SetActive(true);
        // Hide all tabs when closing options
        if (generalPanel != null) generalPanel.SetActive(false);
        if (videoPanel != null) videoPanel.SetActive(false);
        if (audioPanel != null) audioPanel.SetActive(false);
    }

    private void ShowOnly(GameObject target)
    {
        if (generalPanel != null) generalPanel.SetActive(generalPanel == target);
        if (videoPanel != null) videoPanel.SetActive(videoPanel == target);
        if (audioPanel != null) audioPanel.SetActive(audioPanel == target);
    }

    public void OpenGeneralTab()
    {
        ShowOnly(generalPanel);
    }

    public void OpenVideoTab()
    {
        ShowOnly(videoPanel);
    }

    public void OpenAudioTab()
    {
        ShowOnly(audioPanel);
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
        var localPlayerObj = NetworkManager.Singleton?.LocalClient?.PlayerObject;
        if (localPlayerObj == null) return;

        var playerMovement = localPlayerObj.GetComponent<PlayerMovement>();
        if (playerMovement != null) playerMovement.enabled = false;

        var lookAndClick = localPlayerObj.GetComponentInChildren<LookAndClickInteraction>(true);
        if (lookAndClick != null) lookAndClick.enabled = false;
    }

    private void ResumePlayerMovement()
    {
        var localPlayerObj = NetworkManager.Singleton?.LocalClient?.PlayerObject;
        if (localPlayerObj == null) return;

        var playerMovement = localPlayerObj.GetComponent<PlayerMovement>();
        if (playerMovement != null) playerMovement.enabled = true;

        var lookAndClick = localPlayerObj.GetComponentInChildren<LookAndClickInteraction>(true);
        if (lookAndClick != null) lookAndClick.enabled = true;
    }

    private void OnDestroy()
    {
        // Clean up button listeners
        if (resumeButton != null)
            resumeButton.onClick.RemoveListener(ResumeGame);
        
        if (optionsButton != null)
            optionsButton.onClick.RemoveListener(OpenOptions);
        if (optionsBackButton != null)
            optionsBackButton.onClick.RemoveListener(CloseOptions);
        if (generalButton != null)
            generalButton.onClick.RemoveListener(OpenGeneralTab);
        if (videoButton != null)
            videoButton.onClick.RemoveListener(OpenVideoTab);
        if (audioButton != null)
            audioButton.onClick.RemoveListener(OpenAudioTab);
        
        if (exitButton != null)
            exitButton.onClick.RemoveListener(ExitGame);
    }
}
