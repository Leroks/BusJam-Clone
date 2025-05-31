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
        _timer = Bootstrap.Timer;
        if (_timer != null)
        {
            _timer.OnTick += UpdateUI;
        }

        if (Bootstrap.StateMachine != null)
        {
            Bootstrap.StateMachine.OnStateChanged += HandleState;
            HandleState(Bootstrap.StateMachine.Current);
        }
    }

    private void HandleState(GameState state)
    {
        bool isPlaying = state == GameState.Playing;
        gameObject.SetActive(isPlaying);

        if (isPlaying)
        {
            if (levelText != null)
            {
                levelText.text = $"LEVEL {Bootstrap.CurrentLevel}";
            }
            if (_timer != null && _timer.IsRunning)
            {
                _fullDuration = _timer.Remaining; 
            }
            else if (_timer != null)
            {
                 _fullDuration = 10f;
            }
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
        timerText.text = $"{remaining:0}s";
    }

    private void OnDestroy()
    {
        if (_timer != null)
        {
            _timer.OnTick -= UpdateUI;
        }
        if (Bootstrap.StateMachine != null)
        {
            Bootstrap.StateMachine.OnStateChanged -= HandleState;
        }
    }
}