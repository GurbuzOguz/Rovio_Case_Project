using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class SfxService : MonoBehaviour, ISfxService
{
    [SerializeField] private int defaultPoolSize = 8;

    private readonly List<AudioSource> _pool = new List<AudioSource>(16);
    private readonly Dictionary<SfxId, SfxLibrary.Entry> _map = new Dictionary<SfxId, SfxLibrary.Entry>();

    private int _poolIndex;

    private SfxLibrary _library;
    private int _poolSize;
    private bool _loggedLibraryNull;
    private readonly HashSet<SfxId> _missingIdsLogged = new HashSet<SfxId>();
    private AudioSource _musicSource;

    public void Initialize(SfxLibrary library, int poolSize)
    {
        _library = library;
        _poolSize = Mathf.Max(1, poolSize);

        _map.Clear();
        if (_library != null && _library.entries != null)
        {
            for (int i = 0; i < _library.entries.Length; i++)
            {
                var e = _library.entries[i];
                if (e == null)
                {
                    continue;
                }

                // Last definition wins (can be changed to keep first)
                _map[e.id] = e;
            }
        }

        EnsurePool();

        if (_library == null && !_loggedLibraryNull)
        {
            _loggedLibraryNull = true;
        }
    }

    public void Play(SfxId id)
    {
        if (_library == null)
        {
            return;
        }

        if (!_map.TryGetValue(id, out var entry) || entry == null || entry.clip == null)
        {
            if (!_missingIdsLogged.Contains(id))
            {
                _missingIdsLogged.Add(id);
            }
            return;
        }

        var src = NextSource();
        if (src == null)
        {
            return;
        }

        src.PlayOneShot(entry.clip, entry.volume);
    }

    public void PlayAt(SfxId id, Vector3 position)
    {
        // 3D not needed -> spatialBlend 0; keep playback path simple.
        Play(id);
    }

    public void PlayLoop(SfxId id)
    {
        if (_library == null)
        {
            return;
        }

        if (!_map.TryGetValue(id, out var entry) || entry == null || entry.clip == null)
        {
            if (!_missingIdsLogged.Contains(id))
            {
                _missingIdsLogged.Add(id);
            }
            return;
        }

        EnsureMusicSource();

        if (_musicSource.clip == entry.clip && _musicSource.isPlaying)
        {
            return;
        }

        _musicSource.Stop();
        _musicSource.clip = entry.clip;
        _musicSource.volume = entry.volume;
        _musicSource.loop = true;
        _musicSource.Play();
    }

    public void StopLoop()
    {
        if (_musicSource == null)
        {
            return;
        }

        _musicSource.Stop();
        _musicSource.clip = null;
    }

    private AudioSource NextSource()
    {
        if (_pool.Count == 0)
        {
            return null;
        }

        var src = _pool[_poolIndex];
        _poolIndex = (_poolIndex + 1) % _pool.Count;
        return src;
    }

    private void EnsurePool()
    {
        // Do not recreate pool if Initialize is called again in the same scene
        if (_pool.Count > 0)
        {
            return;
        }

        int size = _poolSize > 0 ? _poolSize : defaultPoolSize;
        for (int i = 0; i < size; i++)
        {
            var go = new GameObject($"SfxSource_{i}");
            go.transform.SetParent(transform, false);

            var src = go.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.spatialBlend = 0f; // 2D
            src.loop = false;

            _pool.Add(src);
        }
    }

    private void EnsureMusicSource()
    {
        if (_musicSource != null)
        {
            return;
        }

        var go = new GameObject("SfxMusicSource");
        go.transform.SetParent(transform, false);
        var src = go.AddComponent<AudioSource>();
        src.playOnAwake = false;
        src.spatialBlend = 0f; // 2D
        src.loop = true;
        _musicSource = src;
    }
}

