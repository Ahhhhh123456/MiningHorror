using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class PlayerInventory : MonoBehaviour
{
    // Dictionary: key = item name, value = count
    public Dictionary<string, int> InventoryOreCount = new Dictionary<string, int>();

    public List<string> InventoryItems = new List<string>();
    public Transform holdPosition;
    public GameObject currentHeldItem;

    [Header("Prefab Assignments")]
    public List<ItemPrefabEntry> prefabEntries; // drag prefabs in Inspector
    private Dictionary<string, GameObject> prefabLookup;

    public float playerWeight = 0f;

    private MineType mineType;
    private PlayerMovement playerMovement;

    private ItemType itemType;

    [System.Serializable]
    public class ItemPrefabEntry
    {
        public string itemName;
        public GameObject prefab;
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
    public void UpdateInventory(string itemName)
    {
        // Ore Inventory
        RockData data;
        if (mineType.rockTypes.ContainsKey(itemName))
        {
            data = mineType.rockTypes[itemName];

            // If item exists, increment count; otherwise, add with count 1
            if (InventoryOreCount.ContainsKey(itemName))
            {
                InventoryOreCount[itemName]++;
            }
            else
            {
                InventoryOreCount[itemName] = 1;
            }

            // Update total weight
            playerWeight += data.weight;
            playerMovement.UpdateMoveSpeed();

            Debug.Log($"Picked up {itemName} (Weight {data.weight}). Total weight: {playerWeight}");
            foreach (var kvp in InventoryOreCount)
            {
                Debug.Log($"Item: {kvp.Key}, Count: {kvp.Value}");
            }
        }
        else
        {
            Debug.LogWarning("Unknown item: " + itemName);
        }

        if (itemType.itemWeights.ContainsKey(itemName))
        {
            float itemWeight = itemType.itemWeights[itemName];

            // Add item to list (duplicates allowed)
            InventoryItems.Add(itemName);

            // Update weight
            playerWeight += itemWeight;
            playerMovement.UpdateMoveSpeed();

            Debug.Log($"Picked up {itemName} (Weight {itemWeight}). Total weight: {playerWeight}");

            // Print all items as a comma-separated list
            Debug.Log("Items in inventory: " + string.Join(", ", InventoryItems));
        }

    }

    public void RemoveFromInventory(string itemName)
    {
        // Remove from Ore Inventory
        if (InventoryOreCount.ContainsKey(itemName))
        {
            RockData data = mineType.rockTypes[itemName];

            InventoryOreCount[itemName]--;
            if (InventoryOreCount[itemName] <= 0)
                InventoryOreCount.Remove(itemName);

            // Update weight
            playerWeight -= data.weight;
            playerMovement.UpdateMoveSpeed();

            Debug.Log($"Dropped {itemName} (Weight {data.weight}). Total weight: {playerWeight}");
        }
        // Remove from general items
        if (InventoryItems.Contains(itemName))
        {
            InventoryItems.Remove(itemName);

            if (itemType.itemWeights.ContainsKey(itemName))
            {
                float itemWeight = itemType.itemWeights[itemName];
                playerWeight -= itemWeight;
                playerMovement.UpdateMoveSpeed();
            }

            Debug.Log($"Dropped {itemName}. Items left: " + string.Join(", ", InventoryItems));
        }
    }
    
    public void SelectSlot(int index)
    {
        // Destroy currently held item regardless
        if (currentHeldItem != null)
        {
            Destroy(currentHeldItem);
            currentHeldItem = null;
        }

        // Check if slot has an item
        if (index < InventoryItems.Count)
        {
            string itemName = InventoryItems[index];

            // Make sure prefab exists
            if (prefabLookup.TryGetValue(itemName, out GameObject prefab))
            {
                currentHeldItem = Instantiate(prefab, holdPosition);
                currentHeldItem.transform.localPosition = new Vector3(0f, 0f, 1f); // in front
                currentHeldItem.transform.localRotation = Quaternion.identity;

                Rigidbody rb = currentHeldItem.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.isKinematic = true;
                    rb.useGravity = false;
                }

                MeshCollider mc = currentHeldItem.GetComponent<MeshCollider>();
                if (mc != null)
                    mc.convex = true;
            }
            else
            {
                Debug.LogWarning($"No prefab found for {itemName}");
            }
        }
        else
        {
            // Slot is empty → hands are now empty
            Debug.Log("Switched to empty slot");
        }
    }


}
