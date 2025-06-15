using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RockController : MonoBehaviour
{
    public Sprite breakingSprite;

    private GridManager _gridManager;
    private SpriteRenderer _renderer;
    private int _x, _y;
    private bool _isFalling = false;

    private void Awake()
    {
        _renderer = GetComponent<SpriteRenderer>();
        _gridManager = FindObjectOfType<GridManager>();
    }

    public void Init(int x, int y)
    {
        _x = x;
        _y = y;
    }

    public void TryStartFall()
    {
        if (_isFalling) return;

        GridCell below = _gridManager.GetCell(_x, _y - 1);
        if (below == null || below.type == TileType.Tunnel)
        {
            StartCoroutine(StartFallSequence());
        }
    }

    private IEnumerator StartFallSequence()
    {
        yield return StartCoroutine(ShakeWarning());
        yield return StartCoroutine(RockFallRoutine());
    }

    private IEnumerator RockFallRoutine()
    {
        Debug.Log("RockFallRoutine started");

        _isFalling = true;

        // Shake warning (same as before)
        Vector3 originalPos = transform.position;
        float shakeDuration = 0.5f;
        float shakeStrength = 0.05f;
        float timer = 0f;

        while (timer < shakeDuration)
        {
            float x = Random.Range(-shakeStrength, shakeStrength);
            float y = Random.Range(-shakeStrength, shakeStrength);
            transform.position = originalPos + new Vector3(x, y, 0);
            timer += Time.deltaTime;
            yield return null;
        }

        transform.position = originalPos;

        int fallDistance = 0;

        while (true)
        {
            GridCell below = _gridManager.GetCell(_x, _y - 1);
            if (below == null || below.type == TileType.Tunnel)
            {
                /*_gridManager.ClearCell(_x, _y);
                _y--;
                transform.position = _gridManager.GetWorldPosition(_x, _y);
                _gridManager.SetCell(_x, _y, null); // Optional: maybe leave it null since it's a rock object*/

                _gridManager.ClearCell(_x, _y);

                // Compute next position
                int newY = _y - 1;
                Vector3 startPos = _gridManager.GetWorldPosition(_x, _y);
                Vector3 targetPos = _gridManager.GetWorldPosition(_x, newY);

                // Animate fall vertically only
                float t = 0f;
                float duration = 0.1f;
                while (t < duration)
                {
                    transform.position = Vector3.Lerp(startPos, targetPos, t / duration);
                    t += Time.deltaTime;
                    yield return null;
                }

                // Finalize
                _y = newY;
                transform.position = targetPos;
                _gridManager.SetCell(_x, _y, null);

                // Crush logic
                Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, 0.4f);
                int kills = 0;
                foreach (var hit in hits)
                {

                    Debug.Log($"Crush check: {hit.name} - tag: {hit.tag}");

                    if (hit.CompareTag("Enemy"))
                    {
                        var enemy = hit.GetComponent<EnemyController>();
                        var enemySprite = hit.GetComponent<PookaController>()?.rockDeathSprite
                                       ?? hit.GetComponent<FygarController>()?.rockDeathSprite;

                        if (enemy != null)
                            enemy.ShowDeathAndDestroy(enemy.rockDeathSprite);

                        kills++;
                    }

                    if (hit.CompareTag("Player"))
                    {
                        var player = hit.GetComponent<PlayerController>();
                        if (player != null)
                            player.DieByRock();
                    }
                }

                if (kills > 0)
                {
                    int basePoints = 1000;
                    int bonus = basePoints * kills;
                    GameManager.Instance.AddScore(bonus);
                }

                fallDistance++;
                yield return new WaitForSeconds(0.1f);
            }
            else
            {
                break;
            }
        }

        _isFalling = false;

        if (fallDistance >= 2)
        {
            if (breakingSprite != null)
                _renderer.sprite = breakingSprite;

            yield return new WaitForSeconds(0.3f);
            _gridManager.ClearCell(_x, _y);
            Destroy(gameObject);
        }
    }

    private IEnumerator ShakeWarning()
    {
        Vector3 originalPos = transform.position;
        float shakeDuration = 0.5f;
        float shakeStrength = 0.05f;
        float timer = 0f;

        while (timer < shakeDuration)
        {
            float x = Random.Range(-shakeStrength, shakeStrength);
            float y = Random.Range(-shakeStrength, shakeStrength);
            transform.position = originalPos + new Vector3(x, y, 0);
            timer += Time.deltaTime;
            yield return null;
        }

        transform.position = originalPos;
    }
}
