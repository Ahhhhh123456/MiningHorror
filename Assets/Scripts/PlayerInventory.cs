using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
public class PlayerInventory : NetworkBehaviour
{
    // Dictionary: key = item name, value = count
    [Header("Inventory UI (Ores)")]
    public TextMeshProUGUI OreText;
    public TextMeshProUGUI ItemText;

    public Dictionary<string, int> InventoryOreCount = new Dictionary<string, int>();

    public List<string> InventoryItems = new List<string>();
    public Transform holdPosition;
    public Transform pickaxePosition;
    public GameObject currentHeldItem;
    private Dictionary<string, GameObject> prefabLookup;

    public float playerWeight = 0f;

    private MineType mineType;
    private PlayerMovement playerMovement;

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

    // ✅ Add item (server-only)
    public void AddItemServer(string itemName, OreData oreData = null)
    {
        if (!IsServer) return;

        // Ores
        if (oreData != null)
        {
            if (InventoryOreCount.ContainsKey(oreData.oreName))
                InventoryOreCount[oreData.oreName]++;
            else
                InventoryOreCount[oreData.oreName] = 1;

            playerWeight += oreData.weight;
        }

        // General items
        if (itemType.itemDatabase.ContainsKey(itemName))
        {
            InventoryItems.Add(itemName);
            playerWeight += itemType.itemDatabase[itemName].weight;
        }

        // After server updates, notify client UI
        UpdateInventoryUIClientRpc();
    }

    // ✅ Remove item (server-only)
    public void RemoveItemServer(string itemName, OreData oreData = null)
    {
        if (!IsServer) return;

        if (oreData != null && InventoryOreCount.ContainsKey(oreData.oreName))
        {
            InventoryOreCount[oreData.oreName]--;
            if (InventoryOreCount[oreData.oreName] <= 0)
                InventoryOreCount.Remove(oreData.oreName);

            playerWeight -= oreData.weight;
        }

        if (InventoryItems.Contains(itemName))
        {
            InventoryItems.Remove(itemName);
            if (itemType.itemDatabase.ContainsKey(itemName))
                playerWeight -= itemType.itemDatabase[itemName].weight;
        }

        UpdateInventoryUIClientRpc();
    }

    // ✅ Update UI on the owning client
    [ClientRpc]
    private void UpdateInventoryUIClientRpc(ClientRpcParams clientRpcParams = default)
    {
        if (!IsOwner) return;

        // Update ores
        if (InventoryOreCount.Count == 0)
            OreText.text = "No ores";
        else
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            foreach (var kvp in InventoryOreCount)
            {
                sb.AppendLine($"{kvp.Key} x{kvp.Value}");
            }
            OreText.text = sb.ToString();
        }

        // Update items
        if (InventoryItems.Count == 0)
            ItemText.text = "No items";
        else
        {
            List<string> slots = new List<string>();
            for (int i = 0; i < InventoryItems.Count; i++)
                slots.Add($"{i + 1}: {InventoryItems[i]}");
            ItemText.text = string.Join(" | ", slots);
        }
    }


    public void RemoveFromInventory(string itemName)
    {
        // Ore Inventory
        if (InventoryOreCount.ContainsKey(itemName))
        {
            OreData data = mineType.oreData;

            InventoryOreCount[itemName]--;
            if (InventoryOreCount[itemName] <= 0)
                InventoryOreCount.Remove(itemName);

            playerWeight -= data.weight;
            playerMovement.UpdateMoveSpeed();

            Debug.Log($"Dropped {itemName} (Weight {data.weight}). Total weight: {playerWeight}");
            UpdateOreUIText();
        }

        // General items
        if (InventoryItems.Contains(itemName))
        {
            InventoryItems.Remove(itemName);

            if (itemType.itemDatabase.ContainsKey(itemName))
            {
                float itemWeight = itemType.itemDatabase[itemName].weight;
                playerWeight -= itemWeight;
                playerMovement.UpdateMoveSpeed();
            }
            UpdateItemUIText();
            Debug.Log($"Dropped {itemName}. Items left: " + string.Join(", ", InventoryItems));
        }
    }
    private int currentSlotIndex = -1; // track currently held slot

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
    private void UpdateHeldItemClientRpc(int index)
    {
        // Only update visuals for the corresponding player
        if (!IsOwner) return;

        // Destroy current held item
        if (currentHeldItem != null)
        {
            Destroy(currentHeldItem);
            currentHeldItem = null;
        }

        if (index < 0 || index >= InventoryItems.Count) return;

        string itemName = InventoryItems[index];

        if (!prefabLookup.TryGetValue(itemName, out GameObject prefab)) return;

        // Find the corresponding prefab entry to get rotation/offset
        ItemPrefabEntry entry = prefabEntries.Find(e => e.itemName == itemName);

        // Determine hold position (default vs pickaxe/tool)
        Transform targetHoldPosition = holdPosition;
        if (itemType.itemDatabase.ContainsKey(itemName))
        {
            ItemCategory category = itemType.itemDatabase[itemName].category;
            if (category == ItemCategory.Tool)
                targetHoldPosition = pickaxePosition;
        }

        // Instantiate item
        currentHeldItem = Instantiate(prefab, targetHoldPosition);
        currentHeldItem.name = prefab.name; // remove (Clone)

        // Apply position offset
        currentHeldItem.transform.localPosition = entry != null ? entry.holdPositionOffset : Vector3.zero;

        // Apply rotation
        currentHeldItem.transform.localRotation = entry != null ? Quaternion.Euler(entry.holdRotation) : Quaternion.identity;

        // Physics setup
        Rigidbody rb = currentHeldItem.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }
    }


    // public void SelectSlot(int index)
    // {
    //     if (currentHeldItem != null)
    //     {
    //         Destroy(currentHeldItem);
    //         currentHeldItem = null;
    //     }

    //     if (index >= InventoryItems.Count) return;

    //     string itemName = InventoryItems[index];

    //     if (!prefabLookup.TryGetValue(itemName, out GameObject prefab)) return;

    //     // Find the corresponding prefab entry to get rotation/offset
    //     ItemPrefabEntry entry = prefabEntries.Find(e => e.itemName == itemName);

    //     // Determine hold position (default vs pickaxe/tool)
    //     Transform targetHoldPosition = holdPosition;
    //     if (itemType.itemDatabase.ContainsKey(itemName))
    //     {
    //         ItemCategory category = itemType.itemDatabase[itemName].category;
    //         if (category == ItemCategory.Tool)
    //             targetHoldPosition = pickaxePosition;
    //     }

    //     // Instantiate item
    //     currentHeldItem = Instantiate(prefab, targetHoldPosition);
    //     currentHeldItem.name = prefab.name; // remove (Clone)

    //     // Apply position offset
    //     currentHeldItem.transform.localPosition = entry != null ? entry.holdPositionOffset : Vector3.zero;

    //     // Apply rotation
    //     currentHeldItem.transform.localRotation = entry != null ? Quaternion.Euler(entry.holdRotation) : Quaternion.identity;

    //     // Physics setup
    //     Rigidbody rb = currentHeldItem.GetComponent<Rigidbody>();
    //     if (rb != null)
    //     {
    //         rb.isKinematic = true;
    //         rb.useGravity = false;
    //     }

    //     // Disable colliders while holding
    //     // Collider[] colliders = currentHeldItem.GetComponents<Collider>();
    //     // foreach (var col in colliders)
    //     //     col.enabled = false;
    // }

    private void UpdateOreUIText()
    {
        if (OreText == null) return;

        if (InventoryOreCount.Count == 0)
        {
            OreText.text = "No ores";
            return;
        }

        // Build "OreName xCount" lines
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        foreach (var kvp in InventoryOreCount)
        {
            sb.AppendLine($"{kvp.Key} x{kvp.Value}");
        }

        OreText.text = sb.ToString();
    }

    private void UpdateItemUIText()
    {
        if (ItemText == null) return;

        if (InventoryItems.Count == 0)
        {
            ItemText.text = "No items";
            return;
        }

        List<string> slotDisplays = new List<string>();
        for (int i = 0; i < InventoryItems.Count; i++)
        {
            string itemName = InventoryItems[i];
            int slotNumber = i + 1; // slots usually start at 1 instead of 0
            slotDisplays.Add($"{slotNumber}: {itemName}");
        }

        // Show horizontally with separators
        ItemText.text = string.Join(" | ", slotDisplays);
    }

}
