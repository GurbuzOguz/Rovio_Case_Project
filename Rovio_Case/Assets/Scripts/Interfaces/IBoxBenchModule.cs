using System;
using UnityEngine;

public interface IBoxBenchModule
{
    void ReleaseBenchSlotIfAny(IBenchService benchService);
    bool TrySitOnBench(
        IBenchService benchService,
        IGameStateService gameStateService,
        ISfxService sfxService,
        Action<Vector3> onBenchArrived);
}
