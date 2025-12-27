using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHealth : NetworkBehaviour
{
    [Header("Health Settings")]
    public float maxHealth = 100f;

    private NetworkVariable<float> currentHealth = new NetworkVariable<float>(
        100f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    [Header("UI")]
    public Image healthFill;
    public Canvas playerCanvas;

    // [Header("Regeneration")]
    // public float regenRate = 10f;
    // public float regenDelay = 3f;
    // private float regenTimer = 0f;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
            currentHealth.Value = maxHealth;

        // Listen for value changes
        currentHealth.OnValueChanged += (oldVal, newVal) =>
        {
            if (!IsOwner)
            {
                playerCanvas.gameObject.SetActive(false);
            }

            UpdateHealthUI(newVal);
        };

        // Initial UI update
        if (IsOwner)
            UpdateHealthUI(currentHealth.Value);
    }

    private void Update()
    {
        if (!IsServer) return;

        // // Regeneration
        // if (currentHealth.Value < maxHealth)
        // {
        //     regenTimer += Time.deltaTime;
        //     if (regenTimer >= regenDelay)
        //     {
        //         currentHealth.Value = Mathf.Clamp(currentHealth.Value + regenRate * Time.deltaTime, 0, maxHealth);
        //     }
        // }
    }

    [ServerRpc(RequireOwnership = false)]
    public void TakeDamageServerRpc(float amount)
    {
        if (amount <= 0) return;

        currentHealth.Value = Mathf.Clamp(currentHealth.Value - amount, 0, maxHealth);
        // regenTimer = 0f;
        Debug.Log($"[Server] Player {OwnerClientId} took damage. New HP: {currentHealth.Value}");
    }

    private void UpdateHealthUI(float value)
    {
        if (healthFill != null)
            healthFill.fillAmount = value / maxHealth;
    }
}
