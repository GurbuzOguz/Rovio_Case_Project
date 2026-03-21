using DG.Tweening;
using UnityEngine;

// Sets initial DOTween capacities.
// Can be added to any scene GameObject (preferably a persistent bootstrap object).
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

