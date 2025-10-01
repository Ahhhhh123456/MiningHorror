using UnityEngine;

[CreateAssetMenu(fileName = "OreData", menuName = "Mining/OreData")]
public class OreData : ScriptableObject
{
    public string oreName;
    public int durability;
    public float weight;
    public GameObject dropPrefab;
    public ParticleSystem breakingParticles;
    public ParticleSystem fullyBreakParticles;
}
