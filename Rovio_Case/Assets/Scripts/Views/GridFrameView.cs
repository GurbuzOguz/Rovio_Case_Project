using UnityEngine;
using Zenject;
#if DOTWEEN_EXISTS || true
using DG.Tweening;
#endif

public class GridFrameView : MonoBehaviour
{
    [Header("Prefabs")]
    [SerializeField] private GameObject backgroundPrefab;
    [SerializeField] private GameObject borderPrefab;

    [Header("Layout")]
    [SerializeField] private float borderThickness = 0.1f;
    [SerializeField] private float backgroundYOffset = -0.01f;

    [Header("Animation")]
    [SerializeField] private float backgroundScaleY = 0.01f;
    [SerializeField] private float borderScaleY = 0.1f;
    [SerializeField] private float backgroundAnimDuration = 0.35f;
    [SerializeField] private float borderAnimDuration = 0.35f;

    [Header("Parents (optional)")]
    [SerializeField] private Transform backgroundParent;
    [SerializeField] private Transform bordersParent;

    private IGridService _gridService;

    [Inject]
    public void Construct(IGridService gridService)
    {
        _gridService = gridService;
    }

    private void Start()
    {
        if (_gridService == null)
        {
            Debug.LogError("GridFrameView: IGridService is not injected.");
            return;
        }

        EnsureParents();
        CreateFrame();
    }

    private void EnsureParents()
    {
        if (backgroundParent == null)
        {
            var bgRoot = new GameObject("BackgroundRoot");
            bgRoot.transform.SetParent(transform, false);
            backgroundParent = bgRoot.transform;
        }

        if (bordersParent == null)
        {
            var bordersRoot = new GameObject("BordersRoot");
            bordersRoot.transform.SetParent(transform, false);
            bordersParent = bordersRoot.transform;
        }
    }

    private void CreateFrame()
    {
        if (backgroundPrefab == null && borderPrefab == null)
        {
            return;
        }

        // Grid boyutlarını hesapla
        Vector3 originWorld = _gridService.GridToWorld(0, 0);

        // En az 1x1 grid varsayıyoruz, cell size'ı komşu hücreden bul
        float cellSizeX = 1f;
        float cellSizeZ = 1f;

        if (_gridService.Columns > 1)
        {
            cellSizeX = Mathf.Abs(_gridService.GridToWorld(1, 0).x - originWorld.x);
        }
        else if (_gridService.Columns == 1 && _gridService.Rows > 1)
        {
            // Tek sütunlu durumda da satır aralığından yaklaşık al
            cellSizeX = Mathf.Abs(_gridService.GridToWorld(0, 1).z - originWorld.z);
        }

        if (_gridService.Rows > 1)
        {
            cellSizeZ = Mathf.Abs(_gridService.GridToWorld(0, 1).z - originWorld.z);
        }
        else if (_gridService.Rows == 1 && _gridService.Columns > 1)
        {
            cellSizeZ = Mathf.Abs(_gridService.GridToWorld(1, 0).x - originWorld.x);
        }

        float width = _gridService.Columns * cellSizeX;
        float height = _gridService.Rows * cellSizeZ;

        // Grid merkezini hesapla
        Vector3 center = originWorld + new Vector3(
            (width - cellSizeX) * 0.5f,
            0f,
            (height - cellSizeZ) * 0.5f
        );

        // Background
        if (backgroundPrefab != null)
        {
            var bg = Instantiate(backgroundPrefab, center + Vector3.up * backgroundYOffset, Quaternion.identity, backgroundParent);
            bg.name = "GridBackground";

            var targetScale = new Vector3(width, backgroundScaleY, height);

#if DOTWEEN_EXISTS || true
            bg.transform.localScale = Vector3.zero;
            bg.transform
                .DOScale(targetScale, backgroundAnimDuration)
                .SetEase(Ease.OutQuad);
#else
            bg.transform.localScale = targetScale;
#endif
        }

        if (borderPrefab == null)
        {
            return;
        }

        // Alt border
        var bottomPos = center + new Vector3(0f, 0f, -height * 0.5f - borderThickness * 0.5f);
        var bottom = Instantiate(borderPrefab, bottomPos, Quaternion.identity, bordersParent);
        bottom.name = "Border_Bottom";

        var bottomTargetScale = new Vector3(width + borderThickness * 2f, borderScaleY, borderThickness);

        // Üst border
        var topPos = center + new Vector3(0f, 0f, height * 0.5f + borderThickness * 0.5f);
        var top = Instantiate(borderPrefab, topPos, Quaternion.identity, bordersParent);
        top.name = "Border_Top";

        var topTargetScale = new Vector3(width + borderThickness * 2f, borderScaleY, borderThickness);

        // Sol border
        var leftPos = center + new Vector3(-width * 0.5f - borderThickness * 0.5f, 0f, 0f);
        var left = Instantiate(borderPrefab, leftPos, Quaternion.identity, bordersParent);
        left.name = "Border_Left";

        var leftTargetScale = new Vector3(borderThickness, borderScaleY, height);

        // Sağ border
        var rightPos = center + new Vector3(width * 0.5f + borderThickness * 0.5f, 0f, 0f);
        var right = Instantiate(borderPrefab, rightPos, Quaternion.identity, bordersParent);
        right.name = "Border_Right";

        var rightTargetScale = new Vector3(borderThickness, borderScaleY, height);

#if DOTWEEN_EXISTS || true
        AnimateBorder(bottom.transform, bottomTargetScale);
        AnimateBorder(top.transform, topTargetScale);
        AnimateBorder(left.transform, leftTargetScale);
        AnimateBorder(right.transform, rightTargetScale);
#else
        bottom.transform.localScale = bottomTargetScale;
        top.transform.localScale = topTargetScale;
        left.transform.localScale = leftTargetScale;
        right.transform.localScale = rightTargetScale;
#endif
    }

#if DOTWEEN_EXISTS || true
    private void AnimateBorder(Transform borderTransform, Vector3 targetScale)
    {
        borderTransform.localScale = Vector3.zero;
        borderTransform
            .DOScale(targetScale, borderAnimDuration)
            .SetEase(Ease.OutBack);
    }
#endif
}

