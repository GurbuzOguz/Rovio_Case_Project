using System.Collections.Generic;
using UnityEngine;

public interface IBoxQueueService
{
    void ShiftQueueForwardInColumn(List<BoxController> activeBoxes, IReadOnlyList<Transform> spawnPoints, int queueRowSize, float queueShiftDuration, int fromIndex);
    int GetBackIndex(IReadOnlyList<Transform> spawnPoints, int queueRowSize, int fromIndex);
}
