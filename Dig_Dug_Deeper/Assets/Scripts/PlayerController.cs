using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public float moveDelay = 0.15f; // Delay between moves
    private float _lastMoveTime;

    [Header("Grid Position")]
    [SerializeField] private Vector2Int _startingGridPos; // Set in Inspector
    private Vector2Int _gridPos;


    private GridManager _gridManager;
    private float _tileSize = 1f;


    public Sprite deathSprite;
    private bool _isDead = false;


    void Start()
    {
        if (_gridPos == Vector2Int.zero) // fallback if Init() wasn't called
        {
            Debug.LogWarning("[PlayerController] Init() was not called. Using _startingGridPos.");
            Init(_startingGridPos); // default
        }
    }

    public void Init(Vector2Int gridPos)
    {
        _gridManager = FindObjectOfType<GridManager>();
        _gridPos = gridPos;

        Vector2 offset = new Vector2(
            -(_gridManager.width * _gridManager.tileSize) / 2f + _gridManager.tileSize / 2f,
            -(_gridManager.height * _gridManager.tileSize) / 2f + _gridManager.tileSize / 2f
        );

        transform.position = new Vector2(
            _gridPos.x * _gridManager.tileSize,
            _gridPos.y * _gridManager.tileSize
        ) + offset;
    }

    void Update()
    {
        if (Time.time - _lastMoveTime < moveDelay) return;

        Vector2Int direction = Vector2Int.zero;

        if (Input.GetKey(KeyCode.W)) direction = Vector2Int.up;
        if (Input.GetKey(KeyCode.S)) direction = Vector2Int.down;
        if (Input.GetKey(KeyCode.A)) direction = Vector2Int.left;
        if (Input.GetKey(KeyCode.D)) direction = Vector2Int.right;

        if (direction != Vector2Int.zero)
        {
            TryMove(direction);
            _lastMoveTime = Time.time;
        }

        // Pump mechanic placeholder
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Debug.Log("Start pump");
            // TODO: pump straight line
        }
    }

    void TryMove(Vector2Int dir)
    {
        Vector2Int targetPos = _gridPos + dir;

        // Out of bounds check
        if (targetPos.x < 0 || targetPos.x >= _gridManager.width ||
            targetPos.y < 0 || targetPos.y >= _gridManager.height)
            return;

        GridCell targetCell = _gridManager.GetCell(targetPos.x, targetPos.y);

        // Block movement if the tile is rock or indestructible
        if (targetCell != null &&
            (targetCell.type == TileType.Rock || targetCell.type == TileType.Indestructible))
        {
            Debug.Log("Blocked by rock or indestructible tile.");
            return;
        }

        // Otherwise, allow movement
        _gridPos = targetPos;

        Vector2 offset = new Vector2(
            -(_gridManager.width * _gridManager.tileSize) / 2f + _gridManager.tileSize / 2f,
            -(_gridManager.height * _gridManager.tileSize) / 2f + _gridManager.tileSize / 2f
        );

        transform.position = new Vector2(
            _gridPos.x * _gridManager.tileSize,
            _gridPos.y * _gridManager.tileSize
        ) + offset;

        // Dig dirt if present
        if (targetCell != null && targetCell.type == TileType.Dirt)
        {
            targetCell.ChangeToTunnel();

            // Rock falling check
            GridCell above = _gridManager.GetCell(targetPos.x, targetPos.y + 1);
            if (above != null && above.type == TileType.Rock)
            {
                RockController rock = above.GetComponent<RockController>();
                if (rock != null)
                {
                    rock.TryStartFall();
                }
            }
        }

    }

    public void KillPlayer()
    {
        Debug.Log("Player died!");
        GameManager.Instance.GameOver();

        Destroy(gameObject);
    }

    public void DieByRock()
    {
        if (_isDead) return;
        _isDead = true;
        StartCoroutine(PlayerDeathSequence());
    }

    private IEnumerator PlayerDeathSequence()
    {
        GetComponent<SpriteRenderer>().sprite = deathSprite;
        yield return new WaitForSeconds(0.5f);

        // Show game over screen here
        Debug.Log("Game Over!");
        Time.timeScale = 0f; // optional: pause game

        // Optionally, call UIManager.ShowGameOver()
    }
}
