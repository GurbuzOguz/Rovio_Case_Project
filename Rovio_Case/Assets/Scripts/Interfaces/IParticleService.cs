using UnityEngine;

public interface IParticleService
{
    void Play(ParticleId id, Vector3 position);
    void PlayAttached(ParticleId id, Transform target);
}
