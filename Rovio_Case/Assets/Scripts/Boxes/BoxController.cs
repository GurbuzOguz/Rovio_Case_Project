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

    private IGridService _gridService;

    private int _currentLoad;
    private int _pathIndex;
    private Coroutine _moveRoutine;
    private BoxState _state = BoxState.Idle;

    // Tek bir instance üzerinden tıklama raycast'ini yönetmek için
    private static BoxController _clickHandlerInstance;
    private static Camera _cachedCamera;

    public int ColorId => boxConfig != null ? boxConfig.colorId : -1;
    public int CurrentLoad => _currentLoad;
    public int Capacity => boxConfig != null ? boxConfig.capacity : 0;
    public bool IsFull => _currentLoad >= Capacity && Capacity > 0;
    public float MoveSpeed => boxConfig != null ? boxConfig.moveSpeed : defaultMoveSpeed;

    [Inject]
    public void Construct(IGridService gridService)
    {
        _gridService = gridService;
    }

    private void Awake()
    {
        if (boxConfig == null)
        {
            Debug.LogWarning($"BoxController on {name}: BoxConfig not assigned.");
        }

        if (path == null)
        {
            path = GetComponent<BoxPath>();
        }

        // İlk BoxController instance'ını global click handler olarak kullan
        if (_clickHandlerInstance == null)
        {
            _clickHandlerInstance = this;
        }
    }

    private void OnDestroy()
    {
        if (_clickHandlerInstance == this)
        {
            _clickHandlerInstance = null;
        }
    }

        private void Update()
    {
        // Sadece tek bir instance input'u dinlesin
        if (_clickHandlerInstance != this)
        {
            return;
        }

        if (UnityEngine.InputSystem.Mouse.current != null &&
            UnityEngine.InputSystem.Mouse.current.leftButton.wasPressedThisFrame)
        {
            HandleClickRaycast(UnityEngine.InputSystem.Mouse.current.position.ReadValue());
        }
    }

    private void HandleClickRaycast(Vector2 screenPosition)
    {
        if (_cachedCamera == null)
        {
            _cachedCamera = Camera.main;
            if (_cachedCamera == null)
            {
                Debug.LogWarning("BoxController: Main Camera not found for click handling.");
                return;
            }
        }

        Ray ray = _cachedCamera.ScreenPointToRay(screenPosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 1000f))
        {
            var box = hit.collider.GetComponentInParent<BoxController>();
            if (box != null)
            {
                Debug.Log($"BoxController click hit: {box.name}, state={box._state}");
                box.HandleClicked();
            }
        }
    }

    private void HandleClicked()
    {
        if (_state == BoxState.Idle || _state == BoxState.OnBench)
        {
            StartMove();
        }
    }

    public void StartMove()
    {
        if (path == null || path.LocalWaypoints.Count == 0)
        {
            Debug.LogWarning($"BoxController on {name}: Path is missing or empty.");
            return;
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

        var waypoints = path.LocalWaypoints;
        var origin = path.transform.position;

        Vector3 startPos = transform.position;

        float speed = MoveSpeed;

        while (_pathIndex < waypoints.Count)
        {
            Vector3 targetPos = origin + waypoints[_pathIndex];

            float segmentDuration = Vector3.Distance(transform.position, targetPos) / speed;

#if DOTWEEN_EXISTS || true
            Tween moveTween = transform
                .DOMove(targetPos, segmentDuration)
                .SetEase(Ease.Linear)
                .OnUpdate(TryCollectProductAtCurrentPosition);

            yield return moveTween.WaitForCompletion();
#else
            float elapsed = 0f;
            while ((transform.position - targetPos).sqrMagnitude > 0.0001f)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / segmentDuration);
                transform.position = Vector3.MoveTowards(transform.position, targetPos, speed * Time.deltaTime);
                TryCollectProductAtCurrentPosition();
                yield return null;
            }
#endif

            _pathIndex++;
        }

        OnPathCompleted();
    }

    private void TryCollectProductAtCurrentPosition()
    {
        if (_gridService == null || boxConfig == null)
        {
            return;
        }

        Vector3 pos = transform.position;
        Vector2Int gridPos = _gridService.WorldToGrid(pos);

        if (!_gridService.IsInside(gridPos.x, gridPos.y))
        {
            return;
        }

        if (!_gridService.HasProductAt(gridPos.x, gridPos.y))
        {
            return;
        }

        int productColorId = _gridService.GetProductAt(gridPos.x, gridPos.y);

        if (productColorId != boxConfig.colorId)
        {
            return;
        }

        if (IsFull)
        {
            return;
        }

        _gridService.RemoveProductAt(gridPos.x, gridPos.y);
        _currentLoad++;

        // TODO: product view'i yok etmek için event veya ayrı bir sistem ekleyeceğiz.

        if (IsFull)
        {
            OnBoxFull();
        }
    }

    private void OnBoxFull()
    {
        _state = BoxState.Destroyed;
#if DOTWEEN_EXISTS || true
        transform
            .DOScale(Vector3.zero, 0.25f)
            .SetEase(Ease.InBack)
            .OnComplete(() => Destroy(gameObject));
#else
        Destroy(gameObject);
#endif
    }

    private void OnPathCompleted()
    {
        if (IsFull)
        {
            // Zaten OnBoxFull içinde destroy ediliyor.
            return;
        }

        // Şimdilik bench'e oturduğunu varsayıp state'i güncelliyoruz.
        _state = BoxState.OnBench;
        Debug.Log($"Box {name} reached bench (placeholder).");

        // Sonraki adımda burada Bench sistemi ile entegre olacağız.
    }
}

