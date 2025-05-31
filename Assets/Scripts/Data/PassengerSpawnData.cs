using UnityEngine;
using System;

[Serializable]
public class PassengerSpawnData
{
    public Vector3 position;
    public PassengerColor color;

    public PassengerSpawnData(Vector3 position, PassengerColor color)
    {
        this.position = position;
        this.color = color;
    }
}
