using UnityEngine;
using System.Collections.Generic;

public class PassengerManager
{
    private List<Transform> _activePassengerTransforms = new List<Transform>();
    private PoolService _poolService;
    private GameObject _passengerPrefab;

    public int ActivePassengerCount => _activePassengerTransforms.Count;

    public PassengerManager(PoolService poolService, GameObject passengerPrefab)
    {
        _poolService = poolService;
        _passengerPrefab = passengerPrefab;

        if (_poolService.Get<Transform>("passenger") == null && _passengerPrefab != null)
        {
            _poolService.RegisterPool("passenger", _passengerPrefab.GetComponent<Transform>(), 50);
        }
        else if(_passengerPrefab == null)
        {
            Debug.LogError("PassengerManager: Passenger prefab is null. Cannot register pool.");
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
    }
}
