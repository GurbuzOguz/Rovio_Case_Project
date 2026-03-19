using System;
using UnityEngine;

[CreateAssetMenu(fileName = "ParticleLibrary", menuName = "Game/Particle Library")]
public class ParticleLibrary : ScriptableObject
{
    [Serializable]
    public class Entry
    {
        public ParticleId id;
        public ParticleSystem prefab;
        public bool followTarget;
        [Min(0f)] public float lifetimeOverride;
    }

    public Entry[] entries;
}
