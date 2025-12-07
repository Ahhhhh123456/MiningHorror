using UnityEngine;
using Unity.Netcode;
using UnityEngine.AI; // <--- IMPORTANT
using System.Collections;

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

    private bool isTraversingOffMeshLink = false;


    [Header("Attack Settings")]
    public float attackRange;
    public float attackDamage;
    public float attackCooldown;
    private float nextAttackTime;
    private Warden1Animator monsterAnim;

    [Header("Summoning Settings")]
    public GameObject monsterPrefab;      // Assign your Monster prefab (NetworkObject)
    public float summonDelay = 5f;        // Time monster must be aggro'd before summoning
    public int minSummons = 1;            // Minimum monsters to summon
    public int maxSummons = 3;            // Maximum monsters to summon
    public float summonRadius = 3f;       // Distance from this monster to spawn

    public float summonCooldown = 30f;    // Prevents spamming summons

    private float aggroTimer = 0f;        // Tracks how long chasing
    private static float globalNextSummonTime = 0f;

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
                ChasePlayer();

                // Increment aggro timer while chasing
                aggroTimer += Time.deltaTime;

                // Try to summon reinforcements
                if (aggroTimer >= summonDelay && Time.time >= globalNextSummonTime)
                {
                    globalNextSummonTime = Time.time + summonCooldown; // Update global cooldown
                    aggroTimer = 0f;

                    int count = Random.Range(minSummons, maxSummons + 1);
                    for (int i = 0; i < count; i++)
                    {
                        SpawnReinforcement();
                    }

                    Debug.Log($"Global summon triggered by {name}, next available in {summonCooldown} seconds.");
                }

                return;
            }
            else
            {
                // Reset aggro timer if no target
                aggroTimer = 0f;
            }
        }

        Roam();
        HandleOffMeshLinks();

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
        // Cooldown check
        if (Time.time < nextAttackTime) return;
        nextAttackTime = Time.time + attackCooldown;

        // Start attack animation
        monsterAnim?.SetAttack();

        // Apply damage after animation delay
        StartCoroutine(DealDamageAfterDelay(player, 1.0f)); // 2 = attack animation wind-up
    }

    private IEnumerator DealDamageAfterDelay(Transform player, float delay)
    {
        yield return new WaitForSeconds(delay);

        PlayerHealth health = player.GetComponent<PlayerHealth>();
        if (health != null)
        {
            health.TakeDamageServerRpc(attackDamage);
        }

        Debug.Log($"Monster attacked player {player.name} for {attackDamage} damage.");
    }

    private IEnumerator TraverseLink(NavMeshAgent agent)
    {
        OffMeshLinkData data = agent.currentOffMeshLinkData;

        Vector3 startPos = agent.transform.position;
        Vector3 endPos = data.endPos;

        Debug.Log($"Monster started jumping from {startPos} to {endPos}");

        float duration = 0.5f;  // Adjust for monster jump speed
        float t = 0f;

        // Height of the jump arc
        float jumpHeight = 1.5f;

        agent.updatePosition = false; // manual movement
        agent.updateRotation = false;

        while (t < 1f)
        {
            t += Time.deltaTime / duration;

            // Jump arc using a parabola
            float height = 4 * jumpHeight * (t - t * t);

            agent.transform.position = Vector3.Lerp(startPos, endPos, t) + Vector3.up * height;

            yield return null;
        }

        agent.updatePosition = true;
        agent.updateRotation = true;

        agent.CompleteOffMeshLink();
        isTraversingOffMeshLink = false;
    }

    private void HandleOffMeshLinks()
    {
        if (!agent.isOnOffMeshLink || isTraversingOffMeshLink)
            return;

        isTraversingOffMeshLink = true;
        StartCoroutine(TraverseLink(agent));
    }


    private void SpawnReinforcement()
    {
        if (!IsServer) return; // Only the server spawns monsters

        Vector3 offset = Random.insideUnitSphere * summonRadius;
        offset.y = 0f; // Keep on the same plane

        Vector3 spawnPos = transform.position + offset;

        NavMeshHit hit;
        if (NavMesh.SamplePosition(spawnPos, out hit, 1.5f, NavMesh.AllAreas))
        {
            GameObject monster = Instantiate(monsterPrefab, hit.position, Quaternion.identity);
            NetworkObject netObj = monster.GetComponent<NetworkObject>();
            if (netObj != null)
            {
                netObj.Spawn();
            }
            else
            {
                Debug.LogError("Monster prefab missing NetworkObject component!");
            }
        }
    }
}
