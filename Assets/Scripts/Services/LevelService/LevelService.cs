using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class LevelService
{
    private List<LevelData> _levels;
    public int CurrentLevelNumber { get; private set; } = 1;

    private const string PlayerPrefsLevelKey = "CurrentBusJamLevel";

    public LevelService()
    {
        LoadLevels();
        LoadProgress();
    }

    private void LoadLevels()
    {
        _levels = Resources.LoadAll<LevelData>("Levels").OrderBy(ld => ld.levelId).ToList();
        if (_levels == null || _levels.Count == 0)
        {
            Debug.LogError("No LevelData found in Resources/Levels. Please create some LevelData assets.");
            var defaultLevel = ScriptableObject.CreateInstance<LevelData>();
            defaultLevel.levelId = 1;
            defaultLevel.timerDuration = 30f;
            _levels = new List<LevelData> { defaultLevel };
        }
    }

    public LevelData GetLevelData(int levelNumber)
    {
        if (levelNumber <= 0) levelNumber = 1;

        if (levelNumber > 0 && levelNumber <= _levels.Count)
        {
            LevelData level = _levels.FirstOrDefault(ld => ld.levelId == levelNumber);
            if (level != null) return level;

            Debug.LogWarning($"LevelData for level ID {levelNumber} not found. Returning first available level.");
            return _levels.Count > 0 ? _levels[0] : null;
        }
        
        Debug.LogWarning($"Requested level number {levelNumber} is out of bounds. Max levels: {_levels.Count}. Returning last available level.");
        return _levels.Count > 0 ? _levels.LastOrDefault() : null;
    }

    public LevelData GetCurrentLevelData()
    {
        return GetLevelData(CurrentLevelNumber);
    }

    public void AdvanceToNextLevel()
    {
        if (CurrentLevelNumber < _levels.Count)
        {
            CurrentLevelNumber++;
        }
        else
        {
            Debug.Log("All levels completed! Restarting from level 1");
            CurrentLevelNumber = 1;
        }
        SaveProgress();
    }

    public void LoadProgress()
    {
        CurrentLevelNumber = PlayerPrefs.GetInt(PlayerPrefsLevelKey, 1);
        if (CurrentLevelNumber <= 0) CurrentLevelNumber = 1;
        if (_levels != null && CurrentLevelNumber > _levels.Count && _levels.Count > 0) {
            CurrentLevelNumber = _levels.Count;
        } else if (_levels == null || _levels.Count == 0) {
            CurrentLevelNumber = 1;
        }
    }

    public void SaveProgress()
    {
        PlayerPrefs.SetInt(PlayerPrefsLevelKey, CurrentLevelNumber);
        PlayerPrefs.Save();
    }

    public int GetMaxLevelNumber()
    {
        return _levels?.Count ?? 0;
    }
}
