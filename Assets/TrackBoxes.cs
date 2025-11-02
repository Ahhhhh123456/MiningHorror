using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

public class TrackBoxes : NetworkBehaviour
{
    [Header("Compass Settings")]
    public List<GameObject> targetPrefabs; // list of target prefabs to track
    public float updateRate = 0.1f; // how often to update arrow

    [HideInInspector] public PlayerInventory playerInventory;

    private Vector3 closestTargetPosition;
    private float timer;

    private void Update()
    {
        if (playerInventory == null) return;

        timer += Time.deltaTime;
        if (timer < updateRate) return;
        timer = 0f;

        if (playerInventory.IsHoldingCompass)
        {
            RequestClosestTargetServerRpc();
        }
    }

    public void Initialize(PlayerInventory inventory)
    {
        playerInventory = inventory;
    }

    [ServerRpc(RequireOwnership = false)]
    public void RequestClosestTargetServerRpc()
    {
        Vector3 playerPos = transform.position;

        GameObject closest = null;
        float minDistance = float.MaxValue;

        foreach (var prefab in targetPrefabs)
        {
            foreach (var obj in GameObject.FindGameObjectsWithTag(prefab.tag))
            {
                float dist = Vector3.Distance(playerPos, obj.transform.position);
                if (dist < minDistance)
                {
                    minDistance = dist;
                    closest = obj;
                }
            }
        }

        if (closest != null)
        {
            ClientRpcParams rpcParams = new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = new ulong[] { OwnerClientId }
                }
            };

            SendClosestTargetClientRpc(closest.transform.position, rpcParams);
        }
    }

    [ClientRpc]
    private void SendClosestTargetClientRpc(Vector3 targetPos, ClientRpcParams rpcParams = default)
    {
        closestTargetPosition = targetPos;
        Debug.Log($"Received closest target at {targetPos}");
    }
}
