using System.Text;
using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Zenject;
#if DOTWEEN_EXISTS || true
using DG.Tweening;
#endif

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

    [Header("UI Intro Animation")]
    [SerializeField] private bool playIntroAnimations = true;
    [SerializeField] private float introDuration = 0.35f;
    [SerializeField] private float introStagger = 0.08f;
    [SerializeField] private float introOffsetX = 500f;
    [SerializeField] private Ease introEase = Ease.OutCubic;

    private RectTransform _levelRect;
    private RectTransform _remainingRect;
    private RectTransform _endScreenRect;
    private Vector2 _levelAnchorPos;
    private Vector2 _remainingAnchorPos;
    private Vector2 _endScreenAnchorPos;
    private bool _introPositionsCached;

    private IGridService _gridService;
    private IGameStateService _gameState;
    private ILevelFlowService _levelFlow;
    private ISfxService _sfxService;
    private IHapticService _hapticService;

    [Inject]
    public void Construct(
        IGridService gridService,
        IGameStateService gameState,
        ILevelFlowService levelFlow,
        [InjectOptional] ISfxService sfxService,
        [InjectOptional] IHapticService hapticService)
    {
        _gridService = gridService;
        _gameState = gameState;
        _levelFlow = levelFlow;
        _sfxService = sfxService;
        _hapticService = hapticService;
    }

    private void Awake()
    {
        CacheIntroTargets();

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

#if DOTWEEN_EXISTS || true
        if (playIntroAnimations)
        {
            PlayHudIntro();
        }
#endif

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
        bool showEndScreen = state == GameRunState.LevelComplete || state == GameRunState.LevelFail;
        if (endScreenRoot != null)
        {
            endScreenRoot.SetActive(showEndScreen);
        }

        if (endTitleText != null)
        {
            endTitleText.text = state == GameRunState.LevelComplete ? "LEVEL COMPLETE" :
                state == GameRunState.LevelFail ? "LEVEL FAIL" : "";
        }

#if DOTWEEN_EXISTS || true
        if (playIntroAnimations && showEndScreen)
        {
            PlayEndScreenIntro();
        }
#endif

        if (nextButton != null && _levelFlow != null)
        {
            nextButton.gameObject.SetActive(state == GameRunState.LevelComplete && _levelFlow.HasNextLevel);
        }
    }

    private void OnRetryClicked()
    {
        _sfxService?.Play(SfxId.UiClick);
        _hapticService?.Selection();
        StartCoroutine(RunAfterUiClickDelay(() => _levelFlow?.RestartLevel()));
    }

    private void OnNextClicked()
    {
        _sfxService?.Play(SfxId.UiClick);
        _hapticService?.Selection();
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

    private void CacheIntroTargets()
    {
        if (_introPositionsCached)
        {
            return;
        }

        _levelRect = levelText != null ? levelText.rectTransform : null;
        _remainingRect = remainingText != null ? remainingText.rectTransform : null;
        _endScreenRect = endScreenRoot != null ? endScreenRoot.GetComponent<RectTransform>() : null;

        if (_levelRect != null)
        {
            _levelAnchorPos = _levelRect.anchoredPosition;
        }

        if (_remainingRect != null)
        {
            _remainingAnchorPos = _remainingRect.anchoredPosition;
        }

        if (_endScreenRect != null)
        {
            _endScreenAnchorPos = _endScreenRect.anchoredPosition;
        }

        _introPositionsCached = true;
    }

#if DOTWEEN_EXISTS || true
    private void PlayHudIntro()
    {
        CacheIntroTargets();
        if (_levelRect != null)
        {
            PlaySlideIn(_levelRect, _levelAnchorPos, 0f);
        }

        if (_remainingRect != null)
        {
            PlaySlideIn(_remainingRect, _remainingAnchorPos, introStagger);
        }
    }

    private void PlayEndScreenIntro()
    {
        CacheIntroTargets();
        if (_endScreenRect == null)
        {
            return;
        }

        PlaySlideIn(_endScreenRect, _endScreenAnchorPos, 0f);
    }

    private void PlaySlideIn(RectTransform target, Vector2 finalPos, float delay)
    {
        if (target == null)
        {
            return;
        }

        target.DOKill(false);
        target.anchoredPosition = finalPos + Vector2.right * introOffsetX;
        target
            .DOAnchorPos(finalPos, introDuration)
            .SetEase(introEase)
            .SetDelay(delay)
            .SetLink(gameObject, LinkBehaviour.KillOnDisable);
    }
#endif
}

