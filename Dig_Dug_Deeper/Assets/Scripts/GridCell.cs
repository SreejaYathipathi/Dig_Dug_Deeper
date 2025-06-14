using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GridCell : MonoBehaviour
{
    public int x, y;
    public TileType type;

    private SpriteRenderer _renderer;
    private GridManager _gridManager;
    private bool _isFalling = false;

    public Sprite breakingSprite;

    private void Awake()
    {
        _renderer = GetComponent<SpriteRenderer>();
        _gridManager = FindObjectOfType<GridManager>();
    }

    public void Init(int x, int y, TileType type)
    {
        this.x = x;
        this.y = y;
        this.type = type;
    }

    public void TryStartFall()
    {
        if (type != TileType.Rock || _isFalling) return;

        GridCell below = _gridManager.GetCell(x, y - 1);
        if (below == null || below.type == TileType.Tunnel)
        {
            StartCoroutine(RockFallRoutine());
        }
    }

    private IEnumerator RockFallRoutine()
    {
        _isFalling = true;

        yield return new WaitForSeconds(0.5f); // shake delay

        int fallDistance = 0;

        while (true)
        {
            GridCell below = _gridManager.GetCell(x, y - 1);

            if (below == null || below.type == TileType.Tunnel)
            {
                // Clear old position
                _gridManager.ClearCell(x, y);

                y--;
                transform.position = _gridManager.GetWorldPosition(x, y);
                _gridManager.SetCell(x, y, this);

                fallDistance++;

                yield return new WaitForSeconds(0.1f);
            }
            else
            {
                // Landed on something
                break;
            }
        }

        _isFalling = false;

        // Destroy if fell 2+ tiles
        if (fallDistance >= 2)
        {
            if (breakingSprite != null)
                _renderer.sprite = breakingSprite;

            yield return new WaitForSeconds(0.3f); // delay to show break

            _gridManager.ClearCell(x, y);
            Destroy(gameObject);
        }
    }

public void ChangeToTunnel()
    {
        type = TileType.Tunnel;

        _renderer.sprite = null;
    }
}
