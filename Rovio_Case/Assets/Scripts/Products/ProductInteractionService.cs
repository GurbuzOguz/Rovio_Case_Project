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

        // 1) View pull (varsa)
        _productViewService?.TryConsumeAndPullToBox(cell, boxTransform);

        // 2) Data shift
        var moves = _gridService.RemoveAndShift(cell, shiftDirection);

        // 3) View shift animasyonu bitince unlock
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

