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
            Debug.LogError("PassengerManager: queueSlotTransforms is null. Queue functionality will be impaired.");
            _queueSlots = new Passenger[0];
        }

        if (_passengerPrefab != null)
        {
            _poolService.RegisterPool("passenger", _passengerPrefab.GetComponent<Transform>(), 50, "Passengers");
        }
        else
        {
            Debug.LogError("PassengerManager: Passenger prefab is null. Cannot register pool.");
        }

        if (_inputService != null)
        {
            _inputService.OnPassengerTap += HandlePassengerTap;
        }
        else
        {
            Debug.LogError("PassengerManager: InputService is null. Passenger taps will not be handled.");
        }

        if (_busManager != null)
        {
            _busManager.OnBusAtStopReadyForBoarding += AttemptAutoBoardQueuedPassengers;
        }
        else
        {
            Debug.LogError("PassengerManager: BusManager is null. Cannot subscribe to OnBusAtStopReadyForBoarding.");
        }
    }

    public void Dispose()
    {
        if (_inputService != null)
        {
            _inputService.OnPassengerTap -= HandlePassengerTap;
        }
        if (_busManager != null)
        {
            _busManager.OnBusAtStopReadyForBoarding -= AttemptAutoBoardQueuedPassengers;
        }
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
                Debug.Log($"PassengerManager: Auto-boarding passenger {passengerInQueue.name} from queue slot {i} to bus {busAtStop.name}.");
                
                _queueSlots[i] = null;

                Vector3 boardingPoint = busAtStop.transform.position;
                passengerInQueue.MoveToPosition(boardingPoint, () =>
                {
                    if (busAtStop.AddPassenger(passengerInQueue))
                    {
                        DespawnSinglePassenger(passengerInQueue);
                        Debug.Log($"PassengerManager: Auto-boarded passenger {passengerInQueue.name} successfully.");
                    }
                    else
                    {
                        Debug.LogWarning($"PassengerManager: Auto-boarding for {passengerInQueue.name} failed after move (bus full or wrong color).");
                    }
                });

                if (busAtStop.IsFull)
                {
                    Debug.Log($"PassengerManager: Bus {busAtStop.name} became full during auto-boarding. Stopping further auto-boards for this bus.");
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
                    Debug.LogWarning($"PassengerManager: DynamicGridCellTransform element {i} is null or out of bounds. Skipping.");
                    continue;
                }
                if (i >= levelData.standardGridPassengers.Count) // Safety check for passenger data
                {
                    Debug.LogWarning($"PassengerManager: Not enough passenger color data for grid cell {i}. Skipping.");
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

        // Centering
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
        Debug.Log($"PassengerManager: Created dynamic grid {gridWidth}x{gridHeight} with {_dynamicGridCellTransforms.Count} cells.");
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

    public void RemoveFromActiveList(Passenger passenger)
    {
        if (passenger != null)
        {
            _activePassengerTransforms.Remove(passenger.transform);
        }
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
                    Debug.LogWarning($"Passenger {tappedPassenger.name} failed to board bus {targetBus.name} after move.");
                    if(wasInActiveList && !IsPassengerActive(tappedPassenger)) _activePassengerTransforms.Add(tappedPassenger.transform);
                }
            });
        }
        else
        {
            if (queueIndex != -1)
            {
                Debug.Log($"Tapped passenger {tappedPassenger.name} from queue, but no bus available. Stays in queue.");
                return;
            }

            if (_queueSlotTransforms == null || _queueSlots == null || _queueSlotTransforms.Length == 0)
            {
                Debug.Log($"Passenger {tappedPassenger.name} cannot board a bus and no queue slots available/defined.");
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
                    Debug.Log($"Passenger {tappedPassenger.name} arrived at queue slot {emptyQueueSlotIndex}.");

                    Bus availableBus = FindBusForPassenger(tappedPassenger);
                    if (availableBus != null && _busManager.IsActiveBusAtStop)
                    {
                        Debug.Log($"Passenger {tappedPassenger.name} at queue slot {emptyQueueSlotIndex} can immediately board bus {availableBus.name}.");
                        _queueSlots[emptyQueueSlotIndex] = null;

                        Vector3 boardingPoint = availableBus.transform.position;
                        tappedPassenger.MoveToPosition(boardingPoint, () =>
                        {
                            if (availableBus.AddPassenger(tappedPassenger))
                            {
                                DespawnSinglePassenger(tappedPassenger);
                                Debug.Log($"Passenger {tappedPassenger.name} successfully boarded bus {availableBus.name} from queue after immediate check.");
                            }
                            else
                            {
                                Debug.LogWarning($"Passenger {tappedPassenger.name} failed to board bus {availableBus.name} from queue after immediate check (bus full/color mismatch).");
                            }
                        });
                    }
                });
            }
            else
            {
                Debug.Log($"Passenger {tappedPassenger.name} cannot board a bus and queue is full.");
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
                Debug.Log($"Path for passenger at ({row},{col}) is blocked by passenger at ({rToCheck},{col}).");
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