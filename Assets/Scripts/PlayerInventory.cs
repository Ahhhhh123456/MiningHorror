using UnityEngine;
using System.Collections.Generic;

public class PlayerInventory : MonoBehaviour
{
    public List<string> inventory = new List<string>();

    public float playerWeight = 0f;

    private MineType mineType;

    private PlayerMovement playerMovement;
    public void Start()
    {
        mineType = FindObjectOfType<MineType>();
        playerMovement = GetComponent<PlayerMovement>();
    }

    public void UpdateInventory(string itemName)
    {
        RockData data;
        if (mineType.rockTypes.ContainsKey(itemName))
        {
            data = mineType.rockTypes[itemName];

            // add to inventory list
            inventory.Add(itemName);

            // update total weight
            playerWeight += data.weight;

            playerMovement.updateMoveSpeed();
            Debug.Log($"Updated move speed to {playerMovement.moveSpeed} due to weight {playerWeight}");
            Debug.Log($"Picked up {itemName} (Weight {data.weight}). Total weight: {playerWeight}");
            Debug.Log("Inventory now contains: " + string.Join(", ", inventory));
        }
        else
        {
            Debug.LogWarning("Unknown item: " + itemName);
        }
    }


}
