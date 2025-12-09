using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using System;
using Unity.Collections;
using UnityEngine.SceneManagement;

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
    // public TextMeshProUGUI ItemText;

    [Header("Inventory UI (Hotbar)")]
    public List<UnityEngine.UI.Image> hotbarSlotImages; // Put the slot images here
    public Sprite emptySlotSprite; // optional sprite to show when the slot is empty

    public Color filledSlotColor = Color.green;

    // public Dictionary<string, int> InventoryOreCount = new Dictionary<string, int>();

    // public List<string> InventoryItems = new List<string>();
    public NetworkList<OreEntry> NetworkOres = new NetworkList<OreEntry>();
    public NetworkList<FixedString32Bytes> NetworkItems = new NetworkList<FixedString32Bytes>();
    public Transform holdPosition;
    public Transform pickaxePosition;

    public bool holdTool = false;

    public bool holdPickaxe = false;

    public bool holdShovel = false;

    public bool IsHoldingCompass = false;
    public GameObject currentHeldItem;
    
    private Dictionary<string, GameObject> prefabLookup;

    public float playerWeight = 0f;
    public int currentSlotIndex = -1; // track currently held slot

    public int hotbarSize = 4; // can be set in inspector
    private MineType mineType;
    private PlayerMovement playerMovement;

    public Canvas inventoryCanvas;
    public ItemType itemType;

    private ItemData currentHeldItemData;

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
        // itemType = FindObjectOfType<ItemType>();
    }

    //-------- Functions to reload references on scene change -------------- //

    private void OnEnable()
    {
        if (NetworkManager.Singleton != null)
            NetworkManager.SceneManager.OnLoadEventCompleted += OnSceneLoaded;
    }

    private void OnDisable()
    {
        if (NetworkManager.Singleton != null)
            NetworkManager.SceneManager.OnLoadEventCompleted -= OnSceneLoaded;
    }

    public void ReinitializeSceneReferences()
    {
        mineType = FindObjectOfType<MineType>();
        itemType = FindObjectOfType<ItemType>();
        playerMovement = GetComponent<PlayerMovement>();

        Debug.Log($"[PlayerInventory] Reinitialized references: mineType={mineType}, itemType={itemType}, playerMovement={playerMovement}");
        ResetInventoryServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    public void ResetInventoryServerRpc()
    {
        ResetInventory(); // safe, runs on server
        Debug.Log("[PlayerInventory] Inventory reset on scene load.");
    }

    public void ResetInventory()
    {
        NetworkOres.Clear();
        NetworkItems.Clear();
        playerWeight = 0;

        // Clear held item
        currentSlotIndex = -1;
        ClearHeldItemClientRpc();
    }

    private void OnSceneLoaded(string sceneName, LoadSceneMode loadSceneMode,
                            List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
    {
        if (!IsClient) return; // Only reinitialize for this client

        // Re-fetch scene-specific references
        ReinitializeSceneReferences();
    }

    // ----------------------------------------------------------------------//
    private void Awake()
    {
        // Build dictionary from inspector entries
        prefabLookup = new Dictionary<string, GameObject>();
        foreach (var entry in prefabEntries)
        {
            prefabLookup[entry.itemName] = entry.prefab;
        }

        itemType = FindObjectOfType<ItemType>();
    }
    public override void OnNetworkSpawn()
    {
        if (inventoryCanvas != null)
            inventoryCanvas.gameObject.SetActive(IsOwner);

        // Subscribe to changes so UI updates automatically
        NetworkOres.OnListChanged += OnOresChanged;
        NetworkItems.OnListChanged += OnHotbarSlotsChanged;

        // SERVER: Initialize the hotbar slots
        if (IsServer)
        {
            NetworkItems.Clear();
            for (int i = 0; i < hotbarSize; i++)
            {
                NetworkItems.Add(new FixedString32Bytes("")); // Add an "empty" slot
            }
        }

        // Initialize UI immediately
        UpdateOreUIText();
        UpdateHotbarUIText();
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

    private void OnHotbarSlotsChanged(NetworkListEvent<FixedString32Bytes> changeEvent)
    {
        UpdateHotbarUIText();
    }


    // Add item (server-only)
    public bool AddItemServer(string itemName, OreData oreData = null)
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

            return true;
        }

        // Add items
        if (itemType.itemDatabase.ContainsKey(itemName))
        {
            // Find the first empty slot
            bool slotFound = false;
            for (int i = 0; i < NetworkItems.Count; i++) // NetworkItems.Count is now hotbarSize
            {
                if (NetworkItems[i].ToString() == "")
                {
                    // finds empty slot and fills
                    NetworkItems[i] = new FixedString32Bytes(itemName);
                    playerWeight += itemType.itemDatabase[itemName].weight;
                    slotFound = true;
                    Debug.Log($"Added {itemName} to slot {i}.");
                    break;
                }
            }

            if (!slotFound)
            {
                Debug.Log("Hotbar is full!");
                return false;
                // can also add separate backpack list here
            }

            return true;
        }
        // LogAllPlayerInventories();
        // Debug.Log($"Added {itemName} to inventory. Total weight: {playerWeight}");
        // UI updates are automatic via OnListChanged on the client

        return false;
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
                // ✅ CORRECT FIX: Empty the slot, do not shrink the list
                NetworkItems[i] = new FixedString32Bytes("");

                if (itemType.itemDatabase.ContainsKey(itemName))
                    playerWeight -= itemType.itemDatabase[itemName].weight;
                Debug.Log($"Removed {itemName} from inventory. Total weight: {playerWeight}");
                break;
            }
        }

        //LogAllPlayerInventories();
    }

    // ✅ This is your NEW drop function.
    // Call this from your input script (e.g., when 'G' key is pressed)
    public void RequestDropSelectedItem()
    {
        if (!IsOwner) return;

        // Check if we are holding a valid item
        if (currentSlotIndex >= 0 && currentSlotIndex < NetworkItems.Count)
        {
            if (NetworkItems[currentSlotIndex].ToString() != "")
            {
                // We are holding something. Tell the server to drop it.
                DropItemFromSlotServerRpc(currentSlotIndex);
            }
        }
    }

    [ServerRpc]
    public void DropItemFromSlotServerRpc(int slotIndex)
    {
        // 1. Get the item name from the slot
        string itemName = NetworkItems[slotIndex].ToString();
        if (string.IsNullOrEmpty(itemName)) return; // Slot is already empty

        // 2. Clear the slot (THIS IS THE FIX! We don't RemoveAt, we just empty it)
        NetworkItems[slotIndex] = new FixedString32Bytes("");

        // 3. Adjust weight
        if (itemType.itemDatabase.ContainsKey(itemName))
        {
            float itemWeight = itemType.itemDatabase[itemName].weight;
            playerWeight -= itemWeight;
            if (playerMovement != null)
                playerMovement.UpdateMoveSpeed(); // Tell player movement to update
        }

        Debug.Log($"Dropped {itemName} from slot {slotIndex}");

        // 4. (Optional) Spawn the item prefab in the world
        // You can use your CreateItemInstance method here to drop it in front of the player
        GameObject prefabToDrop = GetPrefabForItem(itemName);
        if (prefabToDrop != null)
        {
            // Calculate a spawn position in front of the player
            Vector3 spawnPos = transform.position + transform.forward * 1.5f + Vector3.up * 0.5f;

            // Instantiate the item's NetworkObject
            NetworkObject droppedItemNetworkObject = Instantiate(prefabToDrop, spawnPos, transform.rotation).GetComponent<NetworkObject>();

            // Set its name correctly so it can be picked up again
            droppedItemNetworkObject.name = prefabToDrop.name;

            // Spawn the item on the network
            droppedItemNetworkObject.Spawn();
        }
    }


    [ClientRpc]
    public void UpdateOreUIClientRpc(ClientRpcParams clientRpcParams = default)
    {
        UpdateOreUIText();
    }

    [ClientRpc]
    private void UpdateHotbarUIClientRpc(ClientRpcParams clientRpcParams = default)
    {
        UpdateHotbarUIText();
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
                NetworkItems[i] = new FixedString32Bytes("");

                if (itemType != null && itemType.itemDatabase != null && itemType.itemDatabase.ContainsKey(itemName))
                {
                    float itemWeight = itemType.itemDatabase[itemName].weight;
                    playerWeight -= itemWeight;
                    if (playerMovement != null)
                        playerMovement.UpdateMoveSpeed();
                    holdTool = false;
                    holdPickaxe = false;
                    holdShovel = false;
                    IsHoldingCompass = false;
                    Debug.Log($"[Inventory] Dropped '{itemName}' (Weight {itemWeight}). Total weight: {playerWeight}");

                }
                else
                {
                    Debug.LogWarning($"[Inventory] itemType or itemDatabase missing for '{itemName}'. Weight not adjusted.");
                }

                UpdateHotbarUIText();
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
        UpdateHotbarUIClientRpc(clientRpcParams);
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

    private void UpdateHotbarUIText()
    {
        if (hotbarSlotImages == null) return;

        for (int i = 0; i < hotbarSize; i++)
        {
            // Make sure we have enough UI slots assigned
            if (i >= hotbarSlotImages.Count) break;

            // Get the item name from the slot
            string itemName = "";
            if (i < NetworkItems.Count) // Safety check
            {
                itemName = NetworkItems[i].ToString();
            }

            if (string.IsNullOrEmpty(itemName))
            {
                // Slot is EMPTY
                hotbarSlotImages[i].sprite = emptySlotSprite; // Use empty sprite
                hotbarSlotImages[i].enabled = (emptySlotSprite != null); // Hide if no empty sprite
                hotbarSlotImages[i].color = Color.white;
            }
            else
            {
                // Slot is FULL
                // This is where you'll set the item's icon!
                // We will add this 'icon' field in the next step.
                // Sprite itemIcon = itemType.itemDatabase[itemName].icon; 
                // hotbarSlotImages[i].sprite = itemIcon;

                // --- Temporary (until we add icons) ---
                hotbarSlotImages[i].enabled = true;
                hotbarSlotImages[i].sprite = null; // Or a default sprite
                hotbarSlotImages[i].color = filledSlotColor;

            }
        }
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

    [ClientRpc]
    public void UpdateHeldItemClientRpc(int index)
    {
        // Tool handling
        Transform targetHoldPosition = holdPosition;
        holdTool = holdPickaxe = holdShovel = false;

        // Destroy previous held item
        if (currentHeldItem != null)
        {
            Destroy(currentHeldItem);
            currentHeldItem = null;
        }

        // Invalid index
        if (index < 0 || index >= NetworkItems.Count)
        {
            holdTool = holdPickaxe = holdShovel = IsHoldingCompass = false;
            currentHeldItemData = null;
            Debug.Log("No item held.");
            return;
        }

        string itemName = NetworkItems[index].ToString();

        if (string.IsNullOrEmpty(itemName))
        {
            holdTool = holdPickaxe = holdShovel = false;
            IsHoldingCompass = false;
            currentHeldItemData = null;

            // Apply fog correctly for empty slot
            if (IsOwner)
            {
                ChangeFog fog = Camera.main.GetComponent<ChangeFog>();
                fog.ApplyDarkFog();
            }

            Debug.Log("Selected slot is empty.");
            return;
        }

        // Get the data for this item
        if (!itemType.itemDatabase.TryGetValue(itemName, out currentHeldItemData))
        {
            Debug.LogWarning($"Item {itemName} not found in database!");
            return;
        }



        if (!prefabLookup.TryGetValue(itemName, out GameObject prefab))
        {
            holdTool = holdPickaxe = holdShovel = false;
            Debug.LogWarning($"Prefab not found for {itemName}");
            return;
        }

        ItemPrefabEntry entry = prefabEntries.Find(e => e.itemName == itemName);

        if (currentHeldItemData.category == ItemCategory.Tool)
        {
            targetHoldPosition = pickaxePosition;
            holdTool = true;
            if (itemName.Contains("Pickaxe")) holdPickaxe = true;
            if (itemName.Contains("Shovel")) holdShovel = true;
            if (itemName.Contains("Torch"))
            {
                if (IsOwner)
                {
                    ChangeFog fog = Camera.main.GetComponent<ChangeFog>();
                    fog.ApplyTorchFog();
                }
            }

            Debug.Log($"Holding tool: {itemName}");
        }
        else
        {
            if (IsOwner)
            {
                ChangeFog fog = Camera.main.GetComponent<ChangeFog>();
                fog.ApplyDarkFog();
                Debug.Log($"Holding Non-tool: {itemName}");
            }
        }

        // Compass handling
        IsHoldingCompass = itemName.Contains("Compass");

        // Instantiate prefab
        currentHeldItem = Instantiate(prefab, targetHoldPosition);
        currentHeldItem.name = prefab.name;
        currentHeldItem.transform.localPosition = entry != null ? entry.holdPositionOffset : Vector3.zero;
        currentHeldItem.transform.localRotation = entry != null ? Quaternion.Euler(entry.holdRotation) : Quaternion.identity;


        //itemType = FindObjectOfType<ItemType>();


        Rigidbody rb = currentHeldItem.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        foreach (var col in currentHeldItem.GetComponentsInChildren<Collider>())
            col.enabled = false;

        // Compass callback
        if (IsHoldingCompass)
        {
            TrackBoxes trackBoxes = GetComponent<TrackBoxes>();
            trackBoxes?.OnCompassEquipped(currentHeldItem.transform);
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
        if (itemInstance.name.Contains("Dynamite"))
        {
            Debug.Log("Created dynamite instance.");
            itemInstance.tag = "Untagged";
        }
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
