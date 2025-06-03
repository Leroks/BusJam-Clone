using UnityEngine;
using System.Collections.Generic;
using System;

public class BusManager
{
    public event Action OnAllBusesDeparted;
    public event Action<Bus> OnBusAtStopReadyForBoarding;

    private List<Transform> _allSpawnedBusTransforms = new List<Transform>();
    private Queue<Bus> _waitingBusesQueue = new Queue<Bus>();
    private Bus _activeBusComponent;
    private bool _isActiveBusAtStop = false;
    
    private PoolService _poolService;
    private GameObject _busPrefab;
    private Transform _busStopTransform;

    private int _initialBusCountForLevel = 0;
    private int _departedBusCount = 0;

    public Bus ActiveBus => _activeBusComponent;
    public bool IsActiveBusAtStop => _isActiveBusAtStop;
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
            _poolService.RegisterPool("bus", _busPrefab.GetComponent<Transform>(), 10, "Buses");
        }
        else
        {
            Debug.LogError("BusManager: Bus prefab is null. Cannot register pool.");
        }
    }

    public void SpawnBusesForLevel(LevelData levelData)
    {
        DespawnAllBuses();

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
                    busComponent.Initialize(busData, _busStopTransform.position); 
                    busComponent.OnBusReadyToDepart += HandleBusDepartureRequest;
                    busComponent.OnDepartureComplete += HandleBusDepartureComplete;
                    busComponent.OnBusArrivedAtStop += HandleBusArrivedAtStop;
                    _waitingBusesQueue.Enqueue(busComponent);
                    _allSpawnedBusTransforms.Add(busInstanceTransform);
                    busInstanceTransform.gameObject.SetActive(false);
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
        if (_activeBusComponent != null)
        {
            _isActiveBusAtStop = false; 
        }

        if (_waitingBusesQueue.Count > 0)
        {
            _activeBusComponent = _waitingBusesQueue.Dequeue();
            _isActiveBusAtStop = false;
            _activeBusComponent.gameObject.SetActive(true);
            _activeBusComponent.StartArrivalSequence(); 
            Debug.Log($"BusManager: Activated new bus: {_activeBusComponent.name}, starting arrival sequence.");
        }
        else
        {
            _activeBusComponent = null;
            _isActiveBusAtStop = false;
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
            _isActiveBusAtStop = false;
            departingBus.StartDeparture();
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
            _isActiveBusAtStop = true;
            OnBusAtStopReadyForBoarding?.Invoke(bus);
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
                        busComp.OnBusArrivedAtStop -= HandleBusArrivedAtStop;
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
        _isActiveBusAtStop = false;
        _departedBusCount = 0;
        _initialBusCountForLevel = 0;
    }
}
