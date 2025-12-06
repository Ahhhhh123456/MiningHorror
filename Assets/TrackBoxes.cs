using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

public class TrackBoxes : NetworkBehaviour
{
    [Header("Compass Settings")]
    public List<GameObject> targetPrefabs; // list of target prefabs to track
    public float updateRate = 0.1f; // how often to update arrow

    private Vector3 closestTargetPosition;
    private float timer;

    public Transform compassArrow;     // the L CompassArrow
    public float smoothRotate;   // smoothing speed

    [ServerRpc(RequireOwnership = false)]
    public void RequestClosestTargetServerRpc(ServerRpcParams rpcParams = default)
    {
        ulong senderId = rpcParams.Receive.SenderClientId;

        // Get the requesting player's transform
        var senderPlayer = NetworkManager.Singleton.ConnectedClients[senderId].PlayerObject;
        Vector3 playerPos = senderPlayer.transform.position;

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
            ClientRpcParams rpcParamsSend = new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = new ulong[] { senderId } // send back to the requester
                }
            };

            SendClosestTargetClientRpc(closest.transform.position, closest.name, rpcParamsSend);
        }
    }

    public void OnCompassEquipped(Transform compassRoot)
    {
        compassArrow = compassRoot.Find("CompassArrow");

        if (compassArrow == null)
            Debug.LogWarning("Compass arrow or tip not found!");
    }

    // [ClientRpc]
    // private void SendClosestTargetClientRpc(Vector3 targetPos, string targetName, ClientRpcParams rpcParams = default)
    // {
    //     closestTargetPosition = targetPos;

    //     Vector3 direction = (closestTargetPosition - transform.position).normalized;

    //     // Log the direction for debugging
    //     Debug.Log($"Direction to closest target: {direction}");

    // }

    [ClientRpc]
    private void SendClosestTargetClientRpc(Vector3 targetPos, string targetName, ClientRpcParams rpcParams = default)
    {
        closestTargetPosition = targetPos;

        Vector3 direction = (closestTargetPosition - compassArrow.position);
        direction.y = 0f; // Keep arrow level

        if (direction != Vector3.zero)
        {
            // Rotate in the opposite direction by flipping forward
            Quaternion targetRot = Quaternion.LookRotation(-direction); // <-- negate direction

            // Rotate arrow base
            if (compassArrow != null)
                compassArrow.rotation = Quaternion.Lerp(
                    compassArrow.rotation,
                    targetRot,
                    Time.deltaTime * smoothRotate);


        }
    }

}
