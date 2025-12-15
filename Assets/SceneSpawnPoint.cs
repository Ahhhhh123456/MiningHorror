using UnityEngine;
using System.Collections;
using Unity.Netcode;

public class SceneSpawnPoint : NetworkBehaviour
{
    public static SceneSpawnPoint Instance;

    [Header("Spawn Point Prefab")]
    public GameObject spawnPointPrefab;

    [Header("Drill Prefab")]
    public GameObject brokenDrillPrefab;

    [Header("Drill Blueprints")]
    public GameObject drillBatteryPrefab;
    public GameObject drillBoosterPrefab;
    public GameObject drillPointPrefab;
    public GameObject drillWheelOnePrefab;
    public GameObject drillWheelTwoPrefab;
    public GameObject drillPipePrefab;

    [Header("Spawn Settings")]
    public int margin = 2;               // Distance from cave edges
    public float minHeadroom = 2f;       // Minimum vertical clearance
    public float maxSlope = 35f;         // Max slope for surface
    public int safeRadius = 1;           // Radius for clear spawn volume

    private static bool hasSpawned = false;

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
        float res = caveGenerator.resolution;

        const int step = 3;
        const int yieldEvery = 3000;
        int checks = 0;

        for (int x = margin; x < width - margin; x += step)
        for (int y = margin; y < height - margin; y += step)
        for (int z = margin; z < depth - margin; z += step)
        {
            checks++;
            if (checks % yieldEvery == 0) yield return null;

            Vector3Int voxelPos = new Vector3Int(x, y, z);

            if (!IsSpawnVolumeClear(voxelPos, safeRadius)) continue;
            if (!IsVerticalClear(voxelPos, Mathf.CeilToInt(minHeadroom), 1)) continue;

            Vector3 approx = new Vector3(x + 0.5f, y + 0.5f, z + 0.5f) * res;

            if (!IsSlopeSafe(approx, maxSlope)) continue;

            if (IsServer) // Only server spawns NetworkObjects
            {
                if (hasSpawned)
                {
                    Debug.LogWarning("[SERVER] Spawn already done — skipping");
                    yield break;
                }

                hasSpawned = true;

                
                // ---- Spawn Point ----
                NetworkObject spawnNet = Instantiate(spawnPointPrefab, approx, Quaternion.identity)
                                        .GetComponent<NetworkObject>();
                spawnNet.Spawn();
                SpawnTracker.Instance.Register(spawnNet);
                latestSpawnPoint = spawnNet.transform;

                // ---- Broken Drill ----
                Vector3 drillOffset = new Vector3(0f, 0f, 1.2f);
                Quaternion drillRotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
                NetworkObject drillNet = Instantiate(brokenDrillPrefab, approx + drillOffset, drillRotation)
                                        .GetComponent<NetworkObject>();
                drillNet.Spawn();
                SpawnTracker.Instance.Register(drillNet);

                // ---- Blueprint Anchors ----
                PlaceBlueprints anchors = drillNet.GetComponent<PlaceBlueprints>();
                if (anchors == null)
                {
                    Debug.LogError("BrokenDrill is missing PlaceBlueprints!");
                }
                else
                {
                    SpawnBlueprintAtAnchor(drillBatteryPrefab, anchors.batteryAnchor);
                    SpawnBlueprintAtAnchor(drillBoosterPrefab, anchors.boosterAnchor);
                    SpawnBlueprintAtAnchor(drillPointPrefab,   anchors.pointAnchor);
                    SpawnBlueprintAtAnchor(drillWheelOnePrefab, anchors.wheelOneAnchor);
                    SpawnBlueprintAtAnchor(drillWheelTwoPrefab, anchors.wheelTwoAnchor);
                    SpawnBlueprintAtAnchor(drillPipePrefab,     anchors.pipeAnchor);
                }
                // ---- Assign spawn to players ----
                foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
                {
                    PlayerSpawn ps = client.PlayerObject.GetComponent<PlayerSpawn>();
                    if (ps != null)
                        ps.SetSpawnPosition(approx);
                }
            }

            // Notify local callbacks
            OnSpawnPointReady?.Invoke(approx);
            callback?.Invoke(approx);

            yield break; // spawn only one
        }

        Debug.LogWarning("No valid spawn point found within cave bounds.");
    }

    private void SpawnBlueprintAtAnchor(GameObject prefab, Transform anchor)
    {
        if (!IsServer || prefab == null || anchor == null)
            return;

        NetworkObject netObj = Instantiate(
            prefab,
            anchor.position,
            anchor.rotation
        ).GetComponent<NetworkObject>();

        netObj.Spawn();
        SpawnTracker.Instance.Register(netObj);
    }



    private bool IsSpawnVolumeClear(Vector3Int pos, int radius)
    {
        for (int x = -radius; x <= radius; x++)
        for (int y = -radius; y <= radius; y++)
        for (int z = -radius; z <= radius; z++)
        {
            int nx = pos.x + x;
            int ny = pos.y + y;
            int nz = pos.z + z;

            if (nx < 0 || nx >= caveGenerator.caveWidth ||
                ny < 0 || ny >= caveGenerator.caveHeight ||
                nz < 0 || nz >= caveGenerator.caveDepth)
                return false;

            if (caveGenerator.densityMap[nx, ny, nz] > caveGenerator.isoLevel)
                return false;
        }
        return true;
    }

    private bool IsVerticalClear(Vector3Int pos, int minUp, int minDown)
    {
        for (int i = 1; i <= minUp; i++)
            if (caveGenerator.densityMap[pos.x, pos.y + i, pos.z] > caveGenerator.isoLevel)
                return false;

        for (int i = 1; i <= minDown; i++)
            if (caveGenerator.densityMap[pos.x, pos.y - i, pos.z] > caveGenerator.isoLevel)
                return false;

        return true;
    }

    private bool IsSlopeSafe(Vector3 worldPos, float maxSlope)
    {
        Vector3[] offsets = { Vector3.zero, Vector3.forward, Vector3.back, Vector3.left, Vector3.right };
        foreach (var off in offsets)
        {
            if (Physics.Raycast(worldPos + off + Vector3.up * 2f, Vector3.down, out RaycastHit hit, 5f))
            {
                if (Vector3.Angle(hit.normal, Vector3.up) > maxSlope)
                    return false;
            }
        }
        return true;
    }


}
