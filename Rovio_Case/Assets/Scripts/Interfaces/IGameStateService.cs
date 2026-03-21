using System;

public interface IGameStateService
{
    GameRunState State { get; }
    event Action<GameRunState> StateChanged;

    void SetPlaying();
    void SetLevelComplete();
    void SetLevelFail();
}

