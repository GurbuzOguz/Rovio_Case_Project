using UnityEngine;
using Zenject;

public class GridDebugTester : MonoBehaviour
{
    private IGridService _gridService;

    [Inject]
    public void Construct(IGridService gridService)
    {
        _gridService = gridService;
    }

    private void Start()
    {
        if (_gridService == null)
        {
            Debug.LogError("GridDebugTester: IGridService is not injected.");
            return;
        }

        Debug.Log($"Grid size: {_gridService.Columns} x {_gridService.Rows}");

        for (int y = 0; y < _gridService.Rows; y++)
        {
            for (int x = 0; x < _gridService.Columns; x++)
            {
                if (_gridService.HasProductAt(x, y))
                {
                    var colorId = _gridService.GetProductAt(x, y);
                    var worldPos = _gridService.GridToWorld(x, y);
                    Debug.Log($"Product colorId {colorId} at ({x},{y}) worldPos={worldPos}");
                }
            }
        }
    }
}

