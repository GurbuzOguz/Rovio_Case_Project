using System.Collections.Generic;
using UnityEngine;
using Zenject;

public class BoxSpawner : MonoBehaviour
{
    [SerializeField] private GameObject boxPrefab;
    [SerializeField] private Transform boxesParent;
    [SerializeField] private List<Transform> spawnPoints = new List<Transform>();
    [SerializeField] private int defaultBoxCapacity = 3;
    [SerializeField] private float defaultBoxMoveSpeed = 3f;
    [SerializeField] private float queueShiftDuration = 0.2f;
    [Tooltip("Kuyruk slotları kaçarlı satır? Örn 3 => 3x3 düzen. spawnPoints sırası: önce ön satır (0..rowSize-1), sonra orta, sonra arka.")]
    [SerializeField] private int queueRowSize = 3;

    private DiContainer _container;
    private LevelLayout _levelLayout;
    private GridConfig _gridConfig;
    private IGridService _gridService;
    private ISfxService _sfxService;
    private readonly List<BoxController> _activeBoxes = new List<BoxController>();
    private readonly HashSet<BoxController> _knownBoxes = new HashSet<BoxController>();
    private readonly List<BoxConfig> _runtimeGeneratedConfigs = new List<BoxConfig>();
    private IBoxFactory _boxFactory;
    private IBoxQueueService _queueService;
    private IBoxSpawnPolicy _spawnPolicy;

    public IReadOnlyList<BoxController> ActiveBoxes => _activeBoxes;

    [Inject]
    public void Construct(LevelLayout levelLayout, GridConfig gridConfig, IGridService gridService, DiContainer container, ISfxService sfxService)
    {
        _levelLayout = levelLayout;
        _gridConfig = gridConfig;
        _gridService = gridService;
        _container = container;
        _sfxService = sfxService;
    }

    private void Awake()
    {
        _boxFactory = new BoxFactory(_container);
        _queueService = new BoxQueueService();
        _spawnPolicy = new NeedBasedBoxSpawnPolicy();

        if (boxesParent == null)
        {
            var parentGo = new GameObject("Boxes");
            parentGo.transform.SetParent(transform, false);
            boxesParent = parentGo.transform;
        }
    }

    private void Start()
    {
        SpawnInitialBoxes();
        _sfxService?.Play(SfxId.ProductSpawn);
    }

    private void SpawnInitialBoxes()
    {
        if (_levelLayout == null)
        {
            Debug.LogError("BoxSpawner: LevelLayout not injected.");
            return;
        }

        if (boxPrefab == null)
        {
            Debug.LogError("BoxSpawner: Box prefab not assigned.");
            return;
        }

        _activeBoxes.Clear();
        _runtimeGeneratedConfigs.Clear();

        int maxByConfig = _levelLayout.initialBoxConfigs.Count;
        int maxBySpawnPoints = spawnPoints.Count;
        int maxBoxes = Mathf.Min(_levelLayout.initialBoxCount, maxBySpawnPoints);

        Debug.Log(
            $"BoxSpawner: initialBoxCount={_levelLayout.initialBoxCount}, " +
            $"initialBoxConfigs={maxByConfig}, spawnPoints={maxBySpawnPoints} " +
            $"=> spawning {maxBoxes} boxes.");

        if (maxBoxes <= 0)
        {
            Debug.LogWarning("BoxSpawner: maxBoxes is 0. Check LevelLayout.initialBoxConfigs and BoxSpawner.spawnPoints.");
            return;
        }

        for (int i = 0; i < maxBoxes; i++)
        {
            SpawnBoxAtSlot(i, GetOrCreateConfigForIndex(i, _levelLayout));
        }
    }

    private void SpawnBoxAtSlot(int slotIndex, BoxConfig config)
    {
        var controller = _boxFactory != null
            ? _boxFactory.SpawnAtSlot(slotIndex, config, boxPrefab, boxesParent, spawnPoints, _levelLayout, _gridConfig)
            : null;
        if (controller == null)
        {
            Debug.LogError("BoxSpawner: Box prefab does not have a BoxController component.");
            return;
        }

        EnsureActiveBoxesSize();
        _activeBoxes[slotIndex] = controller;
        _knownBoxes.Add(controller);

        controller.StartedMovingFromQueue += HandleBoxStartedMovingFromQueue;
        controller.BecameInactive += HandleBoxBecameInactive;
    }

    private void EnsureActiveBoxesSize()
    {
        // queue slot sayısı kadar listeyi büyüt
        while (_activeBoxes.Count < spawnPoints.Count)
        {
            _activeBoxes.Add(null);
        }
    }

    private void HandleBoxStartedMovingFromQueue(BoxController box)
    {
        int slotIndex = _activeBoxes.IndexOf(box);
        if (slotIndex < 0)
        {
            return;
        }

        // Slot'u boşalt (box path'e çıktı)
        _activeBoxes[slotIndex] = null;

        // Arkadakiler öne kay (3'erli satır düzeninde aynı sütun)
        _queueService?.ShiftQueueForwardInColumn(_activeBoxes, spawnPoints, queueRowSize, queueShiftDuration, slotIndex);

        // En arkaya yeni kutu (gerekliyse)
        TrySpawnBoxAtBackIfNeeded(slotIndex);
    }

    private void HandleBoxBecameInactive(BoxController box)
    {
        // Deactivate olan box listede duruyorsa temizle
        int idx = _activeBoxes.IndexOf(box);
        if (idx >= 0)
        {
            _activeBoxes[idx] = null;
        }
    }

    private void TrySpawnBoxAtBackIfNeeded(int fromIndex)
    {
        if (spawnPoints.Count == 0)
        {
            return;
        }

        int backIndex = _queueService != null
            ? _queueService.GetBackIndex(spawnPoints, queueRowSize, fromIndex)
            : -1;
        if (backIndex < 0)
        {
            return;
        }

        EnsureActiveBoxesSize();

        if (_activeBoxes[backIndex] != null)
        {
            return;
        }

        // Grid'de product kalmadıysa spawn yok
        if (_gridService == null)
        {
            return;
        }

        var remaining = _gridService.GetRemainingCountsByColorId();
        if (remaining == null || remaining.Count == 0)
        {
            return;
        }

        int chosenColorId = _spawnPolicy != null
            ? _spawnPolicy.ChooseNextColorIdToSpawn(remaining, _knownBoxes)
            : -1;
        if (chosenColorId < 0)
        {
            return;
        }

        var cfg = ScriptableObject.CreateInstance<BoxConfig>();
        cfg.colorId = chosenColorId;
        cfg.capacity = Mathf.Max(1, defaultBoxCapacity);
        cfg.moveSpeed = Mathf.Max(0.1f, defaultBoxMoveSpeed);
        cfg.name = $"RuntimeBoxConfig_{chosenColorId}_{System.Guid.NewGuid()}";
        _runtimeGeneratedConfigs.Add(cfg);

        SpawnBoxAtSlot(backIndex, cfg);
    }

    private BoxConfig GetOrCreateConfigForIndex(int index, LevelLayout levelLayout)
    {
        if (levelLayout != null &&
            levelLayout.initialBoxConfigs != null &&
            index < levelLayout.initialBoxConfigs.Count &&
            levelLayout.initialBoxConfigs[index] != null)
        {
            return levelLayout.initialBoxConfigs[index];
        }

        // Auto-generate: palette içindeki renklerden sırayla runtime BoxConfig üret
        int colorId = GetPaletteColorIdForIndex(index, levelLayout);

        var config = ScriptableObject.CreateInstance<BoxConfig>();
        config.colorId = colorId;
        config.capacity = Mathf.Max(1, defaultBoxCapacity);
        config.moveSpeed = Mathf.Max(0.1f, defaultBoxMoveSpeed);
        config.name = $"RuntimeBoxConfig_{colorId}_{index}";

        _runtimeGeneratedConfigs.Add(config);
        return config;
    }

    private int GetPaletteColorIdForIndex(int index, LevelLayout levelLayout)
    {
        var palette = levelLayout != null ? levelLayout.productPalette : null;
        if (palette == null || palette.entries == null || palette.entries.Count == 0)
        {
            // Palette yoksa 0 kullan (renk bulunamazsa BoxController defaultBoxColor'a düşer)
            return 0;
        }

        int i = index % palette.entries.Count;
        return palette.entries[i].colorId;
    }
}

