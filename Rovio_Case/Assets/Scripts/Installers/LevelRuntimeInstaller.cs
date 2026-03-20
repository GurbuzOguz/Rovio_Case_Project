using UnityEngine;
using Zenject;

/// <summary>
/// Scene-based alternative to ScriptableObject installers.
/// Binds LevelLayout/GridConfig/IGridService for the currently selected level index.
/// </summary>
public class LevelRuntimeInstaller : MonoInstaller
{
    [Header("Sequence (optional)")]
    [SerializeField] private LevelSequenceConfig levelSequence;

    [Header("Fallback (used if sequence is null/empty)")]
    [SerializeField] private LevelLayout fallbackLevelLayout;
    [SerializeField] private GridConfig fallbackGridConfig;

    public override void InstallBindings()
    {
        if (!Container.HasBinding<LevelLayout>())
        {
            var chosenLayout = ChooseLayout();
            Container.Bind<LevelLayout>().FromInstance(chosenLayout).AsSingle();

            var chosenGrid = chosenLayout != null && chosenLayout.gridConfig != null ? chosenLayout.gridConfig : fallbackGridConfig;
            if (chosenGrid == null)
            {
                chosenGrid = ScriptableObject.CreateInstance<GridConfig>();
            }
            Container.Bind<GridConfig>().FromInstance(chosenGrid).AsSingle();
        }

        if (!Container.HasBinding<IGridService>())
        {
            Container.Bind<IGridService>().To<GridService>().AsSingle();
        }
    }

    private LevelLayout ChooseLayout()
    {
        if (levelSequence != null && levelSequence.levels != null && levelSequence.levels.Count > 0)
        {
            int idx = Mathf.Max(0, PlayerPrefs.GetInt(LevelPrefsKeys.CurrentLevelIndex, 0));
            idx = Mathf.Clamp(idx, 0, levelSequence.levels.Count - 1);
            var layout = levelSequence.levels[idx];
            if (layout != null)
            {
                return layout;
            }
        }

        if (fallbackLevelLayout != null)
        {
            return fallbackLevelLayout;
        }

        return ScriptableObject.CreateInstance<LevelLayout>();
    }
}

