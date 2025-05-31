using UnityEngine;

[CreateAssetMenu(fileName = "LevelData", menuName = "BusJam/Level Data", order = 0)]
public class LevelData : ScriptableObject
{
    public int levelId;
    public float timerDuration = 30f; // Default timer duration
    //TODO: Add more level-specific obstacles, goals, etc.
}
