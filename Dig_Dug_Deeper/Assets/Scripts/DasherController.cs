using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Dasher enemy: dashes in a cardinal direction for a fixed number of tiles,
/// digs through dirt, stops at the first obstacle or collision,
/// crushes player or other enemies, then resumes normal AI.
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

        if (dashCooldownTimer > 0f)
            dashCooldownTimer -= Time.deltaTime;

        if (isDashing || dashCooldownTimer > 0f)
            return;

        if (currentState == EnemyState.Chasing)
        {
            Vector2Int dir = DetermineDashDirection();
            StartDash(dir);
            return;
        }

        if (currentState == EnemyState.Wandering)
        {
            wanderDashTimer -= Time.deltaTime;
            if (wanderDashTimer <= 0f)
            {
                wanderDashTimer = wanderDashInterval;
                Vector2Int[] dirs = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
                Vector2Int randomDir = dirs[Random.Range(0, dirs.Length)];
                StartDash(randomDir);
            }
        }
    }

    private void StartDash(Vector2Int direction)
    {
        if (animator != null)
            animator.SetTrigger(DashTrigger);

        StartCoroutine(DashRoutine(direction));
        dashCooldownTimer = dashCooldown;
        isDashing = true;
    }

    private IEnumerator DashRoutine(Vector2Int direction)
    {
        for (int i = 1; i <= dashLength; i++)
        {
            Vector3 next = transform.position + new Vector3(direction.x, direction.y, 0f);
            if (!GridManager.Instance.IsWithinBounds(next))
                break;

            DestroyDirtAt(next);

            Collider2D hit = Physics2D.OverlapPoint(next);
            if (hit != null)
            {
                if (hit.gameObject.name.Contains("Indestructible"))
                    break;
                if (hit.CompareTag("Player"))
                    hit.GetComponent<PlayerController>()?.CrushMe();
                EnemyController ec = hit.GetComponent<EnemyController>();
                if (ec != null && ec != this)
                    ec.CrushMe();
                break;
            }

            yield return StartCoroutine(StepTo(next, direction));
        }

        isDashing = false;
    }

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
        RotateToDirection(dest);
    }

    /// <summary>
    /// Rotates the enemy sprite to face the movement direction (inverted axes).
    /// </summary>
    /// <param name="dir">Normalized movement direction.</param>
    protected override void RotateToDirection(Vector3 dir)
    {
        if (dir.sqrMagnitude < 0.01f)
            return;

        float absX = Mathf.Abs(dir.x), absY = Mathf.Abs(dir.y);
        if (absX > absY)
        {
            // Horizontal: no rotation, flip when moving right
            sr.flipY = false;                   // clear any vertical flip
            transform.rotation = Quaternion.identity;
            sr.flipX = dir.x > 0f;              // now true when moving right
        }
        else
        {
            // Vertical: rotate Z only, up = -90°, down = +90°
            sr.flipX = false;
            float zAngle = (dir.y > 0f)
                ? -90f   // moving up
                : 90f;  // moving down
            transform.rotation = Quaternion.Euler(0f, 0f, zAngle);
        }
    }


    private Vector2Int DetermineDashDirection()
    {
        Vector2 delta = player.position - transform.position;
        if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
            return new Vector2Int(delta.x > 0 ? 1 : -1, 0);
        else
            return new Vector2Int(0, delta.y > 0 ? 1 : -1);
    }

    private void DestroyDirtAt(Vector3 pos)
    {
        Transform dirt = null;
        foreach (Transform child in GridManager.Instance.transform)
        {
            if (child.name.Contains("DirtTile") && Vector3.Distance(child.position, pos) < 0.1f)
            {
                dirt = child;
                break;
            }
        }
        if (dirt != null)
        {
            Destroy(dirt.gameObject);
            var tunnel = Instantiate(
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