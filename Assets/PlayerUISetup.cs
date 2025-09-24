using UnityEngine;
using Unity.Netcode;

public class PlayerUISetup : NetworkBehaviour
{
    public Canvas playerCanvas; // drag the canvas here in prefab

    void Start()
    {
        if (!IsOwner)
        {
            // Disable the HUD for other players
            if (playerCanvas != null)
                playerCanvas.enabled = false;
        }
    }
}