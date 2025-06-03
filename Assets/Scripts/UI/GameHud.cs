using TMPro;
using UnityEngine;
using UnityEngine.UI;

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
        if (timerBar != null) timerBar.gameObject.SetActive(false);
        if (timerText != null) timerText.gameObject.SetActive(false);
        if (levelText != null) levelText.gameObject.SetActive(false);
        if (tapToStartText != null) tapToStartText.gameObject.SetActive(false);
        if (winText != null) winText.gameObject.SetActive(false);
        if (loseText != null) loseText.gameObject.SetActive(false);

        gameObject.SetActive(true);

        switch (state)
        {
            case GameState.Menu:
                tapToStartText.gameObject.SetActive(true);
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
                levelText.gameObject.SetActive(true);
                levelText.text = $"LEVEL {GameManager.Levels.CurrentLevelIndex + 1}";
                break;
            case GameState.Fail:
                loseText.gameObject.SetActive(true);
                levelText.gameObject.SetActive(true);
                levelText.text = $"LEVEL {GameManager.Levels.CurrentLevelIndex + 1}";
                break;
        }
    }

    public void HideTapToStart()
    {
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
        if (_timer != null)
        {
            _timer.OnTick -= UpdateUI;
        }
        if (GameManager.StateMachine != null)
        {
            GameManager.StateMachine.OnStateChanged -= HandleState;
        }
    }
}