using Zenject;

public class LevelEndTickable : ITickable
{
    private readonly IGridService _gridService;
    private readonly IGameStateService _gameState;

    public LevelEndTickable(IGridService gridService, IGameStateService gameState)
    {
        _gridService = gridService;
        _gameState = gameState;
    }

    public void Tick()
    {
        if (_gameState.State != GameRunState.Playing)
        {
            return;
        }

        if (_gridService != null && _gridService.AreAllProductsCollected())
        {
            _gameState.SetLevelComplete();
        }
    }
}

