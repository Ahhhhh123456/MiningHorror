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

    public void Mining()
    {
        string tag = gameObject.tag;
        holdCount+= 1;

        Debug.Log("Count:" + holdCount);
        if (rockTypes.ContainsKey(tag))
        {
            int value = rockTypes[tag];

            if (holdCount == value)
            {
                Dropped dropscript = GetComponent<Dropped>();
                if (dropscript != null)
                {
                    dropscript.DropItem(gameObject);
                }
                Debug.Log("Mining complete for: " + tag);
                gameObject.SetActive(false);

                holdCount = 0;
            }
        }
        else
        {
            Debug.Log("No entry found for tag: " + tag);
        }
    }
}
