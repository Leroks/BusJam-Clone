using UnityEngine;
using System.Collections.Generic;
using System.Linq; // Added for All()

public class PassengerManager
{
    private List<Transform> _activePassengerTransforms = new List<Transform>();
    private PoolService _poolService;
    private GameObject _passengerPrefab;
    private Transform[] _queueSlotTransforms;
    private Passenger[] _queueSlots;
    private BusManager _busManager;
    private InputService _inputService;
    private GameStateMachine _stateMachine;


    public int ActivePassengerCount => _activePassengerTransforms.Count;
    public bool IsQueueEmpty => _queueSlots == null || _queueSlots.All(p => p == null);


    public PassengerManager(PoolService poolService, GameObject passengerPrefab, Transform[] queueSlotTransforms, BusManager busManager, InputService inputService, GameStateMachine stateMachine)
    {
        _poolService = poolService;
        _passengerPrefab = passengerPrefab;
        _queueSlotTransforms = queueSlotTransforms;
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
                    _activePassengerTransforms.Add(passengerInstance);
                }
                else
                {
                    Debug.LogError("Spawned passenger prefab is missing Passenger component.");
                    passengerPool.Despawn(passengerInstance); // Return to pool
                }
            }
        }
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
        if (_stateMachine.Current != GameState.Playing || tappedPassenger == null || _busManager == null) return;

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
            if (targetBus.AddPassenger(tappedPassenger))
            {
                Debug.Log($"Passenger {tappedPassenger.name} boarding bus {targetBus.name}");
                if (queueIndex != -1)
                {
                    _queueSlots[queueIndex] = null; 
                }
                // DespawnSinglePassenger will remove from _activePassengerTransforms if present
                DespawnSinglePassenger(tappedPassenger); 
            }
        }
        else
        {
            if (queueIndex != -1)
            {
                Debug.Log($"Tapped passenger {tappedPassenger.name} from queue, but no bus available. Stays in queue.");
                return; 
            }

            if (_queueSlotTransforms == null || _queueSlots == null) return; // Guard against null queue infrastructure

            int emptyQueueSlot = FindEmptyQueueSlot();
            if (emptyQueueSlot != -1)
            {
                Debug.Log($"Passenger {tappedPassenger.name} moving to queue slot {emptyQueueSlot}");
                _queueSlots[emptyQueueSlot] = tappedPassenger;
                tappedPassenger.transform.position = _queueSlotTransforms[emptyQueueSlot].position;
                RemoveFromActiveList(tappedPassenger); 
            }
            else
            {
                Debug.Log($"Passenger {tappedPassenger.name} cannot board a bus and queue is full.");
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
