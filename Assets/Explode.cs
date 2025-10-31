using Unity.Netcode;
using UnityEngine;

public class Explode : NetworkBehaviour
{
    public float explosionRadius = 5f;
    public float carveDepth = 1.2f; // how strong it carves the cave
    public ParticleSystem explosionParticles;
    public AudioClip explosionSFX;

    public void Explosion()
    {
        if (IsServer)
            Server_Explosion();
        else
            ExplosionServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    public void ExplosionServerRpc()
    {
        Server_Explosion();
    }

    private void Server_Explosion()
    {
        Vector3 pos = transform.position;

        // 1) Damage ore blocks in radius
        Collider[] hits = Physics.OverlapSphere(pos, explosionRadius);
        foreach (var hit in hits)
        {
            MineType ore = hit.GetComponent<MineType>();
            if (ore != null)
            {
                ore.oreData.durability = 1; // skip mining, instantly break
                ore.MiningOre(Vector3.zero, Vector3.zero); // break now
            }
        }

        // 2) Carve into cave mesh
        var caveGenerator = FindObjectOfType<MarchingCubes>();
        if (caveGenerator != null)
        {
            caveGenerator.MineCaveServerRpc(pos, explosionRadius, carveDepth);
            //caveGenerator.UpdateNavMeshForMining();
        }
        // 3) Play FX for everyone
        PlayExplosionClientRpc(pos);

        // 4) Despawn dynamite object
        NetworkObject.Despawn();
    }

    [ClientRpc]
    void PlayExplosionClientRpc(Vector3 worldPos)
    {
        if (explosionParticles != null)
        {
            var fx = Instantiate(explosionParticles, worldPos, Quaternion.identity);
            fx.Play();
            Destroy(fx.gameObject, fx.main.duration + fx.main.startLifetime.constantMax);
        }

        if (explosionSFX != null)
        {
            AudioManager.instance.PlaySFXClip(explosionSFX.name, transform);
        }
    }
}
