using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "LevelData", menuName = "BusJam/Level Data", order = 0)]
public class LevelData : ScriptableObject
{
    public int levelId;
    public float timerDuration = 30f;

    [Header("Standard Passenger Grid")]
    public bool useStandardPassengerGrid = true; // Does this level use the predefined global grid?
    // Defines the colors and order of passengers to be placed on the standard grid.
    // The number of passengers will be Min(standard grid capacity, gridPassengerColors.Count)
    public List<PassengerColor> standardGridPassengers = new List<PassengerColor>(); 
    
    [Header("Additional Manual Passenger Spawns (Optional)")]
    public List<PassengerSpawnData> passengerSpawns = new List<PassengerSpawnData>(); // Can be used in addition to or instead of grid
    public List<BusData> busConfigurations = new List<BusData>();
    //TODO: Add queue area layouts, other obstacles, goals, etc.
}
