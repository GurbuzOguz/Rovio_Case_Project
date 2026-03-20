using System;

public interface IBoxMovementModule
{
    void StartMove(BoxPath path, float speed, Action onMoveUpdate, Action onPathCompleted);
    void StopMove();
}
