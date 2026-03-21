using UnityEngine;

public interface IBoxFactory
{
    BoxController SpawnAtSlot(
        int slotIndex,
        BoxConfig config,
        GameObject boxPrefab,
        Transform boxesParent,
        System.Collections.Generic.IReadOnlyList<Transform> spawnPoints,
        LevelLayout levelLayout,
        GridConfig gridConfig);
}
