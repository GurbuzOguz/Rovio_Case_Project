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
            return;
        }
    }
}

