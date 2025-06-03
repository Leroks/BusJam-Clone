using UnityEngine;
using System;

[Serializable]
public class BusData
{
    public PassengerColor color;
    public readonly int capacity = 3;

    public BusData(PassengerColor color)
    {
        this.color = color;
    }
}
