using Unity.Netcode;
using UnityEngine;
using System.Collections;

public class Explode : NetworkBehaviour
{
    public float explosionRadius;
    public float carveDepth; // how strong it carves the cave
    public ParticleSystem explosionParticles;

    public float knockbackForce = 3000f;


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
        //StartCoroutine(DoThingAfterSeconds(3f));
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

            // Player check
            PlayerMovement pm = hit.GetComponent<PlayerMovement>();
            if (pm != null)
            {
                Vector3 dir = (pm.transform.position - pos).normalized;
                dir.y += 1.3f;
                dir.Normalize();

                float dist = Vector3.Distance(pm.transform.position, pos);
                float falloff = 1f - (dist / explosionRadius);
                falloff = Mathf.Pow(falloff, 0.5f);   // stronger close to center
                falloff = Mathf.Max(falloff, 0.35f);  // guarantee minimum push

                pm.ApplyExplosionForce(dir * knockbackForce * falloff);
            }
        }

        

        // 2) Carve into cave mesh
        var caveGenerator = FindObjectOfType<MarchingCubes>();
        if (caveGenerator != null)
        {
            caveGenerator.MineCaveServerRpc(pos, explosionRadius, carveDepth, true);
            //caveGenerator.UpdateNavMeshForMining();
        }
        // 3) Play FX for everyone
        PlayExplosionClientRpc(pos);

        // 4) Despawn dynamite object
        NetworkObject.Despawn();
    }

    // IEnumerator DoThingAfterSeconds(float seconds)
    // {
    //     yield return new WaitForSeconds(seconds);
    //     Debug.Log("3 seconds have passed!");
    // }

    [ClientRpc]
    void PlayExplosionClientRpc(Vector3 worldPos)
    {
        if (explosionParticles != null)
        {
            var fx = Instantiate(explosionParticles, worldPos, Quaternion.identity);
            fx.Play();
            Destroy(fx.gameObject, fx.main.duration + fx.main.startLifetime.constantMax);
        }

        AudioManager.instance.PlaySFXClip("eplosion sfx", transform);
    }
}
