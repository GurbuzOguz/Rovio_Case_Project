using System;
using UnityEngine;
using DG.Tweening;

[DisallowMultipleComponent]
public class BoxFeedbackModule : MonoBehaviour, IBoxFeedbackModule
{
    [Header("Visuals")]
    [SerializeField] private bool playSpawnAnimation = true;
    [SerializeField] private float spawnScaleDuration = 0.25f;
    [Tooltip("Scale animasyonu icin gorsel root. Collider olan root'u scale etmeyin.")]
    [SerializeField] private Transform visualRoot;

    [Header("Collect Feedback")]
    [SerializeField] private bool playCollectScaleFeedback = true;
    [SerializeField] private float collectScaleAmount = 0.73f;
    [SerializeField] private float collectScaleUpDuration = 0.08f;
    [SerializeField] private float collectScaleDownDuration = 0.12f;

    private Vector3 _initialScale = Vector3.one;
    private bool _initialized;

    private Sequence _collectScaleTween;

    public void Initialize()
    {
        if (visualRoot == null)
        {
            var r = GetComponentInChildren<Renderer>();
            visualRoot = r != null ? r.transform : transform;
        }

        if (visualRoot == transform && GetComponent<Collider>() != null && transform.childCount > 0)
        {
            visualRoot = transform.GetChild(0);
        }

        _initialScale = visualRoot != null ? visualRoot.localScale : Vector3.one;

        ConfigureCollectScaleTween();
        _initialized = true;
    }

    public void OnEnableModule()
    {
        if (!_initialized)
        {
            Initialize();
        }

        if (playSpawnAnimation)
        {
            PlaySpawnAnimation();
        }
    }

    public void OnDisableModule()
    {
        transform.DOKill(false);
        if (visualRoot != null)
        {
            visualRoot.DOKill(false);
            visualRoot.localScale = _initialScale;
        }

        if (_collectScaleTween != null && _collectScaleTween.IsActive())
        {
            _collectScaleTween.Pause();
            _collectScaleTween.Rewind();
        }
    }

    public void OnDestroyModule()
    {
        if (_collectScaleTween != null && _collectScaleTween.IsActive())
        {
            _collectScaleTween.Kill(false);
        }
    }

    public void PlayClickFeedback(ISfxService sfxService, IHapticService hapticService, IParticleService particleService)
    {
        sfxService?.Play(SfxId.BoxClick);
        hapticService?.Selection();
        particleService?.PlayAttached(ParticleId.BoxClick, transform);
    }

    public void PlayCollectFeedback(ISfxService sfxService, IHapticService hapticService, IParticleService particleService, Vector3 worldPosition)
    {
        sfxService?.Play(SfxId.ProductCollect);
        hapticService?.LightImpact();
        particleService?.Play(ParticleId.ProductCollect, worldPosition);
        PlayCollectScaleFeedback();
    }

    public void PlayBoxFullFeedback(ISfxService sfxService, IHapticService hapticService, IParticleService particleService)
    {
        sfxService?.Play(SfxId.BoxFull);
        hapticService?.HeavyImpact();
        particleService?.PlayAttached(ParticleId.BoxFull, transform);
    }

    public void PlayDepletedFeedback(ISfxService sfxService, IHapticService hapticService, IParticleService particleService)
    {
        sfxService?.Play(SfxId.BoxDepleted);
        hapticService?.Warning();
        particleService?.PlayAttached(ParticleId.BoxDepleted, transform);
    }

    public void PlayBenchSitFeedback(ISfxService sfxService, IParticleService particleService, Vector3 benchPosition)
    {
        sfxService?.Play(SfxId.BenchSit);
        particleService?.Play(ParticleId.BenchSit, benchPosition);
    }

    public void PlayDeactivateScaleAnimation(float duration, Action onComplete)
    {
        transform.DOKill(false);
        if (visualRoot != null)
        {
            visualRoot.DOKill(false);
            visualRoot
                .DOScale(Vector3.zero, duration)
                .SetEase(Ease.InQuad)
                .SetLink(gameObject, LinkBehaviour.KillOnDestroy)
                .OnComplete(() => onComplete?.Invoke());
            return;
        }
        onComplete?.Invoke();
    }

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
            .SetLink(gameObject, LinkBehaviour.KillOnDestroy);
    }

    private void ConfigureCollectScaleTween()
    {
        if (visualRoot == null)
        {
            return;
        }

        if (_collectScaleTween != null && _collectScaleTween.IsActive())
        {
            _collectScaleTween.Kill(false);
        }

        float amt = Mathf.Max(0.001f, collectScaleAmount);
        float upDur = Mathf.Max(0.01f, collectScaleUpDuration);
        float downDur = Mathf.Max(0.01f, collectScaleDownDuration);

        _collectScaleTween = DOTween.Sequence();
        _collectScaleTween
            .SetAutoKill(false)
            .SetLink(gameObject, LinkBehaviour.KillOnDestroy)
            .Pause()
            .Append(visualRoot.DOScale(_initialScale * (1f + amt), upDur).SetEase(Ease.OutQuad))
            .Append(visualRoot.DOScale(_initialScale, downDur).SetEase(Ease.InQuad));
    }

    private void PlayCollectScaleFeedback()
    {
        if (!playCollectScaleFeedback || visualRoot == null)
        {
            return;
        }

        if (_collectScaleTween == null || !_collectScaleTween.IsActive())
        {
            ConfigureCollectScaleTween();
        }

        if (_collectScaleTween == null || !_collectScaleTween.IsActive())
        {
            return;
        }

        _collectScaleTween.Rewind();
        _collectScaleTween.Restart();
    }
}
