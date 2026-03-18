using UnityEngine;
using Zenject;

public class EdgeGapFiller : MonoBehaviour
{
    [SerializeField] private float intervalSeconds = 0.25f;

    private IProductInteractionService _interaction;
    private float _timer;

    [Inject]
    public void Construct(IProductInteractionService interaction)
    {
        _interaction = interaction;
    }

    private void Update()
    {
        if (_interaction == null)
        {
            return;
        }

        _timer += Time.deltaTime;
        if (_timer < intervalSeconds)
        {
            return;
        }

        _timer = 0f;
        _interaction.TryFillEdgeGaps();
    }
}

