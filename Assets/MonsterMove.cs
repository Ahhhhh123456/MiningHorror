using UnityEngine;
using Unity.Netcode;
using UnityEngine.AI; // <--- IMPORTANT

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
    private NavMeshAgent agent;
    private Vector3 spawnPosition;
    private float roamTimer;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.speed = moveSpeed;
        spawnPosition = transform.position;
    }

    private void Start()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;

            if (NetworkManager.Singleton.LocalClient != null &&
                NetworkManager.Singleton.LocalClient.PlayerObject != null)
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
        if (NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client) &&
            client.PlayerObject != null && targetPlayer == null)
        {
            targetPlayer = client.PlayerObject.transform;
        }
    }

    private void Update()
    {
        if (targetPlayer != null)
        {
            float distance = Vector3.Distance(transform.position, targetPlayer.position);
            if (distance < chaseRange)
            {
                ChasePlayer();
                return;
            }
        }

        Roam();
    }

    private void ChasePlayer()
    {
        agent.SetDestination(targetPlayer.position);
        agent.stoppingDistance = stopRange;
    }

    private void Roam()
    {
        roamTimer -= Time.deltaTime;

        if (roamTimer <= 0f || agent.remainingDistance < 0.5f)
        {
            Vector2 randomCircle = Random.insideUnitCircle * roamRadius;
            Vector3 roamTarget = spawnPosition + new Vector3(randomCircle.x, 0, randomCircle.y);

            agent.stoppingDistance = 0;
            agent.SetDestination(roamTarget);
            roamTimer = roamWaitTime;
        }
    }
}
