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
    public void Start()
    {
        playerInventory = FindObjectOfType<PlayerInventory>();
        droppedScript = FindObjectOfType<Dropped>();

        // Debug.Log("Stone: " + boxData.stoneCount);
        // Debug.Log("Iron: " + boxData.ironCount);
        // Debug.Log("Gold: " + boxData.goldCount);
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
        CheckOreValues(boxData);

        if (NetworkManager.Singleton.ConnectedClients.TryGetValue(senderClientId, out var client))
        {
            PlayerInventory inventoryScript = client.PlayerObject.GetComponent<PlayerInventory>();

            List<string> oresToRemove = new List<string>();

            foreach (var ore in inventoryScript.NetworkOres)
            {
                oresToRemove.Add(ore.oreName.ToString());
            }

            foreach (var oreName in oresToRemove)
            {
                inventoryScript.RemoveFromInventory(oreName);
                inventoryScript.RemoveItemServer(oreName);
                RemoveBoxOreValue(boxData, oreName);
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
    
    void CheckOreValues(DrillBoxData boxData)
    {
        var fields = typeof(DrillBoxData).GetFields(BindingFlags.Public | BindingFlags.Instance);

        foreach (var field in fields)
        {
            if (field.FieldType == typeof(int) && field.Name.EndsWith("Count"))
            {
                int value = (int)field.GetValue(boxData);
                Debug.Log($"{field.Name}: {value}");
                totalOres += value;
            }
        }
    }

    void RemoveBoxOreValue(DrillBoxData boxData, string oreName)
    {
        FieldInfo field = typeof(DrillBoxData).GetField(oreName.ToLower() + "Count", BindingFlags.Public | BindingFlags.Instance);

        if (field != null && field.FieldType == typeof(int))
        {
            int currentValue = (int)field.GetValue(boxData);
            int newValue = Mathf.Max(0, currentValue - 1);
            field.SetValue(boxData, newValue);
            totalOres -= 1;
            Debug.Log("Total ores left in box: " + totalOres);
        }
    }



}
