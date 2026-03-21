using System.Collections.Generic;
using UnityEngine;

public class GridService : IGridService
{
    private readonly GridConfig _gridConfig;
    private readonly LevelLayout _levelLayout;
    private readonly float _runtimeCellSize;

    // Cell -> colorId
    private readonly Dictionary<Vector2Int, int> _products =
        new Dictionary<Vector2Int, int>();

    public int Rows => _gridConfig.rows;
    public int Columns => _gridConfig.columns;

    public GridService(GridConfig gridConfig, LevelLayout levelLayout)
    {
        _gridConfig = gridConfig;
        _levelLayout = levelLayout;
        _runtimeCellSize = _gridConfig != null ? _gridConfig.GetRuntimeCellSize() : 1f;

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
        var size = _runtimeCellSize;

        return origin + new Vector3(x * size, 0f, y * size);
    }

    public Vector2Int WorldToGrid(Vector3 worldPosition)
    {
        var origin = _gridConfig.origin;
        var size = _runtimeCellSize;

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

    public bool TryFindClosestAlignedProductCell(AlignedProductQuery query, out Vector2Int cell)
    {
        cell = default;

        if (_products.Count == 0)
        {
            return false;
        }

        var alignment = BuildAlignmentSnapshot(query.WorldPosition, query.AlignTolerance);
        bool columnAligned = alignment.columnAligned;
        bool rowAligned = alignment.rowAligned;

        if (!columnAligned && !rowAligned)
        {
            return false;
        }

        float bestDistSqr = float.PositiveInfinity;
        Vector2Int bestCell = default;
        bool found = false;

        // Search aligned row/column cells for the same color.
        foreach (var kvp in _products)
        {
            if (kvp.Value != query.ColorId)
            {
                continue;
            }

            var c = kvp.Key;
            bool matchesColumn = columnAligned && c.x == alignment.nearestColumn;
            bool matchesRow = rowAligned && c.y == alignment.nearestRow;

            // Must match at least one alignment axis.
            if (!matchesColumn && !matchesRow)
            {
                continue;
            }

            Vector3 cellWorld = GridToWorld(c.x, c.y);
            float distSqr = (new Vector2(query.WorldPosition.x, query.WorldPosition.z) - new Vector2(cellWorld.x, cellWorld.z)).sqrMagnitude;

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

    private (int nearestColumn, int nearestRow, bool columnAligned, bool rowAligned) BuildAlignmentSnapshot(Vector3 worldPosition, float alignTolerance)
    {
        var origin = _gridConfig.origin;
        var size = _runtimeCellSize;

        float localX = worldPosition.x - origin.x;
        float localZ = worldPosition.z - origin.z;

        int nearestColumn = Mathf.Clamp(Mathf.RoundToInt(localX / size), 0, Columns - 1);
        int nearestRow = Mathf.Clamp(Mathf.RoundToInt(localZ / size), 0, Rows - 1);

        float colCenterX = origin.x + nearestColumn * size;
        float rowCenterZ = origin.z + nearestRow * size;

        bool columnAligned = Mathf.Abs(worldPosition.x - colCenterX) <= alignTolerance;
        bool rowAligned = Mathf.Abs(worldPosition.z - rowCenterZ) <= alignTolerance;

        return (nearestColumn, nearestRow, columnAligned, rowAligned);
    }

    public IReadOnlyDictionary<int, int> GetRemainingCountsByColorId()
    {
        // Return a snapshot so caller cannot mutate internal state.
        var counts = new Dictionary<int, int>();
        foreach (var kvp in _products)
        {
            int id = kvp.Value;
            if (counts.TryGetValue(id, out int c))
            {
                counts[id] = c + 1;
            }
            else
            {
                counts[id] = 1;
            }
        }
        return counts;
    }

    public List<GridShiftMove> RemoveAndShift(Vector2Int removedCell, GridShiftDirection direction)
    {
        var moves = new List<GridShiftMove>();

        if (!_products.ContainsKey(removedCell))
        {
            // Cell can already be empty
            return moves;
        }

        // Remove first
        _products.Remove(removedCell);

        // Shift along row/column based on direction.
        // Split grid into halves:
        // - Left/Right: left or right half
        // - Down/Up: bottom or top half
        int midCol = (Columns - 1) / 2; // left: [0..midCol], right: [midCol+1..Columns-1]
        int midRow = (Rows - 1) / 2;    // bottom: [0..midRow], top: [midRow+1..Rows-1]

        switch (direction)
        {
            case GridShiftDirection.Left:
                ShiftRowLeft(removedCell.y, 0, midCol, moves);
                break;
            case GridShiftDirection.Right:
                ShiftRowRight(removedCell.y, midCol + 1, Columns - 1, moves);
                break;
            case GridShiftDirection.Down:
                ShiftColumnDown(removedCell.x, 0, midRow, moves);
                break;
            case GridShiftDirection.Up:
                ShiftColumnUp(removedCell.x, midRow + 1, Rows - 1, moves);
                break;
        }

        return moves;
    }

    public List<GridShiftMove> FillEdgeGaps()
    {
        var moves = new List<GridShiftMove>();

        if (_products.Count == 0)
        {
            return moves;
        }

        int midCol = (Columns - 1) / 2;
        int midRow = (Rows - 1) / 2;

        // Left half: fill left edge gaps
        for (int y = 0; y < Rows; y++)
        {
            var edge = new Vector2Int(0, y);
            if (!_products.ContainsKey(edge))
            {
                ShiftRowLeft(y, 0, midCol, moves);
            }
        }

        // Right half: fill right edge gaps
        for (int y = 0; y < Rows; y++)
        {
            var edge = new Vector2Int(Columns - 1, y);
            if (!_products.ContainsKey(edge))
            {
                ShiftRowRight(y, midCol + 1, Columns - 1, moves);
            }
        }

        // Bottom half: fill bottom edge gaps
        for (int x = 0; x < Columns; x++)
        {
            var edge = new Vector2Int(x, 0);
            if (!_products.ContainsKey(edge))
            {
                ShiftColumnDown(x, 0, midRow, moves);
            }
        }

        // Top half: fill top edge gaps
        for (int x = 0; x < Columns; x++)
        {
            var edge = new Vector2Int(x, Rows - 1);
            if (!_products.ContainsKey(edge))
            {
                ShiftColumnUp(x, midRow + 1, Rows - 1, moves);
            }
        }

        return moves;
    }

    private void ShiftRowLeft(int row, int xStart, int xEnd, List<GridShiftMove> moves)
    {
        if (xStart > xEnd)
        {
            return;
        }

        xStart = Mathf.Clamp(xStart, 0, Columns - 1);
        xEnd = Mathf.Clamp(xEnd, 0, Columns - 1);

        for (int x = xStart; x <= xEnd; x++)
        {
            var empty = new Vector2Int(x, row);
            if (_products.ContainsKey(empty))
            {
                continue;
            }

            // Find first occupied cell to the right
            for (int sx = x + 1; sx <= xEnd; sx++)
            {
                var src = new Vector2Int(sx, row);
                if (_products.TryGetValue(src, out int colorId))
                {
                    _products.Remove(src);
                    _products[empty] = colorId;
                    moves.Add(new GridShiftMove(src, empty, colorId));
                    break;
                }
            }
        }
    }

    private void ShiftRowRight(int row, int xStart, int xEnd, List<GridShiftMove> moves)
    {
        if (xStart > xEnd)
        {
            return;
        }

        xStart = Mathf.Clamp(xStart, 0, Columns - 1);
        xEnd = Mathf.Clamp(xEnd, 0, Columns - 1);

        for (int x = xEnd; x >= xStart; x--)
        {
            var empty = new Vector2Int(x, row);
            if (_products.ContainsKey(empty))
            {
                continue;
            }

            // Find first occupied cell to the left
            for (int sx = x - 1; sx >= xStart; sx--)
            {
                var src = new Vector2Int(sx, row);
                if (_products.TryGetValue(src, out int colorId))
                {
                    _products.Remove(src);
                    _products[empty] = colorId;
                    moves.Add(new GridShiftMove(src, empty, colorId));
                    break;
                }
            }
        }
    }

    private void ShiftColumnDown(int col, int yStart, int yEnd, List<GridShiftMove> moves)
    {
        if (yStart > yEnd)
        {
            return;
        }

        yStart = Mathf.Clamp(yStart, 0, Rows - 1);
        yEnd = Mathf.Clamp(yEnd, 0, Rows - 1);

        for (int y = yStart; y <= yEnd; y++)
        {
            var empty = new Vector2Int(col, y);
            if (_products.ContainsKey(empty))
            {
                continue;
            }

            for (int sy = y + 1; sy <= yEnd; sy++)
            {
                var src = new Vector2Int(col, sy);
                if (_products.TryGetValue(src, out int colorId))
                {
                    _products.Remove(src);
                    _products[empty] = colorId;
                    moves.Add(new GridShiftMove(src, empty, colorId));
                    break;
                }
            }
        }
    }

    private void ShiftColumnUp(int col, int yStart, int yEnd, List<GridShiftMove> moves)
    {
        if (yStart > yEnd)
        {
            return;
        }

        yStart = Mathf.Clamp(yStart, 0, Rows - 1);
        yEnd = Mathf.Clamp(yEnd, 0, Rows - 1);

        for (int y = yEnd; y >= yStart; y--)
        {
            var empty = new Vector2Int(col, y);
            if (_products.ContainsKey(empty))
            {
                continue;
            }

            for (int sy = y - 1; sy >= yStart; sy--)
            {
                var src = new Vector2Int(col, sy);
                if (_products.TryGetValue(src, out int colorId))
                {
                    _products.Remove(src);
                    _products[empty] = colorId;
                    moves.Add(new GridShiftMove(src, empty, colorId));
                    break;
                }
            }
        }
    }
}

