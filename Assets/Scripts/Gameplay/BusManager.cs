using UnityEngine;
using System.Collections.Generic;
using System;

public class BusManager
{
    public event Action OnAllBusesDeparted; // For win condition check
    public event Action<Bus> OnBusAtStopReadyForBoarding; // New event for PassengerManager

    private List<Transform> _allSpawnedBusTransforms = new List<Transform>();
    private Queue<Bus> _waitingBusesQueue = new Queue<Bus>();
    private Bus _activeBusComponent;
    private bool _isActiveBusAtStop = false; // Tracks if the active bus is currently at the stop
    
    private PoolService _poolService;
    private GameObject _busPrefab;
    private Transform _busStopTransform;

    private int _initialBusCountForLevel = 0;
    private int _departedBusCount = 0;

    public Bus ActiveBus => _activeBusComponent;
    public bool IsActiveBusAtStop => _isActiveBusAtStop; // Public accessor
    public int DepartedBusCount => _departedBusCount;
    public int InitialBusCountForLevel => _initialBusCountForLevel;
    public bool AreAllBusesGone => _activeBusComponent == null && _waitingBusesQueue.Count == 0;


    public BusManager(PoolService poolService, GameObject busPrefab, Transform busStopTransform)
    {
        _poolService = poolService;
        _busPrefab = busPrefab;
        _busStopTransform = busStopTransform;

        if (_busPrefab != null)
        {
            // Register the pool. PoolService should ideally handle cases where a pool with the same key is registered multiple times,
            // or this registration should only happen if the pool isn't already registered by another system.
            // For now, we ensure it's registered here, under a "Buses" parent.
            _poolService.RegisterPool("bus", _busPrefab.GetComponent<Transform>(), 10, "Buses");
        }
        else
        {
            Debug.LogError("BusManager: Bus prefab is null. Cannot register pool.");
        }
    }

    public void SpawnBusesForLevel(LevelData levelData)
    {
        DespawnAllBuses(); // Clear previous buses

        if (_busPrefab == null)
        {
            Debug.LogError("BusManager: Bus prefab is not set. Cannot spawn buses.");
            return;
        }
        var busPool = _poolService.Get<Transform>("bus");
        if (busPool == null)
        {
            Debug.LogError("BusManager: Bus pool not found.");
            return;
        }

        _initialBusCountForLevel = levelData.busConfigurations.Count;
        _departedBusCount = 0;

        foreach (var busData in levelData.busConfigurations)
        {
            Transform busInstanceTransform = busPool.Spawn();
            if (busInstanceTransform != null)
            {
                busInstanceTransform.gameObject.SetActive(true);
                Bus busComponent = busInstanceTransform.GetComponent<Bus>();
                if (busComponent != null)
                {
                    // Pass the actual bus stop position from BusManager's _busStopTransform
                    if (_busStopTransform == null)
                    {
                        Debug.LogError("BusManager: _busStopTransform is null. Cannot initialize bus with correct stop position.");
                        // Handle error, maybe don't spawn this bus or use a default
                        busPool.Despawn(busInstanceTransform);
                        continue; 
                    }
                    busComponent.Initialize(busData, _busStopTransform.position); 
                    busComponent.OnBusReadyToDepart += HandleBusDepartureRequest;
                    busComponent.OnDepartureComplete += HandleBusDepartureComplete;
                    busComponent.OnBusArrivedAtStop += HandleBusArrivedAtStop; // Subscribe to new event
                    _waitingBusesQueue.Enqueue(busComponent);
                    _allSpawnedBusTransforms.Add(busInstanceTransform);
                    busInstanceTransform.gameObject.SetActive(false); // Initially inactive
                }
                else
                {
                    Debug.LogError("Spawned bus prefab is missing Bus component.");
                    busPool.Despawn(busInstanceTransform);
                }
            }
        }
        ActivateNextBus();
    }

    private void ActivateNextBus()
    {
        if (_activeBusComponent != null) // If there was a previous active bus, ensure its state is cleared
        {
            _isActiveBusAtStop = false; 
        }

        if (_waitingBusesQueue.Count > 0)
        {
            _activeBusComponent = _waitingBusesQueue.Dequeue();
            _isActiveBusAtStop = false; // New bus is not at stop yet
            _activeBusComponent.gameObject.SetActive(true);
            _activeBusComponent.StartArrivalSequence(); 
            Debug.Log($"BusManager: Activated new bus: {_activeBusComponent.name}, starting arrival sequence.");
        }
        else
        {
            _activeBusComponent = null;
            _isActiveBusAtStop = false; // No active bus, so not at stop
            Debug.Log("BusManager: No more waiting buses.");
            if (_departedBusCount >= _initialBusCountForLevel && _initialBusCountForLevel > 0)
            {
                OnAllBusesDeparted?.Invoke();
            }
        }
    }

    private void HandleBusDepartureRequest(Bus departingBus)
    {
        if (departingBus == _activeBusComponent)
        {
            Debug.Log($"BusManager: Bus {departingBus.name} requesting departure.");
            _isActiveBusAtStop = false; // Bus is no longer at the stop as it's departing
            departingBus.StartDeparture();
            // _activeBusComponent = null; // Keep active bus until departure complete, then ActivateNextBus handles it
        }
    }

    private void HandleBusDepartureComplete(Bus departedBus)
    {
        Debug.Log($"BusManager: Bus {departedBus.name} completed departure.");
        _departedBusCount++;
        ActivateNextBus();
    }

    private void HandleBusArrivedAtStop(Bus bus)
    {
        if (bus == _activeBusComponent)
        {
            Debug.Log($"BusManager: Bus {bus.name} has arrived at the stop.");
            _isActiveBusAtStop = true; // Bus is now at the stop
            OnBusAtStopReadyForBoarding?.Invoke(bus); // Notify subscribers
            // Potentially enable passenger interactions here if they were disabled during bus movement.
        }
    }

    public void DespawnAllBuses()
    {
        var busPool = _poolService?.Get<Transform>("bus");
        if (busPool != null)
        {
            foreach (var busTransform in _allSpawnedBusTransforms)
            {
                if (busTransform != null)
                {
                    Bus busComp = busTransform.GetComponent<Bus>();
                    if (busComp != null)
                    {
                        busComp.OnBusReadyToDepart -= HandleBusDepartureRequest;
                        busComp.OnDepartureComplete -= HandleBusDepartureComplete;
                        busComp.OnBusArrivedAtStop -= HandleBusArrivedAtStop; // Unsubscribe from new event
                    }
                    busPool.Despawn(busTransform);
                }
            }
        }
        _allSpawnedBusTransforms.Clear();
        _waitingBusesQueue.Clear();
        if (_activeBusComponent != null)
        {
             _activeBusComponent.OnBusReadyToDepart -= HandleBusDepartureRequest;
             _activeBusComponent.OnDepartureComplete -= HandleBusDepartureComplete;
             _activeBusComponent.OnBusArrivedAtStop -= HandleBusArrivedAtStop;
        }
        _activeBusComponent = null;
        _isActiveBusAtStop = false; // No active bus
        _departedBusCount = 0;
        _initialBusCountForLevel = 0;
    }
}
