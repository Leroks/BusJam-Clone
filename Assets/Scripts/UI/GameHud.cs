using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class GameHud : MonoBehaviour
{
    [SerializeField] private Image timerBar;
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private TMP_Text tapToStartText;
    [SerializeField] private TMP_Text winText;
    [SerializeField] private TMP_Text loseText;

    private TimerService _timer;
    private float _fullDuration;

    private void Awake()
    {
        _timer = GameManager.Timer;
        if (_timer != null)
        {
            _timer.OnTick += UpdateUI;
        }

        if (GameManager.StateMachine != null)
        {
            GameManager.StateMachine.OnStateChanged += HandleState;
            HandleState(GameManager.StateMachine.Current);
        }
    }

    private void HandleState(GameState state)
    {
        tapToStartText.DOKill();
        tapToStartText.transform.localScale = Vector3.one;
        tapToStartText.gameObject.SetActive(false);

        winText.DOKill();
        winText.transform.localScale = Vector3.one;
        winText.gameObject.SetActive(false);

        loseText.DOKill();
        loseText.transform.localScale = Vector3.one;
        loseText.gameObject.SetActive(false);
        
        timerBar.gameObject.SetActive(false);
        timerText.gameObject.SetActive(false);
        levelText.gameObject.SetActive(false);
        gameObject.SetActive(true);

        switch (state)
        {
            case GameState.Menu:
                tapToStartText.gameObject.SetActive(true);
                tapToStartText.transform.DOScale(1.1f, 0.75f).SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo);
                levelText.gameObject.SetActive(true);
                levelText.text = $"LEVEL {GameManager.Levels.CurrentLevelIndex + 1}";
                break;
            case GameState.Playing:
                timerBar.gameObject.SetActive(true);
                timerText.gameObject.SetActive(true);
                levelText.gameObject.SetActive(true);
                levelText.text = $"LEVEL {GameManager.Levels.CurrentLevelIndex + 1}";
                LevelData currentLevelData = GameManager.Levels?.GetCurrentLevelData();
                _fullDuration = currentLevelData.timerDuration;
                break;
            case GameState.Complete:
                winText.gameObject.SetActive(true);
                winText.transform.localScale = Vector3.zero;
                Sequence winSequence = DOTween.Sequence();
                winSequence.Append(winText.transform.DOScale(1.2f, 0.3f).SetEase(Ease.OutBack));
                winSequence.Append(winText.transform.DOScale(1.0f, 0.2f).SetEase(Ease.OutSine));
                levelText.gameObject.SetActive(true);
                levelText.text = $"LEVEL {GameManager.Levels.CurrentLevelIndex + 1}";
                break;
            case GameState.Fail:
                loseText.gameObject.SetActive(true);
                loseText.transform.localScale = Vector3.zero;
                Sequence loseSequence = DOTween.Sequence();
                loseSequence.Append(loseText.transform.DOScale(1.2f, 0.3f).SetEase(Ease.OutBack));
                loseSequence.Append(loseText.transform.DOScale(1.0f, 0.2f).SetEase(Ease.OutSine));
                levelText.gameObject.SetActive(true);
                levelText.text = $"LEVEL {GameManager.Levels.CurrentLevelIndex + 1}";
                break;
        }
    }

    public void HideTapToStart()
    {
        tapToStartText.DOKill();
        tapToStartText.transform.localScale = Vector3.one;
        tapToStartText.gameObject.SetActive(false);
    }

    private void UpdateUI(float remaining)
    {
        if (_fullDuration > 0)
        {
            timerBar.fillAmount = remaining / _fullDuration;
        }
        else
        {
            timerBar.fillAmount = 0;
        }
        timerText.text = $"Remaining {remaining:0}s";
    }

    private void OnDestroy()
    {
        _timer.OnTick -= UpdateUI;
        GameManager.StateMachine.OnStateChanged -= HandleState;
        
        tapToStartText.DOKill();
        winText.DOKill();
        loseText.DOKill();
    }
}