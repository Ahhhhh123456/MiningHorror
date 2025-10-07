using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class LoadingBar : NetworkBehaviour
{
    [Header("Loading Settings")]
    public float maxLoad = 100f;
    public float loadRate = 30f;

    [Header("UI")]
    public Canvas playerCanvas;
    public Image loadingFill;

    public NetworkVariable<float> currentLoad = new NetworkVariable<float>(
        0f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public override void OnNetworkSpawn()
    {
        if (IsServer)
            currentLoad.Value = 0f; // start empty

        currentLoad.OnValueChanged += (oldVal, newVal) =>
        {
            if (!IsOwner)
                playerCanvas.gameObject.SetActive(false);

            UpdateLoadingUI(newVal);
        };

        if (IsOwner)
            UpdateLoadingUI(currentLoad.Value);
    }

    public void Update()
    {
        if (!IsServer) return;
    }

    [ServerRpc(RequireOwnership = false)]
    public void IncreaseLoadServerRpc(float amount)
    {
        if (amount <= 0) return;

        currentLoad.Value = Mathf.Clamp(currentLoad.Value + amount, 0, maxLoad);

        if (currentLoad.Value >= maxLoad)
        {
            currentLoad.Value = 0f;
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void ResetLoadServerRpc()
    {
        currentLoad.Value = 0f;
    }

    private void UpdateLoadingUI(float value)
    {
        if (loadingFill != null)
            loadingFill.fillAmount = value / maxLoad;
    }


}
