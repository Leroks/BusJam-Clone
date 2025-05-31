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
            // Register passenger pool, but don't spawn dummies here.
            // Ensure passengerPrefab is assigned in the Inspector and has the Passenger component.
            if (passengerPrefab != null)
            {
                Pools.RegisterPool("passenger", passengerPrefab.GetComponent<Transform>(), 50); // Increased pool size
            }
            else
            {
                Debug.LogError("Passenger Prefab not assigned in Bootstrap!");
            }
            
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
                    if (currentLevelData != null)
                    {
                        Timer.Start(currentLevelData.timerDuration);
                        SpawnPassengersForLevel(currentLevelData);
                    }
                    else
                    {
                        Debug.LogError("Cannot start game: CurrentLevelData is null.");
                        // Optionally, transition to an error state or back to menu
                        StateMachine.ChangeState(GameState.Menu); 
                    }
                    break;
                case GameState.Fail:
                case GameState.Complete:
                    DespawnAllPassengers();
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

        private System.Collections.Generic.List<Transform> _activePassengers = new System.Collections.Generic.List<Transform>();

        private void SpawnPassengersForLevel(LevelData levelData)
        {
            DespawnAllPassengers();

            if (passengerPrefab == null || Pools == null)
            {
                Debug.LogError("Passenger prefab or PoolService is not initialized.");
                return;
            }

            var passengerPool = Pools.Get<Transform>("passenger"); // Corrected: Use Get<Transform>
            if (passengerPool == null)
            {
                Debug.LogError("Passenger pool 'passenger' (Transform) not found. Make sure it's registered correctly.");
                if (passengerPrefab != null)
                {
                    passengerPool = Pools.RegisterPool("passenger", passengerPrefab.GetComponent<Transform>(), 50);
                }
                
                if(passengerPool == null) 
                {
                    Debug.LogError("Failed to get or re-register passenger pool. Aborting passenger spawn.");
                    return;
                }
            }

            foreach (var spawnData in levelData.passengerSpawns)
            {
                Transform passengerInstance = passengerPool.Spawn();
                if (passengerInstance != null)
                {
                    passengerInstance.gameObject.SetActive(true);
                    passengerInstance.position = spawnData.position;
                    
                    Passenger passengerComponent = passengerInstance.GetComponent<Passenger>();
                    if (passengerComponent != null)
                    {
                        passengerComponent.Initialize(spawnData.color);
                    }
                    else
                    {
                        Debug.LogError("Spawned passenger does not have a Passenger component!");
                    }
                    _activePassengers.Add(passengerInstance);
                }
            }
        }

        private void DespawnAllPassengers()
        {
            var passengerPool = Pools?.Get<Transform>("passenger");
            if (passengerPool != null)
            {
                foreach (var passengerInstance in _activePassengers)
                {
                    if(passengerInstance != null)
                    {
                        passengerPool.Despawn(passengerInstance);
                    }
                }
            }
            _activePassengers.Clear();
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
