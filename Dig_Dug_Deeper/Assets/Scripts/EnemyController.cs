using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class EnemyController : MonoBehaviour
{
    public float moveDelay = 0.5f;
    protected float _lastMoveTime;

    protected GridManager _gridManager;
    protected Transform _player;
    protected Vector2Int _gridPos;

    protected enum EnemyState { Idle, Chase, Ghost }
    protected EnemyState _state = EnemyState.Idle;

    public void Init(Vector2Int gridPos)
    {
        _gridPos = gridPos;
        _gridManager = FindObjectOfType<GridManager>();
        _player = GameObject.FindGameObjectWithTag("Player").transform;

        if (_player == null)
        {
            Debug.LogError("No GameObject with tag 'Player' found. Make sure the player is tagged correctly.");
            return;
        }

        transform.position = _gridManager.GetWorldPosition(_gridPos.x, _gridPos.y);

        StartCoroutine(DelayedChaseStart());
    }

    private IEnumerator DelayedChaseStart()
    {
        yield return new WaitForSeconds(1f);
        _state = EnemyState.Chase;
    }

    protected virtual void Update()
    {
        if (_state != EnemyState.Chase) return;
        if (Time.time - _lastMoveTime < moveDelay) return;

        Vector2Int direction = GetMoveDirectionTowardPlayer();

        if (direction != Vector2Int.zero)
        {
            Vector2Int nextPos = _gridPos + direction;
            GridCell cell = _gridManager.GetCell(nextPos.x, nextPos.y);

            if (cell != null && cell.type == TileType.Tunnel)
            {
                _gridPos = nextPos;
                transform.position = _gridManager.GetWorldPosition(_gridPos.x, _gridPos.y);
            }
            else
            {
                StartCoroutine(GhostThroughDirt());
            }

            _lastMoveTime = Time.time;
        }
    }

    protected Vector2Int GetMoveDirectionTowardPlayer()
    {
        Vector2Int playerPos = new Vector2Int(
            Mathf.RoundToInt(_player.position.x / _gridManager.tileSize),
            Mathf.RoundToInt(_player.position.y / _gridManager.tileSize)
        );

        Vector2Int diff = playerPos - _gridPos;

        if (Mathf.Abs(diff.x) > Mathf.Abs(diff.y))
            return new Vector2Int((int)Mathf.Sign(diff.x), 0);
        else if (diff.y != 0)
            return new Vector2Int(0, (int)Mathf.Sign(diff.y));

        return Vector2Int.zero;
    }

    private IEnumerator GhostThroughDirt()
    {
        _state = EnemyState.Ghost;
        Color originalColor = GetComponent<SpriteRenderer>().color;
        GetComponent<SpriteRenderer>().color = new Color(1, 1, 1, 0.5f); // transparent

        yield return new WaitForSeconds(1.0f); // ghost duration

        GetComponent<SpriteRenderer>().color = originalColor;
        _state = EnemyState.Chase;
    }
}
