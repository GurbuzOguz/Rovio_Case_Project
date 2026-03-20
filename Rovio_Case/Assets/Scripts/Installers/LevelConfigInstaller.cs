using UnityEngine;
using Zenject;

[CreateAssetMenu(fileName = "LevelConfigInstaller", menuName = "Installers/Level Config Installer")]
public class LevelConfigInstaller : ScriptableObjectInstaller<LevelConfigInstaller>
{
    [Header("Single Level (legacy)")]
    public GridConfig gridConfig;
    public LevelLayout levelLayout;

    [Header("Optional Sequence Override")]
    public LevelSequenceConfig levelSequence;

    public override void InstallBindings()
    {
        LevelLayout chosenLayout = levelLayout;

        if (levelSequence != null && levelSequence.levels != null && levelSequence.levels.Count > 0)
        {
            int idx = Mathf.Max(0, PlayerPrefs.GetInt(LevelPrefsKeys.CurrentLevelIndex, 0));
            idx = Mathf.Clamp(idx, 0, levelSequence.levels.Count - 1);
            chosenLayout = levelSequence.levels[idx];
        }

        if (chosenLayout == null)
        {
            chosenLayout = ScriptableObject.CreateInstance<LevelLayout>();
        }

        GridConfig chosenGrid = chosenLayout.gridConfig != null ? chosenLayout.gridConfig : gridConfig;
        if (chosenGrid == null)
        {
            chosenGrid = ScriptableObject.CreateInstance<GridConfig>();
        }

        Container.Bind<GridConfig>().FromInstance(chosenGrid).AsSingle();
        Container.Bind<LevelLayout>().FromInstance(chosenLayout).AsSingle();

        Container.Bind<IGridService>().To<GridService>().AsSingle();
    }
}

