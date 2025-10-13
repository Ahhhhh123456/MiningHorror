using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class PlayerStamina : NetworkBehaviour
{
    [Header("Stamina Settings")]
    public float maxStamina = 100f;
    public Canvas playerCanvas;
    public NetworkVariable<float> currentStamina = new NetworkVariable<float>(
        100f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    [Header("UI")]
    public Image staminaFill;

    [Header("Regeneration")]
    public float regenRate = 15f;
    public float regenDelay = 2f;
    private float regenTimer = 0f;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
            currentStamina.Value = maxStamina;

        // Listen for value changes
        currentStamina.OnValueChanged += (oldVal, newVal) =>
        {
            if (!IsOwner)
            {
                playerCanvas.gameObject.SetActive(false);
            }

            UpdateStaminaUI(newVal);
        };

        // Initial UI update
        if (IsOwner)
            UpdateStaminaUI(currentStamina.Value);
    }

    private void Update()
    {
        if (!IsServer) return;

        // Regeneration
        if (currentStamina.Value < maxStamina)
        {
            regenTimer += Time.deltaTime;
            if (regenTimer >= regenDelay)
            {
                currentStamina.Value = Mathf.Clamp(currentStamina.Value + regenRate * Time.deltaTime, 0, maxStamina);
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void UseStaminaServerRpc(float amount)
    {
        if (amount <= 0) return;

        currentStamina.Value = Mathf.Clamp(currentStamina.Value - amount, 0, maxStamina);
        regenTimer = 0f;
    }

    private void UpdateStaminaUI(float value)
    {
        if (staminaFill != null)
            staminaFill.fillAmount = value / maxStamina;
    }
}
