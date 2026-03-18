using UnityEngine;
using Zenject;

public class BoxClickHandler : MonoBehaviour
{
    [SerializeField] private LayerMask boxLayerMask = ~0;
    [SerializeField] private float raycastMaxDistance = 1000f;

    private Camera _camera;

    private void Awake()
    {
        _camera = Camera.main;
    }

    private void Update()
    {
#if ENABLE_INPUT_SYSTEM
        if (UnityEngine.InputSystem.Mouse.current == null)
        {
            return;
        }

        if (!UnityEngine.InputSystem.Mouse.current.leftButton.wasPressedThisFrame)
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

        Vector2 screenPos = UnityEngine.InputSystem.Mouse.current.position.ReadValue();
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
}

