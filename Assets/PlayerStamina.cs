using UnityEngine;
using UnityEngine.UI;

public class PlayerStamina : MonoBehaviour
{
    [Header("Stamina Settings")]
    public float maxStamina = 100f;
    public float currentStamina;

    [Header("UI")]
    public Image staminaFill;

    [Header("Regeneration Settings")]
    public float regenRate = 15f;       // Stamina per second
    public float regenDelay = 2f;       // Seconds to wait after last use
    private float regenTimer = 0f;

    void Start()
    {
        currentStamina = maxStamina;
        UpdateStaminaUI();
    }

    void Update()
    {
        // Only regenerate if not full
        if (currentStamina < maxStamina)
        {
            regenTimer += Time.deltaTime;

            // Start regenerating after delay
            if (regenTimer >= regenDelay)
            {
                currentStamina += regenRate * Time.deltaTime;
                currentStamina = Mathf.Clamp(currentStamina, 0, maxStamina);
                UpdateStaminaUI();
            }
        }
    }

    public void UseStamina(float amount)
    {
        if (amount <= 0) return;

        currentStamina = Mathf.Clamp(currentStamina - amount, 0, maxStamina);
        UpdateStaminaUI();

        // Reset regen timer whenever stamina is used
        regenTimer = 0f;

        Debug.Log("Player used stamina. Current: " + currentStamina);
    }

    private void UpdateStaminaUI()
    {
        if (staminaFill != null)
        {
            staminaFill.fillAmount = currentStamina / maxStamina;
        }
    }
}
