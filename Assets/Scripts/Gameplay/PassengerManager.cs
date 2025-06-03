using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class PassengerManager
{
    private const int GRID_COLUMNS = 3;
    private List<Transform> _activePassengerTransforms = new List<Transform>();
    private PoolService _poolService;
    private GameObject _passengerPrefab;
    private Transform[] _queueSlotTransforms;
    private Transform[] _passengerGridCellTransforms;
    private Passenger[] _gridCellOccupants;
    private int _gridRows;
    private Passenger[] _queueSlots;
    private BusManager _busManager;
    private InputService _inputService;
    private GameStateMachine _stateMachine;


    public int ActivePassengerCount => _activePassengerTransforms.Count;
    public bool IsQueueEmpty => _queueSlots == null || _queueSlots.All(p => p == null);


    public PassengerManager(PoolService poolService, GameObject passengerPrefab, Transform[] queueSlotTransforms, Transform[] passengerGridCellTransforms, BusManager busManager, InputService inputService, GameStateMachine stateMachine)
    {
        _poolService = poolService;
        _passengerPrefab = passengerPrefab;
        _queueSlotTransforms = queueSlotTransforms;
        _passengerGridCellTransforms = passengerGridCellTransforms;

        if (_passengerGridCellTransforms != null && _passengerGridCellTransforms.Length > 0)
        {
            if (_passengerGridCellTransforms.Length % GRID_COLUMNS != 0)
            {
                Debug.LogWarning("PassengerManager: passengerGridCellTransforms count is not a multiple of GRID_COLUMNS. Grid layout might be incorrect.");
            }
            _gridRows = Mathf.CeilToInt((float)_passengerGridCellTransforms.Length / GRID_COLUMNS);
            _gridCellOccupants = new Passenger[_passengerGridCellTransforms.Length];
        }
        else
        {
            _gridRows = 0;
            _gridCellOccupants = new Passenger[0];
            Debug.LogWarning("PassengerManager: passengerGridCellTransforms is null or empty. Grid functionality will be impaired.");
        }

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
            _queueSlots = new Passenger[0]; // Avoid null reference, but queue is unusable
        }

        if (_passengerPrefab != null)
        {
            // Register the pool. PoolService should ideally handle cases where a pool with the same key is registered multiple times.
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
                        // This case should be rare if CanBoard was true and bus didn't fill up instantly by another means.
                        // If it happens, passenger is now "floating". Could try to re-queue or handle.
                        Debug.LogWarning($"PassengerManager: Auto-boarding for {passengerInQueue.name} failed after move (bus full or wrong color).");
                        // Attempt to put back in an empty queue slot if possible, or just leave it.
                        // For simplicity, we don't re-queue here. It might get tapped later.
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
        
        var passengerPool = _poolService.Get<Transform>("passenger");
        
        if (levelData.standardGridPassengers.Count > 0 && _passengerGridCellTransforms != null && _passengerGridCellTransforms.Length > 0 && _gridCellOccupants != null)
        {
            for(int i = 0; i < _gridCellOccupants.Length; i++)
            {
                _gridCellOccupants[i] = null;
            }

            int passengersToSpawnOnGrid = Mathf.Min(levelData.standardGridPassengers.Count, _passengerGridCellTransforms.Length);
            
            for (int i = 0; i < passengersToSpawnOnGrid; i++)
            {
                if (_passengerGridCellTransforms[i] == null)
                {
                    Debug.LogWarning($"PassengerManager: PassengerGridCellTransforms element {i} is null. Skipping.");
                    continue;
                }

                Transform passengerInstance = passengerPool.Spawn();
                if (passengerInstance != null)
                {
                    passengerInstance.gameObject.SetActive(true);
                    passengerInstance.position = _passengerGridCellTransforms[i].position;
                    passengerInstance.rotation = _passengerGridCellTransforms[i].rotation;
                    
                    Passenger passengerComponent = passengerInstance.GetComponent<Passenger>();
                    if (passengerComponent != null)
                    {
                        passengerComponent.Initialize(levelData.standardGridPassengers[i]);
                        _activePassengerTransforms.Add(passengerInstance);
                        _gridCellOccupants[i] = passengerComponent;
                    }
                    else
                    {
                        Debug.LogError("Spawned passenger prefab for standard grid is missing Passenger component.");
                        passengerPool.Despawn(passengerInstance);
                    }
                }
            }
        }
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
                // This will handle removing from grid, active list, and returning to pool
                DespawnSinglePassenger(passengerComponent);
            }
        }
        
        // If any remain due to missing components, clear them.
        _activePassengerTransforms.Clear();

        ClearQueue(); // Ensure queue is also cleared (calls DespawnSinglePassenger for queued items)

        // Explicitly clear grid occupants as a final safety net, though DespawnSinglePassenger should handle it.
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

        // If passenger is on the grid, check if path is clear
        if (gridCellIndex != -1)
        {
            if (!IsPathClearForGridPassenger(gridCellIndex))
            {
                Debug.Log($"Path blocked for passenger {tappedPassenger.name} at grid cell {gridCellIndex}.");
                return; // Path is blocked, do not move
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
            if(wasInActiveList) _activePassengerTransforms.Remove(tappedPassenger.transform); // Temporarily remove
            
            if (queueIndex != -1) _queueSlots[queueIndex] = null;
            if (gridCellIndex != -1) _gridCellOccupants[gridCellIndex] = null; // Clear from grid

            Vector3 boardingPoint = targetBus.transform.position;
            tappedPassenger.MoveToPosition(boardingPoint, () =>
            {
                if (targetBus.AddPassenger(tappedPassenger))
                {
                    DespawnSinglePassenger(tappedPassenger); // Despawns and handles grid/active list
                }
                else
                {
                    Debug.LogWarning($"Passenger {tappedPassenger.name} failed to board bus {targetBus.name} after move.");
                    if(wasInActiveList && !IsPassengerActive(tappedPassenger)) _activePassengerTransforms.Add(tappedPassenger.transform);
                }
            });
        }
        else // No bus, try to move to queue (if not already in queue)
        {
            if (queueIndex != -1) // Already in queue, no bus, so do nothing
            {
                Debug.Log($"Tapped passenger {tappedPassenger.name} from queue, but no bus available. Stays in queue.");
                return;
            }

            // If passenger was on grid and no bus, try moving to queue
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

                if (gridCellIndex != -1) _gridCellOccupants[gridCellIndex] = null; // Clear from grid

                _queueSlots[emptyQueueSlotIndex] = tappedPassenger; // Reserve slot

                tappedPassenger.MoveToPosition(_queueSlotTransforms[emptyQueueSlotIndex].position, () =>
                {
                    Debug.Log($"Passenger {tappedPassenger.name} arrived at queue slot {emptyQueueSlotIndex}.");

                    // Check if a bus is now available and at the stop for immediate boarding
                    Bus availableBus = FindBusForPassenger(tappedPassenger); // Re-check for a suitable bus
                    if (availableBus != null && _busManager.IsActiveBusAtStop)
                    {
                        Debug.Log($"Passenger {tappedPassenger.name} at queue slot {emptyQueueSlotIndex} can immediately board bus {availableBus.name}.");
                        _queueSlots[emptyQueueSlotIndex] = null; // Vacate the queue slot

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
        if (_gridCellOccupants == null || passengerCellIndex < 0 || passengerCellIndex >= _gridCellOccupants.Length || _gridRows == 0)
        {
            return true; // Should not happen or grid not set up
        }

        int col = passengerCellIndex % GRID_COLUMNS;
        int row = passengerCellIndex / GRID_COLUMNS;

        // Check all cells in front (same column, lower row index)
        for (int r = 0; r < row; r++)
        {
            int frontCellIndex = r * GRID_COLUMNS + col;
            if (frontCellIndex >= 0 && frontCellIndex < _gridCellOccupants.Length) // Bounds check
            {
                if (_gridCellOccupants[frontCellIndex] != null)
                {
                    return false; // Path is blocked
                }
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
                // DespawnSinglePassenger will handle removing from active lists and grid if necessary
                DespawnSinglePassenger(_queueSlots[i]); 
                _queueSlots[i] = null; // Ensure slot is marked empty
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