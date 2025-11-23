using UnityEngine;
using Unity.Netcode;
using UnityEngine.AI; // <--- IMPORTANT

public class MonsterFollow : NetworkBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed;
    public float chaseRange;
    public float stopRange;
    public float turnSpeed;

    [Header("Roaming Settings")]
    public float roamRadius;
    public float roamWaitTime;

    private Transform targetPlayer;
    private NavMeshAgent agent;
    private Vector3 spawnPosition;
    private float roamTimer;

    [Header("Attack Settings")]
    public float attackRange;
    public float attackDamage;
    public float attackCooldown;
    private float nextAttackTime;
    private Warden1Animator monsterAnim;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.speed = moveSpeed;
        spawnPosition = transform.position;

        monsterAnim = GetComponent<Warden1Animator>();
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
        if (!IsServer) return;

        targetPlayer = GetClosestPlayer();
        if (targetPlayer != null)
        {
            float distance = Vector3.Distance(transform.position, targetPlayer.position);

            // Attack if close enough
            if (distance <= attackRange)
            {
                TryAttackPlayer(targetPlayer);
                agent.ResetPath(); // stop moving while attacking
                return;
            }

            // Chase if in chase range
            if (distance < chaseRange)
            {
                Debug.Log($"Monster is chasing player {targetPlayer.name} at distance {distance}");
                ChasePlayer();
                return;
            }
        }

        Roam();

        if (agent.velocity.magnitude < 0.1f)
        {
            // monsterAnim?.SetIdle();
        }
    }

    private void ChasePlayer()
    {
        agent.SetDestination(targetPlayer.position);
        agent.stoppingDistance = stopRange;

        monsterAnim?.SetWalk();
    }

    private Transform GetClosestPlayer()
    {
        Transform closest = null;
        float closestDist = Mathf.Infinity;
        Vector3 pos = transform.position;

        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            if (client.PlayerObject == null) continue;

            float dist = Vector3.Distance(pos, client.PlayerObject.transform.position);
            if (dist < closestDist)
            {
                closestDist = dist;
                closest = client.PlayerObject.transform;
            }
        }

        return closest;
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

            monsterAnim?.SetWalk();
        }
    }

    private void TryAttackPlayer(Transform player)
    {
        // Cooldown
        if (Time.time < nextAttackTime) return;
        nextAttackTime = Time.time + attackCooldown;

        // Get PlayerHealth component
        PlayerHealth health = player.GetComponent<PlayerHealth>();
        if (health != null)
        {
            health.TakeDamageServerRpc(attackDamage);
        }

        Debug.Log($"Monster attacked player {player.name} for {attackDamage} damage.");
    }
}
