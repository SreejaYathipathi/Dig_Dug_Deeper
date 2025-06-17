using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Controls enemy AI states and movement in the game. Handles wandering, chasing,
/// ghost mode, inflation, pathfinding, and death sequence.
/// </summary>
public class EnemyController : MonoBehaviour
{
    // Enum for enemy AI states
    public enum EnemyState { Wandering, Chasing, Ghost, Returning, FireBreath }
    public EnemyState currentState = EnemyState.Wandering;

    [SerializeField] private int scoreValue = 200;

    // Movement and behavior settings
    public float moveSpeed = 2f;
    public float ghostSpeed = 3f;
    public float playerCheckInterval = 2f;
    public float ghostGracePeriod = 3f;

    // Sprites for different enemy states
    public Sprite normalSprite;
    public Sprite ghostSprite;
    public Sprite deathSprite;

    // References to essential components and player
    protected Transform player;
    protected Rigidbody2D rb;
    protected SpriteRenderer sr;

    // State flags
    protected bool isGhost = false;
    protected Coroutine pathCheckRoutine;
    protected bool isDead = false;
    public bool IsDead => isDead;

    // Ghosting and pathfinding
    protected float timeSinceLastPathFail = 0f;
    protected Vector3 ghostTargetPosition;

    protected Vector3[] pathToPlayer;
    protected int pathIndex = 0;

    // Wandering logic
    public float maxWanderDuration = 2f;
    protected float wanderTime = 0f;
    protected float wanderTimer = 0f;
    protected bool isWanderingMoving = false;
    protected Vector3 wanderTarget;

    protected int inflateStage = 0;
    [SerializeField] protected int maxInflateStage = 4;
    [SerializeField] protected float deflateDelay = 1.5f;
    [SerializeField] protected Sprite[] inflateSprites; // assign in inspector: stage 0 to 3
    protected Coroutine deflateRoutine;
    protected bool isInflating = false;
    private Queue<bool> inflateRequests = new Queue<bool>();
    private bool isInflateCoroutineRunning = false;

    [SerializeField] protected Animator animator; // assign in Inspector

    // Animator parameter hashes for efficiency
    private static readonly int WalkTrigger = Animator.StringToHash("Walk");
    private static readonly int GhostTrigger = Animator.StringToHash("Ghost");
    private static readonly int InflateTrigger = Animator.StringToHash("Inflate");
    private static readonly int DeflateTrigger = Animator.StringToHash("Deflate");
    private static readonly int DieTrigger = Animator.StringToHash("Die");

    protected void Awake()
    {
        //animator = GetComponent<Animator>();
        GameManager.Instance?.RegisterEnemy(this);
    }

    /// <summary>
    /// Unity Start: Cache references and initialize wandering state.
    /// </summary>
    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        player = GameObject.FindGameObjectWithTag("Player").transform;

        // Set initial wandering timer and pick target
        wanderTime = Random.Range(1f, maxWanderDuration);
        PickNewWanderTarget();
        pathCheckRoutine = StartCoroutine(CheckForPlayerPathRoutine());
    }

    private void OnDestroy()
    {
        GameManager.Instance?.UnregisterEnemy(this);
    }

    /// <summary>
    /// Main update loop. Controls state switching and per-frame behaviors.
    /// </summary>
    protected virtual void Update()
    {
        // Get enemy level and skip update if not on active level
        int enemyLevel = LevelManager.Instance.GetPlayerLevelByY(transform.position.y);
        if (enemyLevel != LevelManager.Instance.currentLevel)
            return;

        // Enforce forced wandering during level transitions
        if (Time.time < LevelTransitionManager.Instance.enemyWanderEndTime)
        {
            currentState = EnemyState.Wandering;
            return;
        }

        // Only update if enemy is on-screen and alive
        if (!IsEnemyVisibleToCamera()) return;
        if (GameManager.Instance.isGameOver || isDead || isInflating)
            return;

        // Handle state logic (move, chase, etc)
        switch (currentState)
        {
            case EnemyState.Wandering:
                WanderInTunnels();
                if (animator != null) animator.SetTrigger(WalkTrigger);
                break;
            case EnemyState.Chasing:
                FollowTunnelPathToPlayer();
                if (animator != null) animator.SetTrigger(WalkTrigger);
                break;
            case EnemyState.Ghost:
                GhostMoveToTarget();
                if (animator != null) animator.SetTrigger(GhostTrigger);
                break;
            case EnemyState.Returning:
                SearchForNearbyTunnel();
                if (animator != null) animator.SetTrigger(WalkTrigger);
                break;
            case EnemyState.FireBreath:
                // Optional: Add custom trigger
                if (animator != null) animator.SetTrigger(WalkTrigger);
                break;
        }

    }

    /// <summary>
    /// Moves enemy randomly between tunnel tiles while wandering.
    /// </summary>
    void WanderInTunnels()
    {
        wanderTimer += Time.deltaTime;

        if (!isWanderingMoving)
        {
            PickNewWanderTarget();
        }
        else
        {
            MoveTowards(wanderTarget, moveSpeed * 0.5f);

            if (Vector2.Distance(transform.position, wanderTarget) < 0.05f)
            {
                isWanderingMoving = false;
            }
        }

        // Switch to chasing after wander time expires
        if (wanderTimer >= wanderTime)
        {
            currentState = EnemyState.Chasing;
            pathCheckRoutine = StartCoroutine(CheckForPlayerPathRoutine());
        }
    }

    /// <summary>
    /// Picks a new random tunnel neighbor as the wandering target.
    /// </summary>
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

    /// <summary>
    /// Coroutine: Regularly checks for a path to the player and switches state if needed.
    /// </summary>
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

    /// <summary>
    /// Queues an inflate request; processes one stage per delay period.
    /// </summary>
    public void Inflate()
    {
        if (isDead || isGhost) return;
        if (inflateStage >= maxInflateStage) return;

        inflateRequests.Enqueue(true);
        if (!isInflateCoroutineRunning)
            StartCoroutine(ProcessInflateQueue());

        // Trigger inflate animation
        if (animator != null) animator.SetTrigger(InflateTrigger);
    }

    /// <summary>
    /// Coroutine: Advances inflateStage at minimum intervals.
    /// </summary>
    IEnumerator ProcessInflateQueue()
    {
        isInflateCoroutineRunning = true;
        while (inflateRequests.Count > 0)
        {
            inflateRequests.Dequeue();
            isInflating = true;
            inflateStage++;

            if (inflateStage >= maxInflateStage)
            {
                CrushMe();
                inflateRequests.Clear();
                break;
            }

            if (deflateRoutine != null)
                StopCoroutine(deflateRoutine);
            deflateRoutine = StartCoroutine(DeflateOverTime());

            yield return new WaitForSeconds(0.5f);
        }
        isInflateCoroutineRunning = false;
    }

    /// <summary>
    /// Coroutine: Deflates enemy back to normal after a delay if not crushed.
    /// </summary>
    IEnumerator DeflateOverTime()
    {
        if (animator != null) animator.SetTrigger(DeflateTrigger);
        yield return new WaitForSeconds(deflateDelay);

        if (inflateStage >= maxInflateStage)
            yield break;

        inflateStage = 0;
        isInflating = false;
    }

    /// <summary>
    /// Attempts to find a tunnel path to the player using BFS.
    /// </summary>
    /// <returns>True if a path is found, false otherwise.</returns>
    bool TryFindTunnelPathToPlayer()
    {
        if (GameManager.Instance.isGameOver || isDead)
            return false;

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

    /// <summary>
    /// Handles player collision (enemy kills player).
    /// </summary>
    /// <param name="other">The colliding object.</param>
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isDead || GameManager.Instance.isGameOver)
            return;

        if (other.CompareTag("Player"))
        {
            Debug.Log($"{name} touched the player — killing player.");
            PlayerController player = other.GetComponent<PlayerController>();
            if (player != null)
            {
                player.CrushMe();
            }
        }
    }

    /// <summary>
    /// Moves along the calculated tunnel path to the player.
    /// </summary>
    void FollowTunnelPathToPlayer()
    {
        if (pathToPlayer == null || pathIndex >= pathToPlayer.Length)
        {
            currentState = EnemyState.Wandering;
            return;
        }

        Vector3 nextTarget = pathToPlayer[pathIndex];

        // If next path tile is blocked, try to recalculate
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

        // Move toward next path point
        MoveTowards(nextTarget, moveSpeed);

        if (Vector2.Distance(transform.position, nextTarget) < 0.1f)
        {
            pathIndex++;
        }
    }

    /// <summary>
    /// Checks if a world position is blocked by a rock or indestructible tile.
    /// </summary>
    /// <param name="worldPos">World position to check.</param>
    /// <returns>True if blocked, false otherwise.</returns>
    bool IsBlocked(Vector3 worldPos)
    {
        Collider2D hit = Physics2D.OverlapPoint(worldPos);
        if (hit == null) return false;

        string name = hit.gameObject.name;
        return name.Contains("Rock") || name.Contains("Indestructible");
    }

    /// <summary>
    /// Switches the enemy to ghost mode (move through walls).
    /// </summary>
    void EnterGhostMode()
    {
        currentState = EnemyState.Ghost;
        isGhost = true;

        ghostTargetPosition = player.position;

        if (ghostSprite != null)
            sr.sprite = ghostSprite;

        sr.sortingOrder = 5;
    }

    /// <summary>
    /// Moves through obstacles directly toward last seen player location.
    /// </summary>
    void GhostMoveToTarget()
    {
        MoveTowards(ghostTargetPosition, ghostSpeed);

        if (Vector2.Distance(transform.position, ghostTargetPosition) < 0.05f)
        {
            ExitGhostMode();

            // Check for tunnel, then switch to appropriate state
            if (IsTunnelNearby())
            {
                currentState = EnemyState.Wandering;
                wanderTimer = 0f;
                wanderTime = Random.Range(1f, maxWanderDuration);
                PickNewWanderTarget();
            }
            else
            {
                currentState = EnemyState.Returning;
            }
        }
    }

    /// <summary>
    /// Checks if the enemy is visible to the camera.
    /// </summary>
    /// <returns>True if visible, false otherwise.</returns>
    private bool IsEnemyVisibleToCamera()
    {
        Vector3 viewportPos = Camera.main.WorldToViewportPoint(transform.position);
        return viewportPos.x >= 0 && viewportPos.x <= 1f &&
               viewportPos.y >= 0 && viewportPos.y <= 1f &&
               viewportPos.z >= 0;
    }

    /// <summary>
    /// Checks if there is a tunnel adjacent to the enemy.
    /// </summary>
    /// <returns>True if at least one tunnel neighbor exists.</returns>
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

    /// <summary>
    /// In Returning state: searches for tunnel using OverlapCircle.
    /// </summary>
    void SearchForNearbyTunnel()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, 1.5f);

        foreach (var hit in hits)
        {
            if (hit.CompareTag("Tunnel"))
            {
                ExitGhostMode();

                currentState = EnemyState.Wandering;
                wanderTimer = 0f;
                wanderTime = Random.Range(1f, maxWanderDuration);
                PickNewWanderTarget();
                return;
            }
        }
    }

    /// <summary>
    /// Reverts the enemy visual from ghost mode to normal.
    /// </summary>
    void ExitGhostMode()
    {
        isGhost = false;

        if (normalSprite != null)
            sr.sprite = normalSprite;

        sr.sortingOrder = 1;
    }

    /// <summary>
    /// Moves the enemy toward a world target at given speed.
    /// </summary>
    /// <param name="target">Target world position.</param>
    /// <param name="speed">Move speed.</param>
    void MoveTowards(Vector3 target, float speed)
    {
        Vector3 dir = (target - transform.position).normalized;
        transform.position += dir * speed * Time.deltaTime;

        RotateToDirection(dir);
    }

    /// <summary>
    /// Rotates the enemy sprite to face the movement direction.
    /// </summary>
    /// <param name="dir">Normalized movement direction.</param>
    void RotateToDirection(Vector3 dir)
    {
        if (dir.sqrMagnitude < 0.01f) return;

        float absX = Mathf.Abs(dir.x), absY = Mathf.Abs(dir.y);
        if (absX > absY)
        {
            // Horizontal: just flipX, no Z rotation
            sr.flipY = false; // reset any vertical flip
            sr.flipX = dir.x < 0;
            transform.rotation = Quaternion.identity;
        }
        else
        {
            // Vertical: no flipX, rotate Z
            sr.flipX = false;
            float zAngle = dir.y > 0 ? 90f : -90f;
            transform.rotation = Quaternion.Euler(0f, 0f, zAngle);
        }
    }

    /// <summary>
    /// Called when the enemy is crushed (killed).
    /// </summary>
    public void CrushMe()
    {
        if (isDead) return;
        isDead = true;

        if (animator != null) animator.SetTrigger(DieTrigger);

        ScoreManager.Instance?.AddScore(scoreValue);

        StartCoroutine(DestroySelf());
    }

    /// <summary>
    /// Coroutine: destroys enemy object after a short delay.
    /// </summary>
    IEnumerator DestroySelf()
    {
        yield return new WaitForSeconds(0.3f);
        Destroy(gameObject);
    }
}
