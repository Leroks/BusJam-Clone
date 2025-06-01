using UnityEngine;
using System.Collections.Generic;
using System;
using System.Collections;
using DG.Tweening;

public class Bus : MonoBehaviour
{
    [SerializeField] private Renderer busRenderer;
    [SerializeField] private float departureSpeed = 5f;
    [SerializeField] private float departureDistance = 20f; // How far it moves before despawning

    public event Action<Bus> OnBusReadyToDepart;
    public event Action<Bus> OnDepartureComplete;

    public PassengerColor BusColor { get; private set; }
    public int Capacity { get; private set; }
    public int CurrentPassengerCount { get; private set; }

    private List<Passenger> _passengersOnBoard;

    public bool IsFull => CurrentPassengerCount >= Capacity;

    public void Initialize(BusData data)
    {
        BusColor = data.color;
        Capacity = data.capacity;
        transform.position = data.initialPosition;
        CurrentPassengerCount = 0;
        _passengersOnBoard = new List<Passenger>(Capacity);

        ApplyBusColor();
        // UpdateVisuals(); // e.g., show empty seats
        transform.DOMoveY(transform.position.y + 0.005f, 0.2f).SetEase(Ease.InOutQuad).SetLoops(-1, LoopType.Yoyo); 
    }

    private void ApplyBusColor()
    {
        busRenderer.material.color = GetUnityColor(BusColor);
    }

    // TODO: Move this to a utility class
    private Color GetUnityColor(PassengerColor colorEnum)
    {
        // This method can be shared or put in a utility class if used by many components
        switch (colorEnum)
        {
            case PassengerColor.Red: return Color.red;
            case PassengerColor.Green: return Color.green;
            case PassengerColor.Blue: return Color.blue;
            case PassengerColor.Yellow: return Color.yellow;
            case PassengerColor.Purple: return new Color(0.5f, 0f, 0.5f);
            case PassengerColor.Orange: return new Color(1f, 0.5f, 0f);
            case PassengerColor.Black: return Color.grey;
            default: return Color.white;
        }
    }

    public bool CanBoard(Passenger passenger)
    {
        if (IsFull) return false;
        return passenger.CurrentColor == BusColor;
    }

    public bool AddPassenger(Passenger passenger)
    {
        if (!CanBoard(passenger)) return false;

        CurrentPassengerCount++;
        _passengersOnBoard.Add(passenger);
        // passenger.gameObject.SetActive(false); // Or parent to bus, move to seat
        // UpdateVisuals();

        Debug.Log($"Passenger boarded {BusColor} bus. Current count: {CurrentPassengerCount}/{Capacity}");

        if (IsFull)
        {
            HandleBusFull();
        }
        return true;
    }

    private void HandleBusFull()
    {
        Debug.Log($"{BusColor} bus ({gameObject.name}) is now full and ready to depart!");
        OnBusReadyToDepart?.Invoke(this);
    }

    public void StartDeparture()
    {
        StartCoroutine(DepartRoutine());
    }

    private IEnumerator DepartRoutine()
    {
        Debug.Log($"Bus {gameObject.name} starting departure routine.");
        Vector3 initialPosition = transform.position;
        Vector3 targetPosition = initialPosition + Vector3.right * departureDistance;
        float journeyLength = Vector3.Distance(initialPosition, targetPosition);
        float startTime = Time.time;

        if (journeyLength > 0) // Ensure there's a distance to travel
        {
            float distCovered = (Time.time - startTime) * departureSpeed;
            float fractionOfJourney = distCovered / journeyLength;

            while (fractionOfJourney < 1)
            {
                distCovered = (Time.time - startTime) * departureSpeed;
                fractionOfJourney = distCovered / journeyLength;
                transform.position = Vector3.Lerp(initialPosition, targetPosition, fractionOfJourney);
                yield return null;
            }
        }
        
        transform.position = targetPosition; // Ensure it reaches the exact target

        Debug.Log($"Bus {gameObject.name} reached departure point. Invoking OnDepartureComplete and despawning.");
        
        // Notify completion before despawning itself from the perspective of GameManager
        OnDepartureComplete?.Invoke(this);

        // Despawn logic: Get the pool and despawn this bus instance's transform
        // This assumes GameManager.Pools is accessible or Bus has a reference to its pool.
        // For simplicity, let's assume GameManager will handle the actual despawn from its list
        // and the pool after OnDepartureComplete. The bus just deactivates itself.
        // However, a cleaner way is for the bus to request despawn from the pool service if it knows its pool.
        // For now, just deactivate. GameManager will handle the actual pool despawn via _allSpawnedBusTransforms.
        gameObject.SetActive(false); 
        // If GameManager is not managing the despawn from _allSpawnedBusTransforms based on this event,
        // then the bus should handle its own return to the pool here.
        // Example: GameManager.Pools.Get<Transform>("bus").Despawn(transform);
        // This creates a dependency on GameManager.Pools static access.
    }

    // Add other bus behaviors: movement, opening/closing doors, etc.
}
