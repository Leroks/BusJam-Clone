using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameHud : MonoBehaviour
{
    [SerializeField] private Image timerBar;
        [SerializeField] private TMP_Text timerText;

        private TimerService _timer;

        private void Awake()
        {
            _timer = Bootstrap.Timer;
            _timer.OnTick += UpdateUI;

            Bootstrap.StateMachine.OnStateChanged += HandleState;
            HandleState(Bootstrap.StateMachine.Current);
        }

        private void HandleState(GameState state)
        {
            gameObject.SetActive(state == GameState.Playing);
        }

        private void UpdateUI(float remaining)
        {
            timerBar.fillAmount = remaining / _fullDuration;
            timerText.text = $"{remaining:0}s";
        }

        private float _fullDuration;
        private void Start() => _fullDuration = _timer.Remaining;

        private void OnDestroy()
        {
            _timer.OnTick -= UpdateUI;
            Bootstrap.StateMachine.OnStateChanged -= HandleState;
        }
}