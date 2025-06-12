using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GridCell : MonoBehaviour
{
    public int x, y;
    public TileType type;

    private SpriteRenderer _renderer;

    private void Awake()
    {
        _renderer = GetComponent<SpriteRenderer>();
    }

    public void Init(int x, int y, TileType type)
    {
        this.x = x;
        this.y = y;
        this.type = type;
    }

    public void ChangeToTunnel(Sprite tunnelSprite)
    {
        type = TileType.Tunnel;
        _renderer.sprite = tunnelSprite;
    }
}
