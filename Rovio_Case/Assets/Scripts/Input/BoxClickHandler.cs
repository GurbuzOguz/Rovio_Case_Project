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
        if (Physics.Raycast(ray, out RaycastHit hit, raycastMaxDistance, boxLayerMask))
        {
            var box = hit.collider.GetComponentInParent<BoxController>();
            if (box != null)
            {
                box.OnClickedByInput();
            }
        }
#endif
    }

#if ENABLE_INPUT_SYSTEM
    private bool TryGetPointerDownScreenPosition(out Vector2 screenPos)
    {
        // Device simulator/mobile için touch öncelikli.
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

