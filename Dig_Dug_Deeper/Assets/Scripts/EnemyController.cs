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

    public Sprite ghostSprite;
    private Sprite _originalSprite;
    public Sprite rockDeathSprite;

    protected enum EnemyState { Idle, Wander, Chase, Ghost }
    protected EnemyState _state = EnemyState.Idle;

    public void Init(Vector2Int gridPos)
    {
        _gridPos = gridPos;
        _gridManager = FindObjectOfType<GridManager>();
        _player = GameObject.FindGameObjectWithTag("Player").transform;
        _originalSprite = GetComponent<SpriteRenderer>().sprite;

        if (_player == null)
        {
            Debug.LogError("No GameObject with tag 'Player' found. Make sure the player is tagged correctly.");
            return;
        }

        transform.position = _gridManager.GetWorldPosition(_gridPos.x, _gridPos.y);

        StartCoroutine(WanderThenChase());
    }

    private IEnumerator WanderThenChase()
    {
        _state = EnemyState.Wander;
        Debug.Log($"{name} entered WANDER state");

        float wanderDuration = 3f;
        float wanderTimer = 0f;

        while (wanderTimer < wanderDuration)
        {
            yield return new WaitForSeconds(moveDelay);

            Vector2Int[] directions = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
            Vector2Int dir = directions[Random.Range(0, directions.Length)];
            Vector2Int nextPos = _gridPos + dir;
            GridCell cell = _gridManager.GetCell(nextPos.x, nextPos.y);

            if (cell != null && cell.type == TileType.Dirt)
            {
                _gridPos = nextPos;
                transform.position = _gridManager.GetWorldPosition(_gridPos.x, _gridPos.y);
            }

            wanderTimer += moveDelay;
        }

        _state = EnemyState.Chase;
        Debug.Log($"{name} switched to CHASE state");
    }

    protected virtual void Update()
    {

        if (GameManager.Instance == null || GameManager.Instance.IsGameOver) return;

        if (_state == EnemyState.Idle || _state == EnemyState.Wander) return;
        if (Time.time - _lastMoveTime < moveDelay) return;

        Vector2Int direction = GetMoveDirectionTowardPlayer();

        if (direction != Vector2Int.zero)
        {
            Vector2Int nextPos = _gridPos + direction;
            GridCell cell = _gridManager.GetCell(nextPos.x, nextPos.y);

            bool canMove = false;

            if (_state == EnemyState.Chase && cell != null && cell.type == TileType.Tunnel)
            {
                canMove = true;
            }
            else if (_state == EnemyState.Ghost && cell != null &&
                     (cell.type == TileType.Tunnel || cell.type == TileType.Dirt))
            {
                canMove = true;
            }

            if (canMove)
            {
                _gridPos = nextPos;
                transform.position = _gridManager.GetWorldPosition(_gridPos.x, _gridPos.y);
            }
            else if (_state == EnemyState.Chase)
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

        List<Vector2Int> directions = new List<Vector2Int>();

        if (Mathf.Abs(diff.x) > Mathf.Abs(diff.y))
        {
            directions.Add(new Vector2Int((int)Mathf.Sign(diff.x), 0));
            directions.Add(new Vector2Int(0, (int)Mathf.Sign(diff.y)));
        }
        else
        {
            directions.Add(new Vector2Int(0, (int)Mathf.Sign(diff.y)));
            directions.Add(new Vector2Int((int)Mathf.Sign(diff.x), 0));
        }

        directions.Add(-directions[0]);
        directions.Add(-directions[1]);

        foreach (var dir in directions)
        {
            Vector2Int testPos = _gridPos + dir;
            GridCell cell = _gridManager.GetCell(testPos.x, testPos.y);

            if (cell != null && cell.type == TileType.Tunnel)
                return dir;
        }

        return Vector2Int.zero;
    }

    private IEnumerator GhostThroughDirt()
    {
        _state = EnemyState.Ghost;
        Debug.Log($"{name} entered GHOST state");

        var renderer = GetComponent<SpriteRenderer>();
        renderer.color = new Color(1, 1, 1, 0.5f); // transparent
        if (ghostSprite != null)
            renderer.sprite = ghostSprite;

        while (true)
        {
            yield return new WaitForSeconds(moveDelay);

            Vector2Int direction = GetMoveDirectionTowardPlayer();
            Vector2Int nextPos = _gridPos + direction;
            GridCell cell = _gridManager.GetCell(nextPos.x, nextPos.y);

            if (cell != null)
            {
                _gridPos = nextPos;
                transform.position = _gridManager.GetWorldPosition(_gridPos.x, _gridPos.y);

                if (cell.type == TileType.Tunnel)
                    break;
            }
        }

        renderer.color = Color.white;
        renderer.sprite = _originalSprite;
        _state = EnemyState.Chase;
        Debug.Log($"{name} returned to CHASE state");
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log($"{name} touched the player!");

            PlayerController player = other.GetComponent<PlayerController>();
            if (player != null)
            {
                player.KillPlayer();
            }
        }
    }

    public void ShowDeathAndDestroy(Sprite deathSprite, float delay = 0.3f)
    {
        StopAllCoroutines();
        StartCoroutine(DeathSequence(deathSprite, delay));
    }

    private IEnumerator DeathSequence(Sprite deathSprite, float delay)
    {
        GetComponent<SpriteRenderer>().sprite = deathSprite;
        GameManager.Instance.EnemyDied();
        yield return new WaitForSeconds(delay);
        Destroy(gameObject);
    }
}
