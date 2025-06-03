using UnityEngine;
using System.Linq;

public class GameManager : MonoBehaviour
{
    private static GameManager _instance;
    
    public static GameStateMachine StateMachine { get; private set; }
    public static PoolService Pools { get; private set; }
    public static InputService Input { get; private set; }
    public static TimerService Timer { get; private set; }
    public static LevelService Levels { get; private set; }
    
    [SerializeField] private GameObject passengerPrefab;
    [SerializeField] private GameObject busPrefab;
    [SerializeField] private GameObject passengerGridCellPrefab;
    [SerializeField] private Transform passengerGridOrigin;
    [SerializeField] private Transform[] queueSlotTransforms = new Transform[6];
    [SerializeField] private Transform busStopTransform;
    
    private PassengerManager _passengerManager;
    private BusManager _busManager;

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
            
            // Initialize Pools first as Managers might use it in constructor
            Pools = new PoolService(); 

            // Initialize Managers
            // BusManager needs to be initialized before PassengerManager if PassengerManager needs a reference to it.
            _busManager = new BusManager(Pools, busPrefab, busStopTransform);
            // Updated PassengerManager instantiation
            _passengerManager = new PassengerManager(Pools, passengerPrefab, passengerGridCellPrefab, passengerGridOrigin, queueSlotTransforms, _busManager, Input, StateMachine); 
            _busManager.OnAllBusesDeparted += CheckLevelWinCondition;

            StateMachine.ChangeState(GameState.Menu);
            
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
                    Debug.Log("Entered Menu State. Ready for tap to start.");
                    // TODO: Ensure Menu UI is visible and Game UI is hidden here.
                    if (Input != null)
                    {
                        Input.OnTapWorld += StartGameOnTap; 
                        SetGameplayInputActive(false);
                    }
                    break;
                case GameState.Playing:
                    // TODO: Ensure Game UI is visible and Menu UI is hidden here.
                    if (Input != null)
                    {
                        Input.OnTapWorld -= StartGameOnTap;
                        SetGameplayInputActive(true);
                    }
                    _passengerManager.ClearQueue(); // Use PassengerManager's ClearQueue
                    LevelData currentLevelData = Levels.GetCurrentLevelData();
                    if (currentLevelData != null)
                    {
                        Timer.Start(currentLevelData.timerDuration);
                        _passengerManager.SpawnPassengersForLevel(currentLevelData);
                        _busManager.SpawnBusesForLevel(currentLevelData);
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
                    if (Input != null)
                    {
                        SetGameplayInputActive(false);
                    }
                    _passengerManager.ClearQueue();
                    DespawnAllPassengers();
                    DespawnAllBuses();
                    if (Input != null)
                    {
                        Input.OnTapWorld -= StartGameOnTap; 
                    }
                    if (state == GameState.Complete)
                    {
                        Levels.AdvanceToNextLevel();
                    }
                    StateMachine.ChangeState(GameState.Menu);
                    break;
            }
        }
        
        private void SpawnPassengersForLevel(LevelData levelData)
        {
            _passengerManager.SpawnPassengersForLevel(levelData);
        }

        private void DespawnAllPassengers()
        {
            _passengerManager.DespawnAllPassengers();
        }
        
        private void SpawnBusesForLevel(LevelData levelData)
        {
            _busManager.SpawnBusesForLevel(levelData);
        }

        private void DespawnAllBuses()
        {
            _busManager.DespawnAllBuses();
        }

        private void SetGameplayInputActive(bool isActive)
        {
            // This is a placeholder if we decide to register/unregister events in HandleState
            // For now, the handlers themselves will check StateMachine.Current == GameState.Playing
        }
        
        // ClearQueue, HandlePassengerTap, FindBusForPassenger, FindEmptyQueueSlot, DespawnSinglePassenger
        // have been moved to PassengerManager.cs

        private void CheckLevelWinCondition()
        {
            // This event is now also triggered by BusManager.OnAllBusesDeparted
            // We need to ensure this check is robust for both timer end and bus departure scenarios.
            if (_passengerManager.ActivePassengerCount == 0 && _passengerManager.IsQueueEmpty && 
                _busManager.DepartedBusCount >= _busManager.InitialBusCountForLevel && _busManager.InitialBusCountForLevel > 0)
            {
                Debug.Log("Level Complete! All passengers boarded and buses departed.");
                if(StateMachine.Current == GameState.Playing) StateMachine.ChangeState(GameState.Complete);
            }
            else if (_busManager.AreAllBusesGone && (_passengerManager.ActivePassengerCount > 0 || !_passengerManager.IsQueueEmpty))
            {
                // No more buses, but passengers remain
                Debug.Log("Level Failed! No more buses available for remaining passengers.");
                 if(StateMachine.Current == GameState.Playing) StateMachine.ChangeState(GameState.Fail);
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
                Input.OnTapWorld -= StartGameOnTap;
                Input.Dispose();
            }
            if (_passengerManager != null)
            {
                _passengerManager.Dispose();
            }
            if (Timer != null)
            {
                Timer.OnFinished -= HandleTimerFinished;
                Timer.Dispose();
            }
        }
    }
