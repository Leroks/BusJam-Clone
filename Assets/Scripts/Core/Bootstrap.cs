using UnityEngine;
using UnityEngine.SceneManagement;

public class Bootstrap : MonoBehaviour
{
    private static Bootstrap _instance;
    
    public static GameStateMachine StateMachine { get; private set; }
    public static PoolService Pools { get; private set; }
    public static InputService Input { get; private set; }
    public static TimerService Timer { get; private set; }
    
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

            StateMachine = new GameStateMachine();
            StateMachine.OnStateChanged += HandleState;

            StateMachine.ChangeState(GameState.Menu);
            
            Pools = new PoolService();
            
            var cubePool = Pools.RegisterPool("passengerDummy", passengerPrefab.GetComponent<Transform>(), 20);
            for (int i = 0; i < 30; i++)
                cubePool.Spawn().transform.position = Random.insideUnitCircle * 3f;
            
            Timer = new TimerService();

            Input = new InputService();
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
                    break;
                case GameState.Playing:
                    Timer.Start(10f); // TODO: later level data
                    break;
            }
        }
        
        private void OnDestroy()
        {
            Input?.Dispose();
            Timer?.Dispose();
        }
    }