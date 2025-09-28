using UnityEngine;
using System.Collections.Generic;
public class ItemType : MonoBehaviour
{
    public Dictionary<string, float> itemWeights = new Dictionary<string, float>()
    {
        {"Pickaxe", 5f},
        {"BetterDrillBodyInteractable", 10f},
        {"BetterDrillHeadInteractable", 10f},
        {"WheelOneInteractable", 8.5f},
        {"WheelTwoInteractable", 8.5f}

    };
}
