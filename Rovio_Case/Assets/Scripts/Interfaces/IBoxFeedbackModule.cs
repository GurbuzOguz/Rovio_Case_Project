using System;
using UnityEngine;

public interface IBoxFeedbackModule
{
    void Initialize();
    void OnEnableModule();
    void OnDisableModule();
    void OnDestroyModule();
    void PlayClickFeedback(ISfxService sfxService, IHapticService hapticService, IParticleService particleService);
    void PlayCollectFeedback(ISfxService sfxService, IHapticService hapticService, IParticleService particleService, Vector3 worldPosition);
    void PlayBoxFullFeedback(ISfxService sfxService, IHapticService hapticService, IParticleService particleService);
    void PlayDepletedFeedback(ISfxService sfxService, IHapticService hapticService, IParticleService particleService);
    void PlayBenchSitFeedback(ISfxService sfxService, IParticleService particleService, Vector3 benchPosition);
    void PlayDeactivateScaleAnimation(float duration, Action onComplete);
}
