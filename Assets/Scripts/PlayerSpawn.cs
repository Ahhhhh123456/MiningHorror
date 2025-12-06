using UnityEngine;
using Unity.Netcode;
using System.Collections;

public class PlayerSpawn : NetworkBehaviour
{
    public GameObject playerObject;

    private void OnEnable()
    {
        MarchingCubes.OnCaveFinished += OnCaveFinished;
        SceneSpawnPoint.OnSpawnPointReady += OnSpawnPointReady;
    }

    private void OnDisable()
    {
        MarchingCubes.OnCaveFinished -= OnCaveFinished;
        SceneSpawnPoint.OnSpawnPointReady -= OnSpawnPointReady;
    }

    private void OnCaveFinished()
    {
        if (!IsOwner) return;
        // Optionally, start any logic that should run after cave generation
    }

    private void OnSpawnPointReady(Vector3 pos)
    {
        if (!IsOwner) return;

        Debug.Log($"Spawn point ready at {pos}");
        playerObject = this.gameObject;

        // Automatically teleport player once the spawn point is ready
        TriggerSpawn();
    }

    private void TriggerSpawn()
    {
        if (!IsOwner) return;

        if (SceneSpawnPoint.Instance == null || SceneSpawnPoint.Instance.latestSpawnPoint == null)
        {
            Debug.LogWarning("Spawn point not ready yet!");
            return;
        }

        Vector3 pos = SceneSpawnPoint.Instance.latestSpawnPoint.position;
        Quaternion rot = SceneSpawnPoint.Instance.latestSpawnPoint.rotation;

        MovePlayerServerRpc(pos, rot);
    }

    [ServerRpc(RequireOwnership = false)]
    private void MovePlayerServerRpc(Vector3 pos, Quaternion rot)
    {
        transform.SetPositionAndRotation(pos, rot);
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

    private void Update()
    {
        if (!IsOwner) return;

        // Keep the J key functionality
        if (Input.GetKeyDown(KeyCode.J))
        {
            Debug.Log("J key pressed - teleporting to spawn point.");
            TriggerSpawn();
        }
    }
}
