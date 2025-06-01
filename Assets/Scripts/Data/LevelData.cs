using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "LevelData", menuName = "BusJam/Level Data", order = 0)]
public class LevelData : ScriptableObject
{
    public int levelId;
    public float timerDuration = 30f;
    
    public List<PassengerSpawnData> passengerSpawns = new List<PassengerSpawnData>();
    public List<BusData> busConfigurations = new List<BusData>();
    //TODO: Add queue area layouts, other obstacles, goals, etc.
}
