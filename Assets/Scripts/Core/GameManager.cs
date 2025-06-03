using UnityEngine;
using System.Collections;

public class GameManager : MonoBehaviour
{
    private static GameManager _instance;
    
    public static GameStateMachine StateMachine { get; private set; }
    public static PoolService Pools { get; private set; }
    public static InputService Input { get; private set; }
    public static TimerService Timer { get; private set; }
    public static LevelService Levels { get; private set; }
    public static SaveLoadService SaveLoad { get; private set; }
    
    [SerializeField] private GameObject passengerPrefab;
    [SerializeField] private GameObject busPrefab;
    [SerializeField] private GameObject passengerGridCellPrefab;
    [SerializeField] private Transform passengerGridOrigin;
    [SerializeField] private Transform[] queueSlotTransforms = new Transform[6];
    [SerializeField] private Transform busStopTransform;
    
    private PassengerManager _passengerManager;
    private BusManager _busManager;
    private GameHud _gameHud;

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
            SaveLoad = new SaveLoadService();
            StateMachine.OnStateChanged += HandleState;
            
            Pools = new PoolService(); 
            
            _busManager = new BusManager(Pools, busPrefab, busStopTransform);
            _passengerManager = new PassengerManager(Pools, passengerPrefab, passengerGridCellPrefab, passengerGridOrigin, queueSlotTransforms, _busManager, Input, StateMachine); 
            _busManager.OnAllBusesDeparted += CheckLevelWinCondition;

            _gameHud = FindObjectOfType<GameHud>();
            StateMachine.ChangeState(GameState.Menu);
            
            Timer = new TimerService();
            Timer.OnFinished += HandleTimerFinished;
        }
        
        private void Update()
        {
            Input?.Tick();
            
            Timer.Tick(Time.deltaTime);
        }

        private void HandleState(GameState state)
        {
            switch (state)
            {
                case GameState.Menu:
                    Debug.Log("Menu State tap to start.");
                    Input.OnTapWorld += StartGameOnTap; 
                    break;
                case GameState.Playing:
                    Input.OnTapWorld -= StartGameOnTap;
                    _passengerManager.ClearQueue(); 

                    GameSaveData saveData = SaveLoad.LoadGame();
                    LevelData levelToLoad;
                    float timeToLoad = 0f;

                    if (saveData != null && saveData.isInProgress)
                    {
                        Debug.Log("Resuming game from saved state.");
                        Levels.SetCurrentLevel(saveData.currentLevelIndex);
                        levelToLoad = Levels.GetCurrentLevelData();
                        timeToLoad = saveData.remainingTime;
                        saveData.isInProgress = false; 
                        SaveLoad.SaveGame(saveData);
                    }
                    else
                    {
                        Debug.Log("Starting new level.");
                        levelToLoad = Levels.GetCurrentLevelData();
                        timeToLoad = levelToLoad.timerDuration;
                    }
                    
                    if (levelToLoad != null)
                    {
                        Timer.Start(timeToLoad);
                        _passengerManager.SpawnPassengersForLevel(levelToLoad);
                        _busManager.SpawnBusesForLevel(levelToLoad);
                    }
                    else
                    {
                        Debug.LogError("Cannot start game: CurrentLevelData is null.");
                        StateMachine.ChangeState(GameState.Menu); 
                    }
                    break;
                case GameState.Fail:
                case GameState.Complete:
                    _passengerManager.ClearQueue();
                    DespawnAllPassengers();
                    DespawnAllBuses();

                    Input.OnTapWorld -= StartGameOnTap; 

                    GameSaveData endGameState = SaveLoad.LoadGame();
                    if (endGameState == null) endGameState = new GameSaveData();
                    endGameState.isInProgress = false;
                    endGameState.currentLevelIndex = Levels.CurrentLevelIndex;
                    SaveLoad.SaveGame(endGameState);

                    if (state == GameState.Complete)
                    {
                        Levels.AdvanceToNextLevel();
                    }
                    StartCoroutine(EndLevelSequence(state));
                    break;
            }
        }

        private IEnumerator EndLevelSequence(GameState endState)
        {
            yield return new WaitForSeconds(2.5f);
            
            StateMachine.ChangeState(GameState.Menu);
        }

        private void SaveGameProgress()
        {
            if (StateMachine.Current == GameState.Playing)
            {
                GameSaveData saveData = new GameSaveData
                {
                    currentLevelIndex = Levels.CurrentLevelIndex,
                    remainingTime = Timer.Remaining,
                    isInProgress = true
                    // TODO: Add passenger and bus states
                };
                SaveLoad.SaveGame(saveData);
                Debug.Log("Game progress saved.");
            }
        }

        private void DespawnAllPassengers()
        {
            _passengerManager.DespawnAllPassengers();
        }

        private void DespawnAllBuses()
        {
            _busManager.DespawnAllBuses();
        }

        private void CheckLevelWinCondition()
        {
            if (_passengerManager.ActivePassengerCount == 0 && _passengerManager.IsQueueEmpty && 
                _busManager.DepartedBusCount >= _busManager.InitialBusCountForLevel && _busManager.InitialBusCountForLevel > 0)
            {
                Debug.Log("Level Complete!");
                if(StateMachine.Current == GameState.Playing) StateMachine.ChangeState(GameState.Complete);
            }
        }

        private void StartGameOnTap(Vector3 position)
        {
            if (StateMachine.Current == GameState.Menu)
            {
                if (_gameHud != null)
                {
                    _gameHud.HideTapToStart();
                }
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

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                SaveGameProgress();
            }
        }

        private void OnApplicationQuit()
        {
            SaveGameProgress();
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