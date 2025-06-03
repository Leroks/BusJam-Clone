using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameHud : MonoBehaviour
{
    [SerializeField] private Image timerBar;
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private TMP_Text levelText;

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
        bool isPlaying = state == GameState.Playing;
        gameObject.SetActive(isPlaying);

        if (isPlaying)
        {
            if (levelText != null && GameManager.Levels != null)
            {
                levelText.text = $"LEVEL {GameManager.Levels.CurrentLevelNumber}";
            }

            LevelData currentLevelData = GameManager.Levels?.GetCurrentLevelData();
            _fullDuration = currentLevelData.timerDuration;
        }
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
