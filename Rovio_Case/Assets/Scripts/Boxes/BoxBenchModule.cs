using System;
using UnityEngine;
#if DOTWEEN_EXISTS || true
using DG.Tweening;
#endif

[DisallowMultipleComponent]
public class BoxBenchModule : MonoBehaviour, IBoxBenchModule
{
    private Transform _reservedBenchSlot;

    public void ReleaseBenchSlotIfAny(IBenchService benchService)
    {
        if (_reservedBenchSlot == null)
        {
            return;
        }

        benchService?.ReleaseSlot(_reservedBenchSlot);
        _reservedBenchSlot = null;
    }

    public bool TrySitOnBench(
        IBenchService benchService,
        IGameStateService gameStateService,
        ISfxService sfxService,
        Action<Vector3> onBenchArrived)
    {
        if (benchService == null)
        {
            return false;
        }

        if (!benchService.TryReserveSlot(out _reservedBenchSlot) || _reservedBenchSlot == null)
        {
            sfxService?.Play(SfxId.LevelFail);
            gameStateService?.SetLevelFail();
            return false;
        }

        Vector3 benchPos = _reservedBenchSlot.position;
#if DOTWEEN_EXISTS || true
        transform
            .DOMove(benchPos, 0.25f)
            .SetEase(Ease.OutQuad)
            .SetLink(gameObject, LinkBehaviour.KillOnDisable)
            .OnComplete(() => onBenchArrived?.Invoke(benchPos));
#else
        transform.position = benchPos;
        onBenchArrived?.Invoke(benchPos);
#endif
        return true;
    }
}
