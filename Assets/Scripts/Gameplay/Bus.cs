using UnityEngine;
using System.Collections.Generic;

public class Bus : MonoBehaviour
{
    [SerializeField] private Renderer busRenderer;
    // [SerializeField] private Transform[] seatPoints; // Optional: for visual placement of passengers

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
        Debug.Log($"{BusColor} bus is now full and ready to depart!");
        // Trigger departure logic here (e.g., start an animation, notify a service)
        // For now, maybe just deactivate or "despawn" it after a delay.
        // This will be expanded later.
    }

    // Add other bus behaviors: movement, opening/closing doors, etc.
}
