using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Zenject;
#if DOTWEEN_EXISTS || true
using DG.Tweening;
#endif

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
    private readonly List<BoxConfig> _runtimeGeneratedConfigs = new List<BoxConfig>();

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
        Transform spawnPoint = spawnPoints[slotIndex];
        Vector3 spawnPosition = spawnPoint != null ? spawnPoint.position : Vector3.zero;

        var boxGo = _container != null
            ? _container.InstantiatePrefab(boxPrefab, spawnPosition, Quaternion.identity, boxesParent)
            : Object.Instantiate(boxPrefab, spawnPosition, Quaternion.identity, boxesParent);
        boxGo.name = $"Box_{slotIndex}";

        var controller = boxGo.GetComponent<BoxController>();
        if (controller == null)
        {
            Debug.LogError("BoxSpawner: Box prefab does not have a BoxController component.");
            Destroy(boxGo);
            return;
        }

        BoxPath sharedPath = _gridConfig != null ? _gridConfig.boxPath : null;
        controller.Initialize(config, sharedPath, _levelLayout != null ? _levelLayout.productPalette : null);

        EnsureActiveBoxesSize();
        _activeBoxes[slotIndex] = controller;

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
        ShiftQueueForwardInColumn(slotIndex);

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

    private void ShiftQueueForwardInColumn(int fromIndex)
    {
        EnsureActiveBoxesSize();

        if (queueRowSize <= 0)
        {
            queueRowSize = 1;
        }

        int totalSlots = spawnPoints.Count;
        int rowCount = Mathf.CeilToInt(totalSlots / (float)queueRowSize);

        int col = fromIndex % queueRowSize;
        int row = fromIndex / queueRowSize;

        // row0 = ön, rowCount-1 = arka; sadece arkadakileri öne kaydır
        for (int r = row + 1; r < rowCount; r++)
        {
            int srcIndex = r * queueRowSize + col;
            int dstIndex = (r - 1) * queueRowSize + col;

            if (srcIndex >= totalSlots || dstIndex >= totalSlots)
            {
                continue;
            }

            var b = _activeBoxes[srcIndex];
            if (b == null)
            {
                continue;
            }

            if (b.State != BoxState.Idle)
            {
                continue;
            }

            Transform targetSlot = spawnPoints[dstIndex];
            if (targetSlot == null)
            {
                continue;
            }

#if DOTWEEN_EXISTS || true
            b.transform
                .DOMove(targetSlot.position, queueShiftDuration)
                .SetEase(Ease.OutQuad)
                .SetLink(b.gameObject, LinkBehaviour.KillOnDisable);
#else
            b.transform.position = targetSlot.position;
#endif

            _activeBoxes[dstIndex] = b;
            _activeBoxes[srcIndex] = null;
        }
    }

    private void TrySpawnBoxAtBackIfNeeded(int fromIndex)
    {
        if (spawnPoints.Count == 0)
        {
            return;
        }

        if (queueRowSize <= 0)
        {
            queueRowSize = 1;
        }

        int totalSlots = spawnPoints.Count;
        int rowCount = Mathf.CeilToInt(totalSlots / (float)queueRowSize);
        int col = fromIndex % queueRowSize;
        int backIndex = (rowCount - 1) * queueRowSize + col;
        if (backIndex < 0 || backIndex >= totalSlots)
        {
            // fallback: son slot
            backIndex = totalSlots - 1;
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

        int chosenColorId = ChooseNextColorIdToSpawn(remaining);
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

    private int ChooseNextColorIdToSpawn(IReadOnlyDictionary<int, int> remaining)
    {
        // Mevcut kutuların o renk için kalan kapasite toplamını hesapla (queue + moving + bench)
        // Not: burada sahnedeki tüm BoxController'ları tarıyoruz ama sadece spawn anında (seyrek).
        // Spawn anında seyrek çağrılır, toplam box sayısı küçük (9 + bench) olduğu için acceptable.
        var allBoxes = Object.FindObjectsByType<BoxController>(FindObjectsSortMode.None);

        int bestColor = -1;
        int bestNeed = 0;

        foreach (var kv in remaining)
        {
            int colorId = kv.Key;
            int remainingCount = kv.Value;

            int capacityCoverage = 0;
            for (int i = 0; i < allBoxes.Length; i++)
            {
                var b = allBoxes[i];
                if (b == null)
                {
                    continue;
                }

                if (b.ColorId != colorId)
                {
                    continue;
                }

                capacityCoverage += Mathf.Max(0, b.Capacity - b.CurrentLoad);
            }

            int need = remainingCount - capacityCoverage;
            if (need > bestNeed)
            {
                bestNeed = need;
                bestColor = colorId;
            }
        }

        // need <= 0 ise hiçbir renge yeni kutu gerekmiyor
        return bestNeed > 0 ? bestColor : -1;
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

