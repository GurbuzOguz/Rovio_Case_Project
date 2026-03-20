using UnityEngine;

public class ProductInteractionService : IProductInteractionService
{
    private readonly IGridService _gridService;
    private readonly IProductViewService _productViewService;

    private bool _isBusy;

    public ProductInteractionService(IGridService gridService, IProductViewService productViewService)
    {
        _gridService = gridService;
        _productViewService = productViewService;
    }

    public bool TryConsumeAndShift(Vector2Int cell, Transform boxTransform, GridShiftDirection shiftDirection)
    {
        if (_isBusy)
        {
            return false;
        }

        _isBusy = true;

        // 1) Pull view if available
        _productViewService?.TryConsumeAndPullToBox(cell, boxTransform);

        // 2) Shift data
        var moves = _gridService.RemoveAndShift(cell, shiftDirection);

        // 3) Unlock after view shift animation completes
        if (_productViewService != null)
        {
            _productViewService.ApplyShiftMoves(moves, () => _isBusy = false);
        }
        else
        {
            _isBusy = false;
        }

        return true;
    }

    public bool TryFillEdgeGaps()
    {
        if (_isBusy)
        {
            return false;
        }

        _isBusy = true;

        var moves = _gridService.FillEdgeGaps();
        if (_productViewService != null)
        {
            _productViewService.ApplyShiftMoves(moves, () => _isBusy = false);
        }
        else
        {
            _isBusy = false;
        }

        return true;
    }
}

