using UnityEngine;
using UnityEngine.SceneManagement;

public class Bootstrap : MonoBehaviour
{
    private static Bootstrap _instance;
    
    public static GameStateMachine StateMachine { get; private set; }
    public static PoolService Pools { get; private set; }
    public static InputService Input { get; private set; }
    public static TimerService Timer { get; private set; }
    public static LevelService Levels { get; private set; }
    
    [SerializeField] private GameObject passengerPrefab;

        private void Awake()
        {
            if (_instance != null)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);

            Input = new InputService();
            StateMachine = new GameStateMachine();
            Levels = new LevelService();
            StateMachine.OnStateChanged += HandleState;

            StateMachine.ChangeState(GameState.Menu);
            
            Pools = new PoolService();
            
            var cubePool = Pools.RegisterPool("passengerDummy", passengerPrefab.GetComponent<Transform>(), 20);
            for (int i = 0; i < 30; i++)
                cubePool.Spawn().transform.position = Random.insideUnitCircle * 3f;
            
            Timer = new TimerService();
            Timer.OnFinished += HandleTimerFinished;
        }
        
        private void Update()
        {
            Input?.Tick();
            
            Timer.Tick(UnityEngine.Time.deltaTime);
        }

        private void HandleState(GameState state)
        {
            switch (state)
            {
                case GameState.Menu:
                    SceneManager.LoadScene("Game");
                    if (Input != null)
                    {
                        Input.OnTap += StartGameOnTap;
                    }
                    break;
                case GameState.Playing:
                    if (Input != null)
                    {
                        Input.OnTap -= StartGameOnTap;
                    }
                    LevelData currentLevelData = Levels.GetCurrentLevelData();
                    Timer.Start(currentLevelData.timerDuration);
                    break;
                case GameState.Fail:
                case GameState.Complete:
                    if (Input != null)
                    {
                        Input.OnTap -= StartGameOnTap;
                    }
                    if (state == GameState.Complete)
                    {
                        Levels.AdvanceToNextLevel();
                    }
                    StateMachine.ChangeState(GameState.Menu);
                    break;
            }
        }

        private void StartGameOnTap(Vector3 position)
        {
            if (StateMachine.Current == GameState.Menu)
            {
                StateMachine.ChangeState(GameState.Playing);
            }
        }

        private void HandleTimerFinished()
        {
            if (StateMachine.Current == GameState.Playing)
            {
                StateMachine.ChangeState(GameState.Fail);
            }
        }
        
        private void OnDestroy()
        {
            if (StateMachine != null)
            {
                StateMachine.OnStateChanged -= HandleState;
            }
            if (Input != null)
            {
                Input.OnTap -= StartGameOnTap;
                Input.Dispose();
            }
            if (Timer != null)
            {
                Timer.OnFinished -= HandleTimerFinished;
                Timer.Dispose();
            }
        }
    }
