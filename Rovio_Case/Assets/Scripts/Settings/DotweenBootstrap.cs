using DG.Tweening;
using UnityEngine;

/// <summary>
/// DOTween için başlangıç kapasitesini ayarlar.
/// Sahnedeki herhangi bir GameObject'e eklenebilir (tercihen kalıcı bootstrap objesi).
/// </summary>
public class DotweenBootstrap : MonoBehaviour
{
    [Header("Tweens Capacity")]
    [SerializeField] private int tweenersCapacity = 500;
    [SerializeField] private int sequencesCapacity = 100;

    private void Awake()
    {
        DOTween.SetTweensCapacity(tweenersCapacity, sequencesCapacity);
    }
}

