using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FygarController : EnemyController
{
    public GameObject firePrefab; // Assign in inspector
    public float fireDuration = 1f;

    protected override void Update()
    {
        if (GameManager.Instance == null || GameManager.Instance.IsGameOver) return;

        if (_state == EnemyState.Idle || _state == EnemyState.Wander) return;
        if (Time.time - _lastMoveTime < moveDelay) return;

        if (_state == EnemyState.Chase && CanBreatheFire())
        {
            StartCoroutine(BreatheFire());
            return;
        }

        base.Update(); // fallback to EnemyController movement
    }

    private bool CanBreatheFire()
    {
        Vector2Int playerPos = new Vector2Int(
            Mathf.RoundToInt(_player.position.x / _gridManager.tileSize),
            Mathf.RoundToInt(_player.position.y / _gridManager.tileSize)
        );

        // Must be on the same Y row
        if (playerPos.y != _gridPos.y) return false;

        int dir = playerPos.x > _gridPos.x ? 1 : -1;

        // Check for obstruction between Fygar and Player
        for (int x = _gridPos.x + dir; x != playerPos.x; x += dir)
        {
            GridCell cell = _gridManager.GetCell(x, _gridPos.y);
            if (cell == null || cell.type == TileType.Dirt || cell.type == TileType.Rock)
                return false;
        }

        return true;
    }

    private IEnumerator BreatheFire()
    {
        _state = EnemyState.Idle;
        Debug.Log($"{name} is breathing fire!");

        // Create fire sprite in facing direction
        Vector3 firePos = transform.position + new Vector3(transform.localScale.x > 0 ? 1 : -1, 0, 0);
        GameObject flame = Instantiate(firePrefab, firePos, Quaternion.identity);

        // Optional: flip fire to match direction
        if (_player.position.x < transform.position.x)
        {
            flame.GetComponent<SpriteRenderer>().flipX = true;
        }

        // Damage player if still in range
        if (Mathf.RoundToInt(_player.position.y / _gridManager.tileSize) == _gridPos.y &&
            Mathf.Abs(_player.position.x - transform.position.x) <= 2f)
        {
            _player.GetComponent<PlayerController>()?.KillPlayer();
        }

        yield return new WaitForSeconds(fireDuration);
        Destroy(flame);

        _state = EnemyState.Chase;
    }
}
