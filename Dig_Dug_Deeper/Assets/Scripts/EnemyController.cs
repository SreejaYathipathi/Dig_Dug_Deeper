using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyController : MonoBehaviour
{
    public enum EnemyState { Wandering, Chasing, Ghost, Returning }
    public EnemyState currentState = EnemyState.Wandering;

    public float moveSpeed = 2f;
    public float ghostSpeed = 3f;
    public float playerCheckInterval = 2f;
    public float ghostDurationMin = 2f;
    public float ghostDurationMax = 4f;
    public float ghostGracePeriod = 3f;

    public Sprite normalSprite;
    public Sprite ghostSprite;
    public Sprite deathSprite;

    private Transform player;
    private Rigidbody2D rb;
    private SpriteRenderer sr;

    private bool isGhost = false;
    private Coroutine pathCheckRoutine;

    private float timeSinceLastPathFail = 0f;
    private float ghostTimer = 0f;

    private Vector3[] pathToPlayer;
    private int pathIndex = 0;

    // ✨ WANDERING movement logic
    private float maxWanderDuration = 2f;
    private float wanderTime = 0f;
    private float wanderTimer = 0f;
    private bool isWanderingMoving = false; // ✨ NEW
    private Vector3 wanderTarget;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        player = GameObject.FindGameObjectWithTag("Player").transform;

        wanderTime = Random.Range(1f, maxWanderDuration); // how long to wander
        PickNewWanderTarget();
    }

    private void Update()
    {
        if (GameManager.Instance.isGameOver) return;

        switch (currentState)
        {
            case EnemyState.Wandering:
                WanderInTunnels();
                break;
            case EnemyState.Chasing:
                FollowTunnelPathToPlayer();
                break;
            case EnemyState.Ghost:
                GhostMoveToPlayer();
                break;
            case EnemyState.Returning:
                SearchForNearbyTunnel();
                break;
        }
    }

    void WanderInTunnels()
    {
        wanderTimer += Time.deltaTime;

        if (!isWanderingMoving)
        {
            PickNewWanderTarget(); // ✨ pick next move
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

    void FollowTunnelPathToPlayer()
    {
        if (pathToPlayer == null || pathIndex >= pathToPlayer.Length)
        {
            currentState = EnemyState.Wandering;
            return;
        }

        MoveTowards(pathToPlayer[pathIndex], moveSpeed);

        if (Vector2.Distance(transform.position, pathToPlayer[pathIndex]) < 0.1f)
        {
            pathIndex++;
        }
    }

    void EnterGhostMode()
    {
        currentState = EnemyState.Ghost;
        isGhost = true;
        ghostTimer = Random.Range(ghostDurationMin, ghostDurationMax);

        if (ghostSprite != null)
            sr.sprite = ghostSprite;

        sr.sortingOrder = 10;
    }

    void GhostMoveToPlayer()
    {
        MoveTowards(player.position, ghostSpeed);
        ghostTimer -= Time.deltaTime;

        if (ghostTimer <= 0f)
        {
            currentState = EnemyState.Returning;
        }
    }

    void SearchForNearbyTunnel()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, 1.5f);

        foreach (var hit in hits)
        {
            if (hit.CompareTag("Tunnel"))
            {
                ExitGhostMode();
                return;
            }
        }
    }

    void ExitGhostMode()
    {
        isGhost = false;
        currentState = EnemyState.Wandering;

        if (normalSprite != null)
            sr.sprite = normalSprite;

        sr.sortingOrder = 0;
    }

    void MoveTowards(Vector3 target, float speed)
    {
        Vector3 dir = (target - transform.position).normalized;
        transform.position += dir * speed * Time.deltaTime;
    }

    public void CrushMe()
    {
        GetComponent<SpriteRenderer>().sprite = deathSprite;
        StartCoroutine(DestroySelf());
    }

    IEnumerator DestroySelf()
    {
        yield return new WaitForSeconds(0.3f);
        Destroy(gameObject);
    }
}
