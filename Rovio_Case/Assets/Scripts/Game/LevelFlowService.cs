using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelFlowService : ILevelFlowService
{
    private readonly LevelSequenceConfig _sequence;

    public int CurrentLevelIndex { get; private set; }
    public int LevelCount => _sequence != null && _sequence.levels != null ? _sequence.levels.Count : 0;
    public bool HasNextLevel => CurrentLevelIndex + 1 < LevelCount;

    public LevelFlowService(LevelSequenceConfig sequence)
    {
        _sequence = sequence;
        CurrentLevelIndex = Mathf.Max(0, PlayerPrefs.GetInt(LevelPrefsKeys.CurrentLevelIndex, 0));
        if (LevelCount > 0)
        {
            CurrentLevelIndex = Mathf.Clamp(CurrentLevelIndex, 0, LevelCount - 1);
        }
        else
        {
            CurrentLevelIndex = 0;
        }
    }

    public void RestartLevel()
    {
        ReloadActiveScene();
    }

    public void LoadFirstLevel()
    {
        PlayerPrefs.SetInt(LevelPrefsKeys.CurrentLevelIndex, 0);
        PlayerPrefs.Save();
        ReloadActiveScene();
    }

    public void LoadNextLevel()
    {
        if (LevelCount <= 0)
        {
            ReloadActiveScene();
            return;
        }

        int next = HasNextLevel ? CurrentLevelIndex + 1 : 0;
        PlayerPrefs.SetInt(LevelPrefsKeys.CurrentLevelIndex, next);
        PlayerPrefs.Save();
        ReloadActiveScene();
    }

    private static void ReloadActiveScene()
    {
        var scene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(scene.buildIndex);
    }
}

