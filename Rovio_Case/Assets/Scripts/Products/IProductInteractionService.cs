using UnityEngine;

public interface IProductInteractionService
{
    /// <summary>
    /// Product tüketimi + shift işlemini atomik yapar. Şu anda başka bir işlem çalışıyorsa false döner.
    /// </summary>
    bool TryConsumeAndShift(Vector2Int cell, Transform boxTransform, GridShiftDirection shiftDirection);

    /// <summary>
    /// Edge boşluklarını doldurmak için stabilizasyon uygular (busy ise false).
    /// </summary>
    bool TryFillEdgeGaps();
}

