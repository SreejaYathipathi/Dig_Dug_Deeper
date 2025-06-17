using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public float moveTime = 0.1f; // Time taken to move between tiles
    public LayerMask obstacleLayer; // Used to detect obstacles like rocks and indestructible walls
    private bool isMoving = false; // Whether the player is currently moving
    private Vector2 input; // Input direction
    private Vector3 targetPos; // Destination position
    public Sprite deathSprite; // Sprite to show on death
    [SerializeField] private float deathDelay = 0.5f;
    private Vector2 lastMoveDir = Vector2.right;
    [SerializeField] private GameObject pumpPrefab;
    [SerializeField] private float pumpDistance = 2f;

    void Update()
    {
        if (GameManager.Instance.isGameOver) return;
        if (isMoving) return;

        input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));

        if (input != Vector2.zero)
        {
            lastMoveDir = input.normalized;  // ✅ MOVE HERE
        }

        // Only allow one axis at a time
        if (Mathf.Abs(input.x) > 0.1f) input.y = 0;

        if (input != Vector2.zero)
        {
            Vector3 nextPos = transform.position + new Vector3(input.x, input.y, 0);

            // Bounds check
            if (!GridManager.Instance.IsWithinBounds(nextPos))
                return;

            // Check only for obstacles (Rock, Indestructible)
            Collider2D hit = Physics2D.OverlapPoint(nextPos);

            if (hit != null)
            {
                Debug.Log($"Hit {hit.gameObject.name} on layer {LayerMask.LayerToName(hit.gameObject.layer)}");

                if (hit.gameObject.layer == LayerMask.NameToLayer("Obstacle"))
                {
                    Debug.Log("Blocked by obstacle.");
                    return;
                }
            }

            DestroyDirtAt(nextPos);

            StartCoroutine(MoveTo(nextPos));
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            TryPumpEnemy();
        }
    }

    // Destroys dirt tile and spawns a tunnel at the given position
    void DestroyDirtAt(Vector3 position)
    {
        Transform dirtToDestroy = null;

        // Look for a dirt tile very close to the given position
        foreach (Transform child in GridManager.Instance.transform)
        {
            if (child.name.Contains("DirtTile") && Vector3.Distance(child.position, position) < 0.1f)
            {
                dirtToDestroy = child;
                break;
            }
        }

        // If found, destroy dirt and spawn a tunnel
        if (dirtToDestroy != null)
        {
            Destroy(dirtToDestroy.gameObject);

            GameObject tunnel = Instantiate(GridManager.Instance.tunnelPrefab, position, Quaternion.identity, GridManager.Instance.transform);
            tunnel.tag = "Tunnel";

            ScoreManager.Instance?.AddScore(10);
        }
    }

    // Smoothly move player to the destination over time
    IEnumerator MoveTo(Vector3 dest)
    {
        isMoving = true;
        float t = 0;

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

    // Called when the player is crushed by a rock
    public void CrushMe()
    {
        GetComponent<SpriteRenderer>().sprite = deathSprite;

        // Disable movement immediately
        enabled = false;

        // Start delayed Game Over sequence
        StartCoroutine(HandleDeath());
    }

    void TryPumpEnemy()
    {
        Vector3 spawnPos = transform.position + (Vector3)(lastMoveDir * pumpDistance);

        GameObject pump = Instantiate(pumpPrefab, spawnPos, Quaternion.identity);

        // Optional: rotate the pump sprite based on direction
        if (lastMoveDir == Vector2.left)
            pump.transform.rotation = Quaternion.Euler(0, 0, 180);
        else if (lastMoveDir == Vector2.right)
            pump.transform.rotation = Quaternion.Euler(0, 0, 0);
        else if (lastMoveDir == Vector2.up)
            pump.transform.rotation = Quaternion.Euler(0, 0, 90);
        else if (lastMoveDir == Vector2.down)
            pump.transform.rotation = Quaternion.Euler(0, 0, -90);
    }

    private IEnumerator HandleDeath()
    {
        yield return new WaitForSeconds(deathDelay); // wait to show death sprite

        ScoreManager.Instance.EvaluateHighScore();
        GameManager.Instance.GameOver();
        FindObjectOfType<UIManager>()?.TriggerGameOver();

        yield return new WaitForSeconds(0.3f); // optional: hold longer before removing

        Destroy(gameObject);
    }

    // Delay before actually destroying the player object
    IEnumerator DestroySelf()
    {
        yield return new WaitForSeconds(0.3f);
        Destroy(gameObject);
    }

}
