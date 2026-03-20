using UnityEngine;

public interface IProductInteractionService
{
    /// <summary>
    /// Performs product consume + shift atomically. Returns false if another operation is running.
    /// </summary>
    bool TryConsumeAndShift(Vector2Int cell, Transform boxTransform, GridShiftDirection shiftDirection);

    /// <summary>
    /// Applies stabilization to fill edge gaps (returns false when busy).
    /// </summary>
    bool TryFillEdgeGaps();
}

