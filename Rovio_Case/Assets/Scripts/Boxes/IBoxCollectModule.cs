using UnityEngine;

public interface IBoxCollectModule
{
    void ResetCollectTimer();
    bool ShouldDeactivateBecauseColorDepleted(BoxState state, IGridService gridService, BoxConfig boxConfig);
    bool TryCollectAlignedProductIfAny(
        IGridService gridService,
        LevelLayout levelLayout,
        IProductViewService productViewService,
        BoxConfig boxConfig,
        Transform boxTransform,
        bool isFull,
        out Vector3 collectedWorldPosition);
}
