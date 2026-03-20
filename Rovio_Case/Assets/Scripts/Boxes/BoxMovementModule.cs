using System;
using System.Collections;
using UnityEngine;
#if DOTWEEN_EXISTS || true
using DG.Tweening;
#endif

[DisallowMultipleComponent]
public class BoxMovementModule : MonoBehaviour, IBoxMovementModule
{
    private Coroutine _moveRoutine;

    public void StartMove(BoxPath path, float speed, Action onMoveUpdate, Action onPathCompleted)
    {
        if (path == null || path.LocalWaypoints == null || path.LocalWaypoints.Count == 0)
        {
            return;
        }

        StopMove();
        _moveRoutine = StartCoroutine(MoveAlongPathRoutine(path, Mathf.Max(0.01f, speed), onMoveUpdate, onPathCompleted));
    }

    public void StopMove()
    {
        if (_moveRoutine != null)
        {
            StopCoroutine(_moveRoutine);
            _moveRoutine = null;
        }

#if DOTWEEN_EXISTS || true
        transform.DOKill(false);
#endif
    }

    private IEnumerator MoveAlongPathRoutine(BoxPath path, float speed, Action onMoveUpdate, Action onPathCompleted)
    {
        var waypoints = path.LocalWaypoints;
        var origin = path.transform.position;

        for (int i = 0; i < waypoints.Count; i++)
        {
            Vector3 targetPos = origin + waypoints[i];
            float segmentDuration = Vector3.Distance(transform.position, targetPos) / speed;

#if DOTWEEN_EXISTS || true
            Tween moveTween = transform
                .DOMove(targetPos, segmentDuration)
                .SetEase(Ease.Linear)
                .OnUpdate(() => onMoveUpdate?.Invoke());
            moveTween.SetLink(gameObject, LinkBehaviour.KillOnDisable);

            yield return moveTween.WaitForCompletion();
#else
            while ((transform.position - targetPos).sqrMagnitude > 0.0001f)
            {
                transform.position = Vector3.MoveTowards(transform.position, targetPos, speed * Time.deltaTime);
                onMoveUpdate?.Invoke();
                yield return null;
            }
#endif
        }

        _moveRoutine = null;
        onPathCompleted?.Invoke();
    }
}
