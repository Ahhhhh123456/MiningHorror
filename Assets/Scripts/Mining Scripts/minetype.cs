using Unity.Netcode;
using UnityEngine;

public class MineType : NetworkBehaviour
{
    public OreData oreData; // assign in Inspector per prefab
    public int holdCount = 0;

    public void MiningOre(Vector3 cameraForward, Vector3 hitNormal)
    {
        if (IsServer)
        {
            HandleMining(cameraForward, hitNormal);
        }
        else
        {
            MineServerRpc(cameraForward, hitNormal);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    void MineServerRpc(Vector3 cameraForward, Vector3 hitNormal, ServerRpcParams rpcParams = default)
    {
        HandleMining(cameraForward, hitNormal);
    }

    private void HandleMining(Vector3 cameraForward, Vector3 hitNormal)
    {
        holdCount++;

        // Play breaking particle for everyone
        if (holdCount == 1 && oreData.breakingParticles != null)
        {
            PlayParticleClientRpc(oreData.breakingParticles.name, transform.position);
            AudioManager.instance.PlaySFXClip("mine" + Random.Range(1, 5), transform);
        }
        
        if (holdCount % 30 == 0 && oreData.breakingParticles != null)
            AudioManager.instance.PlaySFXClip("mine" + Random.Range(1, 5), transform);

        if (holdCount >= oreData.durability)
        {
            if (oreData.fullyBreakParticles != null)
                PlayParticleClientRpc(oreData.fullyBreakParticles.name, transform.position);

            Dropped dropscript = GetComponent<Dropped>();
            if (dropscript != null)
                dropscript.DropItem(cameraForward, hitNormal);

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
