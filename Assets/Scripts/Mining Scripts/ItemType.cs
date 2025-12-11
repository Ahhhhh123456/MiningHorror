using UnityEngine;
using System.Collections.Generic;

public class ItemType : MonoBehaviour
{
    public Dictionary<string, ItemData> itemDatabase = new Dictionary<string, ItemData>()
    {
        {"Pickaxe", new ItemData(5f, ItemCategory.Tool)},
        {"Shovel", new ItemData(4f, ItemCategory.Tool)},
        {"DrillBattery", new ItemData(8f, ItemCategory.Part)},
        {"DrillBooster", new ItemData(15f, ItemCategory.Part)},
        {"DrillPipe", new ItemData(4f, ItemCategory.Part)},
        {"DrillPoint", new ItemData(15f, ItemCategory.Part)},
        {"Ladder", new ItemData(5f, ItemCategory.Misc)},
        {"Dynamite", new ItemData(2f, ItemCategory.Misc)},
        {"Compass", new ItemData(0.01f, ItemCategory.Misc)},
        {"JumpPad", new ItemData(3f, ItemCategory.Misc)},
        {"DrillBody", new ItemData(20f, ItemCategory.Part)}, // <-- Testing purposes
        {"Torch", new ItemData(1f, ItemCategory.Tool)}
    };
    
}

[System.Serializable]
public class ItemData
{
    public float weight;
    public ItemCategory category;

    public ItemData(float weight, ItemCategory category)
    {
        this.weight = weight;
        this.category = category;
    }
}

public enum ItemCategory
{
    Tool,
    Part,
    Misc
}