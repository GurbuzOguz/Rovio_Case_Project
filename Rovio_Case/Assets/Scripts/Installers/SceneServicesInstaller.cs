using UnityEngine;
using Zenject;

public class SceneServicesInstaller : MonoInstaller
{
    [Header("Scene Services")]
    [SerializeField] private ProductViewService productViewService;
    [SerializeField] private BenchSpawner benchSpawner;

    public override void InstallBindings()
    {
        if (productViewService != null)
        {
            Container.Bind<IProductViewService>().FromInstance(productViewService).AsSingle();
        }

        if (benchSpawner != null)
        {
            Container.Bind<IBenchService>().FromInstance(benchSpawner).AsSingle();
        }
    }
}

