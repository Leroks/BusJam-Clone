using UnityEngine;
using System.Collections.Generic;
using System;

public class BusManager
{
    public event Action OnAllBusesDeparted; // For win condition check

    private List<Transform> _allSpawnedBusTransforms = new List<Transform>();
    private Queue<Bus> _waitingBusesQueue = new Queue<Bus>();
    private Bus _activeBusComponent;
    
    private PoolService _poolService;
    private GameObject _busPrefab;
    private Transform _busStopTransform;

    private int _initialBusCountForLevel = 0;
    private int _departedBusCount = 0;

    public Bus ActiveBus => _activeBusComponent;
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
                    busComponent.Initialize(busData);
                    busComponent.OnBusReadyToDepart += HandleBusDepartureRequest;
                    busComponent.OnDepartureComplete += HandleBusDepartureComplete;
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
        if (_waitingBusesQueue.Count > 0)
        {
            _activeBusComponent = _waitingBusesQueue.Dequeue();
            _activeBusComponent.gameObject.SetActive(true);
            if (_busStopTransform != null)
            {
                _activeBusComponent.transform.position = _busStopTransform.position;
                _activeBusComponent.transform.rotation = _busStopTransform.rotation;
            }
            else
            {
                Debug.LogError("BusManager: BusStopTransform not assigned!");
            }
            Debug.Log($"BusManager: Activated new bus: {_activeBusComponent.name}");
        }
        else
        {
            _activeBusComponent = null;
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
            departingBus.StartDeparture();
            _activeBusComponent = null; 
        }
    }

    private void HandleBusDepartureComplete(Bus departedBus)
    {
        Debug.Log($"BusManager: Bus {departedBus.name} completed departure.");
        _departedBusCount++;
        // The bus deactivates itself. Bootstrap.DespawnAllBuses will handle pool return at level end.
        // Or, if BusManager should own the full lifecycle including pool return immediately:
        // _allSpawnedBusTransforms.Remove(departedBus.transform);
        // _poolService.Get<Transform>("bus").Despawn(departedBus.transform);
        ActivateNextBus();
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
                    }
                    busPool.Despawn(busTransform);
                }
            }
        }
        _allSpawnedBusTransforms.Clear();
        _waitingBusesQueue.Clear();
        if (_activeBusComponent != null) // Ensure active bus events are also cleared if it wasn't part of the loop
        {
             _activeBusComponent.OnBusReadyToDepart -= HandleBusDepartureRequest;
             _activeBusComponent.OnDepartureComplete -= HandleBusDepartureComplete;
        }
        _activeBusComponent = null;
        _departedBusCount = 0;
        _initialBusCountForLevel = 0;
    }
}
