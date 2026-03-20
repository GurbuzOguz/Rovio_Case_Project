using System;
using System.Collections;
using UnityEngine;
using DG.Tweening;

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

        transform.DOKill(false);
    }

    private IEnumerator MoveAlongPathRoutine(BoxPath path, float speed, Action onMoveUpdate, Action onPathCompleted)
    {
        var waypoints = path.LocalWaypoints;
        var origin = path.transform.position;

        for (int i = 0; i < waypoints.Count; i++)
        {
            Vector3 targetPos = origin + waypoints[i];
            float segmentDuration = Vector3.Distance(transform.position, targetPos) / speed;

            Tween moveTween = transform
                .DOMove(targetPos, segmentDuration)
                .SetEase(Ease.Linear)
                .OnUpdate(() => onMoveUpdate?.Invoke());
            moveTween.SetLink(gameObject, LinkBehaviour.KillOnDestroy);

            yield return moveTween.WaitForCompletion();
        }

        _moveRoutine = null;
        onPathCompleted?.Invoke();
    }
}
