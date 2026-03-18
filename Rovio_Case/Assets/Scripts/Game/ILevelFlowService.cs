public interface ILevelFlowService
{
    int CurrentLevelIndex { get; }
    int LevelCount { get; }
    bool HasNextLevel { get; }

    void RestartLevel();
    void LoadNextLevel();
}

