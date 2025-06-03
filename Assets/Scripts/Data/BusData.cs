using UnityEngine;
using System;

[Serializable]
public class BusData
{
    public PassengerColor color;
    public int capacity = 3;

    public BusData()
    {
        this.color = PassengerColor.Red;
        this.capacity = 3;
    }

    public BusData(PassengerColor color)
    {
        this.color = color;
        this.capacity = 3;
    }
}
