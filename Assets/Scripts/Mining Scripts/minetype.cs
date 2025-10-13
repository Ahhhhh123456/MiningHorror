using Unity.Netcode;
using UnityEngine;

public class MineType : NetworkBehaviour
{
    public OreData oreData; // assign in Inspector per prefab
    public int holdCount = 0;

    public void MiningOre()
    {
        if (IsServer)
        {
            HandleMining();
        }
        else
        {
            MineServerRpc();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    void MineServerRpc(ServerRpcParams rpcParams = default)
    {
        HandleMining();
    }

    private void HandleMining()
    {
        holdCount++;

        // Play breaking particle for everyone
        if (holdCount == 1 && oreData.breakingParticles != null)
            PlayParticleClientRpc(oreData.breakingParticles.name, transform.position);

        if (holdCount >= oreData.durability)
        {
            if (oreData.fullyBreakParticles != null)
                PlayParticleClientRpc(oreData.fullyBreakParticles.name, transform.position);

            Dropped dropscript = GetComponent<Dropped>();
            if (dropscript != null)
                dropscript.DropItem();

            DespawnOreServerRpc(); // despawn block for all clients
            holdCount = 0;
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void DespawnOreServerRpc()
    {
        if (NetworkObject != null && NetworkObject.IsSpawned)
        {
            NetworkObject.Despawn();
        }
    }

    [ClientRpc]
    void PlayParticleClientRpc(string particleName, Vector3 position)
    {
        ParticleSystem prefab = (particleName == oreData.breakingParticles.name) ? oreData.breakingParticles :
                                (particleName == oreData.fullyBreakParticles.name) ? oreData.fullyBreakParticles :
                                null;

        if (prefab != null)
        {
            ParticleSystem ps = Instantiate(prefab, position, Quaternion.identity);
            ps.Play();
            Destroy(ps.gameObject, ps.main.duration + ps.main.startLifetime.constantMax);
        }
    }
}
