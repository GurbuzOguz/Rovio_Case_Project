using UnityEngine;

public interface IProductInteractionService
{
    // Performs product consume + shift atomically. Returns false if another operation is running.
    bool TryConsumeAndShift(Vector2Int cell, Transform boxTransform, GridShiftDirection shiftDirection);

    // Applies stabilization to fill edge gaps (returns false when busy).
    bool TryFillEdgeGaps();
}

