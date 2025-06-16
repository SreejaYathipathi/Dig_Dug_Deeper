using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public float moveTime = 0.1f; // smooth movement
    public LayerMask obstacleLayer; // set this to detect dirt/walls
    private bool isMoving = false;
    private Vector2 input;
    private Vector3 targetPos;
    public Sprite deathSprite;

    void Update()
    {
        if (GameManager.Instance.isGameOver) return;
        if (isMoving) return;

        input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));

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

            GameObject tunnel = Instantiate(GridManager.Instance.tunnelPrefab, position, Quaternion.identity, GridManager.Instance.transform);
            tunnel.tag = "Tunnel";
        }
    }

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

    public void CrushMe()
    {
        GetComponent<SpriteRenderer>().sprite = deathSprite;
        GameManager.Instance.GameOver();
        StartCoroutine(DestroySelf());
    }

    IEnumerator DestroySelf()
    {
        yield return new WaitForSeconds(0.3f);
        Destroy(gameObject);
    }

}
