using UnityEngine;
using Unity.Netcode;
public class ChangeFog : NetworkBehaviour
{
    public void ApplyTorchFog()
    {
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Exponential;
        RenderSettings.fogColor = Color.black;
        RenderSettings.fogDensity = 0.2f;

        Camera cam = GetComponent<Camera>();
        cam.backgroundColor = Color.black;
    }

    public void ApplyDarkFog()
    {
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Exponential;
        RenderSettings.fogColor = Color.black;
        RenderSettings.fogDensity = 0.4f;

        Camera cam = GetComponent<Camera>();
        cam.backgroundColor = Color.black;
    }

    [ClientRpc]
    private void ApplyTorchFog_ClientRpc()
    {
        if (!IsOwner) return; // ensure only the local player changes their fog

        ChangeFog fog = Camera.main.GetComponent<ChangeFog>();
        fog.ApplyTorchFog();
    }
}
