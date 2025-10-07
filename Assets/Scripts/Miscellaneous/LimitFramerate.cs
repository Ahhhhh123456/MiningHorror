using UnityEngine;

public class FramerateLimiter : MonoBehaviour
{
    [SerializeField] private int targetFPS = 144;

    void Awake()
    {
        QualitySettings.vSyncCount = 0;   // Disable VSync so it doesn't override Application.targetFrameRate
        Application.targetFrameRate = targetFPS;
    }

    void OnValidate()
    {
        if (Application.isPlaying)
            Application.targetFrameRate = targetFPS;
    }
}