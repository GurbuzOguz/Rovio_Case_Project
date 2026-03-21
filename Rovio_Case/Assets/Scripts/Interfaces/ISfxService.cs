using UnityEngine;

public interface ISfxService
{
    void Play(SfxId id);
    void PlayAt(SfxId id, Vector3 position);
    void PlayLoop(SfxId id);
    void StopLoop();
}

