using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using System;
using Unity.Collections;

[Serializable]
public struct OreEntry : INetworkSerializable, IEquatable<OreEntry>
{
    public FixedString32Bytes oreName;
    public int count;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref oreName);
        serializer.SerializeValue(ref count);
    }

    // Required for NetworkList
    public bool Equals(OreEntry other)
    {
        return oreName.Equals(other.oreName) && count == other.count;
    }

    public override bool Equals(object obj)
    {
        return obj is OreEntry other && Equals(other);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            return (oreName.GetHashCode() * 397) ^ count;
        }
    }
}

public class PlayerInventory : NetworkBehaviour
{
    // Dictionary: key = item name, value = count
    [Header("Inventory UI (Ores)")]
    public TextMeshProUGUI OreText;
    public TextMeshProUGUI ItemText;

    // public Dictionary<string, int> InventoryOreCount = new Dictionary<string, int>();

    // public List<string> InventoryItems = new List<string>();
    public NetworkList<OreEntry> NetworkOres = new NetworkList<OreEntry>();
    public NetworkList<FixedString32Bytes> NetworkItems = new NetworkList<FixedString32Bytes>();
    public Transform holdPosition;
    public Transform pickaxePosition;

    public bool holdPickaxe = false;
    public GameObject currentHeldItem;
    private Dictionary<string, GameObject> prefabLookup;

    public float playerWeight = 0f;
    public int currentSlotIndex = -1; // track currently held slot

    public int maxSlots = 2;
    private MineType mineType;
    private PlayerMovement playerMovement;

    public Canvas inventoryCanvas;
    public ItemType itemType;

    [Header("Prefab Assignments")]
    public List<ItemPrefabEntry> prefabEntries; // drag prefabs in Inspector

    [System.Serializable]
    public class ItemPrefabEntry
    {
        public string itemName;
        public GameObject prefab;
        public Vector3 holdRotation = Vector3.zero; // Euler rotation when held
        public Vector3 holdPositionOffset = Vector3.zero; // optional offset for fine-tuning
    }

    public void Start()
    {
        mineType = FindObjectOfType<MineType>();
        playerMovement = GetComponent<PlayerMovement>();
        itemType = FindObjectOfType<ItemType>();
    }

    private void Awake()
    {
        // Build dictionary from inspector entries
        prefabLookup = new Dictionary<string, GameObject>();
        foreach (var entry in prefabEntries)
        {
            prefabLookup[entry.itemName] = entry.prefab;
        }
    }
    public override void OnNetworkSpawn()
    {
        if (inventoryCanvas != null)
            inventoryCanvas.gameObject.SetActive(IsOwner);

        // Subscribe to changes so UI updates automatically
        NetworkOres.OnListChanged += OnOresChanged;
        NetworkItems.OnListChanged += OnItemsChanged;

        // Initialize UI immediately
        UpdateOreUIText();
        UpdateItemUIText();
    }


    public void LogAllPlayerInventories()
    {
        foreach (var clientPair in NetworkManager.Singleton.ConnectedClients)
        {
            ulong clientId = clientPair.Key;
            var inventory = clientPair.Value.PlayerObject.GetComponent<PlayerInventory>();
            Debug.Log($"Client {clientId} has {inventory.NetworkOres.Count} ores and {inventory.NetworkItems.Count} items.");
        }
    }

    private void OnOresChanged(NetworkListEvent<OreEntry> changeEvent)
    {
        UpdateOreUIText();
    }

    private void OnItemsChanged(NetworkListEvent<FixedString32Bytes> changeEvent)
    {
        UpdateItemUIText();
    }


    // ✅ Add item (server-only)
    public void AddItemServer(string itemName, OreData oreData = null)
    {

        // Add ores
        if (oreData != null)
        {
            // Check if ore already exists in the NetworkList
            bool found = false;
            for (int i = 0; i < NetworkOres.Count; i++)
            {
                if (NetworkOres[i].oreName.ToString() == oreData.oreName)
                {
                    var entry = NetworkOres[i];
                    entry.count++;
                    NetworkOres[i] = entry; // Update the NetworkList entry
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                NetworkOres.Add(new OreEntry
                {
                    oreName = new FixedString32Bytes(oreData.oreName),
                    count = 1
                });
            }

            playerWeight += oreData.weight;
        }

        // Add items
        if (itemType.itemDatabase.ContainsKey(itemName))
        {
            NetworkItems.Add(new FixedString32Bytes(itemName));
            playerWeight += itemType.itemDatabase[itemName].weight;
        }
        // LogAllPlayerInventories();
        // Debug.Log($"Added {itemName} to inventory. Total weight: {playerWeight}");
        // UI updates are automatic via OnListChanged on the client
    }



    // ✅ Remove item (server-only)
    public void RemoveItemServer(string itemName, OreData oreData = null)
    {
        if (!IsServer) return;

        // Remove ore
        if (oreData != null)
        {
            for (int i = 0; i < NetworkOres.Count; i++)
            {
                if (NetworkOres[i].oreName.ToString() == oreData.oreName)
                {
                    var entry = NetworkOres[i];
                    entry.count--;
                    if (entry.count <= 0)
                        NetworkOres.RemoveAt(i);
                    else
                        NetworkOres[i] = entry;

                    playerWeight -= oreData.weight;
                    Debug.Log($"Removed {oreData.oreName} from inventory. Total weight: {playerWeight}");
                    break;
                }
            }
        }

        // Remove general item
        for (int i = 0; i < NetworkItems.Count; i++)
        {
            if (NetworkItems[i].ToString() == itemName)
            {
                NetworkItems.RemoveAt(i);

                if (itemType.itemDatabase.ContainsKey(itemName))
                    playerWeight -= itemType.itemDatabase[itemName].weight;
                Debug.Log($"Removed {itemName} from inventory. Total weight: {playerWeight}");
                break;
            }
        }

        //LogAllPlayerInventories();
    }


    [ClientRpc]
    public void UpdateOreUIClientRpc(ClientRpcParams clientRpcParams = default)
    {
        UpdateOreUIText();
    }

    [ClientRpc]
    private void UpdateItemUIClientRpc(ClientRpcParams clientRpcParams = default)
    {
        UpdateItemUIText();
    }


    // Called by LookAndClick
    public void RemoveFromInventory(string itemName)
    {
        Debug.Log($"[Inventory] Attempting to remove '{itemName}' from inventory.");

        // Early validation
        if (string.IsNullOrEmpty(itemName))
        {
            Debug.LogWarning("[Inventory] Item name is null or empty.");
            return;
        }

        // Debug which references are valid
        Debug.Log($"mineType={mineType != null}, itemType={itemType != null}, playerMovement={playerMovement != null}");

        // ---- Ore Inventory ----
        for (int i = 0; i < NetworkOres.Count; i++)
        {
            string oreName = NetworkOres[i].oreName.ToString();
            if (oreName.Equals(itemName, StringComparison.OrdinalIgnoreCase))
            {
                var entry = NetworkOres[i];
                entry.count--;

                if (entry.count <= 0)
                    NetworkOres.RemoveAt(i);
                else
                    NetworkOres[i] = entry;

                // Adjust weight if possible
                if (mineType != null && mineType.oreData != null)
                {
                    playerWeight -= mineType.oreData.weight;
                    if (playerMovement != null)
                        playerMovement.UpdateMoveSpeed();

                    Debug.Log($"[Inventory] Dropped ore '{itemName}' (Weight {mineType.oreData.weight}). Total weight: {playerWeight}");
                }
                else
                {
                    Debug.LogWarning($"[Inventory] mineType or oreData was null while removing '{itemName}'. Weight not adjusted.");
                }

                break; // Stop after removing one matching ore
            }
        }

        // ---- General Items ----
        for (int i = 0; i < NetworkItems.Count; i++)
        {
            if (NetworkItems[i].ToString().Equals(itemName, StringComparison.OrdinalIgnoreCase))
            {
                NetworkItems.RemoveAt(i);

                if (itemType != null && itemType.itemDatabase != null && itemType.itemDatabase.ContainsKey(itemName))
                {
                    float itemWeight = itemType.itemDatabase[itemName].weight;
                    playerWeight -= itemWeight;
                    if (playerMovement != null)
                        playerMovement.UpdateMoveSpeed();

                    Debug.Log($"[Inventory] Dropped '{itemName}' (Weight {itemWeight}). Total weight: {playerWeight}");
                }
                else
                {
                    Debug.LogWarning($"[Inventory] itemType or itemDatabase missing for '{itemName}'. Weight not adjusted.");
                }

                UpdateItemUIText();
                break; // Stop after removing one matching item
            }
        }
    }


    [ServerRpc(RequireOwnership = false)]
    public void ClearHeldItemServerRpc()
    {
        // Clear the slot
        currentSlotIndex = -1;

        // Update visuals for this player
        ClearHeldItemClientRpc();
    }

    [ClientRpc]
    public void ClearHeldItemClientRpc()
    {
        if (currentHeldItem != null)
        {
            Destroy(currentHeldItem);
            currentHeldItem = null;
        }
    }

    public void UpdateInventoryUIForOwner()
    {
        var clientRpcParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new ulong[] { OwnerClientId }
            }
        };

        UpdateOreUIClientRpc(clientRpcParams);
        UpdateItemUIClientRpc(clientRpcParams);
    }

    private void UpdateOreUIText()
    {
        if (OreText == null) return;

        if (NetworkOres.Count == 0)
        {
            OreText.text = "No ores";
            return;
        }

        var sb = new System.Text.StringBuilder();
        foreach (var entry in NetworkOres)
            sb.AppendLine($"{entry.oreName.ToString()} x{entry.count}");

        OreText.text = sb.ToString();
    }

    private void UpdateItemUIText()
    {
        if (ItemText == null) return;

        if (NetworkItems.Count == 0)
        {
            ItemText.text = "No items";
            return;
        }

        var slots = new List<string>();
        for (int i = 0; i < NetworkItems.Count; i++)
            slots.Add($"{i + 1}: {NetworkItems[i].ToString()}");

        ItemText.text = string.Join(" | ", slots);
    }


    public void SelectSlot(int index)
    {
        if (!IsOwner) return; // Only the owning player can request slot changes

        currentSlotIndex = index;
        SelectSlotServerRpc(index);
    }

    [ServerRpc(RequireOwnership = false)]
    private void SelectSlotServerRpc(int index)
    {
        currentSlotIndex = index;
        // Tell all clients to update visuals for this player
        UpdateHeldItemClientRpc(index);
    }


    // Update visuals for this player
    [ClientRpc]
    public void UpdateHeldItemClientRpc(int index)
    {
        
        // Destroy current held item if it exists
        if (currentHeldItem != null)
        {
            Destroy(currentHeldItem);
            currentHeldItem = null;
        }

        // If invalid index, clear held item and reset flags
        if (index < 0 || index >= NetworkItems.Count)
        {
            holdPickaxe = false;
            Debug.Log("No item held.");
            return;
        }

        string itemName = NetworkItems[index].ToString();

        if (!prefabLookup.TryGetValue(itemName, out GameObject prefab))
        {
            holdPickaxe = false;
            Debug.LogWarning($"Prefab not found for {itemName}");
            return;
        }

        ItemPrefabEntry entry = prefabEntries.Find(e => e.itemName == itemName);

        // Determine hold position (tool vs default)
        Transform targetHoldPosition = holdPosition;
        if (itemType.itemDatabase.ContainsKey(itemName) &&
            itemType.itemDatabase[itemName].category == ItemCategory.Tool)
        {
            targetHoldPosition = pickaxePosition;
            holdPickaxe = true;
            Debug.Log("Holding pickaxe for mining.");
        }
        else
        {
            holdPickaxe = false;
            Debug.Log("Not holding pickaxe.");
        }

        // Instantiate the held item for all clients
        currentHeldItem = Instantiate(prefab, targetHoldPosition);
        currentHeldItem.name = prefab.name;
        currentHeldItem.transform.localPosition = entry != null ? entry.holdPositionOffset : Vector3.zero;
        currentHeldItem.transform.localRotation = entry != null ? Quaternion.Euler(entry.holdRotation) : Quaternion.identity;

        Rigidbody rb = currentHeldItem.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }
    }

    public GameObject GetPrefabForItem(string itemName)
    {
        if (prefabLookup.TryGetValue(itemName, out GameObject prefab))
            return prefab;
        return null;
    }

    public int GetOreCount(string oreName)
    {
        foreach (var ore in NetworkOres)
        {
            if (ore.oreName.ToString().ToLower() == oreName.ToLower())
                return ore.count; // or however your PlayerInventory stores amounts
        }
        return 0;
    }


    public GameObject CreateItemInstance(string itemName, Transform parent = null)
    {
        if (!prefabLookup.TryGetValue(itemName, out GameObject prefab))
        {
            Debug.LogWarning($"Prefab not found for {itemName}");
            return null;
        }

        // Find prefab entry for rotation/offset
        ItemPrefabEntry entry = prefabEntries.Find(e => e.itemName == itemName);

        // Choose hold or pickaxe position
        Transform targetHoldPosition = holdPosition;
        if (itemType.itemDatabase.ContainsKey(itemName) &&
            itemType.itemDatabase[itemName].category == ItemCategory.Tool)
        {
            targetHoldPosition = pickaxePosition;
        }

        // Instantiate just like UpdateHeldItemClientRpc
        GameObject itemInstance = Instantiate(prefab, targetHoldPosition);
        itemInstance.name = prefab.name;
        itemInstance.transform.localPosition = entry != null ? entry.holdPositionOffset : Vector3.zero;
        itemInstance.transform.localRotation = entry != null ? Quaternion.Euler(entry.holdRotation) : Quaternion.identity;
        Debug.Log($"Found rotation for {itemName}: {itemInstance.transform.localRotation.eulerAngles}");
        // Re-parent if needed (for world drops)
        if (parent != null)
        {
            itemInstance.transform.SetParent(null);
            itemInstance.transform.position = parent.position + parent.forward * 0.2f + Vector3.up * 0.2f;
        }

        return itemInstance;
    }
}
