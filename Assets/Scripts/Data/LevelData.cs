using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "LevelData", menuName = "BusJam/Level Data", order = 0)]
public class LevelData : ScriptableObject
{
    public int levelId;
    public float timerDuration = 30f;

    [Header("Passenger Grid Configuration")]
    // Defines the colors and order of passengers to be placed on the standard grid 
    // (defined by Transforms in GameManager).
    // The number of passengers spawned will be Min(grid capacity, standardGridPassengers.Count).
    public List<PassengerColor> standardGridPassengers = new List<PassengerColor>(); 
    
    // Manual passengerSpawns list and useStandardPassengerGrid boolean removed.
    public List<BusData> busConfigurations = new List<BusData>();
    //TODO: Add queue area layouts, other obstacles, goals, etc.
}
