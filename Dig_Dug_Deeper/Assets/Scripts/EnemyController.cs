using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyController : MonoBehaviour
{
    // Enum for enemy AI states
    public enum EnemyState { Wandering, Chasing, Ghost, Returning }
    public EnemyState currentState = EnemyState.Wandering;

    // Movement and behavior settings
    public float moveSpeed = 2f;
    public float ghostSpeed = 3f;
    public float playerCheckInterval = 2f;
    public float ghostGracePeriod = 3f;

    // Sprites for different enemy states
    public Sprite normalSprite;
    public Sprite ghostSprite;
    public Sprite deathSprite;

    // References
    private Transform player;
    private Rigidbody2D rb;
    private SpriteRenderer sr;

    // State flags
    private bool isGhost = false;
    private Coroutine pathCheckRoutine;
    private bool isDead = false;

    // Ghosting and pathfinding
    private float timeSinceLastPathFail = 0f;
    private Vector3 ghostTargetPosition;

    private Vector3[] pathToPlayer;
    private int pathIndex = 0;

    // Wandering logic
    public float maxWanderDuration = 2f;
    private float wanderTime = 0f;
    private float wanderTimer = 0f;
    private bool isWanderingMoving = false;
    private Vector3 wanderTarget;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        player = GameObject.FindGameObjectWithTag("Player").transform;

        // Initialize wandering timer and target
        wanderTime = Random.Range(1f, maxWanderDuration); // how long to wander
        PickNewWanderTarget();
        pathCheckRoutine = StartCoroutine(CheckForPlayerPathRoutine());
    }

    private void Update()
    {
        if (GameManager.Instance.isGameOver || isDead) return;

        // Execute behavior based on state
        switch (currentState)
        {
            case EnemyState.Wandering:
                WanderInTunnels();
                break;
            case EnemyState.Chasing:
                FollowTunnelPathToPlayer();
                break;
            case EnemyState.Ghost:
                GhostMoveToTarget();
                break;
            case EnemyState.Returning:
                SearchForNearbyTunnel();
                break;
        }
    }

    // Wandering movement between tunnel tiles
    void WanderInTunnels()
    {
        wanderTimer += Time.deltaTime;

        if (!isWanderingMoving)
        {
            PickNewWanderTarget(); // pick next move
        }
        else
        {
            MoveTowards(wanderTarget, moveSpeed * 0.5f); // slower

            if (Vector2.Distance(transform.position, wanderTarget) < 0.05f)
            {
                isWanderingMoving = false;
            }
        }

        if (wanderTimer >= wanderTime)
        {
            currentState = EnemyState.Chasing;
            pathCheckRoutine = StartCoroutine(CheckForPlayerPathRoutine());
        }
    }

    // Picks a random neighboring tunnel tile to wander to
    void PickNewWanderTarget()
    {
        Vector2Int currentPos = Vector2Int.RoundToInt(transform.position);
        List<Vector2Int> tunnelOptions = new List<Vector2Int>();

        Vector2Int[] directions = new Vector2Int[]
        {
        Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right
        };

        foreach (var dir in directions)
        {
            Vector2Int neighbor = currentPos + dir;
            if (GridManager.Instance.IsTunnelAt((Vector2)neighbor))
            {
                tunnelOptions.Add(neighbor);
            }
        }

        Debug.Log($"[{name}] Tunnel Neighbors: {tunnelOptions.Count}");

        if (tunnelOptions.Count > 0)
        {
            Vector2Int chosen = tunnelOptions[Random.Range(0, tunnelOptions.Count)];
            wanderTarget = new Vector3(chosen.x, chosen.y, 0);
            isWanderingMoving = true;
            Debug.Log($"[{name}] Wandering to {wanderTarget}");
        }
        else
        {
            wanderTarget = transform.position;
            isWanderingMoving = false;
            Debug.LogWarning($"[{name}] No tunnel neighbors to wander!");
        }
    }

    // Periodically checks if a tunnel path to the player can be found
    IEnumerator CheckForPlayerPathRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(playerCheckInterval);

            if (currentState == EnemyState.Ghost || currentState == EnemyState.Returning)
                continue;

            if (TryFindTunnelPathToPlayer())
            {
                currentState = EnemyState.Chasing;
                timeSinceLastPathFail = 0f;
            }
            else
            {
                timeSinceLastPathFail += playerCheckInterval;

                if (timeSinceLastPathFail >= ghostGracePeriod)
                {
                    EnterGhostMode();
                    timeSinceLastPathFail = 0f;
                }
            }
        }
    }

    // Attempts to find a tunnel-based path to the player
    bool TryFindTunnelPathToPlayer()
    {
        Vector2Int start = Vector2Int.RoundToInt(transform.position);
        Vector2Int goal = Vector2Int.RoundToInt(player.position);

        Queue<Vector2Int> frontier = new Queue<Vector2Int>();
        Dictionary<Vector2Int, Vector2Int> cameFrom = new Dictionary<Vector2Int, Vector2Int>();

        frontier.Enqueue(start);
        cameFrom[start] = start;

        Vector2Int[] directions = new Vector2Int[]
        {
            Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right
        };

        while (frontier.Count > 0)
        {
            Vector2Int current = frontier.Dequeue();

            if (current == goal)
            {
                List<Vector3> path = new List<Vector3>();
                Vector2Int step = goal;

                while (step != start)
                {
                    path.Add(new Vector3(step.x, step.y, 0));
                    step = cameFrom[step];
                }

                path.Reverse();
                pathToPlayer = path.ToArray();
                pathIndex = 0;
                return true;
            }

            foreach (var dir in directions)
            {
                Vector2Int next = current + dir;

                if (cameFrom.ContainsKey(next))
                    continue;

                if (GridManager.Instance.IsTunnelAt((Vector2)next))
                {
                    frontier.Enqueue(next);
                    cameFrom[next] = current;
                }
            }
        }

        return false;
    }

    // Follows the calculated tunnel path to the player
    void FollowTunnelPathToPlayer()
    {

        if (pathToPlayer == null || pathIndex >= pathToPlayer.Length)
        {
            currentState = EnemyState.Wandering;
            return;
        }

        Vector3 nextTarget = pathToPlayer[pathIndex];

        // If the next tile is now blocked by a rock or obstacle
        if (IsBlocked(nextTarget))
        {
            Debug.LogWarning($"[{name}] Path blocked at {nextTarget}. Recalculating...");

            if (TryFindTunnelPathToPlayer())
            {
                Debug.Log($"[{name}] Recalculated new path to player.");
            }
            else
            {
                Debug.LogWarning($"[{name}] Could not find new path. Entering Ghost Mode.");
                EnterGhostMode();
            }

            return;
        }

        // Normal movement toward current path point
        MoveTowards(nextTarget, moveSpeed);

        if (Vector2.Distance(transform.position, nextTarget) < 0.1f)
        {
            pathIndex++;
        }
    }

    // Checks whether a tile is now blocked by a Rock or Indestructible tile
    bool IsBlocked(Vector3 worldPos)
    {
        Collider2D hit = Physics2D.OverlapPoint(worldPos);
        if (hit == null) return false;

        string name = hit.gameObject.name;
        return name.Contains("Rock") || name.Contains("Indestructible");
    }

    // Switches the enemy into ghost mode
    void EnterGhostMode()
    {

        currentState = EnemyState.Ghost;
        isGhost = true;

        ghostTargetPosition = player.position; //Save player's location at that moment

        if (ghostSprite != null)
            sr.sprite = ghostSprite;

        sr.sortingOrder = 10;
    }

    // Moves through walls toward the stored player location
    void GhostMoveToTarget()
    {
        MoveTowards(ghostTargetPosition, ghostSpeed);

        if (Vector2.Distance(transform.position, ghostTargetPosition) < 0.05f)
        {
            ExitGhostMode();

            // Immediately check for tunnel and go into Wandering if found
            if (IsTunnelNearby())
            {
                currentState = EnemyState.Wandering;
                wanderTimer = 0f;
                wanderTime = Random.Range(1f, maxWanderDuration);
                PickNewWanderTarget();
            }
            else
            {
                currentState = EnemyState.Returning; // fallback
            }
        }
    }

    // Checks for adjacent tunnel tiles
    bool IsTunnelNearby()
    {
        Vector2Int currentPos = Vector2Int.RoundToInt(transform.position);
        Vector2Int[] directions = new Vector2Int[]
        {
        Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right
        };

        foreach (var dir in directions)
        {
            Vector2Int checkPos = currentPos + dir;
            if (GridManager.Instance.IsTunnelAt((Vector2)checkPos))
                return true;
        }

        return false;
    }

    // Searches for tunnel using OverlapCircle in Returning state
    void SearchForNearbyTunnel()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, 1.5f);

        foreach (var hit in hits)
        {
            if (hit.CompareTag("Tunnel"))
            {
                ExitGhostMode();

                // Switch to Wandering
                currentState = EnemyState.Wandering;

                // Reset wander timer
                wanderTimer = 0f;
                wanderTime = Random.Range(1f, maxWanderDuration);

                PickNewWanderTarget();
                return;
            }
        }
    }

    // Reverts ghost visual effects
    void ExitGhostMode()
    {

        isGhost = false;

        if (normalSprite != null)
            sr.sprite = normalSprite;

        sr.sortingOrder = 0;
    }

    // Moves the object toward a world position at a given speed
    void MoveTowards(Vector3 target, float speed)
    {
        Vector3 dir = (target - transform.position).normalized;
        transform.position += dir * speed * Time.deltaTime;
    }

    // Called when enemy is crushed
    public void CrushMe()
    {
        if (isDead) return; // prevent double-call
        isDead = true;

        GetComponent<SpriteRenderer>().sprite = deathSprite;
        StartCoroutine(DestroySelf());
    }

    // Destroys enemy after delay
    IEnumerator DestroySelf()
    {
        yield return new WaitForSeconds(0.3f);
        Destroy(gameObject);
    }
}
