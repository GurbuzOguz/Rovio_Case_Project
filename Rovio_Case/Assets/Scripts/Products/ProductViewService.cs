using System.Collections.Generic;
using UnityEngine;
using Zenject;
using DG.Tweening;

public class ProductViewService : MonoBehaviour, IProductViewService
{
    [Header("Pull Animation")]
    [SerializeField] private float pullDuration = 0.18f;
    [SerializeField] private float pullScaleTo = 0.2f;
    [SerializeField] private Ease pullMoveEase = Ease.InBack;
    [SerializeField] private Ease pullScaleEase = Ease.InQuad;

    private readonly Dictionary<Vector2Int, ProductView> _views = new Dictionary<Vector2Int, ProductView>();
    private IGridService _gridService;

    [Inject]
    public void Construct(IGridService gridService)
    {
        _gridService = gridService;
    }

    public void Register(Vector2Int cell, ProductView view)
    {
        if (view == null)
        {
            return;
        }

        _views[cell] = view;
    }

    public void Unregister(Vector2Int cell)
    {
        _views.Remove(cell);
    }

    public bool TryConsumeAndPullToBox(Vector2Int cell, Transform boxTransform)
    {
        if (!_views.TryGetValue(cell, out var view) || view == null)
        {
            _views.Remove(cell);
            return false;
        }

        _views.Remove(cell);

        if (boxTransform != null)
        {
            var t = view.transform;
            t.DOKill(false);

            Vector3 targetPos = boxTransform.position;
            t.DOMove(targetPos, pullDuration)
                .SetEase(pullMoveEase)
                .SetLink(view.gameObject, LinkBehaviour.KillOnDestroy)
                .OnComplete(() =>
                {
                    if (view != null)
                    {
                        Destroy(view.gameObject);
                    }
                });
            t.DOScale(Vector3.one * pullScaleTo, pullDuration)
                .SetEase(pullScaleEase)
                .SetLink(view.gameObject, LinkBehaviour.KillOnDestroy);
            return true;
        }

        Destroy(view.gameObject);
        return true;
    }

    public void ApplyShiftMoves(List<GridShiftMove> moves, System.Action onComplete = null)
    {
        if (moves == null || moves.Count == 0)
        {
            onComplete?.Invoke();
            return;
        }

        int pendingTweens = 0;
        bool anyTweenScheduled = false;

        // Collect sources first, then update dictionary mapping
        for (int i = 0; i < moves.Count; i++)
        {
            var m = moves[i];
            if (!_views.TryGetValue(m.from, out var view) || view == null)
            {
                continue;
            }

            _views.Remove(m.from);
            _views[m.to] = view;
            view.Initialize(m.to, m.colorId);

            if (_gridService != null)
            {
                Vector3 targetWorld = _gridService.GridToWorld(m.to.x, m.to.y);
                pendingTweens++;
                anyTweenScheduled = true;

                bool settled = false;
                System.Action settle = () =>
                {
                    if (settled)
                    {
                        return;
                    }
                    settled = true;
                    pendingTweens--;
                    if (pendingTweens <= 0)
                    {
                        onComplete?.Invoke();
                    }
                };

                view.transform
                    .DOMove(new Vector3(targetWorld.x, view.transform.position.y, targetWorld.z), 0.2f)
                    .SetEase(Ease.OutQuad)
                    .SetLink(view.gameObject, LinkBehaviour.KillOnDestroy)
                    .OnComplete(() => settle())
                    .OnKill(() => settle());
            }
        }

        if (!anyTweenScheduled)
        {
            onComplete?.Invoke();
        }
    }
}

