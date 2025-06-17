using System.Collections;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public float moveTime = 0.1f;                    // Duration to move between tiles
    public LayerMask obstacleLayer;                  // Layer mask for obstacles
    private bool isMoving = false;                   // Is the player moving
    private Vector2 input;                           // Movement input vector
    private Vector3 targetPos;                       // Target position
    public Sprite deathSprite;                       // Sprite displayed on death
    [SerializeField] private float deathDelay = 0.5f; // Fallback delay before game over
    private Vector2 lastMoveDir = Vector2.right;     // Last movement direction
    [SerializeField] private GameObject pumpPrefab;  // Prefab for pump effect
    [SerializeField] private float pumpDistance = 2f;// Distance to spawn pump

    [Header("Animation")]
    [SerializeField] private Animator _animator;     // Animator component reference

    void Start()
    {
        // Initialize player's current level
        LevelManager.Instance.currentLevel = LevelManager.Instance.GetPlayerLevelByY(transform.position.y);
        // No explicit Idle trigger needed; default Animator state handles idle.
    }

    void Update()
    {
        if (GameManager.Instance.isGameOver || isMoving)
            return;

        // Read input
        input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        if (input != Vector2.zero)
            lastMoveDir = input.normalized;

        // Restrict to one axis at a time
        if (Mathf.Abs(input.x) > 0.1f)
            input.y = 0;

        if (input != Vector2.zero)
        {
            Vector3 nextPos = transform.position + (Vector3)input;

            // Bounds check
            if (!GridManager.Instance.IsWithinBounds(nextPos))
                return;

            // Obstacle check
            Collider2D hit = Physics2D.OverlapPoint(nextPos);
            if (hit != null && hit.gameObject.layer == LayerMask.NameToLayer("Obstacle"))
                return;

            // Handle level transition and camera bounds externally
            if (!IsInsideCameraBounds(nextPos))
            {
                if (!LevelTransitionManager.Instance.TryTransitionDown(transform.position, input))
                {
                    if (LevelTransitionManager.Instance.IsBlockedByEnemies(transform.position, input))
                        Debug.Log("Enemies not cleared yet.");
                    return;
                }
            }

            // Dig tunnel
            DestroyDirtAt(nextPos);
            StartCoroutine(MoveTo(nextPos));
        }

        // Pump action
        if (Input.GetKeyDown(KeyCode.Space))
            TryPumpEnemy();
    }

    void TryPumpEnemy()
    {
        // Trigger pump animation
        if (_animator != null)
            _animator.SetTrigger("Pump");

        // Spawn pump effect
        Vector3 spawnPos = transform.position + (Vector3)(lastMoveDir * pumpDistance);
        Instantiate(pumpPrefab, spawnPos, Quaternion.identity);
    }

    public void CrushMe()
    {
        // Trigger die animation
        if (_animator != null)
            _animator.SetTrigger("Die");

        // Change to death sprite
        GetComponent<SpriteRenderer>().sprite = deathSprite;

        // Disable further input
        enabled = false;

        // Wait for die animation then handle game over
        StartCoroutine(WaitForDieSequence());
    }

    private IEnumerator WaitForDieSequence()
    {
        if (_animator != null)
        {
            // Wait until die state begins
            while (!_animator.GetCurrentAnimatorStateInfo(0).IsName("Die"))
                yield return null;

            // Wait for the die animation length
            float length = _animator.GetCurrentAnimatorStateInfo(0).length;
            yield return new WaitForSeconds(length);
        }
        else
        {
            // Fallback delay
            yield return new WaitForSeconds(deathDelay);
        }

        // Perform game over sequence
        ScoreManager.Instance.EvaluateHighScore();
        GameManager.Instance.GameOver();
        FindObjectOfType<UIManager>()?.TriggerGameOver();

        // Optional cleanup delay
        yield return new WaitForSeconds(0.3f);
        Destroy(gameObject);
    }

    IEnumerator MoveTo(Vector3 dest)
    {
        isMoving = true;
        float t = 0f;
        Vector3 start = transform.position;

        while (t < moveTime)
        {
            t += Time.deltaTime;
            transform.position = Vector3.Lerp(start, dest, t / moveTime);
            yield return null;
        }

        transform.position = dest;
        isMoving = false;
    }

    private bool IsInsideCameraBounds(Vector3 worldPos)
    {
        Camera cam = Camera.main;
        Vector3 min = cam.ViewportToWorldPoint(new Vector3(0f, 0f, 0f));
        Vector3 max = cam.ViewportToWorldPoint(new Vector3(1f, 1f, 0f));
        return worldPos.x >= min.x && worldPos.x <= max.x
            && worldPos.y >= min.y && worldPos.y <= max.y;
    }

    void DestroyDirtAt(Vector3 position)
    {
        Transform dirtToDestroy = null;
        foreach (Transform child in GridManager.Instance.transform)
        {
            if (child.name.Contains("DirtTile") && Vector3.Distance(child.position, position) < 0.1f)
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
                position,
                Quaternion.identity,
                GridManager.Instance.transform
            );
            tunnel.tag = "Tunnel";
            ScoreManager.Instance?.AddScore(10);
        }
    }
}
