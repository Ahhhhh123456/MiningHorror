using Unity.Netcode;
using UnityEngine;

public class BoxBreak : NetworkBehaviour
{
    [Header("Box Settings")]
    public DrillBoxData boxData;

    private PlayerInventory playerInventory;

    public void Start()
    {
        playerInventory = FindObjectOfType<PlayerInventory>();
        Debug.Log("Stone: " + boxData.stoneCount);
        Debug.Log("Iron: " + boxData.ironCount);
        Debug.Log("Gold: " + boxData.goldCount);
    }

    public void WhoBrokeBox()
    {
        BreakingBoxServerRpc(OwnerClientId);
    }

    [ServerRpc(RequireOwnership = false)]
    public void BreakingBoxServerRpc(ulong activatingClientId)
    {
        if (!IsServer)
        {
            Debug.LogWarning("BreakingBox should only run on the server!");
            return;
        }
        

        

        if (NetworkManager.Singleton.ConnectedClients.TryGetValue(activatingClientId, out var client))
        {
            var playerOreInventory = client.PlayerObject.GetComponent<PlayerInventory>();

            Debug.Log("Test:" + playerOreInventory.NetworkOres);

            foreach (var ore in playerOreInventory.NetworkOres)
            {
                string oreName = ore.oreName.ToString();
                int count = ore.count;

                Debug.Log($"Player {activatingClientId}: {oreName} x{count}");

                if (oreName == "Stone")
                {
                    Debug.Log("Removing stone from player inventory and box.");
                    playerOreInventory.RemoveFromInventory(oreName);
                    playerOreInventory.RemoveItemServer(oreName);
                    playerOreInventory.UpdateInventoryUIForOwner();
    
                    boxData.stoneCount -= count;
                }
                Debug.Log("Removing Ore");
            }

        }
        else
        {
            Debug.LogWarning($"No connected client found for ID {activatingClientId}");
        }
        

        Debug.Log(boxData.stoneCount + " stones left in box.");

    }

}
