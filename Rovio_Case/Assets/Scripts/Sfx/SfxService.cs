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

                // Son tanım override eder (istersen ilk tanımda bırakırız)
                _map[e.id] = e;
            }
        }

        EnsurePool();

        if (_library == null && !_loggedLibraryNull)
        {
            _loggedLibraryNull = true;
            Debug.LogWarning("SfxService: SfxLibrary is null. No SFX will play until you assign one in SceneServicesInstaller.");
        }
        else if (_library != null && _map.Count == 0)
        {
            Debug.LogWarning("SfxService: SfxLibrary has no valid entries (entries null/empty or missing ids/clips). Assign clips for SfxId values.");
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
                Debug.LogWarning($"SfxService: Missing AudioClip for id={id}. Add it to SfxLibrary entries.");
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
        // 3D istemiyorsun -> spatialBlend 0; yine de basitçe aynı şekilde çalıyoruz.
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
                Debug.LogWarning($"SfxService: Missing AudioClip for id={id}. Add it to SfxLibrary entries.");
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
        // Aynı sahnede tekrar Initialize olursa pool'ü yeniden oluşturma
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

