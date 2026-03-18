using UnityEngine;
using Zenject;

public class SceneServicesInstaller : MonoInstaller
{
    [Header("Scene Services")]
    [SerializeField] private ProductViewService productViewService;
    [SerializeField] private BenchSpawner benchSpawner;

    [Header("Game Flow (Optional)")]
    [SerializeField] private LevelSequenceConfig levelSequence;

    public override void InstallBindings()
    {
        // Game state / flow (bind once)
        if (!Container.HasBinding<IGameStateService>())
        {
            Container.Bind<IGameStateService>().To<GameStateService>().AsSingle();
        }

        if (levelSequence != null && !Container.HasBinding<LevelSequenceConfig>())
        {
            Container.Bind<LevelSequenceConfig>().FromInstance(levelSequence).AsSingle();
        }

        if (!Container.HasBinding<ILevelFlowService>())
        {
            // Needs LevelSequenceConfig; if not bound, LevelFlowService will still work (LevelCount=0)
            Container.Bind<ILevelFlowService>().To<LevelFlowService>().AsSingle();
        }

        if (!Container.HasBinding<Zenject.ITickable>())
        {
            // Win detector: polls AreAllProductsCollected
            Container.BindInterfacesTo<LevelEndTickable>().AsSingle();
        }

        if (productViewService != null)
        {
            Container.Bind<IProductViewService>().FromInstance(productViewService).AsSingle();
        }

        if (benchSpawner != null)
        {
            Container.Bind<IBenchService>().FromInstance(benchSpawner).AsSingle();
        }

        // Requires IGridService + IProductViewService
        Container.Bind<IProductInteractionService>().To<ProductInteractionService>().AsSingle();
    }
}

