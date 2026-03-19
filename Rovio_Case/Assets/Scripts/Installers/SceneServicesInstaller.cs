using UnityEngine;
using Zenject;

public class SceneServicesInstaller : MonoInstaller
{
    [Header("Scene Services")]
    [SerializeField] private ProductViewService productViewService;
    [SerializeField] private BenchSpawner benchSpawner;

    [Header("Game Flow (Optional)")]
    [SerializeField] private LevelSequenceConfig levelSequence;

    [Header("SFX")]
    [SerializeField] private SfxLibrary sfxLibrary;
    [SerializeField] private int sfxPoolSize = 10;

    [Header("Particles")]
    [SerializeField] private ParticleLibrary particleLibrary;

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

        if (!Container.HasBinding<IHapticService>())
        {
            Container.Bind<IHapticService>().To<HapticService>().AsSingle();
        }

        if (!Container.HasBinding<IParticleService>())
        {
            var go = new GameObject("ParticleService");
            var particleService = go.AddComponent<ParticleService>();
            particleService.Initialize(particleLibrary);
            Container.Bind<IParticleService>().FromInstance(particleService).AsSingle();
        }

        if (!Container.HasBinding<Zenject.ITickable>())
        {
            // Win detector: polls AreAllProductsCollected
            Container.BindInterfacesTo<LevelEndTickable>().AsSingle();
        }

        // SFX service (2D, overlapping via AudioSource pool)
        if (!Container.HasBinding<ISfxService>())
        {
            var go = new GameObject("SfxService");
            var sfxService = go.AddComponent<SfxService>();
            sfxService.Initialize(sfxLibrary, sfxPoolSize);
            Container.Bind<ISfxService>().FromInstance(sfxService).AsSingle();

            // Listen win/fail -> play end sound
            Container.Bind<LevelEndSfxListener>().AsSingle().NonLazy();
            Container.BindInterfacesTo<GameStartSfxInitializer>().AsSingle().NonLazy();
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

