using UnityEngine;
using System.Collections.Generic;
using System.Linq; // Added for All()

public class PassengerManager
{
    private List<Transform> _activePassengerTransforms = new List<Transform>();
    private PoolService _poolService;
    private GameObject _passengerPrefab;
    private Transform[] _queueSlotTransforms;
    private Transform[] _passengerGridCellTransforms; // To be assigned in constructor
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
    }

    public void Dispose()
    {
        if (_inputService != null)
        {
            _inputService.OnPassengerTap -= HandlePassengerTap;
        }
    }

    public void SpawnPassengersForLevel(LevelData levelData)
    {
        DespawnAllPassengers(); // Clear previous level's passengers

        if (_passengerPrefab == null)
        {
            Debug.LogError("PassengerManager: Passenger prefab is not set. Cannot spawn passengers.");
            return;
        }
        
        var passengerPool = _poolService.Get<Transform>("passenger");
        if (passengerPool == null)
        {
            Debug.LogError("PassengerManager: Passenger pool not found or not registered.");
            return;
        }

        // Always attempt grid generation if data is present
        if (levelData.standardGridPassengers.Count > 0 && _passengerGridCellTransforms != null && _passengerGridCellTransforms.Length > 0)
        {
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
                    passengerInstance.rotation = _passengerGridCellTransforms[i].rotation; // Optional: match rotation
                    
                    Passenger passengerComponent = passengerInstance.GetComponent<Passenger>();
                    if (passengerComponent != null)
                    {
                        passengerComponent.Initialize(levelData.standardGridPassengers[i]);
                        _activePassengerTransforms.Add(passengerInstance);
                    }
                    else
                    {
                        Debug.LogError("Spawned passenger prefab for standard grid is missing Passenger component.");
                        passengerPool.Despawn(passengerInstance); // Return to pool
                    }
                }
            }
        }
        // Manual spawn logic removed
    }

    public void DespawnSinglePassenger(Passenger passengerToDespawn)
    {
        if (passengerToDespawn == null) return;

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
            passengerToDespawn.gameObject.SetActive(false); // Fallback
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
        var passengerPool = _poolService?.Get<Transform>("passenger");
        if (passengerPool != null)
        {
            // Iterate over a copy if modifying the list during iteration
            List<Transform> toDespawn = new List<Transform>(_activePassengerTransforms);
            foreach (var passengerTransform in toDespawn)
            {
                if(passengerTransform != null)
                {
                    passengerPool.Despawn(passengerTransform);
                }
            }
        }
        else // Fallback if pool somehow not available
        {
             foreach (var passengerTransform in _activePassengerTransforms)
             {
                 if(passengerTransform != null) passengerTransform.gameObject.SetActive(false);
             }
        }
        _activePassengerTransforms.Clear();
        ClearQueue(); // Ensure queue is also cleared
    }

    // Methods moved from GameManager.cs
    private void HandlePassengerTap(Passenger tappedPassenger)
    {
        if (_stateMachine.Current != GameState.Playing || tappedPassenger == null || _busManager == null || tappedPassenger.IsMoving) return; // Don't interact if already moving

        // Check if passenger is from queue or general spawn
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

        if (targetBus != null)
        {
            // Temporarily remove from active list to prevent re-tapping during move
            // It will be fully handled (despawned) after movement.
            bool wasInActiveList = _activePassengerTransforms.Contains(tappedPassenger.transform);
            if(wasInActiveList) _activePassengerTransforms.Remove(tappedPassenger.transform);
            
            if (queueIndex != -1) _queueSlots[queueIndex] = null;


            // Define a boarding point on the bus (e.g., its transform position or a specific child transform)
            Vector3 boardingPoint = targetBus.transform.position; // Or targetBus.GetBoardingPoint();

            tappedPassenger.MoveToPosition(boardingPoint, () =>
            {
                if (targetBus.AddPassenger(tappedPassenger)) // Finalize boarding
                {
                    Debug.Log($"Passenger {tappedPassenger.name} completed move and boarded bus {targetBus.name}");
                    DespawnSinglePassenger(tappedPassenger); // Now despawn after movement and successful boarding
                }
                else
                {
                    // Failed to board after move (e.g., bus became full during transit)
                    // Handle this case: maybe move to queue or just stay put and re-add to active list
                    Debug.LogWarning($"Passenger {tappedPassenger.name} failed to board bus {targetBus.name} after move. Bus might be full.");
                    if(wasInActiveList && !IsPassengerActive(tappedPassenger)) _activePassengerTransforms.Add(tappedPassenger.transform); // Re-add if not handled
                    if (queueIndex != -1 && _queueSlots[queueIndex] == null) _queueSlots[queueIndex] = tappedPassenger; // Put back in queue if from queue
                }
            });
        }
        else // No bus available, try to move to queue
        {
            if (queueIndex != -1)
            {
                // Passenger was already in queue and tapped, but no bus. Stays in queue.
                Debug.Log($"Tapped passenger {tappedPassenger.name} from queue, but no bus available. Stays in queue.");
                return; 
            }

            if (_queueSlotTransforms == null || _queueSlots == null || _queueSlotTransforms.Length == 0)
            {
                 Debug.Log($"Passenger {tappedPassenger.name} cannot board a bus and no queue slots available/defined.");
                return; // No queue infrastructure
            }

            int emptyQueueSlotIndex = FindEmptyQueueSlot();
            if (emptyQueueSlotIndex != -1)
            {
                // Temporarily remove from active list
                bool wasInActiveList = _activePassengerTransforms.Contains(tappedPassenger.transform);
                if(wasInActiveList) _activePassengerTransforms.Remove(tappedPassenger.transform);

                _queueSlots[emptyQueueSlotIndex] = tappedPassenger; // Reserve slot

                tappedPassenger.MoveToPosition(_queueSlotTransforms[emptyQueueSlotIndex].position, () =>
                {
                    Debug.Log($"Passenger {tappedPassenger.name} moved to queue slot {emptyQueueSlotIndex}");
                    // Passenger is now in the queue slot, already marked in _queueSlots.
                    // No need to call RemoveFromActiveList again as it was done before move.
                });
            }
            else
            {
                Debug.Log($"Passenger {tappedPassenger.name} cannot board a bus and queue is full.");
                // Passenger remains in its current position, still active if it was.
            }
        }
    }

    private Bus FindBusForPassenger(Passenger passenger)
    {
        if (_busManager?.ActiveBus != null && _busManager.ActiveBus.gameObject.activeInHierarchy && _busManager.ActiveBus.CanBoard(passenger))
        {
            return _busManager.ActiveBus;
        }
        return null;
    }

    public void ClearQueue() // Made public to be callable from GameManager if needed during state changes
    {
        if (_queueSlots == null) return;

        for (int i = 0; i < _queueSlots.Length; i++)
        {
            if (_queueSlots[i] != null)
            {
                var passengerPool = _poolService?.Get<Transform>("passenger");
                if (passengerPool != null)
                {
                    // Check if this passenger is also in the active list, if so, DespawnSinglePassenger will handle it.
                    // If it's only in the queue (moved from active list), then despawn it here.
                    // To be safe, let DespawnSinglePassenger handle it, which also removes from active list if present.
                    DespawnSinglePassenger(_queueSlots[i]);
                }
                else
                {
                     if(_queueSlots[i].gameObject != null) _queueSlots[i].gameObject.SetActive(false); // Fallback
                }
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
