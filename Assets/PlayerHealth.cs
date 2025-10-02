using Unity.Netcode;
using TMPro;
using UnityEngine;

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
    public TextMeshProUGUI healthText; // assign prefab Text
    public Canvas playerCanvas;        // assign prefab Canvas

    public override void OnNetworkSpawn()
    {
        // Show UI only for owning player
        if (!IsOwner && playerCanvas != null)
        {
            playerCanvas.gameObject.SetActive(false);
        }

        // Initialize health on server
        if (IsServer)
            currentHealth.Value = maxHealth;

        // Listen for health changes
        currentHealth.OnValueChanged += (oldVal, newVal) =>
        {
            UpdateHealthUI(newVal);
        };

        // Initial UI update for owner
        if (IsOwner)
            UpdateHealthUI(currentHealth.Value);
    }

    [ServerRpc(RequireOwnership = false)]
    public void TakeDamageServerRpc(float amount)
    {
        if (amount <= 0) return;

        currentHealth.Value = Mathf.Clamp(currentHealth.Value - amount, 0, maxHealth);
        Debug.Log($"[Server] Player {OwnerClientId} took damage. New HP: {currentHealth.Value}");
    }

    private void UpdateHealthUI(float newHealth)
    {
        if (!IsOwner || healthText == null) return;

        healthText.text = "Health: " + newHealth.ToString("F1");
    }
}
