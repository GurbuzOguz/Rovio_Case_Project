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

    public bool TryFindAlignedProductCell(Vector3 worldPosition, float alignTolerance, int colorId, out Vector2Int cell)
    {
        cell = default;

        if (_products.Count == 0)
        {
            return false;
        }

        var origin = _gridConfig.origin;
        var size = _gridConfig.cellSize;

        // Grid üzerindeki hücre merkezleri:
        // xCenter = origin.x + x*size
        // zCenter = origin.z + y*size
        // Not: Box gridin dışında dolaşabileceği için en yakın satır/sütunu clamp ediyoruz.
        float localX = worldPosition.x - origin.x;
        float localZ = worldPosition.z - origin.z;

        int nearestColumn = Mathf.Clamp(Mathf.RoundToInt(localX / size), 0, Columns - 1);
        int nearestRow = Mathf.Clamp(Mathf.RoundToInt(localZ / size), 0, Rows - 1);

        float colCenterX = origin.x + nearestColumn * size;
        float rowCenterZ = origin.z + nearestRow * size;

        bool columnAligned = Mathf.Abs(worldPosition.x - colCenterX) <= alignTolerance;
        bool rowAligned = Mathf.Abs(worldPosition.z - rowCenterZ) <= alignTolerance;

        if (!columnAligned && !rowAligned)
        {
            return false;
        }

        float bestDistSqr = float.PositiveInfinity;
        Vector2Int bestCell = default;
        bool found = false;

        // Aynı renk için, hizalı sütun/satır üzerindeki productları tara.
        foreach (var kvp in _products)
        {
            if (kvp.Value != colorId)
            {
                continue;
            }

            var c = kvp.Key;
            bool matchesColumn = columnAligned && c.x == nearestColumn;
            bool matchesRow = rowAligned && c.y == nearestRow;

            // İki hizalamadan en az birine uymalı
            if (!matchesColumn && !matchesRow)
            {
                continue;
            }

            Vector3 cellWorld = GridToWorld(c.x, c.y);
            float distSqr = (new Vector2(worldPosition.x, worldPosition.z) - new Vector2(cellWorld.x, cellWorld.z)).sqrMagnitude;

            if (distSqr < bestDistSqr)
            {
                bestDistSqr = distSqr;
                bestCell = c;
                found = true;
            }
        }

        if (!found)
        {
            return false;
        }

        cell = bestCell;
        return true;
    }
}

