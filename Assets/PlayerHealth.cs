using UnityEngine;
using TMPro; // Needed for TextMeshProUGUI

public class PlayerHealth : MonoBehaviour
{
    [Header("Health Settings")]
    public int maxHealth = 100;
    private int currentHealth;

    [Header("UI")]
    public TextMeshProUGUI healthText; // Drag the HealthText object here

    void Start()
    {
        currentHealth = maxHealth;
        UpdateHealthUI();
        Debug.Log("PlayerHealth script started. Initial health: " + currentHealth);
    }
    
    
    void Update()
    {
        // Press H to simulate taking 10 damage
        if (Input.GetKeyDown(KeyCode.N))
        {
            TakeDamage(10);
        }
    }

    public void TakeDamage(int amount)
    {
        currentHealth = Mathf.Clamp(currentHealth - amount, 0, maxHealth);
        UpdateHealthUI();
        Debug.Log("Player took damage. Current health: " + currentHealth);
    }

    private void UpdateHealthUI()
    {
        if (healthText != null)
        {
            healthText.text = "Health: " + currentHealth;
        }
    }
}
