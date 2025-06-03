using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class PassengerManager
{
    private List<Transform> _activePassengerTransforms = new List<Transform>();
    private PoolService _poolService;
    private GameObject _passengerPrefab;
    private GameObject _gridCellPrefab3D;
    private Transform _gridOrigin;
    private List<Transform> _dynamicGridCellTransforms = new List<Transform>();
    private List<GameObject> _instantiatedGridCells = new List<GameObject>();

    private Transform[] _queueSlotTransforms;
    private Passenger[] _gridCellOccupants;
    private Passenger[] _queueSlots;
    private BusManager _busManager;
    private InputService _inputService;
    private GameStateMachine _stateMachine;


    public int ActivePassengerCount => _activePassengerTransforms.Count;
    public bool IsQueueEmpty => _queueSlots == null || _queueSlots.All(p => p == null);


    public PassengerManager(PoolService poolService, GameObject passengerPrefab, GameObject gridCellPrefab3D, Transform gridOrigin, Transform[] queueSlotTransforms, BusManager busManager, InputService inputService, GameStateMachine stateMachine)
    {
        _poolService = poolService;
        _passengerPrefab = passengerPrefab;
        _gridCellPrefab3D = gridCellPrefab3D;
        _gridOrigin = gridOrigin;
        _queueSlotTransforms = queueSlotTransforms;

        _gridCellOccupants = new Passenger[0]; 

        _busManager = busManager;
        _inputService = inputService;
        _stateMachine = stateMachine;

        if (_queueSlotTransforms != null)
        {
            _queueSlots = new Passenger[_queueSlotTransforms.Length];
        }
        else
        {
            _queueSlots = new Passenger[0];
        }
        
        _poolService.RegisterPool("passenger", _passengerPrefab.GetComponent<Transform>(), 20, "Passengers");
        
        _inputService.OnPassengerTap += HandlePassengerTap;
        
        _busManager.OnBusAtStopReadyForBoarding += AttemptAutoBoardQueuedPassengers;
    }

    public void Dispose()
    {
        _inputService.OnPassengerTap -= HandlePassengerTap;
        _busManager.OnBusAtStopReadyForBoarding -= AttemptAutoBoardQueuedPassengers;
    }

    private void AttemptAutoBoardQueuedPassengers(Bus busAtStop)
    {
        if (busAtStop == null || _queueSlots == null || _stateMachine.Current != GameState.Playing) return;

        Debug.Log($"PassengerManager: Bus {busAtStop.name} arrived. Checking queue for auto-boarding.");

        for (int i = 0; i < _queueSlots.Length; i++)
        {
            Passenger passengerInQueue = _queueSlots[i];
            if (passengerInQueue != null && !passengerInQueue.IsMoving && busAtStop.CanBoard(passengerInQueue))
            {
                _queueSlots[i] = null;

                Vector3 boardingPoint = busAtStop.transform.position;
                passengerInQueue.MoveToPosition(boardingPoint, () =>
                {
                    if (busAtStop.AddPassenger(passengerInQueue))
                    {
                        DespawnSinglePassenger(passengerInQueue);
                    }
                });

                if (busAtStop.IsFull)
                {
                    Debug.Log("Bus at stop full stop auto boarding");
                    break; 
                }
            }
        }
    }

    public void SpawnPassengersForLevel(LevelData levelData)
    {
        DespawnAllPassengers();
        CreateDynamicGrid(levelData);

        var passengerPool = _poolService.Get<Transform>("passenger");
        
        if (levelData.standardGridPassengers.Count > 0 && _dynamicGridCellTransforms.Count > 0)
        {
            for(int i = 0; i < _gridCellOccupants.Length; i++) _gridCellOccupants[i] = null;

            int passengersToSpawnOnGrid = Mathf.Min(levelData.standardGridPassengers.Count, _dynamicGridCellTransforms.Count);
            
            for (int i = 0; i < passengersToSpawnOnGrid; i++)
            {
                if (i >= _dynamicGridCellTransforms.Count || _dynamicGridCellTransforms[i] == null)
                {
                    continue;
                }
                if (i >= levelData.standardGridPassengers.Count)
                {
                    continue;
                }


                Transform passengerInstance = passengerPool.Spawn();
                if (passengerInstance != null)
                {
                    passengerInstance.gameObject.SetActive(true);
                    passengerInstance.position = _dynamicGridCellTransforms[i].position; 
                    passengerInstance.rotation = Quaternion.identity;

                    Passenger passengerComponent = passengerInstance.GetComponent<Passenger>();
                    if (passengerComponent != null)
                    {
                        passengerComponent.Initialize(levelData.standardGridPassengers[i]);
                        _activePassengerTransforms.Add(passengerInstance);
                        if (i < _gridCellOccupants.Length)
                        {
                            _gridCellOccupants[i] = passengerComponent;
                        } else
                        {
                            Debug.LogWarning("warning");
                        }
                    }
                }
            }
        }
    }
    
    private void CreateDynamicGrid(LevelData levelData)
    {
        foreach (GameObject oldCell in _instantiatedGridCells)
        {
            GameObject.Destroy(oldCell);
        }
        _instantiatedGridCells.Clear();
        _dynamicGridCellTransforms.Clear();

        int gridWidth = levelData.gridWidth;
        int gridHeight = levelData.gridHeight;
        _gridCellOccupants = new Passenger[gridWidth * gridHeight];

        float cellSpacingX = 0.65f;
        float cellSpacingZ = 0.65f;

        // starting offset to center the grid
        float startX = _gridOrigin != null ? _gridOrigin.position.x : 0f;
        float startY = _gridOrigin != null ? _gridOrigin.position.y : 0f;
        float startZ = _gridOrigin != null ? _gridOrigin.position.z : 0f;

        // Centering the grid
        startX -= (gridWidth - 1) * cellSpacingX / 2.0f;
        startZ -= (gridHeight - 1) * cellSpacingZ / 2.0f;

        for (int r = 0; r < gridHeight; r++) // Rows
        {
            for (int c = 0; c < gridWidth; c++) // Columns
            {
                Vector3 cellPosition = new Vector3(
                    startX + c * cellSpacingX,
                    startY,
                    startZ + r * cellSpacingZ
                );

                GameObject cellInstance = GameObject.Instantiate(_gridCellPrefab3D, cellPosition, Quaternion.identity);
                if (_gridOrigin != null)
                {
                    cellInstance.transform.SetParent(_gridOrigin, true);
                }
                
                _instantiatedGridCells.Add(cellInstance);
                _dynamicGridCellTransforms.Add(cellInstance.transform);
            }
        }
        Debug.Log($"grid created: {gridWidth}x{gridHeight} with {_dynamicGridCellTransforms.Count} cells.");
    }

    private void ClearPassengerFromGrid(Passenger passenger)
    {
        if (passenger == null || _gridCellOccupants == null) return;
        for (int i = 0; i < _gridCellOccupants.Length; i++)
        {
            if (_gridCellOccupants[i] == passenger)
            {
                _gridCellOccupants[i] = null;
                return;
            }
        }
    }
    
    public void DespawnSinglePassenger(Passenger passengerToDespawn)
    {
        if (passengerToDespawn == null) return;

        ClearPassengerFromGrid(passengerToDespawn);

        Transform passengerTransform = passengerToDespawn.transform;
        if (_activePassengerTransforms.Contains(passengerTransform))
        {
            _activePassengerTransforms.Remove(passengerTransform);
        }
        
        var passengerPool = _poolService.Get<Transform>("passenger");
        if (passengerPool != null)
        {
            passengerPool.Despawn(passengerTransform);
        }
        else
        {
            passengerToDespawn.gameObject.SetActive(false);
        }
    }
    
    public bool IsPassengerActive(Passenger passenger)
    {
        return passenger != null && _activePassengerTransforms.Contains(passenger.transform);
    }

    public void DespawnAllPassengers()
    {
        List<Transform> passengersToDespawnTransforms = new List<Transform>(_activePassengerTransforms);

        foreach (var passengerTransform in passengersToDespawnTransforms)
        {
            if (passengerTransform != null)
            {
                Passenger passengerComponent = passengerTransform.GetComponent<Passenger>();
                DespawnSinglePassenger(passengerComponent);
            }
        }
        
        _activePassengerTransforms.Clear();

        ClearQueue();

        if (_gridCellOccupants != null)
        {
            for (int i = 0; i < _gridCellOccupants.Length; i++)
            {
                _gridCellOccupants[i] = null;
            }
        }
    }
    
    private void HandlePassengerTap(Passenger tappedPassenger)
    {
        if (_stateMachine.Current != GameState.Playing || tappedPassenger == null || _busManager == null || tappedPassenger.IsMoving) return;

        int gridCellIndex = -1;
        if (_gridCellOccupants != null)
        {
            for (int i = 0; i < _gridCellOccupants.Length; i++)
            {
                if (_gridCellOccupants[i] == tappedPassenger)
                {
                    gridCellIndex = i;
                    break;
                }
            }
        }

        if (gridCellIndex != -1)
        {
            if (!IsPathClearForGridPassenger(gridCellIndex))
            {
                Debug.Log($"Path blocked for passenger {tappedPassenger.name} at grid cell {gridCellIndex}.");
                return;
            }
        }
        
        int queueIndex = -1;
        if (_queueSlots != null)
        {
            for (int i = 0; i < _queueSlots.Length; i++)
            {
                if (_queueSlots[i] == tappedPassenger)
                {
                    queueIndex = i;
                    break;
                }
            }
        }

        Bus targetBus = FindBusForPassenger(tappedPassenger);
        bool canBoardActiveBus = targetBus != null && _busManager != null && _busManager.IsActiveBusAtStop;

        if (canBoardActiveBus)
        {
            bool wasInActiveList = _activePassengerTransforms.Contains(tappedPassenger.transform);
            if(wasInActiveList) _activePassengerTransforms.Remove(tappedPassenger.transform);
            
            if (queueIndex != -1) _queueSlots[queueIndex] = null;
            if (gridCellIndex != -1) _gridCellOccupants[gridCellIndex] = null;

            Vector3 boardingPoint = targetBus.transform.position;
            tappedPassenger.MoveToPosition(boardingPoint, () =>
            {
                if (targetBus.AddPassenger(tappedPassenger))
                {
                    DespawnSinglePassenger(tappedPassenger);
                }
                else
                {
                    if(wasInActiveList && !IsPassengerActive(tappedPassenger)) _activePassengerTransforms.Add(tappedPassenger.transform);
                }
            });
        }
        else
        {
            if (queueIndex != -1)
            {
                return;
            }

            if (_queueSlotTransforms == null || _queueSlots == null || _queueSlotTransforms.Length == 0)
            {
                return;
            }

            int emptyQueueSlotIndex = FindEmptyQueueSlot();
            if (emptyQueueSlotIndex != -1)
            {
                bool wasInActiveList = _activePassengerTransforms.Contains(tappedPassenger.transform);
                if(wasInActiveList) _activePassengerTransforms.Remove(tappedPassenger.transform);

                if (gridCellIndex != -1) _gridCellOccupants[gridCellIndex] = null;

                _queueSlots[emptyQueueSlotIndex] = tappedPassenger;

                tappedPassenger.MoveToPosition(_queueSlotTransforms[emptyQueueSlotIndex].position, () =>
                {
                    Bus availableBus = FindBusForPassenger(tappedPassenger);
                    if (availableBus != null && _busManager.IsActiveBusAtStop)
                    {
                        _queueSlots[emptyQueueSlotIndex] = null;

                        Vector3 boardingPoint = availableBus.transform.position;
                        tappedPassenger.MoveToPosition(boardingPoint, () =>
                        {
                            if (availableBus.AddPassenger(tappedPassenger))
                            {
                                DespawnSinglePassenger(tappedPassenger);
                            }
                        });
                    }
                });
            }
            else
            {
                Debug.Log("Cannot board and queue is full");
            }
        }
    }

    private bool IsPathClearForGridPassenger(int passengerCellIndex)
    {
        LevelData currentLevel = GameManager.Levels?.GetCurrentLevelData();
        if (currentLevel == null || _gridCellOccupants == null || passengerCellIndex < 0 || passengerCellIndex >= _gridCellOccupants.Length || currentLevel.gridHeight == 0)
        {
            return true; 
        }

        int gridWidth = currentLevel.gridWidth;
        int gridHeight = currentLevel.gridHeight;

        int col = passengerCellIndex % gridWidth;
        int row = passengerCellIndex / gridWidth;
        
        for (int rToCheck = row + 1; rToCheck < gridHeight; rToCheck++)
        {
            int cellInPathIndex = rToCheck * gridWidth + col;
            if (cellInPathIndex < _gridCellOccupants.Length && _gridCellOccupants[cellInPathIndex] != null)
            {
                Debug.Log($"Path of passenger at ({row},{col}) is blocked by passenger at ({rToCheck},{col}).");
                return false; // Path is blocked
            }
        }
        return true; // Path is clear
    }

    private Bus FindBusForPassenger(Passenger passenger)
    {
        if (_busManager?.ActiveBus != null && _busManager.ActiveBus.gameObject.activeInHierarchy && _busManager.ActiveBus.CanBoard(passenger))
        {
            return _busManager.ActiveBus;
        }
        return null;
    }

    public void ClearQueue()
    {
        if (_queueSlots == null) return;

        for (int i = 0; i < _queueSlots.Length; i++)
        {
            if (_queueSlots[i] != null)
            {
                DespawnSinglePassenger(_queueSlots[i]); 
                _queueSlots[i] = null;
            }
        }
    }

    private int FindEmptyQueueSlot()
    {
        if (_queueSlots == null) return -1;
        for (int i = 0; i < _queueSlots.Length; i++)
        {
            if (_queueSlots[i] == null)
            {
                return i;
            }
        }
        return -1;
    }
}