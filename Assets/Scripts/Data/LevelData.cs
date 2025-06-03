using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "LevelData", menuName = "BusJam/Level Data", order = 0)]
public class LevelData : ScriptableObject
{
    public int levelId;
    public float timerDuration = 30f;
    public int gridWidth = 4; // Default width
    public int gridHeight = 4; // Default height

    [Header("Passenger Grid Configuration")]
    // Defines the colors and order of passengers to be placed on the standard grid.
    // The size of this list should ideally match gridWidth * gridHeight.
    public List<PassengerColor> standardGridPassengers = new List<PassengerColor>(); 
    
    // Manual passengerSpawns list and useStandardPassengerGrid boolean removed.
    public List<BusData> busConfigurations = new List<BusData>();
    //TODO: Add queue area layouts, other obstacles, goals, etc.
}
