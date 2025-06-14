using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    public float moveDelay = 0.15f; // Delay between moves
    private float _lastMoveTime;

    [Header("Grid Position")]
    [SerializeField] private Vector2Int _startingGridPos; // Set in Inspector
    private Vector2Int _gridPos;


    private GridManager _gridManager;
    private float _tileSize = 1f;


    void Start()
    {

        _gridManager = FindObjectOfType<GridManager>();
        _gridPos = _startingGridPos;

        // Same offset used in GridManager
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

        if (targetPos.x < 0 || targetPos.x >= _gridManager.width ||
            targetPos.y < 0 || targetPos.y >= _gridManager.height)
            return; // Out of bounds

        _gridPos = targetPos;

        Vector2 offset = new Vector2(
            -(_gridManager.width * _gridManager.tileSize) / 2f + _gridManager.tileSize / 2f,
            -(_gridManager.height * _gridManager.tileSize) / 2f + _gridManager.tileSize / 2f
        );

        transform.position = new Vector2(
            _gridPos.x * _gridManager.tileSize,
            _gridPos.y * _gridManager.tileSize
        ) + offset;

        GridCell targetCell = _gridManager.GetCell(targetPos.x, targetPos.y);

        if (targetCell != null && targetCell.type == TileType.Dirt)
        {
            targetCell.ChangeToTunnel();

            // Check if there's a rock above
            GridCell above = _gridManager.GetCell(targetPos.x, targetPos.y + 1);
            if (above != null && above.type == TileType.Rock)
            {
                above.TryStartFall();
            }
        }

    }
}
