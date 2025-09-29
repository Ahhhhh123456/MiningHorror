using UnityEngine;
using UnityEngine;
using System.Collections.Generic;

public class ItemType : MonoBehaviour
{
    public Dictionary<string, ItemData> itemDatabase = new Dictionary<string, ItemData>()
    {
        {"Pickaxe", new ItemData(5f, ItemCategory.Tool)},
        {"DrillBody", new ItemData(10f, ItemCategory.Part)},
        {"DrillHead", new ItemData(10f, ItemCategory.Part)},
        {"Wheel", new ItemData(8.5f, ItemCategory.Part)}
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