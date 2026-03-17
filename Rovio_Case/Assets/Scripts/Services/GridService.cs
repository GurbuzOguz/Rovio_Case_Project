using System.Collections.Generic;
using UnityEngine;

public class GridService : IGridService
{
    private readonly GridConfig _gridConfig;
    private readonly LevelLayout _levelLayout;

    // colorId -> int
    private readonly Dictionary<Vector2Int, int> _products =
        new Dictionary<Vector2Int, int>();

    public int Rows => _gridConfig.rows;
    public int Columns => _gridConfig.columns;

    public GridService(GridConfig gridConfig, LevelLayout levelLayout)
    {
        _gridConfig = gridConfig;
        _levelLayout = levelLayout;

        InitializeProducts();
    }

    private void InitializeProducts()
    {
        _products.Clear();
        foreach (var cell in _levelLayout.products)
        {
            var coord = new Vector2Int(cell.x, cell.y);
            if (!IsInside(coord.x, coord.y))
            {
                continue;
            }

            if (cell.colorId < 0)
            {
                continue;
            }

            _products[coord] = cell.colorId;
        }
    }

    public bool IsInside(int x, int y)
    {
        return x >= 0 && x < Columns && y >= 0 && y < Rows;
    }

    public Vector3 GridToWorld(int x, int y)
    {
        var origin = _gridConfig.origin;
        var size = _gridConfig.cellSize;

        return origin + new Vector3(x * size, 0f, y * size);
    }

    public Vector2Int WorldToGrid(Vector3 worldPosition)
    {
        var origin = _gridConfig.origin;
        var size = _gridConfig.cellSize;

        var local = worldPosition - origin;
        int x = Mathf.RoundToInt(local.x / size);
        int y = Mathf.RoundToInt(local.z / size);

        return new Vector2Int(x, y);
    }

    public int GetProductAt(int x, int y)
    {
        var coord = new Vector2Int(x, y);
        if (_products.TryGetValue(coord, out var colorId))
        {
            return colorId;
        }

        return -1;
    }

    public bool HasProductAt(int x, int y)
    {
        var coord = new Vector2Int(x, y);
        return _products.ContainsKey(coord);
    }

    public void AddProductAt(int x, int y, int colorId)
    {
        if (!IsInside(x, y))
        {
            return;
        }

        if (colorId < 0)
        {
            return;
        }

        var coord = new Vector2Int(x, y);
        _products[coord] = colorId;
    }

    public void RemoveProductAt(int x, int y)
    {
        var coord = new Vector2Int(x, y);
        _products.Remove(coord);
    }

    public bool AreAllProductsCollected()
    {
        return _products.Count == 0;
    }
}

