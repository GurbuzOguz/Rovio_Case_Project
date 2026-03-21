using UnityEngine;

public interface IBenchService
{
    int Capacity { get; }
    int OccupiedCount { get; }

    // Reserves an available bench slot. Returns false if none is available.
    bool TryReserveSlot(out Transform slot);

    // Releases a previously reserved slot.
    void ReleaseSlot(Transform slot);
}

