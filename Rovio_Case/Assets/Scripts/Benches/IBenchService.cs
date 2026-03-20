using UnityEngine;

public interface IBenchService
{
    int Capacity { get; }
    int OccupiedCount { get; }

    /// <summary>
    /// Reserves an available bench slot. Returns false if none is available.
    /// </summary>
    bool TryReserveSlot(out Transform slot);

    /// <summary>
    /// Releases a previously reserved slot.
    /// </summary>
    void ReleaseSlot(Transform slot);
}

