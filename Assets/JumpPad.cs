using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

public class JumpPad : MonoBehaviour
{
    PlayerMovement playerMovement;

    private float knockbackForce = 25f;

    private HashSet<ulong> clientsTouching = new HashSet<ulong>();

    private void OnCollisionEnter(Collision collision)
    {
        NetworkObject netObj = collision.gameObject.GetComponent<NetworkObject>();
        if (netObj != null && collision.gameObject.CompareTag("Player"))
        {
            clientsTouching.Add(netObj.OwnerClientId);
            Debug.Log($"Player touched me! ClientId: {netObj.OwnerClientId}");
            playerMovement = collision.gameObject.GetComponent<PlayerMovement>();
            if (playerMovement != null)
            {
                playerMovement.ApplyExplosionForceClientRpc(knockbackForce * Vector3.up);
            }

        }
    }


}
