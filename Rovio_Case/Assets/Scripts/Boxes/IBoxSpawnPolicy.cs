using System.Collections.Generic;

public interface IBoxSpawnPolicy
{
    int ChooseNextColorIdToSpawn(IReadOnlyDictionary<int, int> remainingByColor, IEnumerable<BoxController> knownBoxes);
}
