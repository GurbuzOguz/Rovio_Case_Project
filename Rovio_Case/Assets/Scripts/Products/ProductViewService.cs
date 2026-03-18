using System.Collections.Generic;
using UnityEngine;
#if DOTWEEN_EXISTS || true
using DG.Tweening;
#endif

public class ProductViewService : MonoBehaviour, IProductViewService
{
    [Header("Pull Animation")]
    [SerializeField] private float pullDuration = 0.18f;
    [SerializeField] private float pullScaleTo = 0.2f;
    [SerializeField] private Ease pullEase = Ease.InBack;

    private readonly Dictionary<Vector2Int, ProductView> _views = new Dictionary<Vector2Int, ProductView>();

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

#if DOTWEEN_EXISTS || true
        if (boxTransform != null)
        {
            var t = view.transform;
            t.DOKill(false);

            Vector3 targetPos = boxTransform.position;
            Sequence seq = DOTween.Sequence();
            seq.Join(t.DOMove(targetPos, pullDuration).SetEase(pullEase));
            seq.Join(t.DOScale(Vector3.one * pullScaleTo, pullDuration).SetEase(pullEase));
            seq.OnComplete(() =>
            {
                if (view != null)
                {
                    Destroy(view.gameObject);
                }
            });
            return true;
        }
#endif

        Destroy(view.gameObject);
        return true;
    }
}

