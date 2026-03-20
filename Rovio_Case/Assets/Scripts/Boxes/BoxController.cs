using System;
using UnityEngine;
using Zenject;

public enum BoxState
{
    Idle,
    Moving,
    OnBench,
    Destroyed
}

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(BoxFeedbackModule))]
[RequireComponent(typeof(BoxMovementModule))]
[RequireComponent(typeof(BoxCollectModule))]
[RequireComponent(typeof(BoxBenchModule))]
public class BoxController : MonoBehaviour, IBox
{
    [SerializeField] private BoxConfig boxConfig;
    [SerializeField] private BoxPath path;

    [Header("Movement Override")]
    [SerializeField] private float defaultMoveSpeed = 12f;

    [Header("Visuals")]
    [SerializeField] private Color defaultBoxColor = Color.white;

    private IGridService _gridService;
    private LevelLayout _levelLayout;
    private ProductPalette _paletteOverride;
    private IBenchService _benchService;
    private IProductViewService _productViewService;
    private IGameStateService _gameStateService;
    private ISfxService _sfxService;
    private IHapticService _hapticService;
    private IParticleService _particleService;

    private IBoxFeedbackModule _feedbackModule;
    private IBoxMovementModule _movementModule;
    private IBoxCollectModule _collectModule;
    private IBoxBenchModule _benchModule;

    private BoxFeedbackModule _feedbackModuleComponent;
    private BoxMovementModule _movementModuleComponent;
    private BoxCollectModule _collectModuleComponent;
    private BoxBenchModule _benchModuleComponent;

    private int _currentLoad;
    private BoxState _state = BoxState.Idle;

    public BoxState State => _state;
    public event Action<BoxController> StartedMovingFromQueue;
    public event Action<BoxController> BecameInactive;

    public int ColorId => boxConfig != null ? boxConfig.colorId : -1;
    public int CurrentLoad => _currentLoad;
    public int Capacity => boxConfig != null ? boxConfig.capacity : 0;
    public bool IsFull => _currentLoad >= Capacity && Capacity > 0;
    public float MoveSpeed => boxConfig != null ? boxConfig.moveSpeed : defaultMoveSpeed;

    public void Initialize(BoxConfig config, BoxPath sharedPath, ProductPalette paletteOverride = null)
    {
        boxConfig = config;
        path = sharedPath != null ? sharedPath : path;
        _paletteOverride = paletteOverride;
        ApplyColorFromPalette();
    }

    [Inject]
    public void Construct(
        IGridService gridService,
        LevelLayout levelLayout,
        [InjectOptional] IBenchService benchService,
        [InjectOptional] IProductViewService productViewService,
        [InjectOptional] IProductInteractionService productInteractionService,
        [InjectOptional] IGameStateService gameStateService,
        [InjectOptional] ISfxService sfxService,
        [InjectOptional] IHapticService hapticService,
        [InjectOptional] IParticleService particleService)
    {
        _gridService = gridService;
        _levelLayout = levelLayout;
        _benchService = benchService;
        _productViewService = productViewService;
        _gameStateService = gameStateService;
        _sfxService = sfxService;
        _hapticService = hapticService;
        _particleService = particleService;
    }

    private void Awake()
    {
        EnsureModules();
        _feedbackModule.Initialize();

        if (boxConfig == null)
        {
            Debug.LogWarning($"BoxController on {name}: BoxConfig not assigned.");
        }
    }

    private void OnEnable()
    {
        _feedbackModule?.OnEnableModule();
    }

    private void OnDisable()
    {
        _feedbackModule?.OnDisableModule();
    }

    private void OnDestroy()
    {
        _feedbackModule?.OnDestroyModule();
        _benchModule?.ReleaseBenchSlotIfAny(_benchService);
    }

    public void OnClickedByInput()
    {
        if (_state != BoxState.Idle && _state != BoxState.OnBench)
        {
            return;
        }

        _benchModule?.ReleaseBenchSlotIfAny(_benchService);
        _feedbackModule?.PlayClickFeedback(_sfxService, _hapticService, _particleService);
        StartMove();
    }

    public void StartMove()
    {
        if (path == null || path.LocalWaypoints == null || path.LocalWaypoints.Count == 0)
        {
            Debug.LogWarning($"BoxController on {name}: Shared BoxPath is missing or empty.");
            return;
        }

        if (ShouldDeactivateBecauseColorDepleted())
        {
            DeactivateBecauseColorDepleted();
            return;
        }

        if (_state == BoxState.Idle)
        {
            StartedMovingFromQueue?.Invoke(this);
        }

        _state = BoxState.Moving;
        _collectModule?.ResetCollectTimer();
        _movementModule?.StartMove(path, MoveSpeed, TickCollect, OnPathCompleted);
    }

    private void TickCollect()
    {
        if (ShouldDeactivateBecauseColorDepleted())
        {
            DeactivateBecauseColorDepleted();
            return;
        }

        if (_collectModule == null || IsFull)
        {
            return;
        }

        if (_collectModule.TryCollectAlignedProductIfAny(
            _gridService,
            _levelLayout,
            _productViewService,
            boxConfig,
            transform,
            IsFull,
            out Vector3 cellWorld))
        {
            _feedbackModule?.PlayCollectFeedback(_sfxService, _hapticService, _particleService, cellWorld);
            _currentLoad++;

            if (ShouldDeactivateBecauseColorDepleted())
            {
                DeactivateBecauseColorDepleted();
                return;
            }

            if (IsFull)
            {
                OnBoxFull();
            }
        }
    }

    private bool ShouldDeactivateBecauseColorDepleted()
    {
        return _collectModule != null &&
               _collectModule.ShouldDeactivateBecauseColorDepleted(_state, _gridService, boxConfig);
    }

    private void DeactivateBecauseColorDepleted()
    {
        _feedbackModule?.PlayDepletedFeedback(_sfxService, _hapticService, _particleService);
        _state = BoxState.Destroyed;
        _benchModule?.ReleaseBenchSlotIfAny(_benchService);
        _movementModule?.StopMove();
        PlayDeactivateAndDisable(0.2f);
    }

    private void OnBoxFull()
    {
        _feedbackModule?.PlayBoxFullFeedback(_sfxService, _hapticService, _particleService);
        _state = BoxState.Destroyed;
        _benchModule?.ReleaseBenchSlotIfAny(_benchService);
        _movementModule?.StopMove();
        PlayDeactivateAndDisable(0.25f);
    }

    private void OnPathCompleted()
    {
        if (IsFull)
        {
            return;
        }

        if (_benchService == null)
        {
            Debug.LogWarning($"Box {name}: IBenchService not injected. Staying at end of path.");
            _state = BoxState.OnBench;
            return;
        }

        bool satOnBench = _benchModule != null && _benchModule.TrySitOnBench(
            _benchService,
            _gameStateService,
            _sfxService,
            benchPos => _feedbackModule?.PlayBenchSitFeedback(_sfxService, _particleService, benchPos));

        _state = BoxState.OnBench;
        if (!satOnBench)
        {
            return;
        }
    }

    private void PlayDeactivateAndDisable(float duration)
    {
        if (_feedbackModule != null)
        {
            _feedbackModule.PlayDeactivateScaleAnimation(duration, DeactivateBox);
        }
        else
        {
            DeactivateBox();
        }
    }

    private void DeactivateBox()
    {
        var col = GetComponent<Collider>();
        if (col != null)
        {
            col.enabled = false;
        }

        BecameInactive?.Invoke(this);
        gameObject.SetActive(false);
    }

    private void ApplyColorFromPalette()
    {
        var renderer = GetComponentInChildren<Renderer>();
        if (renderer == null)
        {
            return;
        }

        int colorId = boxConfig != null ? boxConfig.colorId : -1;
        Color color = defaultBoxColor;
        ProductPalette palette = _paletteOverride != null
            ? _paletteOverride
            : (_levelLayout != null ? _levelLayout.productPalette : null);

        if (palette != null && palette.entries != null)
        {
            for (int i = 0; i < palette.entries.Count; i++)
            {
                if (palette.entries[i].colorId == colorId)
                {
                    color = palette.entries[i].displayColor;
                    break;
                }
            }
        }

        var mpb = new MaterialPropertyBlock();
        renderer.GetPropertyBlock(mpb);

#if UNITY_2021_2_OR_NEWER
        if (renderer.sharedMaterial != null && renderer.sharedMaterial.HasProperty("_BaseColor"))
        {
            mpb.SetColor("_BaseColor", color);
        }
        else
#endif
        if (renderer.sharedMaterial != null && renderer.sharedMaterial.HasProperty("_Color"))
        {
            mpb.SetColor("_Color", color);
        }

        renderer.SetPropertyBlock(mpb);
    }

    private void Reset()
    {
        EnsureModules();
    }

    private void OnValidate()
    {
        EnsureModules();
    }

    private void EnsureModules()
    {
        _feedbackModuleComponent = GetOrAddComponent(ref _feedbackModuleComponent);
        _movementModuleComponent = GetOrAddComponent(ref _movementModuleComponent);
        _collectModuleComponent = GetOrAddComponent(ref _collectModuleComponent);
        _benchModuleComponent = GetOrAddComponent(ref _benchModuleComponent);

        _feedbackModule = _feedbackModuleComponent;
        _movementModule = _movementModuleComponent;
        _collectModule = _collectModuleComponent;
        _benchModule = _benchModuleComponent;
    }

    private T GetOrAddComponent<T>(ref T cache) where T : Component
    {
        if (cache == null)
        {
            cache = GetComponent<T>();
        }

        if (cache == null)
        {
            cache = gameObject.AddComponent<T>();
        }

        return cache;
    }
}

