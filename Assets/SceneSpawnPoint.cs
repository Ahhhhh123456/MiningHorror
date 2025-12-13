using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;

public class SceneSpawnPoint : NetworkBehaviour
{
    public static SceneSpawnPoint Instance;

    [Header("Spawn Point Prefab")]
    public GameObject spawnPointPrefab;

    private MarchingCubes caveGenerator;

    public Transform latestSpawnPoint { get; private set; }

    public static event System.Action<Vector3> OnSpawnPointReady;

    private void Awake()
    {
        Instance = this;
        caveGenerator = GetComponent<MarchingCubes>();
        if (caveGenerator == null)
            Debug.LogError("MarchingCubes component not found!");
    }

    
    public IEnumerator RandomSpawnLocation(System.Action<Vector3> callback)
    {
        var density = caveGenerator.densityMap;
        if (density == null) yield break;

        int width = caveGenerator.caveWidth;
        int height = caveGenerator.caveHeight;
        int depth = caveGenerator.caveDepth;

        float iso = caveGenerator.isoLevel;
        float res = caveGenerator.resolution;

        const int step = 3;
        const int yieldEvery = 3000;
        int checks = 0;

        for (int x = 1; x < width - 1; x += step)
        for (int y = 1; y < height - 3; y += step)
        for (int z = 1; z < depth - 1; z += step)
        {
            checks++;
            if (checks % yieldEvery == 0)
                yield return null;

            if (density[x, y, z] <= iso) continue;
            if (density[x, y + 1, z] > iso) continue;
            if (density[x, y + 2, z] > iso) continue;

            Vector3 approx = new Vector3(x + 0.5f, y + 3f, z + 0.5f) * res;

            if (!Physics.Raycast(approx, Vector3.down, out RaycastHit hit, 10f))
                continue;

            if (Vector3.Angle(hit.normal, Vector3.up) > 35f)
                continue;

            Vector3 spawnPos = hit.point + Vector3.up * 0.1f;

            // ---- INSTANTIATE ----
            GameObject obj = Instantiate(spawnPointPrefab, spawnPos, Quaternion.identity);
            obj.name = "SpawnPoint";

            // ✅ CRITICAL FIX
            latestSpawnPoint = obj.transform;

            NetworkObject netObj = obj.GetComponent<NetworkObject>();
            if (IsServer && netObj != null)
            {
                netObj.Spawn();
                SpawnTracker.Instance.Register(netObj);

                // ✅ restore player spawn assignment
                foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
                {
                    PlayerSpawn ps = client.PlayerObject.GetComponent<PlayerSpawn>();
                    if (ps != null)
                        ps.SetSpawnPosition(spawnPos);
                }
            }

            // ✅ restore event + callback
            OnSpawnPointReady?.Invoke(spawnPos);
            callback?.Invoke(spawnPos);

            yield break;
        }

        Debug.LogWarning("No valid spawn point found.");
    }


}
