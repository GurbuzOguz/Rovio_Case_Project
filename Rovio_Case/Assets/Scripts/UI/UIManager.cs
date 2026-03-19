using System.Text;
using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Zenject;

[DisallowMultipleComponent]
public class UIManager : MonoBehaviour
{
    [Header("HUD")]
    [Tooltip("Örn: 'Level 1/3'")]
    public TMP_Text levelText;

    [Tooltip("Örn: 'Remaining: 0:12 1:8 2:4'")]
    public TMP_Text remainingText;

    [Header("End Screen")]
    public GameObject endScreenRoot;
    public TMP_Text endTitleText;
    public Button retryButton;
    public Button nextButton;

    [Header("Options")]
    public bool autoRefreshRemainingEachFrame = true;
    [SerializeField] private float uiClickActionDelay = 0.08f;

    private IGridService _gridService;
    private IGameStateService _gameState;
    private ILevelFlowService _levelFlow;
    private ISfxService _sfxService;

    [Inject]
    public void Construct(
        IGridService gridService,
        IGameStateService gameState,
        ILevelFlowService levelFlow,
        [InjectOptional] ISfxService sfxService)
    {
        _gridService = gridService;
        _gameState = gameState;
        _levelFlow = levelFlow;
        _sfxService = sfxService;
    }

    private void Awake()
    {
        if (retryButton != null)
        {
            retryButton.onClick.RemoveListener(OnRetryClicked);
            retryButton.onClick.AddListener(OnRetryClicked);
        }

        if (nextButton != null)
        {
            nextButton.onClick.RemoveListener(OnNextClicked);
            nextButton.onClick.AddListener(OnNextClicked);
        }
    }

    private void OnEnable()
    {
        if (_gameState != null)
        {
            _gameState.StateChanged += HandleStateChanged;
        }

        RefreshLevelText();
        RefreshRemainingText();
        HandleStateChanged(_gameState != null ? _gameState.State : GameRunState.Playing);
    }

    private void OnDisable()
    {
        if (_gameState != null)
        {
            _gameState.StateChanged -= HandleStateChanged;
        }
    }

    private void Update()
    {
        if (autoRefreshRemainingEachFrame)
        {
            RefreshRemainingText();
        }
    }

    public void RefreshLevelText()
    {
        if (levelText == null || _levelFlow == null)
        {
            return;
        }

        levelText.text = $"Level {_levelFlow.CurrentLevelIndex + 1}/{Mathf.Max(1, _levelFlow.LevelCount)}";
    }

    public void RefreshRemainingText()
    {
        if (remainingText == null || _gridService == null)
        {
            return;
        }

        var remaining = _gridService.GetRemainingCountsByColorId();
        if (remaining == null)
        {
            remainingText.text = "";
            return;
        }

        var sb = new StringBuilder(128);
        sb.Append("Remaining: ");
        foreach (var kv in remaining)
        {
            sb.Append(kv.Key);
            sb.Append(":");
            sb.Append(kv.Value);
            sb.Append("  ");
        }

        remainingText.text = sb.ToString();
    }

    private void HandleStateChanged(GameRunState state)
    {
        if (endScreenRoot != null)
        {
            endScreenRoot.SetActive(state == GameRunState.LevelComplete || state == GameRunState.LevelFail);
        }

        if (endTitleText != null)
        {
            endTitleText.text = state == GameRunState.LevelComplete ? "LEVEL COMPLETE" :
                state == GameRunState.LevelFail ? "LEVEL FAIL" : "";
        }

        if (nextButton != null && _levelFlow != null)
        {
            nextButton.gameObject.SetActive(state == GameRunState.LevelComplete && _levelFlow.HasNextLevel);
        }
    }

    private void OnRetryClicked()
    {
        _sfxService?.Play(SfxId.UiClick);
        StartCoroutine(RunAfterUiClickDelay(() => _levelFlow?.RestartLevel()));
    }

    private void OnNextClicked()
    {
        _sfxService?.Play(SfxId.UiClick);
        StartCoroutine(RunAfterUiClickDelay(() => _levelFlow?.LoadNextLevel()));
    }

    private IEnumerator RunAfterUiClickDelay(System.Action action)
    {
        if (uiClickActionDelay > 0f)
        {
            yield return new WaitForSeconds(uiClickActionDelay);
        }

        action?.Invoke();
    }
}

