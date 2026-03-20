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
            return;
        }

        if (boxPrefab == null)
        {
            return;
        }

        _activeBoxes.Clear();
        _runtimeGeneratedConfigs.Clear();

        int maxByConfig = _levelLayout.initialBoxConfigs.Count;
        int maxBySpawnPoints = spawnPoints.Count;
        int maxBoxes = Mathf.Min(_levelLayout.initialBoxCount, maxBySpawnPoints);

        if (maxBoxes <= 0)
        {
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
}

