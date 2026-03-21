using System;
using System.Collections.Generic;
using UnityEngine;

public interface IProductViewService
{
    // Registers the product view at the given grid cell.
    void Register(Vector2Int cell, ProductView view);

    // Removes the registration for the given grid cell.
    void Unregister(Vector2Int cell);

    // Consumes the product view at the cell (unregisters it) and starts pull animation to the box.
    // Returns false when no view exists.
    bool TryConsumeAndPullToBox(Vector2Int cell, Transform boxTransform);

    // Moves views to their new cells after grid shift.
    void ApplyShiftMoves(List<GridShiftMove> moves, Action onComplete = null);
}

