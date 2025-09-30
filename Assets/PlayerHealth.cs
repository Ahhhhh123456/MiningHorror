using UnityEngine;
using TMPro; // Needed for TextMeshProUGUI

public class PlayerHealth : MonoBehaviour
{
    [Header("Health Settings")]
    public float maxHealth = 100f;
    private float currentHealth;

    [Header("UI")]
    public TextMeshProUGUI healthText; // Drag the HealthText object here

    void Start()
    {
        currentHealth = maxHealth;
        UpdateHealthUI();
        Debug.Log("PlayerHealth script started. Initial health: " + currentHealth);
    }
    
    public void TakeDamage(float amount)
    {
        currentHealth = Mathf.Clamp(currentHealth - amount, 0, maxHealth);
        UpdateHealthUI();
        Debug.Log("Player took damage. Current health: " + currentHealth);
    }

    private void UpdateHealthUI()
    {
        if (healthText != null)
        {
            healthText.text = "Health: " + currentHealth.ToString("F1");

        }
    }
}
