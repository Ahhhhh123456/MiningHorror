using UnityEngine;
using System.Collections.Generic;
public class ItemType : MonoBehaviour
{
    public Dictionary<string, float> itemWeights = new Dictionary<string, float>()
    {
        {"Pickaxe", 5f},
        {"DrillBody", 10f},
        {"DrillHead", 10f},
        {"WheelOne", 8.5f},
        {"WheelTwo", 8.5f}

    };
}
