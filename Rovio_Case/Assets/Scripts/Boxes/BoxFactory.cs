using System.Collections.Generic;
using UnityEngine;
using Zenject;

public class BoxFactory : IBoxFactory
{
    private readonly DiContainer _container;

    public BoxFactory(DiContainer container)
    {
        _container = container;
    }

    public BoxController SpawnAtSlot(
        int slotIndex,
        BoxConfig config,
        GameObject boxPrefab,
        Transform boxesParent,
        IReadOnlyList<Transform> spawnPoints,
        LevelLayout levelLayout,
        GridConfig gridConfig)
    {
        Transform spawnPoint = spawnPoints != null && slotIndex >= 0 && slotIndex < spawnPoints.Count
            ? spawnPoints[slotIndex]
            : null;
        Vector3 spawnPosition = spawnPoint != null ? spawnPoint.position : Vector3.zero;

        var boxGo = _container != null
            ? _container.InstantiatePrefab(boxPrefab, spawnPosition, Quaternion.identity, boxesParent)
            : Object.Instantiate(boxPrefab, spawnPosition, Quaternion.identity, boxesParent);
        boxGo.name = $"Box_{slotIndex}";

        var controller = boxGo.GetComponent<BoxController>();
        if (controller == null)
        {
            Object.Destroy(boxGo);
            return null;
        }

        BoxPath sharedPath = gridConfig != null ? gridConfig.boxPath : null;
        controller.Initialize(config, sharedPath, levelLayout != null ? levelLayout.productPalette : null);
        return controller;
    }
}
