using UnityEngine;
using Zenject;

public class GameFlowInstaller : MonoInstaller
{
    [Header("Configs")]
    [SerializeField] private LevelSequenceConfig levelSequence;

    public override void InstallBindings()
    {
        if (levelSequence != null)
        {
            Container.Bind<LevelSequenceConfig>().FromInstance(levelSequence).AsSingle();
        }
        else
        {
            Container.Bind<LevelSequenceConfig>().FromInstance(ScriptableObject.CreateInstance<LevelSequenceConfig>()).AsSingle();
        }

        Container.Bind<IGameStateService>().To<GameStateService>().AsSingle();
        Container.Bind<ILevelFlowService>().To<LevelFlowService>().AsSingle();

        // Win detector (poll each tick)
        Container.BindInterfacesTo<LevelEndTickable>().AsSingle();
    }
}

