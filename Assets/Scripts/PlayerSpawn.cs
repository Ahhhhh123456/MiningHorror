using Unity.Netcode;
using UnityEngine;
using System.Collections;

public class PlayerSpawn : NetworkBehaviour
{
    [SerializeField] private Transform spawnPoint;

    // public override void OnNetworkSpawn()
    // {
    //     if (IsServer && spawnPoint != null)
    //     {
    //         StartCoroutine(DelayedSpawn());
    //         OnPlayerSpawnClientRpc();
    //     }


    // }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.J) && IsServer && spawnPoint != null)
        {
            NetworkObject.transform.SetPositionAndRotation(spawnPoint.position, spawnPoint.rotation);
            OnPlayerSpawnClientRpc();
        }
    }

    private IEnumerator DelayedSpawn()
    {
        yield return null; // wait one frame
        NetworkObject.transform.SetPositionAndRotation(spawnPoint.position, spawnPoint.rotation);
    }

    [ClientRpc]
    void OnPlayerSpawnClientRpc()
    {
        NetworkObject.transform.SetPositionAndRotation(spawnPoint.position, spawnPoint.rotation);
    }

}
