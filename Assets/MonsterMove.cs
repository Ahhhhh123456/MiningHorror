using UnityEngine;
using Unity.Netcode;

public class MonsterFollow : NetworkBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 3f;
    public float chaseRange = 10f;
    public float stopRange = 2f;
    public float turnSpeed = 20f;

    [Header("Roaming Settings")]
    public float roamRadius = 5f;
    public float roamWaitTime = 2f;

    private Transform targetPlayer;
    private Rigidbody rb;
    private Vector3 roamTarget;
    private float roamTimer;
    private Vector3 spawnPosition;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        spawnPosition = transform.position;
        SetNewRoamTarget();
    }

    private void Start()
    {
        // Subscribe to player spawning events
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;

            // Check for existing local player (host)
            if (NetworkManager.Singleton.LocalClient != null && NetworkManager.Singleton.LocalClient.PlayerObject != null)
            {
                targetPlayer = NetworkManager.Singleton.LocalClient.PlayerObject.transform;
            }
        }
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
        }
    }

    private void OnClientConnected(ulong clientId)
    {
        if (NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client))
        {
            if (client.PlayerObject != null && targetPlayer == null)
            {
                targetPlayer = client.PlayerObject.transform;
                Debug.Log("Monster now targeting spawned player: " + client.PlayerObject.name);
            }
        }
    }

    private void FixedUpdate()
    {
        // If we have a player in range, chase them
        if (targetPlayer != null)
        {
            float distance = Vector3.Distance(transform.position, targetPlayer.position);
            if (distance < chaseRange)
            {
                ChasePlayer(targetPlayer.position);
                return;
            }
        }

        // Otherwise, roam randomly
        Roam();
    }

    private void ChasePlayer(Vector3 playerPos)
    {
        Vector3 direction = (playerPos - transform.position).normalized;
        Quaternion lookRotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z));
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * turnSpeed);
        rb.MovePosition(transform.position + direction * moveSpeed * Time.fixedDeltaTime);
    }

    private void Roam()
    {
        roamTimer -= Time.fixedDeltaTime;
        float distanceToTarget = Vector3.Distance(transform.position, roamTarget);

        if (distanceToTarget < 0.5f || roamTimer <= 0f)
        {
            SetNewRoamTarget();
            roamTimer = roamWaitTime;
        }

        Vector3 direction = (roamTarget - transform.position).normalized;
        Quaternion lookRotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z));
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * turnSpeed);
        rb.MovePosition(transform.position + direction * moveSpeed * Time.fixedDeltaTime);
    }

    private void SetNewRoamTarget()
    {
        Vector2 randomCircle = Random.insideUnitCircle * roamRadius;
        roamTarget = spawnPosition + new Vector3(randomCircle.x, 0, randomCircle.y);
    }
}
