using System;
using System.Collections.Generic;
using UnityEngine;

public interface IProductViewService
{
    /// <summary>Registers the product view at the given grid cell.</summary>
    void Register(Vector2Int cell, ProductView view);

    /// <summary>Removes the registration for the given grid cell.</summary>
    void Unregister(Vector2Int cell);

    /// <summary>
    /// Consumes the product view at the cell (unregisters it) and starts pull animation to the box.
    /// Returns false when no view exists.
    /// </summary>
    bool TryConsumeAndPullToBox(Vector2Int cell, Transform boxTransform);

    /// <summary>Moves views to their new cells after grid shift.</summary>
    void ApplyShiftMoves(List<GridShiftMove> moves, Action onComplete = null);
}

