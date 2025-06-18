using System.Collections;
using UnityEngine;

/// <summary>
/// Dasher enemy: dashes in a cardinal direction for a fixed number of tiles,
/// digging through dirt, stopping at indestructible rock, and crushing player or other enemies.
/// Inherits other AI behaviors (Wandering, Chasing, Ghost, Returning, FireBreath) from EnemyController.
/// </summary>
public class DasherController : EnemyController
{
    [Header("Dash Settings")]
    [Tooltip("Number of tiles to dash in one go.")]
    [SerializeField] private int dashLength = 5;
    [Tooltip("Dash speed in tiles per second.")]
    [SerializeField] private float dashSpeed = 5f;

    private bool isDashing = false;
    private bool hasDashed = false;

    /// <summary>
    /// Overrides base Update to insert dash behavior when in Chasing state.
    /// </summary>
    protected override void Update()
    {
        // Skip if not on the active level
        int enemyLevel = LevelManager.Instance.GetPlayerLevelByY(transform.position.y);
        if (enemyLevel != LevelManager.Instance.currentLevel)
            return;

        // Enforce wandering during level transitions
        if (Time.time < LevelTransitionManager.Instance.enemyWanderEndTime)
        {
            currentState = EnemyState.Wandering;
            return;
        }

        // Skip when off-screen, game over, dead, or inflating
        if (!IsEnemyVisibleToCamera() ||
            GameManager.Instance.isGameOver ||
            isDead ||
            isInflating)
            return;

        // Do not run any state logic while dashing
        if (isDashing)
            return;

        // Insert dash on first frame of Chasing
        if (currentState == EnemyState.Chasing)
        {
            if (!hasDashed)
            {
                Vector2Int direction = DetermineDashDirection();
                StartCoroutine(DashRoutine(direction));
                hasDashed = true;
            }
            else
            {
                // After dash completes, fall back to normal AI
                base.Update();
            }
        }
        else
        {
            // Reset dash availability when leaving Chasing
            hasDashed = false;
            base.Update();
        }
    }

    /// <summary>
    /// Chooses the cardinal direction (up/down/left/right) that best points toward the player.
    /// </summary>
    /// <returns>Cardinal direction as a Vector2Int.</returns>
    private Vector2Int DetermineDashDirection()
    {
        Vector2 delta = (player.position - transform.position);
        if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
            return new Vector2Int(delta.x > 0 ? 1 : -1, 0);
        else
            return new Vector2Int(0, delta.y > 0 ? 1 : -1);
    }

    /// <summary>
    /// Performs the dash: moves step-by-step, digs dirt, stops at indestructible rock,
    /// and crushes any player or enemy encountered.
    /// </summary>
    /// <param name="direction">Cardinal direction to dash in.</param>
    private IEnumerator DashRoutine(Vector2Int direction)
    {
        isDashing = true;

        for (int step = 1; step <= dashLength; step++)
        {
            Vector3 nextPos = transform.position + new Vector3(direction.x, direction.y, 0f);

            // 1) Bounds check
            if (!GridManager.Instance.IsWithinBounds(nextPos))
                break;

            // 2) Check for unbreakable rock
            Collider2D hit = Physics2D.OverlapPoint(nextPos);
            if (hit != null && hit.gameObject.name.Contains("Indestructable"))
                break;

            // 3) Dig any dirt present
            DestroyDirtAt(nextPos);

            // 4) Crush player if encountered
            if (hit != null && hit.CompareTag("Player"))
            {
                PlayerController pc = hit.GetComponent<PlayerController>();
                pc?.CrushMe();
            }

            // 5) Crush other enemies if encountered
            if (hit != null)
            {
                EnemyController ec = hit.GetComponent<EnemyController>();
                if (ec != null && ec != this)
                    ec.CrushMe();
            }

            // 6) Move one tile toward dash direction
            yield return StartCoroutine(StepTo(nextPos, direction));
        }

        isDashing = false;
    }

    /// <summary>
    /// Smoothly moves this object one tile toward the destination at dashSpeed.
    /// Also rotates sprite to face movement direction.
    /// </summary>
    /// <param name="dest">World position of the next tile.</param>
    /// <param name="dir">Cardinal direction vector used for rotation.</param>
    private IEnumerator StepTo(Vector3 dest, Vector2Int dir)
    {
        float t = 0f;
        Vector3 start = transform.position;
        Vector3 moveDir = new Vector3(dir.x, dir.y, 0f).normalized;

        while (t < 1f)
        {
            t += Time.deltaTime * dashSpeed;
            transform.position = Vector3.Lerp(start, dest, Mathf.Min(t, 1f));
            yield return null;
        }

        transform.position = dest;
        RotateToDirection(moveDir);
    }

    /// <summary>
    /// Destroys a dirt tile at the given position and spawns a tunnel in its place.
    /// Awards score for digging.
    /// </summary>
    /// <param name="pos">World position of the dirt tile.</param>
    private void DestroyDirtAt(Vector3 pos)
    {
        Transform dirtToDestroy = null;
        foreach (Transform child in GridManager.Instance.transform)
        {
            if (child.name.Contains("DirtTile") && Vector3.Distance(child.position, pos) < 0.1f)
            {
                dirtToDestroy = child;
                break;
            }
        }

        if (dirtToDestroy != null)
        {
            Destroy(dirtToDestroy.gameObject);
            GameObject tunnel = Instantiate(
                GridManager.Instance.tunnelPrefab,
                pos,
                Quaternion.identity,
                GridManager.Instance.transform
            );
            tunnel.tag = "Tunnel";
            ScoreManager.Instance?.AddScore(10);
        }
    }
}
