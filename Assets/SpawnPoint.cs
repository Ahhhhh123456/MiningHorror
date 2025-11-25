using UnityEngine;

public class SceneSpawnPoint : MonoBehaviour
{
    public static SceneSpawnPoint Instance;

    public Transform spawnLocation;

    private void Awake()
    {
        Instance = this;
    }
}