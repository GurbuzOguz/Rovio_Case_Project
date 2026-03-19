using Zenject;

public class LevelEndSfxListener
{
    private readonly ISfxService _sfx;
    private readonly IGameStateService _gameState;

    public LevelEndSfxListener(ISfxService sfx, IGameStateService gameState)
    {
        _sfx = sfx;
        _gameState = gameState;
        _gameState.StateChanged += HandleStateChanged;
    }

    private void HandleStateChanged(GameRunState state)
    {
        if (state == GameRunState.LevelComplete)
        {
            _sfx?.Play(SfxId.LevelComplete);
        }
        else if (state == GameRunState.LevelFail)
        {
            _sfx?.Play(SfxId.LevelFail);
        }
    }
}

