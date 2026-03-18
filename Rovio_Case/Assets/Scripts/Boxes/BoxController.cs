using System;
using System.Collections;
using UnityEngine;
using Zenject;
#if DOTWEEN_EXISTS || true
using DG.Tweening;
#endif

public enum BoxState
{
    Idle,
    Moving,
    OnBench,
    Destroyed
}

[RequireComponent(typeof(Collider))]
public class BoxController : MonoBehaviour, IBox
{
    [SerializeField] private BoxConfig boxConfig;
    [SerializeField] private BoxPath path;

    [Header("Movement Override")]
    [SerializeField] private float defaultMoveSpeed = 3f;

    [Header("Visuals")]
    [SerializeField] private Color defaultBoxColor = Color.white;
    [SerializeField] private bool playSpawnAnimation = true;
    [SerializeField] private float spawnScaleDuration = 0.25f;
    [Tooltip("Scale animasyonu için görsel root. Collider olan root'u scale etmeyin.")]
    [SerializeField] private Transform visualRoot;

    [Header("Collect (Align)")]
    [SerializeField] private float alignTolerance = 0.2f;
    [SerializeField] private float collectInterval = 0.05f;
    [SerializeField] private float pullMaxDistance = 2.0f;
    [SerializeField] private bool onlyPullFromEdgeZones = true;
    [SerializeField] private float cornerOutsideMargin = 0.15f;

    private IGridService _gridService;
    private LevelLayout _levelLayout;
    private ProductPalette _paletteOverride;
    private IBenchService _benchService;
    private Transform _reservedBenchSlot;
    private IProductViewService _productViewService;

    private Vector3 _initialScale;

    private int _currentLoad;
    private int _pathIndex;
    private Coroutine _moveRoutine;
    private BoxState _state = BoxState.Idle;
    private float _collectTimer;

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
        [InjectOptional] IProductViewService productViewService)
    {
        _gridService = gridService;
        _levelLayout = levelLayout;
        _benchService = benchService;
        _productViewService = productViewService;
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

    private void Awake()
    {
        if (visualRoot == null)
        {
            // Collider olan root'u scale etmeyelim: mümkünse renderer child'ını görsel root yap
            var r = GetComponentInChildren<Renderer>();
            visualRoot = r != null ? r.transform : transform;
        }

        // Eğer hala root ise ve üzerinde collider varsa, ilk child'ı kullanmayı dene
        if (visualRoot == transform && GetComponent<Collider>() != null && transform.childCount > 0)
        {
            visualRoot = transform.GetChild(0);
        }

        _initialScale = visualRoot.localScale;

        if (boxConfig == null)
        {
            Debug.LogWarning($"BoxController on {name}: BoxConfig not assigned.");
        }

    }

    private void OnEnable()
    {
#if DOTWEEN_EXISTS || true
        if (playSpawnAnimation)
        {
            PlaySpawnAnimation();
        }
#endif
    }

#if DOTWEEN_EXISTS || true
    private void OnDisable()
    {
        // Objeyi deactivate ettiğimizde aktif tween'leri temizle
        transform.DOKill(false);
        if (visualRoot != null)
        {
            visualRoot.DOKill(false);
        }
    }
#endif

#if DOTWEEN_EXISTS || true
    private void PlaySpawnAnimation()
    {
        if (visualRoot == null)
        {
            return;
        }

        visualRoot.DOKill(false);
        visualRoot.localScale = Vector3.zero;
        visualRoot
            .DOScale(_initialScale, spawnScaleDuration)
            .SetEase(Ease.OutBack)
            .SetLink(gameObject, LinkBehaviour.KillOnDisable);
    }
#endif

    private void OnDestroy()
    {
        ReleaseBenchSlotIfAny();
    }

    public void OnClickedByInput()
    {
        if (_state == BoxState.Idle || _state == BoxState.OnBench)
        {
            ReleaseBenchSlotIfAny();
            StartMove();
        }
    }

    public void StartMove()
    {
        if (path == null || path.LocalWaypoints == null || path.LocalWaypoints.Count == 0)
        {
            Debug.LogWarning($"BoxController on {name}: Shared BoxPath is missing or empty.");
            return;
        }

        if (_state == BoxState.Idle)
        {
            StartedMovingFromQueue?.Invoke(this);
        }

        if (_moveRoutine != null)
        {
            StopCoroutine(_moveRoutine);
        }

        _moveRoutine = StartCoroutine(MoveAlongPath());
    }

    private IEnumerator MoveAlongPath()
    {
        _state = BoxState.Moving;
        _pathIndex = 0;
        _collectTimer = 0f;

        float speed = MoveSpeed;
        var waypoints = path.LocalWaypoints;
        var origin = path.transform.position;

        while (_pathIndex < waypoints.Count)
        {
            Vector3 targetPos = origin + waypoints[_pathIndex];

            float segmentDuration = Vector3.Distance(transform.position, targetPos) / speed;

#if DOTWEEN_EXISTS || true
            Tween moveTween = transform
                .DOMove(targetPos, segmentDuration)
                .SetEase(Ease.Linear)
                .OnUpdate(TryCollectAlignedProductIfAny);
            moveTween.SetLink(gameObject, LinkBehaviour.KillOnDisable);

            yield return moveTween.WaitForCompletion();
#else
            float elapsed = 0f;
            while ((transform.position - targetPos).sqrMagnitude > 0.0001f)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / segmentDuration);
                transform.position = Vector3.MoveTowards(transform.position, targetPos, speed * Time.deltaTime);
                TryCollectAlignedProductIfAny();
                yield return null;
            }
#endif

            _pathIndex++;
        }

        OnPathCompleted();
    }

    private void TryCollectAlignedProductIfAny()
    {
        if (_gridService == null || boxConfig == null)
        {
            return;
        }

        _collectTimer += Time.deltaTime;
        if (_collectTimer < collectInterval)
        {
            return;
        }
        _collectTimer = 0f;

        if (IsFull)
        {
            return;
        }

        Vector3 pos = transform.position;
        if (onlyPullFromEdgeZones && !IsInEdgePullZone(pos))
        {
            return;
        }

        if (!_gridService.TryFindAlignedProductCell(pos, alignTolerance, boxConfig.colorId, out var cell))
        {
            return;
        }

        // Ürün çok uzaktaysa çekme
        Vector3 cellWorld = _gridService.GridToWorld(cell.x, cell.y);
        float dist = Vector2.Distance(new Vector2(pos.x, pos.z), new Vector2(cellWorld.x, cellWorld.z));
        if (dist > pullMaxDistance)
        {
            return;
        }

        if (_productViewService == null)
        {
            Debug.LogWarning("BoxController: IProductViewService not injected. Only grid data will be removed (no pull animation).");
        }

        // Önce view tarafını tüket (varsa) ve box'a çek
        _productViewService?.TryConsumeAndPullToBox(cell, transform);

        _gridService.RemoveProductAt(cell.x, cell.y);
        _currentLoad++;

        if (IsFull)
        {
            OnBoxFull();
        }
    }

    private bool IsInEdgePullZone(Vector3 worldPos)
    {
        if (_levelLayout == null || _levelLayout.gridConfig == null)
        {
            return true;
        }

        var gc = _levelLayout.gridConfig;
        float minX = gc.origin.x;
        float maxX = gc.origin.x + (gc.columns - 1) * gc.cellSize;
        float minZ = gc.origin.z;
        float maxZ = gc.origin.z + (gc.rows - 1) * gc.cellSize;

        bool xOutside = worldPos.x < (minX - cornerOutsideMargin) || worldPos.x > (maxX + cornerOutsideMargin);
        bool zOutside = worldPos.z < (minZ - cornerOutsideMargin) || worldPos.z > (maxZ + cornerOutsideMargin);

        // Kenar bölgeleri: X veya Z ekseninde grid bounds'un dışında olmak yeterli
        return xOutside || zOutside;
    }

    private void OnBoxFull()
    {
        _state = BoxState.Destroyed;
        ReleaseBenchSlotIfAny();

        // Hareket coroutine'u varsa durdur
        if (_moveRoutine != null)
        {
            StopCoroutine(_moveRoutine);
            _moveRoutine = null;
        }

#if DOTWEEN_EXISTS || true
        // Box üzerindeki tüm tweens'leri anında durdur (null target uyarılarını engeller)
        transform.DOKill(false);

        if (visualRoot != null)
        {
            visualRoot.DOKill(false);
            visualRoot
                .DOScale(Vector3.zero, 0.25f)
                .SetEase(Ease.InQuad)
                .SetLink(gameObject, LinkBehaviour.KillOnDisable)
                .OnComplete(DeactivateBox);
        }
        else
        {
            DeactivateBox();
        }
#else
        DeactivateBox();
#endif
    }

    private void DeactivateBox()
    {
        // Collider ve scriptler dursun, GC ve destroy maliyeti olmasın
        var col = GetComponent<Collider>();
        if (col != null)
        {
            col.enabled = false;
        }

        BecameInactive?.Invoke(this);
        gameObject.SetActive(false);
    }

    private void ReleaseBenchSlotIfAny()
    {
        if (_reservedBenchSlot == null)
        {
            return;
        }

        if (_benchService != null)
        {
            _benchService.ReleaseSlot(_reservedBenchSlot);
        }

        _reservedBenchSlot = null;
    }

    private void OnPathCompleted()
    {
        if (IsFull)
        {
            // Zaten OnBoxFull içinde destroy ediliyor.
            return;
        }

        // Bench'e oturma
        if (_benchService == null)
        {
            Debug.LogWarning($"Box {name}: IBenchService not injected. Staying at end of path.");
            _state = BoxState.OnBench;
            return;
        }

        if (!_benchService.TryReserveSlot(out _reservedBenchSlot) || _reservedBenchSlot == null)
        {
            Debug.LogError("BENCH FULL -> Level Fail (placeholder)");
            _state = BoxState.OnBench;
            return;
        }

        _state = BoxState.OnBench;

#if DOTWEEN_EXISTS || true
        transform
            .DOMove(_reservedBenchSlot.position, 0.25f)
            .SetEase(Ease.OutQuad)
            .SetLink(gameObject, LinkBehaviour.KillOnDisable);
#else
        transform.position = _reservedBenchSlot.position;
#endif
    }
}

