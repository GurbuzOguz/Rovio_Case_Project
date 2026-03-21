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

    // Finds the closest product cell aligned with the given query.
    bool TryFindClosestAlignedProductCell(AlignedProductQuery query, out Vector2Int cell);

    // Remaining product counts on the grid (colorId -> count).
    IReadOnlyDictionary<int, int> GetRemainingCountsByColorId();

    // Removes a product from the given cell and fills the gap by shifting in the given direction.
    // Updates internal data and returns all shift moves.
    List<GridShiftMove> RemoveAndShift(Vector2Int removedCell, GridShiftDirection direction);

    // Fills empty edge cells by shifting products in the corresponding half.
    // (No remove operation; shift only). Returns performed moves.
    List<GridShiftMove> FillEdgeGaps();
}

