using UnityEngine;
using Unity.Cinemachine;
using DG.Tweening;

public class CameraMovement : MonoBehaviour
{
    [SerializeField] private CinemachineCamera virtualCamera;

    private void Start()
    {
        if (virtualCamera == null)
        {
            virtualCamera = GetComponent<CinemachineCamera>();
        }

        if (virtualCamera == null)
        {
            return;
        }

        DOTween.To(
            () => virtualCamera.Lens.OrthographicSize,
            x => virtualCamera.Lens.OrthographicSize = x,
            24,
            1f
        ).SetEase(Ease.InOutBack);
    }
}