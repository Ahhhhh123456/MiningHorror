using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class PlayerInventory : MonoBehaviour
{
    // Dictionary: key = item name, value = count
    public Dictionary<string, int> InventoryOreCount = new Dictionary<string, int>();

    public List<string> InventoryItems = new List<string>();
    public Transform holdPosition;
    public Transform pickaxePosition;
    public GameObject currentHeldItem;
    private Dictionary<string, GameObject> prefabLookup;

    public float playerWeight = 0f;

    private MineType mineType;
    private PlayerMovement playerMovement;

    private ItemType itemType;

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
    public void UpdateInventory(string itemName)
    {
        // Ore Inventory
        RockData data;
        if (mineType.rockTypes.ContainsKey(itemName))
        {
            data = mineType.rockTypes[itemName];

            if (InventoryOreCount.ContainsKey(itemName))
                InventoryOreCount[itemName]++;
            else
                InventoryOreCount[itemName] = 1;

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

        // General items
        if (itemType.itemDatabase.ContainsKey(itemName))
        {
            float itemWeight = itemType.itemDatabase[itemName].weight;

            InventoryItems.Add(itemName);
            playerWeight += itemWeight;
            playerMovement.UpdateMoveSpeed();

            Debug.Log($"Picked up {itemName} (Weight {itemWeight}). Total weight: {playerWeight}");
            Debug.Log("Items in inventory: " + string.Join(", ", InventoryItems));
        }
        else
        {
            Debug.LogWarning($"Unknown item in itemDatabase: {itemName}");
        }
    }


    public void RemoveFromInventory(string itemName)
    {
        // Ore Inventory
        if (InventoryOreCount.ContainsKey(itemName))
        {
            RockData data = mineType.rockTypes[itemName];

            InventoryOreCount[itemName]--;
            if (InventoryOreCount[itemName] <= 0)
                InventoryOreCount.Remove(itemName);

            playerWeight -= data.weight;
            playerMovement.UpdateMoveSpeed();

            Debug.Log($"Dropped {itemName} (Weight {data.weight}). Total weight: {playerWeight}");
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

            Debug.Log($"Dropped {itemName}. Items left: " + string.Join(", ", InventoryItems));
        }
    }

    
    public void SelectSlot(int index)
    {
        if (currentHeldItem != null)
        {
            Destroy(currentHeldItem);
            currentHeldItem = null;
        }

        if (index >= InventoryItems.Count) return;

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

        // Disable colliders while holding
        // Collider[] colliders = currentHeldItem.GetComponents<Collider>();
        // foreach (var col in colliders)
        //     col.enabled = false;
    }



}
