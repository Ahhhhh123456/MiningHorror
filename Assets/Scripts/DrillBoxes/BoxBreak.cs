using Unity.Netcode;
using UnityEngine;
using System.Collections.Generic;
using System.Reflection;

public class BoxBreak : NetworkBehaviour
{
    [Header("Box Settings")]
    public DrillBoxData boxData;

    private int totalOres = 0;
    private PlayerInventory playerInventory;

    private Dropped droppedScript;

    private NetworkedBoxData networkedBoxData;
    public void Start()
    {
        playerInventory = FindObjectOfType<PlayerInventory>();
        droppedScript = FindObjectOfType<Dropped>();
        networkedBoxData = GetComponent<NetworkedBoxData>();
        networkedBoxData.InitializeFromDrillBoxData(boxData);
    }

    public void WhoBrokeBox()
    {
        BreakingBoxServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    public void BreakingBoxServerRpc(ServerRpcParams rpcParams = default)
    {
        if (!IsServer)
        {
            Debug.LogWarning("BreakingBox should only run on the server!");
            return;
        }


        ulong senderClientId = rpcParams.Receive.SenderClientId;

        //Debug.Log($"Player {senderClientId} is breaking the box.");
        totalOres = 0;
        //CheckOreValues(networkedBoxData);
        var boxOreNeeds = CheckOreValues(networkedBoxData);

        if (NetworkManager.Singleton.ConnectedClients.TryGetValue(senderClientId, out var client))
        {
            PlayerInventory inventoryScript = client.PlayerObject.GetComponent<PlayerInventory>();

            foreach (var kvp in boxOreNeeds) // kvp.Key = "coal", kvp.Value = amount needed
            {
                string oreName = kvp.Key;
                int needed = kvp.Value;

                if (needed <= 0) continue; // skip ores not needed

                // Count how many player actually has
                int playerHas = inventoryScript.GetOreCount(oreName); // assume you have a method like this

                int toRemove = Mathf.Min(playerHas, needed);

                for (int i = 0; i < toRemove; i++)
                {
                    inventoryScript.RemoveFromInventory(oreName);
                    inventoryScript.RemoveItemServer(oreName);
                    RemoveBoxOreValue(oreName); // update the NetworkedBoxData
                }
            }

            if (totalOres == 0)
            {
                Debug.Log("All ores removed from box, despawning.");

                // Get the NetworkObject prefab from BoxData
                NetworkObject prefab = boxData.dropPrefab;
                string prefabName = boxData.dropPrefab.name;
                if (prefab == null)
                {
                    Debug.LogWarning("Box drop prefab is null!");
                    return;
                }

                // Spawn position and rotation
                Transform dropTransform = this.transform;
                Vector3 spawnPos = dropTransform.position;
                Quaternion spawnRot = dropTransform.rotation;

                // Instantiate the network prefab
                NetworkObject droppedNetObj = Instantiate(prefab, spawnPos, spawnRot);
                droppedNetObj.name = prefabName;
                // Optional: add Rigidbody for physics if not already present
                Rigidbody rb = droppedNetObj.GetComponent<Rigidbody>();
                if (rb == null) rb = droppedNetObj.gameObject.AddComponent<Rigidbody>();
                rb.isKinematic = false;
                rb.useGravity = true;
                rb.AddForce(dropTransform.forward * 2f, ForceMode.Impulse);

                // Spawn it on the network
                droppedNetObj.Spawn();

                // Despawn the box
                GetComponent<NetworkObject>().Despawn();
            }
        }

    }
    
    Dictionary<string, int> CheckOreValues(NetworkedBoxData networkedBoxData)
    {
        Dictionary<string, int> oreCounts = new Dictionary<string, int>();
        totalOres = 0;

        var fields = typeof(NetworkedBoxData).GetFields(BindingFlags.Public | BindingFlags.Instance);

        foreach (var field in fields)
        {
            if (field.FieldType == typeof(NetworkVariable<int>))
            {
                NetworkVariable<int> valueVar = (NetworkVariable<int>)field.GetValue(networkedBoxData);
                int value = valueVar.Value;

                // Normalize field name (e.g., "coalCount" → "coal")
                string oreName = field.Name.Replace("Count", "").ToLower();

                oreCounts[oreName] = value;
                totalOres += value;

                Debug.Log($"{field.Name}: {value}");
            }
        }

        Debug.Log("Total ores in box: " + totalOres);
        return oreCounts;
    }



    void RemoveBoxOreValue(string oreName)
    {
        if (!IsServer) return;

        NetworkVariable<int> variable = oreName.ToLower() switch
        {
            "coal" => networkedBoxData.coalCount,
            "iron" => networkedBoxData.ironCount,
            "gold" => networkedBoxData.goldCount,
            _ => null
        };

        if (variable != null)
        {
            variable.Value = Mathf.Max(0, variable.Value - 1);
            totalOres -= 1;
            Debug.Log($"Total ores left in box: {totalOres}");
        }
    }



}
