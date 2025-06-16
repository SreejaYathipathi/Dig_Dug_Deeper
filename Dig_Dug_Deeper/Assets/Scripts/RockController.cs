using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RockController : MonoBehaviour
{
    public float checkDelay = 0.3f;
    public float fallSpeed = 5f;
    public Sprite shatteredSprite;

    private bool isFalling = false;
    private bool waitingToFall = false;

    void Update()
    {
        if (!isFalling && !waitingToFall)
        {
            Vector2 below = (Vector2)transform.position + Vector2.down;
            if (IsEmpty(below))
            {
                StartCoroutine(StartFallDelay());
            }
        }
    }

    IEnumerator StartFallDelay()
    {
        waitingToFall = true;
        yield return new WaitForSeconds(checkDelay);

        Vector2 below = (Vector2)transform.position + Vector2.down;
        if (IsEmpty(below))
        {
            StartCoroutine(FallRoutine());
        }
        else
        {
            waitingToFall = false;
        }
    }

    IEnumerator FallRoutine()
    {
        isFalling = true;
        int fallDistance = 0;
        Vector2 direction = Vector2.down;

        while (true)
        {
            Vector2 nextPos = (Vector2)transform.position + direction;
            if (!IsEmpty(nextPos))
            {
                isFalling = false;

                if (fallDistance > 2)
                {
                    if (shatteredSprite != null)
                        GetComponent<SpriteRenderer>().sprite = shatteredSprite;

                    yield return new WaitForSeconds(0.3f);
                    Destroy(gameObject);
                }
                else
                {
                    StartCoroutine(DestroyAfterDelay());
                }

                yield break;
            }

            // Move
            Vector3 start = transform.position;
            Vector3 end = start + (Vector3)direction;

            float t = 0;
            while (t < 1f)
            {
                t += Time.deltaTime * fallSpeed;
                transform.position = Vector3.Lerp(start, end, t);
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
        }
    }

    IEnumerator DestroyAfterDelay()
    {
        yield return new WaitForSeconds(1.5f);

        if (shatteredSprite != null)
            GetComponent<SpriteRenderer>().sprite = shatteredSprite;

        yield return new WaitForSeconds(0.3f);
        Destroy(gameObject);
    }

    bool IsEmpty(Vector2 worldPos)
    {
        foreach (Transform child in GridManager.Instance.transform)
        {
            if (child.name.Contains("DirtTile") && Vector3.Distance(child.position, worldPos) < 0.1f)
                return false;

            if (child.name.Contains("Rock") || child.name.Contains("Indestructible"))
                if (Vector3.Distance(child.position, worldPos) < 0.1f)
                    return false;
        }

        return true;
    }
}
