using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Collections;
public class PlayerSpawn : NetworkBehaviour
{
    private void OnEnable()
    {
        MarchingCubes.OnCaveFinished += TriggerSpawn;
    }

    private void OnDisable()
    {
        MarchingCubes.OnCaveFinished -= TriggerSpawn;
    }

    private void TriggerSpawn()
    {
        if (!IsOwner) return;  // Only the local player requests teleport

        Debug.Log("Triggering player spawn...");
        Vector3 pos = SceneSpawnPoint.Instance.spawnLocation.position;
        Quaternion rot = SceneSpawnPoint.Instance.spawnLocation.rotation;

        MovePlayerServerRpc(pos, rot);
    }

    [ServerRpc(RequireOwnership = false)]
    private void MovePlayerServerRpc(Vector3 pos, Quaternion rot)
    {
        // Teleport server-side
        transform.SetPositionAndRotation(pos, rot);

        // Tell all clients
        MovePlayerClientRpc(pos, rot);
    }

    [ClientRpc]
    private void MovePlayerClientRpc(Vector3 pos, Quaternion rot)
    {
        StartCoroutine(DelayedMove(pos, rot));
    }

    private IEnumerator DelayedMove(Vector3 pos, Quaternion rot)
    {
        yield return new WaitForSeconds(0.1f);
        transform.SetPositionAndRotation(pos, rot);
    }
}

