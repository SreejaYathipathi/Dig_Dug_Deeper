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
    [SerializeField] private int _dashLength = 5;
    [Tooltip("Dash speed in tiles per second.")]
    [SerializeField] private float _dashSpeed = 5f;

    private bool _isDashing = false;
    private bool _hasDashed = false;

    // Animator parameter hash for dash
    private static readonly int DashTrigger = Animator.StringToHash("Dash");

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
        if (_isDashing)
            return;

        // Insert dash on first frame of Chasing
        if (currentState == EnemyState.Chasing)
        {
            if (!_hasDashed)
            {
                // Trigger dash animation
                animator.SetTrigger(DashTrigger);

                Vector2Int direction = DetermineDashDirection();
                StartCoroutine(DashRoutine(direction));
                _hasDashed = true;
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
            _hasDashed = false;
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
        _isDashing = true;

        for (int step = 1; step <= _dashLength; step++)
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
                hit.GetComponent<PlayerController>()?.CrushMe();

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

        _isDashing = false;
    }

    /// <summary>
    /// Smoothly moves this object one tile toward the destination at dashSpeed.
    /// </summary>
    /// <param name="destination">World position of the next tile.</param>
    /// <param name="direction">Cardinal direction vector for movement and rotation.</param>
    private IEnumerator StepTo(Vector3 destination, Vector2Int direction)
    {
        float t = 0f;
        Vector3 start = transform.position;
        Vector3 moveDir = new Vector3(direction.x, direction.y, 0f).normalized;

        while (t < 1f)
        {
            t += Time.deltaTime * _dashSpeed;
            transform.position = Vector3.Lerp(start, destination, Mathf.Min(t, 1f));
            yield return null;
        }

        transform.position = destination;
        RotateSprite(moveDir);
    }

    /// <summary>
    /// Rotates the sprite based on movement direction, assuming the default sprite faces left.
    /// </summary>
    /// <param name="dir">Normalized movement direction.</param>
    private void RotateSprite(Vector3 dir)
    {
        if (dir.sqrMagnitude < 0.01f) return;

        if (Mathf.Abs(dir.x) > Mathf.Abs(dir.y))
        {
            // Horizontal movement: default faces left, flip for right
            sr.flipX = dir.x > 0f;
            transform.rotation = Quaternion.identity;
        }
        else
        {
            // Vertical movement: no horizontal flip, rotate Z
            sr.flipX = false;
            float zAngle = dir.y > 0f ? 90f : -90f;
            transform.rotation = Quaternion.Euler(0f, 0f, zAngle);
        }
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
