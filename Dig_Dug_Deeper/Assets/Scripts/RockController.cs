using System.Collections;
using UnityEngine;

public class RockController : MonoBehaviour
{
    [Header("Fall Settings")]
    public float checkDelay = 0.3f;             // Delay before starting to fall after detecting empty space
    public float fallSpeed = 3f;                // Tiles per second
    public Sprite shatteredSprite;              // Sprite to show when rock shatters
    public float shortFallDestroyDelay = 1f;    // Delay before destroying rock if it falls only a little

    private bool isFalling = false;             // True while the rock is actively falling
    private bool waitingToFall = false;         // True during the initial check delay

    private BoxCollider2D _boxCollider;
    private SpriteRenderer _sr;

    private void Awake()
    {
        _boxCollider = GetComponent<BoxCollider2D>();
        _sr = GetComponent<SpriteRenderer>();
        _boxCollider.isTrigger = false;        // Initially, collider is solid
    }

    private void Update()
    {
        if (!isFalling && !waitingToFall)
        {
            Vector2 below = (Vector2)transform.position + Vector2.down;
            if (IsEmpty(below))
                StartCoroutine(StartFallDelay());
        }
    }

    private IEnumerator StartFallDelay()
    {
        waitingToFall = true;
        yield return new WaitForSeconds(checkDelay);

        Vector2 below = (Vector2)transform.position + Vector2.down;
        if (IsEmpty(below))
        {
            // Enable trigger mode so OnTriggerEnter2D will fire during fall
            _boxCollider.isTrigger = true;
            StartCoroutine(FallRoutine());
        }
        else
        {
            waitingToFall = false;
        }
    }

    private IEnumerator FallRoutine()
    {
        // Wait one frame to let trigger enable propagate
        yield return null;

        isFalling = true;
        int fallDistance = 0;
        Vector2 direction = Vector2.down;

        while (true)
        {
            // Check next tile
            Vector3 currentPos = transform.position;
            Vector2 nextPos = (Vector2)currentPos + direction;
            if (!IsEmpty(nextPos))
            {
                // Landed
                isFalling = false;
                _boxCollider.isTrigger = false;

                if (fallDistance > 2)
                {
                    // Hard impact: shatter
                    if (shatteredSprite != null) _sr.sprite = shatteredSprite;
                    Instantiate(GridManager.Instance.tunnelPrefab, transform.position, Quaternion.identity, GridManager.Instance.transform);
                    yield return new WaitForSeconds(0.3f);
                    Destroy(gameObject);
                }
                else
                {
                    // Short fall: wait then destroy
                    yield return new WaitForSeconds(shortFallDestroyDelay);
                    StartCoroutine(DestroyAfterDelay());
                }
                yield break;
            }

            // Leave tunnel behind
            Instantiate(GridManager.Instance.tunnelPrefab, currentPos, Quaternion.identity, GridManager.Instance.transform);

            // Animate one-tile fall
            Vector3 end = currentPos + (Vector3)direction;
            float t = 0f;
            float duration = 1f / fallSpeed;
            while (t < duration)
            {
                t += Time.deltaTime;
                float progress = Mathf.Clamp01(t / duration);
                transform.position = Vector3.Lerp(currentPos, end, progress);
                yield return null;
            }
            transform.position = end;
            fallDistance++;

            // Next frame
            yield return null;
        }
    }

    private IEnumerator DestroyAfterDelay()
    {
        yield return new WaitForSeconds(1.5f);
        if (shatteredSprite != null) _sr.sprite = shatteredSprite;
        Instantiate(GridManager.Instance.tunnelPrefab, transform.position, Quaternion.identity, GridManager.Instance.transform);
        yield return new WaitForSeconds(0.3f);
        Destroy(gameObject);
    }

    private bool IsEmpty(Vector2 worldPos)
    {
        foreach (Transform child in GridManager.Instance.transform)
        {
            // Dirt blocks fall
            if (child.name.Contains("DirtTile") && Vector3.Distance(child.position, worldPos) < 0.1f)
                return false;
            // Rocks/indestructibles block fall
            if ((child.name.Contains("Rock") || child.name.Contains("Indestructible")) &&
                Vector3.Distance(child.position, worldPos) < 0.1f)
                return false;
        }
        return true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!isFalling) return;  // Only crush while falling

        if (other.CompareTag("Player"))
            other.GetComponent<PlayerController>()?.CrushMe();
        else if (other.CompareTag("Enemy"))
            other.GetComponent<EnemyController>()?.CrushMe();
    }
}
