using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "LevelData", menuName = "BusJam/Level Data", order = 0)]
public class LevelData : ScriptableObject
{
    public int levelId;
    public float timerDuration = 45f;
    public int gridWidth = 3;
    public int gridHeight = 2;

    [Header("Passenger Grid Configuration")]
    public List<PassengerColor> standardGridPassengers = new List<PassengerColor>(); 
    
    public List<BusData> busConfigurations = new List<BusData>();
}
