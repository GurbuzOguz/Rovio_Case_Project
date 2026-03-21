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
    [Tooltip("How many slots per queue row? E.g. 3 => 3x3 layout. spawnPoints order: front row first, then middle, then back.")]
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
    private int _customSpawnOrderCursor;

    public IReadOnlyList<BoxController> ActiveBoxes => _activeBoxes;

    public bool IsFrontRowBox(BoxController box)
    {
        if (box == null)
        {
            return false;
        }

        EnsureActiveBoxesSize();
        int idx = _activeBoxes.IndexOf(box);
        if (idx < 0)
        {
            return false;
        }

        int frontRowCount = Mathf.Max(1, queueRowSize);
        return idx < frontRowCount;
    }

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
            return;
        }

        if (boxPrefab == null)
        {
            return;
        }

        _activeBoxes.Clear();
        _runtimeGeneratedConfigs.Clear();
        _customSpawnOrderCursor = 0;

        int maxByConfig = _levelLayout.initialBoxConfigs.Count;
        int maxBySpawnPoints = spawnPoints.Count;
        int maxBoxes = Mathf.Min(_levelLayout.initialBoxCount, maxBySpawnPoints);

        if (maxBoxes <= 0)
        {
            return;
        }

        var remaining = _gridService != null
            ? _gridService.GetRemainingCountsByColorId()
            : null;
        var plannedCoverageByColor = new Dictionary<int, int>();

        int nextSlotIndex = 0;
        for (int i = 0; i < maxBoxes; i++)
        {
            var baseConfig = GetOrCreateConfigForIndex(i, _levelLayout);
            int chosenColorId = ChooseNextInitialColorId(remaining, plannedCoverageByColor, i);
            if (chosenColorId < 0)
            {
                break;
            }

            var spawnConfig = BuildInitialSpawnConfig(chosenColorId, baseConfig, remaining, plannedCoverageByColor, i);
            if (spawnConfig == null)
            {
                continue;
            }

            if (nextSlotIndex >= spawnPoints.Count)
            {
                break;
            }

            // Keep the queue compact by filling slots from front to back.
            SpawnBoxAtSlot(nextSlotIndex, spawnConfig);
            nextSlotIndex++;
        }

    }

    private void SpawnBoxAtSlot(int slotIndex, BoxConfig config)
    {
        var controller = _boxFactory != null
            ? _boxFactory.SpawnAtSlot(slotIndex, config, boxPrefab, boxesParent, spawnPoints, _levelLayout, _gridConfig)
            : null;
        if (controller == null)
        {
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
        // Expand list to match queue slot count
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

        // Free the slot (box left queue and entered path)
        _activeBoxes[slotIndex] = null;

        // Shift boxes forward in the same column
        _queueService?.ShiftQueueForwardInColumn(_activeBoxes, spawnPoints, queueRowSize, queueShiftDuration, slotIndex);

        // Spawn a new box at the back when needed
        TrySpawnBoxAtBackIfNeeded(slotIndex);
    }

    private void HandleBoxBecameInactive(BoxController box)
    {
        // Clean list entry if a box became inactive
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

        // No spawn if there are no products left
        if (_gridService == null)
        {
            return;
        }

        var remaining = _gridService.GetRemainingCountsByColorId();
        if (remaining == null || remaining.Count == 0)
        {
            return;
        }

        int chosenColorId = ChooseNextRuntimeColorId(remaining);
        if (chosenColorId < 0)
        {
            return;
        }

        var cfg = ScriptableObject.CreateInstance<BoxConfig>();
        cfg.colorId = chosenColorId;
        int remainingForChosenColor = GetUncoveredNeedForColor(chosenColorId, remaining);
        int maxCapacity = Mathf.Max(1, defaultBoxCapacity);
        cfg.capacity = Mathf.Clamp(remainingForChosenColor, 1, maxCapacity);
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

        // Auto-generate runtime BoxConfig using palette colors in order
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
        // Priority: colors that actually exist on the grid.
        // Prevents spawning boxes that would instantly deactivate on click.
        if (_gridService != null)
        {
            var remaining = _gridService.GetRemainingCountsByColorId();
            if (remaining != null && remaining.Count > 0)
            {
                var activeColorIds = new List<int>(remaining.Keys);
                activeColorIds.Sort();
                int activeIdx = Mathf.Abs(index) % activeColorIds.Count;
                return activeColorIds[activeIdx];
            }
        }

        var palette = levelLayout != null ? levelLayout.productPalette : null;
        if (palette == null || palette.entries == null || palette.entries.Count == 0)
        {
            // Fallback to color 0 when no palette is available
            return 0;
        }

        int i = index % palette.entries.Count;
        return palette.entries[i].colorId;
    }

    private BoxConfig BuildInitialSpawnConfig(
        int chosenColorId,
        BoxConfig baseConfig,
        IReadOnlyDictionary<int, int> remainingByColor,
        Dictionary<int, int> plannedCoverageByColor,
        int slotIndex)
    {
        if (baseConfig == null)
        {
            return null;
        }

        int colorId = chosenColorId;
        int baseCapacity = Mathf.Max(1, baseConfig.capacity);
        int uncoveredNeed = GetUncoveredNeedForInitialPlan(colorId, remainingByColor, plannedCoverageByColor);
        if (uncoveredNeed <= 0)
        {
            return null;
        }

        int capacity = Mathf.Min(baseCapacity, uncoveredNeed);
        plannedCoverageByColor[colorId] = plannedCoverageByColor.TryGetValue(colorId, out int covered)
            ? covered + capacity
            : capacity;

        var cfg = ScriptableObject.CreateInstance<BoxConfig>();
        cfg.colorId = colorId;
        cfg.capacity = capacity;
        cfg.moveSpeed = Mathf.Max(0.1f, baseConfig.moveSpeed > 0f ? baseConfig.moveSpeed : defaultBoxMoveSpeed);
        cfg.name = $"InitialRuntimeBoxConfig_{colorId}_{slotIndex}";
        _runtimeGeneratedConfigs.Add(cfg);
        return cfg;
    }

    private static int ChooseNextInitialColorIdByNeed(
        IReadOnlyDictionary<int, int> remainingByColor,
        Dictionary<int, int> plannedCoverageByColor,
        int seed)
    {
        if (remainingByColor == null || remainingByColor.Count == 0)
        {
            return -1;
        }

        int bestNeed = 0;
        var candidates = new List<int>();

        foreach (var kv in remainingByColor)
        {
            int colorId = kv.Key;
            int planned = plannedCoverageByColor != null && plannedCoverageByColor.TryGetValue(colorId, out int covered)
                ? covered
                : 0;
            int need = Mathf.Max(0, kv.Value - planned);
            if (need <= 0)
            {
                continue;
            }

            if (need > bestNeed)
            {
                bestNeed = need;
                candidates.Clear();
                candidates.Add(colorId);
            }
            else if (need == bestNeed)
            {
                candidates.Add(colorId);
            }
        }

        if (candidates.Count == 0)
        {
            return -1;
        }

        candidates.Sort();
        int idx = Mathf.Abs(seed) % candidates.Count;
        return candidates[idx];
    }

    private int ChooseNextInitialColorId(
        IReadOnlyDictionary<int, int> remainingByColor,
        Dictionary<int, int> plannedCoverageByColor,
        int seed)
    {
        bool hasCustomOrder = _levelLayout != null &&
                              _levelLayout.customBoxSpawnColorOrder != null &&
                              _levelLayout.customBoxSpawnColorOrder.Count > 0;

        if (hasCustomOrder)
        {
            // Custom order is authoritative when provided.
            return ChooseFromCustomOrderForInitial(remainingByColor, plannedCoverageByColor);
        }

        return ChooseNextInitialColorIdByNeed(remainingByColor, plannedCoverageByColor, seed);
    }

    private int ChooseNextRuntimeColorId(IReadOnlyDictionary<int, int> remainingByColor)
    {
        bool hasCustomOrder = _levelLayout != null &&
                              _levelLayout.customBoxSpawnColorOrder != null &&
                              _levelLayout.customBoxSpawnColorOrder.Count > 0;

        if (hasCustomOrder)
        {
            // Custom order is authoritative when provided.
            return ChooseFromCustomOrderForRuntime(remainingByColor);
        }

        return _spawnPolicy != null
            ? _spawnPolicy.ChooseNextColorIdToSpawn(remainingByColor, _knownBoxes)
            : -1;
    }

    private int ChooseFromCustomOrderForInitial(
        IReadOnlyDictionary<int, int> remainingByColor,
        Dictionary<int, int> plannedCoverageByColor)
    {
        var order = _levelLayout != null ? _levelLayout.customBoxSpawnColorOrder : null;
        if (order == null || order.Count == 0)
        {
            return -1;
        }

        int attempts = order.Count;
        for (int i = 0; i < attempts; i++)
        {
            int idx = (_customSpawnOrderCursor + i) % order.Count;
            int colorId = order[idx];
            if (GetUncoveredNeedForInitialPlan(colorId, remainingByColor, plannedCoverageByColor) > 0)
            {
                _customSpawnOrderCursor = (idx + 1) % order.Count;
                return colorId;
            }
        }

        return -1;
    }

    private int ChooseFromCustomOrderForRuntime(IReadOnlyDictionary<int, int> remainingByColor)
    {
        var order = _levelLayout != null ? _levelLayout.customBoxSpawnColorOrder : null;
        if (order == null || order.Count == 0)
        {
            return -1;
        }

        int attempts = order.Count;
        for (int i = 0; i < attempts; i++)
        {
            int idx = (_customSpawnOrderCursor + i) % order.Count;
            int colorId = order[idx];
            if (GetUncoveredNeedForColor(colorId, remainingByColor) > 0)
            {
                _customSpawnOrderCursor = (idx + 1) % order.Count;
                return colorId;
            }
        }

        return -1;
    }

    private static int GetUncoveredNeedForInitialPlan(
        int colorId,
        IReadOnlyDictionary<int, int> remainingByColor,
        Dictionary<int, int> plannedCoverageByColor)
    {
        if (remainingByColor == null || !remainingByColor.TryGetValue(colorId, out int remainingCount))
        {
            return 0;
        }

        int planned = plannedCoverageByColor != null && plannedCoverageByColor.TryGetValue(colorId, out int covered)
            ? covered
            : 0;

        return Mathf.Max(0, remainingCount - planned);
    }

    private int GetUncoveredNeedForColor(int colorId, IReadOnlyDictionary<int, int> remainingByColor)
    {
        if (remainingByColor == null || !remainingByColor.TryGetValue(colorId, out int remainingCount))
        {
            return 0;
        }

        int capacityCoverage = 0;
        foreach (var box in _knownBoxes)
        {
            if (box == null || !box.gameObject.activeInHierarchy || box.ColorId != colorId)
            {
                continue;
            }

            capacityCoverage += Mathf.Max(0, box.Capacity - box.CurrentLoad);
        }

        return Mathf.Max(0, remainingCount - capacityCoverage);
    }
}

