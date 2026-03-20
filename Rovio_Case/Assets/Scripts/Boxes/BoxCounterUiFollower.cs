using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(BoxController))]
public class BoxCounterUiFollower : MonoBehaviour
{
    [Header("Screen UI")]
    [SerializeField] private Vector3 worldOffset = new Vector3(0f, 1.8f, 0f);
    [SerializeField] private TMP_FontAsset fontAsset;
    [SerializeField] private int fontSize = 34;
    [SerializeField] private Color textColor = Color.white;
    [SerializeField] private bool useOutline = true;
    [SerializeField] private Color outlineColor = Color.black;
    [SerializeField, Range(0f, 1f)] private float outlineWidth = 0.2f;
    [SerializeField] private int sortingOrder = 500;

    [Header("Collect Bounce")]
    [SerializeField] private float bounceDuration = 0.16f;
    [SerializeField] private float bounceStrength = 0.22f;

    private static Canvas s_overlayCanvas;

    private BoxController _box;
    private Camera _camera;
    private RectTransform _labelRect;
    private TextMeshProUGUI _labelText;
    private int _lastLoad = -1;
    private int _lastCapacity = -1;

    private void Awake()
    {
        _box = GetComponent<BoxController>();
        _camera = Camera.main;
    }

    private void OnEnable()
    {
        EnsureLabel();
        RefreshText(force: true);
    }

    private void LateUpdate()
    {
        if (_box == null || _labelRect == null)
        {
            return;
        }

        if (_camera == null)
        {
            _camera = Camera.main;
            if (_camera == null)
            {
                return;
            }
        }

        Vector3 worldPos = transform.position + worldOffset;
        Vector3 screenPos = _camera.WorldToScreenPoint(worldPos);
        bool visible = screenPos.z > 0f;
        _labelRect.gameObject.SetActive(visible);
        if (!visible)
        {
            return;
        }

        _labelRect.position = screenPos;
        RefreshText(force: false);
    }

    private void OnDisable()
    {
        DisposeLabel();
    }

    private void OnDestroy()
    {
        DisposeLabel();
    }

    private void RefreshText(bool force)
    {
        if (_labelText == null || _box == null)
        {
            return;
        }

        int load = _box.CurrentLoad;
        int capacity = Mathf.Max(0, _box.Capacity);
        if (!force && load == _lastLoad && capacity == _lastCapacity)
        {
            return;
        }

        bool increased = !force && load > _lastLoad;
        _lastLoad = load;
        _lastCapacity = capacity;
        _labelText.text = $"{load}/{capacity}";

        if (increased)
        {
            _labelRect.DOKill(false);
            _labelRect.localScale = Vector3.one;
            _labelRect
                .DOPunchScale(Vector3.one * bounceStrength, bounceDuration, 1, 0f)
                .SetEase(Ease.OutQuad)
                .SetLink(_labelRect.gameObject, LinkBehaviour.KillOnDestroy);
        }
    }

    private void EnsureLabel()
    {
        if (_labelRect != null && _labelText != null)
        {
            return;
        }

        if (s_overlayCanvas == null)
        {
            var canvasGo = new GameObject("BoxCounterOverlayCanvas");
            s_overlayCanvas = canvasGo.AddComponent<Canvas>();
            s_overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            s_overlayCanvas.sortingOrder = sortingOrder;
            canvasGo.AddComponent<CanvasScaler>();
            canvasGo.AddComponent<GraphicRaycaster>();
            DontDestroyOnLoad(canvasGo);
        }

        var labelGo = new GameObject($"BoxCounter_{gameObject.GetInstanceID()}");
        labelGo.transform.SetParent(s_overlayCanvas.transform, false);
        _labelRect = labelGo.AddComponent<RectTransform>();
        _labelRect.sizeDelta = new Vector2(140f, 48f);
        _labelRect.localScale = Vector3.one;

        _labelText = labelGo.AddComponent<TextMeshProUGUI>();
        _labelText.alignment = TextAlignmentOptions.Center;
        if (fontAsset != null)
        {
            _labelText.font = fontAsset;
        }
        _labelText.fontSize = fontSize;
        _labelText.color = textColor;
        _labelText.outlineColor = outlineColor;
        _labelText.outlineWidth = useOutline ? outlineWidth : 0f;
        _labelText.text = "0/0";
        _labelText.raycastTarget = false;
    }

    private void DisposeLabel()
    {
        if (_labelRect != null)
        {
            _labelRect.DOKill(false);
            Destroy(_labelRect.gameObject);
            _labelRect = null;
            _labelText = null;
        }
    }
}
