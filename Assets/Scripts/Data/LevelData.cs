using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "LevelData", menuName = "BusJam/Level Data", order = 0)]
public class LevelData : ScriptableObject
{
    public int levelId;
    public float timerDuration = 30f;
    public int gridWidth = 4;
    public int gridHeight = 4;

    [Header("Passenger Grid Configuration")]
    public List<PassengerColor> standardGridPassengers = new List<PassengerColor>(); 
    
    public List<BusData> busConfigurations = new List<BusData>();
}
