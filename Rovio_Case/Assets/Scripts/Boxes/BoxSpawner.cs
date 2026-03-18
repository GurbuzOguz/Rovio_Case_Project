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

    private DiContainer _container;
    private LevelLayout _levelLayout;
    private GridConfig _gridConfig;
    private readonly List<BoxController> _activeBoxes = new List<BoxController>();
    private readonly List<BoxConfig> _runtimeGeneratedConfigs = new List<BoxConfig>();

    public IReadOnlyList<BoxController> ActiveBoxes => _activeBoxes;

    [Inject]
    public void Construct(LevelLayout levelLayout, GridConfig gridConfig, DiContainer container)
    {
        _levelLayout = levelLayout;
        _gridConfig = gridConfig;
        _container = container;
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
            SpawnBox(i);
        }
    }

    private void SpawnBox(int index)
    {
        Transform spawnPoint = spawnPoints[index];
        Vector3 spawnPosition = spawnPoint != null ? spawnPoint.position : Vector3.zero;

        var boxGo = _container != null
            ? _container.InstantiatePrefab(boxPrefab, spawnPosition, Quaternion.identity, boxesParent)
            : Object.Instantiate(boxPrefab, spawnPosition, Quaternion.identity, boxesParent);
        boxGo.name = $"Box_{_activeBoxes.Count}";

        var controller = boxGo.GetComponent<BoxController>();
        if (controller == null)
        {
            Debug.LogError("BoxSpawner: Box prefab does not have a BoxController component.");
            Destroy(boxGo);
            return;
        }

        BoxConfig config = GetOrCreateConfigForIndex(index, _levelLayout);

        BoxPath sharedPath = _gridConfig != null ? _gridConfig.boxPath : null;
        controller.Initialize(config, sharedPath, _levelLayout != null ? _levelLayout.productPalette : null);

        _activeBoxes.Add(controller);
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

