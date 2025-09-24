using UnityEngine;
using TMPro;
public class PlayerStamina : MonoBehaviour
{
    [Header("Stamina Settings")]
    public float maxStamina = 100f;
    private float currentStamina;
    //[Header("UI")]
    // public TextMeshProUGUI staminaText; // Drag the StaminaText object here
    void Start()
    {
        currentStamina = maxStamina;
        // UpdateStaminaUI();
    }

    public void UseStamina(float amount)
    { 
        currentStamina = Mathf.Clamp(currentStamina - amount, 0, maxStamina);
        // UpdateStaminaUI();
        Debug.Log("Player used stamina. Current stamina: " + currentStamina);
    }
    // Update is called once per frame
    private void UpdateStaminaUI()
    {
        // if (staminaText != null)
        // {
        //     staminaText.text = "Stamina: " + currentStamina.ToString("F1");
        // }
    }
}
