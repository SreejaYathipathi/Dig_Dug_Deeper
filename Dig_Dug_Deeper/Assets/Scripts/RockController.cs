using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RockController : MonoBehaviour
{
    public float checkDelay = 0.3f; // Delay before starting to fall after detecting empty space
    public float fallSpeed = 3f; // Speed at which the rock falls
    public Sprite shatteredSprite; // Sprite to show when rock shatters
    public float shortFallDestroyDelay = 1f; // Delay before destroying rock if it falls a short distance

    private bool isFalling = false; // Whether the rock is currently falling
    private bool waitingToFall = false; // Whether the rock is waiting to start falling

    void Update()
    {
        // If rock is not falling or waiting, check below it
        if (!isFalling && !waitingToFall)
        {
            Vector2 below = (Vector2)transform.position + Vector2.down;
            // If the space below is empty, begin falling sequence
            if (IsEmpty(below))
            {
                StartCoroutine(StartFallDelay());
            }
        }
    }

    // Wait for a short delay before beginning the fall
    IEnumerator StartFallDelay()
    {
        waitingToFall = true;
        yield return new WaitForSeconds(checkDelay);

        Vector2 below = (Vector2)transform.position + Vector2.down;

        // Recheck if the space is still empty after delay
        if (IsEmpty(below))
        {
            StartCoroutine(FallRoutine());
        }
        else
        {
            waitingToFall = false; // Cancel if blocked
        }
    }

    // Handles the full fall behavior
    IEnumerator FallRoutine()
    {
        yield return null;
        isFalling = true;
        int fallDistance = 0;
        Vector2 direction = Vector2.down;

        while (true)
        {
            Vector3 currentPos = transform.position;
            Vector2 nextPos = (Vector2)currentPos + direction;

            if (!IsEmpty(nextPos))
            {
                isFalling = false;

                if (fallDistance > 2)
                {
                    if (shatteredSprite != null)
                        GetComponent<SpriteRenderer>().sprite = shatteredSprite;

                    Instantiate(GridManager.Instance.tunnelPrefab, transform.position, Quaternion.identity, GridManager.Instance.transform);

                    yield return new WaitForSeconds(0.3f);
                    Destroy(gameObject);
                }
                else
                {
                    yield return new WaitForSeconds(shortFallDestroyDelay);
                    StartCoroutine(DestroyAfterDelay());
                }

                yield break;
            }

            // Leave tunnel behind at the current position
            Instantiate(GridManager.Instance.tunnelPrefab, currentPos, Quaternion.identity, GridManager.Instance.transform);

            // Animate rock falling to next tile
            Vector3 end = currentPos + (Vector3)direction;
            float t = 0f;
            float duration = 1f / fallSpeed;
            Debug.DrawLine(currentPos, end, Color.yellow, 1f);

            while (t < duration)
            {
                t += Time.deltaTime;
                float progress = Mathf.Clamp01(t / duration);
                transform.position = Vector3.Lerp(currentPos, end, progress);
                yield return null;
            }

            transform.position = end;
            fallDistance++;

            // Check for crush
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, 0.1f);
            foreach (var hit in hits)
            {
                if (hit.CompareTag("Player"))
                {
                    hit.GetComponent<PlayerController>()?.CrushMe();
                }
                else if (hit.CompareTag("Enemy"))
                {
                    hit.GetComponent<EnemyController>()?.CrushMe();
                }
            }

            // Wait one frame before continuing to fall
            yield return null;
        }
    }

    // Handles delayed destruction after a short fall
    IEnumerator DestroyAfterDelay()
    {
        yield return new WaitForSeconds(1.5f);

        if (shatteredSprite != null)
            GetComponent<SpriteRenderer>().sprite = shatteredSprite;

        // Become tunnel even on short fall
        Instantiate(GridManager.Instance.tunnelPrefab, transform.position, Quaternion.identity, GridManager.Instance.transform);

        yield return new WaitForSeconds(0.3f);
        Destroy(gameObject);
    }

    // Checks whether a tile at the given position is empty (walkable)
    bool IsEmpty(Vector2 worldPos)
    {
        foreach (Transform child in GridManager.Instance.transform)
        {
            // Check for blocking dirt tile
            if (child.name.Contains("DirtTile") && Vector3.Distance(child.position, worldPos) < 0.1f)
                return false;

            // Check for blocking rock or indestructible tile
            if (child.name.Contains("Rock") || child.name.Contains("Indestructible"))
                if (Vector3.Distance(child.position, worldPos) < 0.1f)
                    return false;
        }

        return true;
    }
}
