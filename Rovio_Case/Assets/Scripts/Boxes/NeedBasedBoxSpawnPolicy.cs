using System.Collections.Generic;
using UnityEngine;

public class NeedBasedBoxSpawnPolicy : IBoxSpawnPolicy
{
    public int ChooseNextColorIdToSpawn(IReadOnlyDictionary<int, int> remainingByColor, IEnumerable<BoxController> knownBoxes)
    {
        if (remainingByColor == null || remainingByColor.Count == 0)
        {
            return -1;
        }

        int bestColor = -1;
        int bestNeed = 0;

        foreach (var kv in remainingByColor)
        {
            int colorId = kv.Key;
            int remainingCount = kv.Value;
            int capacityCoverage = 0;

            if (knownBoxes != null)
            {
                foreach (var box in knownBoxes)
                {
                    if (box == null || !box.gameObject.activeInHierarchy || box.ColorId != colorId)
                    {
                        continue;
                    }

                    capacityCoverage += Mathf.Max(0, box.Capacity - box.CurrentLoad);
                }
            }

            int need = remainingCount - capacityCoverage;
            if (need > bestNeed)
            {
                bestNeed = need;
                bestColor = colorId;
            }
        }

        return bestNeed > 0 ? bestColor : -1;
    }
}
