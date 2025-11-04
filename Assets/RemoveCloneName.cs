using Unity.Netcode;
using UnityEngine;

public class OreNameFix : NetworkBehaviour
{
    public override void OnNetworkSpawn()
    {
        // Removes "(Clone)" reliably on **every client**
        gameObject.name = gameObject.name.Replace("(Clone)", "").Trim();
    }
}
