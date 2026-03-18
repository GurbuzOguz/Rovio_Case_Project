using UnityEngine;

public interface IBenchService
{
    int Capacity { get; }
    int OccupiedCount { get; }

    /// <summary>
    /// Boş bir bench slot'u ayırır. Slot yoksa false döner.
    /// </summary>
    bool TryReserveSlot(out Transform slot);

    /// <summary>
    /// Daha önce ayrılmış slot'u serbest bırakır.
    /// </summary>
    void ReleaseSlot(Transform slot);
}

