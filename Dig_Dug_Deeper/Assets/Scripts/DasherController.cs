using System.Collections;
using UnityEngine;

/// <summary>
/// Dasher enemy: dashes in a cardinal direction for a fixed number of tiles,
/// only when centered on grid, digs through dirt, stops at true obstacles,
/// and crushes player or other enemies upon collision.
/// </summary>
public class DasherController : EnemyController
{
    [Header("Dash Settings")]
    [Tooltip("Number of tiles the enemy can dash in one go.")]
    [SerializeField] private int dashLength = 5;
    [Tooltip("Speed at which the dash occurs (tiles per second).")]
    [SerializeField] private float dashSpeed = 5f;
    [Tooltip("Cooldown time between dashes (seconds).")]
    [SerializeField] private float dashCooldown = 5f;

    [Header("Wander Dash Settings")]
    [Tooltip("Seconds between random dash attempts while wandering.")]
    [SerializeField] private float wanderDashInterval = 5f;

    [Header("Layer Masks")]
    [Tooltip("Layer mask for indestructible obstacles.")]
    [SerializeField] private LayerMask obstacleMask;
    [Tooltip("Layer mask for player collisions.")]
    [SerializeField] private LayerMask playerMask;
    [Tooltip("Layer mask for enemy collisions.")]
    [SerializeField] private LayerMask enemyMask;
    [Tooltip("Layer mask for dirt tiles.")]
    [SerializeField] private LayerMask dirtMask;

    private float dashCooldownTimer = 0f;
    private float wanderDashTimer;
    private bool isDashing = false;

    private static readonly int DashTrigger = Animator.StringToHash("Dash");

    private new void Awake()
    {
        base.Awake();
        dashCooldownTimer = 0f;
        wanderDashTimer = wanderDashInterval;
    }

    protected override void Update()
    {
        base.Update();

        // Countdown dash cooldown
        if (dashCooldownTimer > 0f)
        {
            dashCooldownTimer -= Time.deltaTime;
            return;
        }

        // Prevent dash if already in progress
        if (isDashing)
            return;

        // Only dash when exactly at grid center
        if (!IsAtGridCenter())
            return;

        // Dash when chasing
        if (currentState == EnemyState.Chasing)
        {
            StartDash(DetermineDashDirection());
            return;
        }

        // Random dash while wandering
        if (currentState == EnemyState.Wandering)
        {
            wanderDashTimer -= Time.deltaTime;
            if (wanderDashTimer <= 0f)
            {
                wanderDashTimer = wanderDashInterval;
                Vector2Int[] dirs = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
                StartDash(dirs[Random.Range(0, dirs.Length)]);
            }
        }
    }

    private void StartDash(Vector2Int direction)
    {
        isDashing = true;
        dashCooldownTimer = dashCooldown;
        animator?.SetTrigger(DashTrigger);
        StartCoroutine(DashRoutine(direction));
    }

    private IEnumerator DashRoutine(Vector2Int direction)
    {
        int combinedMask = playerMask | enemyMask;

        for (int i = 0; i < dashLength; i++)
        {
            RotateToDirection(new Vector3(-direction.x, -direction.y, 0f));
            Vector3 nextPos = transform.position + new Vector3(direction.x, direction.y);

            if (!GridManager.Instance.IsWithinBounds(nextPos))
                break;

            if (Physics2D.OverlapPoint(nextPos, obstacleMask))
                break;

            Collider2D charHit = Physics2D.OverlapPoint(nextPos, combinedMask);
            if (charHit)
            {
                if (charHit.CompareTag("Player"))
                    charHit.GetComponent<PlayerController>()?.CrushMe();
                else if (charHit.CompareTag("Enemy") && charHit.gameObject != gameObject)
                    charHit.GetComponent<EnemyController>()?.CrushMe();
                break;
            }

            yield return StartCoroutine(StepTo(nextPos));
            DigDirtAt(nextPos);
        }

        isDashing = false;
    }

    private IEnumerator StepTo(Vector3 dest)
    {
        float t = 0f;
        Vector3 start = transform.position;
        while (t < 1f)
        {
            t += Time.deltaTime * dashSpeed;
            transform.position = Vector3.Lerp(start, dest, Mathf.Min(t, 1f));
            yield return null;
        }
        transform.position = new Vector3(Mathf.Round(dest.x), Mathf.Round(dest.y), dest.z);
        RotateToDirection((dest - start).normalized);
    }

    private bool IsAtGridCenter()
    {
        float dx = Mathf.Abs(transform.position.x - Mathf.Round(transform.position.x));
        float dy = Mathf.Abs(transform.position.y - Mathf.Round(transform.position.y));
        return dx < 0.01f && dy < 0.01f;
    }

    /// <summary>
    /// Override base rotation: use dash logic while dashing,
    /// and previous working logic when not dashing.
    /// </summary>
    /// <param name="dir">Normalized direction vector.</param>
    protected override void RotateToDirection(Vector3 dir)
    {
        if (dir.sqrMagnitude < 0.01f)
            return;

        if (isDashing)
        {
            // Dash rotation: horizontal priority, flip sprite for left
            if (Mathf.Abs(dir.x) > Mathf.Abs(dir.y))
            {
                sr.flipY = false;
                transform.rotation = Quaternion.identity;
                sr.flipX = dir.x < 0f;
            }
            else
            {
                sr.flipX = false;
                transform.rotation = Quaternion.Euler(0, 0, dir.y > 0f ? 90f : -90f);
            }
        }
        else
        {
            // Normal rotation: horizontal priority, flip sprite for right
            if (Mathf.Abs(dir.x) > Mathf.Abs(dir.y))
            {
                sr.flipY = false;
                transform.rotation = Quaternion.identity;
                sr.flipX = dir.x > 0f;
            }
            else
            {
                sr.flipX = false;
                transform.rotation = Quaternion.Euler(0, 0, dir.y > 0f ? -90f : 90f);
            }
        }
    }

    private Vector2Int DetermineDashDirection()
    {
        Vector2 delta = player.position - transform.position;
        if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
            return new Vector2Int(delta.x > 0 ? 1 : -1, 0);
        return new Vector2Int(0, delta.y > 0 ? 1 : -1);
    }

    /// <summary>
    /// Digs any overlapping dirt tiles at the given grid cell.
    /// Ensure the Dasher's Collider2D is not set as trigger, otherwise unintended collisions may occur.
    /// </summary>
    /// <summary>
    /// Digs a single dirt tile at the given grid cell by iterating children.
    /// </summary>
    private void DigDirtAt(Vector3 position)
    {
        Transform dirtToDestroy = null;
        // Iterate through all grid children to find dirt
        foreach (Transform child in GridManager.Instance.transform)
        {
            if (child.name.Contains("DirtTile") && Vector3.Distance(child.position, position) < 0.2f)
            {
                dirtToDestroy = child;
                break;
            }
        }
        if (dirtToDestroy != null)
        {
            // Remove dirt and spawn tunnel
            Destroy(dirtToDestroy.gameObject);
            GameObject tunnel = Instantiate(
                GridManager.Instance.tunnelPrefab,
                position,
                Quaternion.identity,
                GridManager.Instance.transform
            );
            tunnel.tag = "Tunnel";
            ScoreManager.Instance?.AddScore(10);
        }
    }
}


