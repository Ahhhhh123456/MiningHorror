using UnityEngine;
using System.Collections.Generic;

public class PlayerInventory : MonoBehaviour
{
    // Dictionary: key = item name, value = count
    public Dictionary<string, int> InventoryOreCount = new Dictionary<string, int>();

    public List<string> InventoryItems = new List<string>();

    public float playerWeight = 0f;

    private MineType mineType;
    private PlayerMovement playerMovement;

    private ItemType itemType;

    public void Start()
    {
        mineType = FindObjectOfType<MineType>();
        playerMovement = GetComponent<PlayerMovement>();
        itemType = FindObjectOfType<ItemType>();
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
}
