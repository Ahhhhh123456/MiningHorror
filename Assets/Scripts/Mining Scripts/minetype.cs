using UnityEngine;
using System.Collections.Generic;

public class MineType : MonoBehaviour
{
    public Dictionary<string, RockData> rockTypes = new Dictionary<string, RockData>()
    {
        { "Stone", new RockData { durability = 100, weight = 1f } },
        { "Iron",  new RockData { durability = 200, weight = 4f } },
        { "Gold",  new RockData { durability = 300, weight = 8f } },
    };

    public int holdCount = 0;

    [Header("Effects")]
    public ParticleSystem breakParticles; 


    public void Mining()
    {
        string tag = gameObject.tag;
        holdCount += 1;
                
        Debug.Log("Count:" + holdCount);
        if (rockTypes.ContainsKey(tag))
        {
            RockData data = rockTypes[tag];

            if (holdCount == data.durability)
            {
                if (breakParticles != null)
                {
                    Debug.Log("Playing break particles");
                    breakParticles.Play();
                }

                Dropped dropscript = GetComponent<Dropped>();
                if (dropscript != null)
                {
                    dropscript.DropItem(gameObject);
                }


                //Debug.Log("Mining complete for: " + tag);
                gameObject.SetActive(false);

                holdCount = 0;
            }
        }
        else
        {
            //Debug.Log("No entry found for tag: " + tag);
        }
    }
}
