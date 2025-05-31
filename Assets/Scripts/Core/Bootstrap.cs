using Services.InputService;
using UnityEngine;
using UnityEngine.SceneManagement;
using Services.Pooling;
using Services.Timer;

namespace Core
{
    public class Bootstrap : MonoBehaviour
    {
        private static Bootstrap _instance;
        
        public static GameStateMachine StateMachine { get; private set; }
        public static PoolService Pools { get; private set; }
        public static IInputService Input { get; private set; }
        public static ITimerService Timer { get; private set; }
        
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

            Input = InputServiceFactory.Create();
        }
        
        private void Update()
        {
            (Input as MouseInputService)?.Tick();
            (Input as TouchInputService)?.Tick();
            
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
}