using UnityEngine;
using System.Collections.Generic;

public class MineType : MonoBehaviour
{


    public Dictionary<string, int> rockTypes = new Dictionary<string, int>()
    {
        { "Stone", 100 },
        { "Iron", 200 },
        { "Gold", 300 },
    };

    public int holdCount = 0;

    public void Mining(GameObject item)
    {
        string tag = item.tag;
        holdCount += 1;

        if (rockTypes.ContainsKey(tag))
        {
            int value = rockTypes[tag];
            //Debug.Log("Value for " + tag + " = " + value);
            if (holdCount == value)
            {
                
            }
        }
        else
        {
            Debug.Log("No entry found for tag: " + tag);
        }
    }
}
