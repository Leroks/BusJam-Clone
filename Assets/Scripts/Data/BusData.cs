using UnityEngine;
using System;

[Serializable]
public class BusData
{
    public PassengerColor color;
    public int capacity = 3;
    public Vector3 initialPosition;
    // public string busId; // Optional: for specific bus types or tracking
    // public Vector3[] pathPoints; // Optional: for predefined bus paths

    public BusData(PassengerColor color, int capacity, Vector3 initialPosition)
    {
        this.color = color;
        this.capacity = capacity;
        this.initialPosition = initialPosition;
    }
}
