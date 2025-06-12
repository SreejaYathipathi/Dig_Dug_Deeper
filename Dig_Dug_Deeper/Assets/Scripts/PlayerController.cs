using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    public float moveDelay = 0.15f; // Delay between moves
    private float _lastMoveTime;

    private Vector2Int _gridPos;
    private GridManager _gridManager;
    private float _tileSize = 1f;

    public Sprite tunnelSprite;

    void Start()
    {
        _gridManager = FindObjectOfType<GridManager>();

        // Convert world pos to grid coordinates
        _gridPos = new Vector2Int(
            Mathf.RoundToInt(transform.position.x),
            Mathf.RoundToInt(transform.position.y)
        );

        transform.position = new Vector2(_gridPos.x, _gridPos.y);
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
        GridCell targetCell = _gridManager.GetCell(targetPos.x, targetPos.y);

        if (targetCell == null) return; // Out of bounds

        // Move player
        _gridPos = targetPos;
        transform.position = new Vector2(_gridPos.x, _gridPos.y);

        // Digging logic
        if (targetCell.type == TileType.Dirt)
        {
            targetCell.ChangeToTunnel(tunnelSprite);
        }
    }
}
