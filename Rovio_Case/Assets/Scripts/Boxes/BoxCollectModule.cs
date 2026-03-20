using UnityEngine;

[DisallowMultipleComponent]
public class BoxCollectModule : MonoBehaviour, IBoxCollectModule
{
    [Header("Collect (Align)")]
    [SerializeField] private float alignTolerance = 0.2f;
    [SerializeField] private float collectInterval = 0.01f;
    [SerializeField] private float pullMaxDistance = 3.0f;
    [SerializeField] private bool onlyPullFromEdgeZones = true;
    [SerializeField] private float cornerOutsideMargin = 0.3f;

    private float _collectTimer;

    public void ResetCollectTimer()
    {
        _collectTimer = 0f;
    }

    public bool ShouldDeactivateBecauseColorDepleted(BoxState state, IGridService gridService, BoxConfig boxConfig)
    {
        if (state == BoxState.Destroyed || gridService == null || boxConfig == null)
        {
            return false;
        }

        var counts = gridService.GetRemainingCountsByColorId();
        if (counts == null)
        {
            return false;
        }

        return !counts.TryGetValue(boxConfig.colorId, out int remaining) || remaining <= 0;
    }

    public bool TryCollectAlignedProductIfAny(
        IGridService gridService,
        LevelLayout levelLayout,
        IProductViewService productViewService,
        BoxConfig boxConfig,
        Transform boxTransform,
        bool isFull,
        out Vector3 collectedWorldPosition)
    {
        collectedWorldPosition = default;

        if (gridService == null || boxConfig == null || boxTransform == null || isFull)
        {
            return false;
        }

        _collectTimer += Time.deltaTime;
        if (_collectTimer < collectInterval)
        {
            return false;
        }
        _collectTimer = 0f;

        Vector3 pos = boxTransform.position;
        if (onlyPullFromEdgeZones && !IsInEdgePullZone(levelLayout, pos))
        {
            return false;
        }

        if (!gridService.TryFindAlignedProductCell(pos, alignTolerance, boxConfig.colorId, out var cell))
        {
            return false;
        }

        Vector3 cellWorld = gridService.GridToWorld(cell.x, cell.y);
        float dist = Vector2.Distance(new Vector2(pos.x, pos.z), new Vector2(cellWorld.x, cellWorld.z));
        if (dist > pullMaxDistance)
        {
            return false;
        }

        var shiftDir = DetermineShiftDirection(levelLayout, pos);
        productViewService?.TryConsumeAndPullToBox(cell, boxTransform);
        var moves = gridService.RemoveAndShift(cell, shiftDir);
        productViewService?.ApplyShiftMoves(moves);
        collectedWorldPosition = cellWorld;
        return true;
    }

    private bool IsInEdgePullZone(LevelLayout levelLayout, Vector3 worldPos)
    {
        if (levelLayout == null || levelLayout.gridConfig == null)
        {
            return true;
        }

        var gc = levelLayout.gridConfig;
        float minX = gc.origin.x;
        float maxX = gc.origin.x + (gc.columns - 1) * gc.cellSize;
        float minZ = gc.origin.z;
        float maxZ = gc.origin.z + (gc.rows - 1) * gc.cellSize;

        bool xOutside = worldPos.x < (minX - cornerOutsideMargin) || worldPos.x > (maxX + cornerOutsideMargin);
        bool zOutside = worldPos.z < (minZ - cornerOutsideMargin) || worldPos.z > (maxZ + cornerOutsideMargin);
        return xOutside || zOutside;
    }

    private GridShiftDirection DetermineShiftDirection(LevelLayout levelLayout, Vector3 worldPos)
    {
        var gc = levelLayout != null ? levelLayout.gridConfig : null;
        if (gc == null)
        {
            return GridShiftDirection.Left;
        }

        float minX = gc.origin.x;
        float maxX = gc.origin.x + (gc.columns - 1) * gc.cellSize;
        float minZ = gc.origin.z;
        float maxZ = gc.origin.z + (gc.rows - 1) * gc.cellSize;

        float leftDist = (minX - worldPos.x);
        float rightDist = (worldPos.x - maxX);
        float downDist = (minZ - worldPos.z);
        float upDist = (worldPos.z - maxZ);

        float best = float.NegativeInfinity;
        GridShiftDirection dir = GridShiftDirection.Left;

        if (leftDist > best)
        {
            best = leftDist;
            dir = GridShiftDirection.Left;
        }
        if (rightDist > best)
        {
            best = rightDist;
            dir = GridShiftDirection.Right;
        }
        if (downDist > best)
        {
            best = downDist;
            dir = GridShiftDirection.Down;
        }
        if (upDist > best)
        {
            best = upDist;
            dir = GridShiftDirection.Up;
        }

        return dir;
    }
}
