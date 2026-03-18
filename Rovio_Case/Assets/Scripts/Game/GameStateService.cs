using System;

public class GameStateService : IGameStateService
{
    public GameRunState State { get; private set; } = GameRunState.Playing;
    public event Action<GameRunState> StateChanged;

    public void SetPlaying()
    {
        SetState(GameRunState.Playing);
    }

    public void SetLevelComplete()
    {
        SetState(GameRunState.LevelComplete);
    }

    public void SetLevelFail()
    {
        SetState(GameRunState.LevelFail);
    }

    private void SetState(GameRunState newState)
    {
        if (State == newState)
        {
            return;
        }

        State = newState;
        StateChanged?.Invoke(State);
    }
}

