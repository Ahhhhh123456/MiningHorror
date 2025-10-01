using UnityEngine;
using Unity.Netcode;

[CreateAssetMenu(fileName = "OreData", menuName = "Mining/OreData")]
public class OreData : ScriptableObject
{
    public string oreName;
    public int durability;
    public float weight;
    public NetworkObject dropPrefab; // ✅ NetworkObject, not GameObject
    public ParticleSystem breakingParticles;
    public ParticleSystem fullyBreakParticles;
}
