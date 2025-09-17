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
    public ParticleSystem ParticleFullyBreak;

    public ParticleSystem ParticleBreaking; 


    public void Mining()
    {
        string tag = gameObject.tag;
        holdCount += 1;

        Debug.Log("Count:" + holdCount);
        if (rockTypes.ContainsKey(tag))
        {
            RockData data = rockTypes[tag];

            // Particles midway through mining. Keep for later use maybe.
            // if (tag != "Stone" && holdCount == data.durability / 2)
            // {
            //     if (ParticleBreaking != null)
            //     {
            //         ParticleSystem PB = Instantiate(ParticleBreaking, transform.position, Quaternion.identity);
            //         PB.Play();
            //         Destroy(PB.gameObject, PB.main.duration + PB.main.startLifetime.constantMax);
            //     }
            // }

            // Particles on first hit of object
            if (holdCount == 1)
            {
                if (ParticleBreaking != null)
                {
                    ParticleSystem PB = Instantiate(ParticleBreaking, transform.position, Quaternion.identity);
                    PB.Play();
                    Destroy(PB.gameObject, PB.main.duration + PB.main.startLifetime.constantMax);
                }
            }

            if (holdCount == data.durability)
                {

                    if (ParticleFullyBreak != null)
                    {
                        ParticleSystem PFB = Instantiate(ParticleFullyBreak, transform.position, Quaternion.identity);
                        PFB.Play();
                        Destroy(PFB.gameObject, PFB.main.duration + PFB.main.startLifetime.constantMax);

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
