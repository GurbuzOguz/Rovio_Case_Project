using System.Collections.Generic;
using UnityEngine;
#if DOTWEEN_EXISTS || true
using DG.Tweening;
#endif

public class BoxQueueService : IBoxQueueService
{
    public void ShiftQueueForwardInColumn(List<BoxController> activeBoxes, IReadOnlyList<Transform> spawnPoints, int queueRowSize, float queueShiftDuration, int fromIndex)
    {
        if (activeBoxes == null || spawnPoints == null || spawnPoints.Count == 0 || fromIndex < 0)
        {
            return;
        }

        int rowSize = Mathf.Max(1, queueRowSize);
        int totalSlots = spawnPoints.Count;
        int rowCount = Mathf.CeilToInt(totalSlots / (float)rowSize);

        int col = fromIndex % rowSize;
        int row = fromIndex / rowSize;

        for (int r = row + 1; r < rowCount; r++)
        {
            int srcIndex = r * rowSize + col;
            int dstIndex = (r - 1) * rowSize + col;
            if (srcIndex >= totalSlots || dstIndex >= totalSlots || srcIndex >= activeBoxes.Count || dstIndex >= activeBoxes.Count)
            {
                continue;
            }

            var box = activeBoxes[srcIndex];
            if (box == null || box.State != BoxState.Idle)
            {
                continue;
            }

            Transform targetSlot = spawnPoints[dstIndex];
            if (targetSlot == null)
            {
                continue;
            }

#if DOTWEEN_EXISTS || true
            box.transform
                .DOMove(targetSlot.position, queueShiftDuration)
                .SetEase(Ease.OutQuad)
                .SetLink(box.gameObject, LinkBehaviour.KillOnDisable);
#else
            box.transform.position = targetSlot.position;
#endif

            activeBoxes[dstIndex] = box;
            activeBoxes[srcIndex] = null;
        }
    }

    public int GetBackIndex(IReadOnlyList<Transform> spawnPoints, int queueRowSize, int fromIndex)
    {
        if (spawnPoints == null || spawnPoints.Count == 0)
        {
            return -1;
        }

        int rowSize = Mathf.Max(1, queueRowSize);
        int totalSlots = spawnPoints.Count;
        int rowCount = Mathf.CeilToInt(totalSlots / (float)rowSize);
        int col = Mathf.Abs(fromIndex) % rowSize;
        int backIndex = (rowCount - 1) * rowSize + col;

        if (backIndex < 0 || backIndex >= totalSlots)
        {
            backIndex = totalSlots - 1;
        }

        return backIndex;
    }
}
