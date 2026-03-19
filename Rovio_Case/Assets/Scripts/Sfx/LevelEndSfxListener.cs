using Zenject;

public class LevelEndSfxListener
{
    private readonly ISfxService _sfx;
    private readonly IGameStateService _gameState;
    private readonly IHapticService _haptic;

    public LevelEndSfxListener(ISfxService sfx, IGameStateService gameState, IHapticService haptic)
    {
        _sfx = sfx;
        _gameState = gameState;
        _haptic = haptic;
        _gameState.StateChanged += HandleStateChanged;
    }

    private void HandleStateChanged(GameRunState state)
    {
        if (state == GameRunState.LevelComplete)
        {
            _sfx?.Play(SfxId.LevelComplete);
            _haptic?.Success();
        }
        else if (state == GameRunState.LevelFail)
        {
            _sfx?.Play(SfxId.LevelFail);
            _haptic?.Failure();
        }
    }
}

