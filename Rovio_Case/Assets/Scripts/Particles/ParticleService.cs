using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class ParticleService : MonoBehaviour, IParticleService
{
    private readonly Dictionary<ParticleId, ParticleLibrary.Entry> _map = new Dictionary<ParticleId, ParticleLibrary.Entry>();
    private readonly HashSet<ParticleId> _missingLogged = new HashSet<ParticleId>();

    private ParticleLibrary _library;

    public void Initialize(ParticleLibrary library)
    {
        _library = library;
        _map.Clear();
        _missingLogged.Clear();

        if (_library == null || _library.entries == null)
        {
            return;
        }

        for (int i = 0; i < _library.entries.Length; i++)
        {
            var e = _library.entries[i];
            if (e == null || e.prefab == null)
            {
                continue;
            }

            _map[e.id] = e;
        }
    }

    public void Play(ParticleId id, Vector3 position)
    {
        if (!TryGetEntry(id, out var entry))
        {
            return;
        }

        var ps = Instantiate(entry.prefab, position, Quaternion.identity, transform);
        ps.Play(true);
        Destroy(ps.gameObject, ResolveLifetime(ps, entry));
    }

    public void PlayAttached(ParticleId id, Transform target)
    {
        if (target == null)
        {
            return;
        }

        if (!TryGetEntry(id, out var entry))
        {
            return;
        }

        var parent = entry.followTarget ? target : transform;
        var ps = Instantiate(entry.prefab, target.position, Quaternion.identity, parent);
        ps.Play(true);
        Destroy(ps.gameObject, ResolveLifetime(ps, entry));
    }

    private bool TryGetEntry(ParticleId id, out ParticleLibrary.Entry entry)
    {
        if (_map.TryGetValue(id, out entry) && entry != null && entry.prefab != null)
        {
            return true;
        }

        if (!_missingLogged.Contains(id))
        {
            _missingLogged.Add(id);
            Debug.LogWarning($"ParticleService: Missing prefab for id={id}. Assign it in ParticleLibrary.");
        }

        entry = null;
        return false;
    }

    private static float ResolveLifetime(ParticleSystem ps, ParticleLibrary.Entry entry)
    {
        if (entry != null && entry.lifetimeOverride > 0f)
        {
            return entry.lifetimeOverride;
        }

        if (ps == null)
        {
            return 2f;
        }

        var main = ps.main;
        float baseLife = main.duration + main.startLifetime.constantMax;
        return Mathf.Max(0.5f, baseLife + 0.25f);
    }
}
