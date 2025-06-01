using UnityEngine;
using System.Linq;

public class Bootstrap : MonoBehaviour
{
    private static Bootstrap _instance;
    
    public static GameStateMachine StateMachine { get; private set; }
    public static PoolService Pools { get; private set; }
    public static InputService Input { get; private set; }
    public static TimerService Timer { get; private set; }
    public static LevelService Levels { get; private set; }
    
    [SerializeField] private GameObject passengerPrefab;
    [SerializeField] private GameObject busPrefab;
    [SerializeField] private Transform[] queueSlotTransforms = new Transform[6]; // For queue visuals/positions
    [SerializeField] private Transform busStopTransform; // Assign in Inspector: where the active bus waits

    private Passenger[] _queueSlots = new Passenger[6]; // Tracks passengers in queue slots
    
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
            _passengerManager = new PassengerManager(Pools, passengerPrefab);
            _busManager = new BusManager(Pools, busPrefab, busStopTransform);
            _busManager.OnAllBusesDeparted += CheckLevelWinCondition;
            
            Input.OnPassengerTap += HandlePassengerTap;

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
                    ClearQueue();
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
                    ClearQueue();
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

        private void ClearQueue()
        {
            for (int i = 0; i < _queueSlots.Length; i++)
            {
                if (_queueSlots[i] != null)
                {
                    // If passengers in queue are from the main pool, despawn them
                    // For now, assume they are managed and just clear the reference
                    // This needs careful handling if queue passengers are also in _activePassengers
                    // Let's assume for now that if a passenger is in queue, it's not in _activePassengers.
                    // And when they move from _activePassengers to queue, they are removed from _activePassengers.
                    
                    // If they are pooled objects, they should be despawned.
                    var passengerPool = Pools?.Get<Transform>("passenger");
                    if (passengerPool != null)
                    {
                        passengerPool.Despawn(_queueSlots[i].transform);
                    }
                    else
                    {
                        _queueSlots[i].gameObject.SetActive(false); // Fallback
                    }
                    _queueSlots[i] = null;
                }
            }
        }
        
        private void HandlePassengerTap(Passenger tappedPassenger)
        {
            if (StateMachine.Current != GameState.Playing || tappedPassenger == null) return;

            // Check if passenger is from queue or general spawn
            int queueIndex = -1;
            for (int i = 0; i < _queueSlots.Length; i++)
            {
                if (_queueSlots[i] == tappedPassenger)
                {
                    queueIndex = i;
                    break;
                }
            }

            // Try to find a suitable bus
            Bus targetBus = FindBusForPassenger(tappedPassenger);

            if (targetBus != null)
            {
                // Passenger can board a bus
                if (targetBus.AddPassenger(tappedPassenger))
                {
                    Debug.Log($"Passenger {tappedPassenger.name} boarding bus {targetBus.name}");
                    // Remove passenger from its current location (either _activePassengers or _queueSlots)
                    if (queueIndex != -1)
                    {
                        _queueSlots[queueIndex] = null; // Clear from queue slot
                        _passengerManager.DespawnSinglePassenger(tappedPassenger);
                    }
                    // Check if it was an active passenger (not from queue)
                    else if (_passengerManager.IsPassengerActive(tappedPassenger)) 
                    {
                        // _passengerManager.RemoveFromActiveList(tappedPassenger); // DespawnSinglePassenger will also remove
                        _passengerManager.DespawnSinglePassenger(tappedPassenger);
                    }
                }
            }
            else
            {
                // No suitable bus, try to move to queue (if not already in queue and trying to move again)
                if (queueIndex != -1)
                {
                    Debug.Log($"Tapped passenger {tappedPassenger.name} from queue, but no bus available. Stays in queue.");
                    return; // Already in queue, no bus, do nothing more.
                }

                int emptyQueueSlot = FindEmptyQueueSlot();
                if (emptyQueueSlot != -1)
                {
                    Debug.Log($"Passenger {tappedPassenger.name} moving to queue slot {emptyQueueSlot}");
                    _queueSlots[emptyQueueSlot] = tappedPassenger;
                    tappedPassenger.transform.position = queueSlotTransforms[emptyQueueSlot].position; // Teleport for now
                    _passengerManager.RemoveFromActiveList(tappedPassenger); // It's no longer "active" in the spawn area
                }
                else
                {
                    Debug.Log($"Passenger {tappedPassenger.name} cannot board a bus and queue is full.");
                    // Optionally, provide player feedback (e.g., shake passenger, sound effect)
                }
            }
        }

        private Bus FindBusForPassenger(Passenger passenger)
        {
            if (_busManager.ActiveBus != null && _busManager.ActiveBus.gameObject.activeInHierarchy && _busManager.ActiveBus.CanBoard(passenger))
            {
                return _busManager.ActiveBus;
            }
            return null;
        }
        
        private void CheckLevelWinCondition()
        {
            // This event is now also triggered by BusManager.OnAllBusesDeparted
            // We need to ensure this check is robust for both timer end and bus departure scenarios.
            if (_passengerManager.ActivePassengerCount == 0 && _queueSlots.All(p => p == null) && 
                _busManager.DepartedBusCount >= _busManager.InitialBusCountForLevel && _busManager.InitialBusCountForLevel > 0)
            {
                Debug.Log("Level Complete! All passengers boarded and buses departed.");
                if(StateMachine.Current == GameState.Playing) StateMachine.ChangeState(GameState.Complete);
            }
            else if (_busManager.AreAllBusesGone && (_passengerManager.ActivePassengerCount > 0 || !_queueSlots.All(p=> p == null)))
            {
                // No more buses, but passengers remain
                Debug.Log("Level Failed! No more buses available for remaining passengers.");
                 if(StateMachine.Current == GameState.Playing) StateMachine.ChangeState(GameState.Fail);
            }
        }

        private int FindEmptyQueueSlot()
        {
            for (int i = 0; i < _queueSlots.Length; i++)
            {
                if (_queueSlots[i] == null)
                {
                    return i;
                }
            }
            return -1;
        }
        
        private void DespawnSinglePassenger(Passenger passengerToDespawn)
        {
            // This method is now effectively a wrapper if PassengerManager handles the actual despawn
            _passengerManager.DespawnSinglePassenger(passengerToDespawn);
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
                Input.OnPassengerTap -= HandlePassengerTap;
                Input.Dispose();
            }
            if (Timer != null)
            {
                Timer.OnFinished -= HandleTimerFinished;
                Timer.Dispose();
            }
        }
    }
