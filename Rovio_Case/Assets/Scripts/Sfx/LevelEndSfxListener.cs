using Zenject;
using UnityEngine;

public class LevelEndSfxListener
{
    private readonly ISfxService _sfx;
    private readonly IGameStateService _gameState;
    private readonly IHapticService _haptic;
    private readonly IParticleService _particles;

    public LevelEndSfxListener(ISfxService sfx, IGameStateService gameState, IHapticService haptic, IParticleService particles)
    {
        _sfx = sfx;
        _gameState = gameState;
        _haptic = haptic;
        _particles = particles;
        _gameState.StateChanged += HandleStateChanged;
    }

    private void HandleStateChanged(GameRunState state)
    {
        if (state == GameRunState.LevelComplete)
        {
            _sfx?.Play(SfxId.LevelComplete);
            _haptic?.Success();
            PlayAtCameraCenter(ParticleId.LevelComplete);
        }
        else if (state == GameRunState.LevelFail)
        {
            _haptic?.Failure();
        }
    }

    private void PlayAtCameraCenter(ParticleId id)
    {
        if (_particles == null)
        {
            return;
        }

        var cam = Camera.main;
        if (cam == null)
        {
            return;
        }

        Vector3 pos = cam.transform.position + cam.transform.forward * 8f;
        _particles.Play(id, pos);
    }
}

