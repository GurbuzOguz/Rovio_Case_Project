using UnityEngine;
using Unity.Cinemachine;
using DG.Tweening;
using Zenject;

public class CameraMovement : MonoBehaviour
{
    [SerializeField] private CinemachineCamera virtualCamera;
    [Header("Intro")]
    [SerializeField] private float introTargetOrthoSize = 24f;
    [SerializeField] private float introDuration = 1f;

    [Header("Win Camera Motion")]
    [SerializeField] private float winOrthoSize = 22f;
    [SerializeField] private float winDuration = 0.5f;

    [Header("Lose Camera Motion")]
    [SerializeField] private float loseShakeDuration = 0.45f;
    [SerializeField] private float loseShakeStrength = 0.5f;
    [SerializeField] private int loseShakeVibrato = 16;

    private IGameStateService _gameStateService;

    [Inject]
    public void Construct([InjectOptional] IGameStateService gameStateService)
    {
        _gameStateService = gameStateService;
    }

    private void OnEnable()
    {
        if (_gameStateService != null)
        {
            _gameStateService.StateChanged += HandleStateChanged;
        }
    }

    private void OnDisable()
    {
        if (_gameStateService != null)
        {
            _gameStateService.StateChanged -= HandleStateChanged;
        }

        if (virtualCamera != null)
        {
            virtualCamera.transform.DOKill(false);
        }
    }

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
            introTargetOrthoSize,
            introDuration
        )
        .SetEase(Ease.InOutBack)
        .SetLink(gameObject, LinkBehaviour.KillOnDisable);
    }

    private void HandleStateChanged(GameRunState state)
    {
        if (virtualCamera == null)
        {
            return;
        }

        if (state == GameRunState.LevelComplete)
        {
            PlayWinMotion();
        }
        else if (state == GameRunState.LevelFail)
        {
            PlayLoseMotion();
        }
    }

    private void PlayWinMotion()
    {
        DOTween.To(
            () => virtualCamera.Lens.OrthographicSize,
            x => virtualCamera.Lens.OrthographicSize = x,
            winOrthoSize,
            winDuration
        )
        .SetEase(Ease.OutCubic)
        .SetLink(gameObject, LinkBehaviour.KillOnDisable);
    }

    private void PlayLoseMotion()
    {
        virtualCamera.transform.DOKill(false);
        virtualCamera.transform
            .DOShakePosition(loseShakeDuration, loseShakeStrength, loseShakeVibrato, 90f, false, true)
            .SetEase(Ease.OutQuad)
            .SetLink(gameObject, LinkBehaviour.KillOnDisable);
    }
}