using System.Collections.Generic;
using UnityEngine;

public interface IGridService
{
    int Rows { get; }
    int Columns { get; }

    bool IsInside(int x, int y);

    Vector3 GridToWorld(int x, int y);
    Vector2Int WorldToGrid(Vector3 worldPosition);

    int GetProductAt(int x, int y);
    bool HasProductAt(int x, int y);
    void AddProductAt(int x, int y, int colorId);
    void RemoveProductAt(int x, int y);
    bool AreAllProductsCollected();

    /// <summary>
    /// Finds the closest product cell aligned with the given query.
    /// </summary>
    bool TryFindClosestAlignedProductCell(AlignedProductQuery query, out Vector2Int cell);

    /// <summary>Remaining product counts on the grid (colorId -> count).</summary>
    IReadOnlyDictionary<int, int> GetRemainingCountsByColorId();

    /// <summary>
    /// Removes a product from the given cell and fills the gap by shifting in the given direction.
    /// Updates internal data and returns all shift moves.
    /// </summary>
    List<GridShiftMove> RemoveAndShift(Vector2Int removedCell, GridShiftDirection direction);

    /// <summary>
    /// Fills empty edge cells by shifting products in the corresponding half.
    /// (No remove operation; shift only). Returns performed moves.
    /// </summary>
    List<GridShiftMove> FillEdgeGaps();
}

