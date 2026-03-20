using UnityEngine;
using Zenject;

public class BoxClickHandler : MonoBehaviour
{
    [SerializeField] private LayerMask boxLayerMask = ~0;
    [SerializeField] private float raycastMaxDistance = 1000f;

    private Camera _camera;
    private IGameStateService _gameState;

    [Inject]
    public void Construct([InjectOptional] IGameStateService gameState)
    {
        _gameState = gameState;
    }

    private void Awake()
    {
        _camera = Camera.main;
    }

    private void Update()
    {
#if ENABLE_INPUT_SYSTEM
        if (_gameState != null && _gameState.State != GameRunState.Playing)
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

        if (!TryGetPointerDownScreenPosition(out Vector2 screenPos))
        {
            return;
        }

        Ray ray = _camera.ScreenPointToRay(screenPos);
        if (TryGetClickedBox(ray, out BoxController box))
        {
            box.OnClickedByInput();
        }
#endif
    }

#if ENABLE_INPUT_SYSTEM
    private bool TryGetClickedBox(Ray ray, out BoxController box)
    {
        // Scan all hits so a front collider does not hide a box behind it.
        var hits = Physics.RaycastAll(ray, raycastMaxDistance, boxLayerMask, QueryTriggerInteraction.Ignore);
        if (hits == null || hits.Length == 0)
        {
            box = null;
            return false;
        }

        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        for (int i = 0; i < hits.Length; i++)
        {
            var candidate = hits[i].collider != null
                ? hits[i].collider.GetComponentInParent<BoxController>()
                : null;
            if (candidate != null)
            {
                box = candidate;
                return true;
            }
        }

        box = null;
        return false;
    }

    private bool TryGetPointerDownScreenPosition(out Vector2 screenPos)
    {
        // Prioritize touch for device simulator/mobile.
        var touch = UnityEngine.InputSystem.Touchscreen.current;
        if (touch != null && touch.primaryTouch.press.wasPressedThisFrame)
        {
            screenPos = touch.primaryTouch.position.ReadValue();
            return true;
        }

        var mouse = UnityEngine.InputSystem.Mouse.current;
        if (mouse != null && mouse.leftButton.wasPressedThisFrame)
        {
            screenPos = mouse.position.ReadValue();
            return true;
        }

        screenPos = default;
        return false;
    }
#endif
}

