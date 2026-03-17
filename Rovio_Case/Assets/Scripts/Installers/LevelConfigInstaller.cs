using UnityEngine;
using Zenject;

[CreateAssetMenu(fileName = "LevelConfigInstaller", menuName = "Installers/Level Config Installer")]
public class LevelConfigInstaller : ScriptableObjectInstaller<LevelConfigInstaller>
{
    public GridConfig gridConfig;
    public LevelLayout levelLayout;

    public override void InstallBindings()
    {
        Container.Bind<GridConfig>().FromInstance(gridConfig).AsSingle();
        Container.Bind<LevelLayout>().FromInstance(levelLayout).AsSingle();

        Container.Bind<IGridService>().To<GridService>().AsSingle();
    }
}

