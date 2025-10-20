using UnityEngine;
using Unity.Netcode;

public class PlayerSetup : NetworkBehaviour
{
    [SerializeField] private Camera mainCamera;
    [SerializeField] private AudioListener audioListener;

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            // Enable camera and audio for the local player
            mainCamera.enabled = true;
            audioListener.enabled = true;
        }
        else
        {
            // Disable for remote players
            mainCamera.enabled = false;
            audioListener.enabled = false;
        }
    }
}
