using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

public class NextScene : MonoBehaviour
{
    private HashSet<ulong> clientsTouching = new HashSet<ulong>();
    private int countDown = 100;

    // Name of the scene to load
    [SerializeField] private string sceneToLoad = "NextSceneName";

    private void OnCollisionEnter(Collision collision)
    {
        NetworkObject netObj = collision.gameObject.GetComponent<NetworkObject>();
        if (netObj != null && collision.gameObject.CompareTag("Player"))
        {
            clientsTouching.Add(netObj.OwnerClientId);
            Debug.Log($"Player touched me! ClientId: {netObj.OwnerClientId} | Count: {clientsTouching.Count}");
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        NetworkObject netObj = collision.gameObject.GetComponent<NetworkObject>();
        if (netObj != null && collision.gameObject.CompareTag("Player"))
        {
            clientsTouching.Remove(netObj.OwnerClientId);
            Debug.Log($"Player stopped touching me! ClientId: {netObj.OwnerClientId} | Count: {clientsTouching.Count}");
            countDown = 100; // reset countdown if a player leaves
        }
    }

    private void Update()
    {
        if (NetworkManager.Singleton == null) return;

        int connectedPlayers = NetworkManager.Singleton.ConnectedClientsList.Count;

        // Only decrement countdown if all players are touching
        if (clientsTouching.Count == connectedPlayers && connectedPlayers > 0)
        {
            countDown--;
            if (countDown <= 0)
            {
                Debug.Log("All players are touching me! Countdown finished!");

                // Only host/server should initiate scene change
                if (NetworkManager.Singleton.IsServer)
                {
                    NetworkManager.Singleton.SceneManager.LoadScene(
                        sceneToLoad,
                        UnityEngine.SceneManagement.LoadSceneMode.Single
                    );
                }

                countDown = 100; // reset if needed
            }
        }
        else
        {
            if (countDown != 100) countDown = 100; // reset countdown if not all players touching
        }
    }
}
