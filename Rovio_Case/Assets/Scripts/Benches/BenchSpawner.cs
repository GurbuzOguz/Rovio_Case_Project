using System.Collections.Generic;
using UnityEngine;
using Zenject;
using DG.Tweening;

public class BenchSpawner : MonoBehaviour, IBenchService
{
    [SerializeField] private List<Transform> benchSlots = new List<Transform>();

    [Header("Animation")]
    [SerializeField] private bool animateOnStart = true;
    [SerializeField] private float spawnScaleDuration = 0.3f;
    [SerializeField] private float spawnStagger = 0.02f;

    private LevelLayout _levelLayout;
    private readonly List<Vector3> _initialScales = new List<Vector3>();
    private readonly HashSet<Transform> _reservedSlots = new HashSet<Transform>();
    private int _activeSlotCount;

    public int Capacity => _activeSlotCount;
    public int OccupiedCount => _reservedSlots.Count;

    [Inject]
    public void Construct(LevelLayout levelLayout)
    {
        _levelLayout = levelLayout;
    }

    private void Awake()
    {
        CacheInitialScales();
    }

    private void Start()
    {
        ApplyBenchCapacity(_levelLayout != null ? _levelLayout.benchCapacity : 0, animateOnStart);
    }

    private void CacheInitialScales()
    {
        _initialScales.Clear();
        for (int i = 0; i < benchSlots.Count; i++)
        {
            _initialScales.Add(benchSlots[i] != null ? benchSlots[i].localScale : Vector3.one);
        }
    }

    public void ApplyBenchCapacity(int capacity, bool animate)
    {
        int activeCount = Mathf.Clamp(capacity, 0, benchSlots.Count);
        _activeSlotCount = activeCount;
        _reservedSlots.RemoveWhere(t => t == null);

        for (int i = 0; i < benchSlots.Count; i++)
        {
            var slot = benchSlots[i];
            if (slot == null)
            {
                continue;
            }

            bool shouldBeActive = i < activeCount;
            slot.gameObject.SetActive(shouldBeActive);

            if (!shouldBeActive)
            {
                _reservedSlots.Remove(slot);
                continue;
            }

            if (animate)
            {
                Vector3 targetScale = i < _initialScales.Count ? _initialScales[i] : slot.localScale;
                slot.localScale = Vector3.zero;
                slot.DOScale(targetScale, spawnScaleDuration)
                    .SetEase(Ease.OutBack)
                    .SetDelay(i * spawnStagger)
                    .SetLink(slot.gameObject, LinkBehaviour.KillOnDestroy);
            }
        }
    }

    public bool TryReserveSlot(out Transform slot)
    {
        _reservedSlots.RemoveWhere(t => t == null);

        for (int i = 0; i < _activeSlotCount && i < benchSlots.Count; i++)
        {
            var s = benchSlots[i];
            if (s == null || !s.gameObject.activeInHierarchy)
            {
                continue;
            }

            if (_reservedSlots.Contains(s))
            {
                continue;
            }

            _reservedSlots.Add(s);
            slot = s;
            return true;
        }

        slot = null;
        return false;
    }

    public void ReleaseSlot(Transform slot)
    {
        if (slot == null)
        {
            return;
        }

        _reservedSlots.Remove(slot);
    }
}

