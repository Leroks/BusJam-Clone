using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class LevelService
{
    private List<LevelData> _levels;
    public int CurrentLevelIndex { get; private set; } = 0;

    private const string PlayerPrefsLevelKey = "CurrentBusJamLevelIndex";

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

    public LevelData GetLevelDataByIndex(int index)
    {
        if (_levels == null || _levels.Count == 0) return null;
        if (index >= 0 && index < _levels.Count)
        {
            return _levels[index];
        }
        return _levels[Mathf.Clamp(index, 0, _levels.Count - 1)];
    }

    public LevelData GetCurrentLevelData()
    {
        return GetLevelDataByIndex(CurrentLevelIndex);
    }

    public void SetCurrentLevel(int levelIndex)
    {
        CurrentLevelIndex = Mathf.Clamp(levelIndex, 0, _levels.Count - 1);
        SaveProgress();
        Debug.Log($"Current level set to index: {CurrentLevelIndex}");
    }

    public void AdvanceToNextLevel()
    {
        int nextLevelIndex = CurrentLevelIndex + 1;
        if (nextLevelIndex >= _levels.Count)
        {
            Debug.Log("All levels completed! Restarting from level index 0");
            nextLevelIndex = 0;
        }
        SetCurrentLevel(nextLevelIndex);
    }

    public void LoadProgress()
    {
        CurrentLevelIndex = PlayerPrefs.GetInt(PlayerPrefsLevelKey, 0); 
        if (_levels != null && _levels.Count > 0)
        {
            CurrentLevelIndex = Mathf.Clamp(CurrentLevelIndex, 0, _levels.Count - 1);
        }
        else
        {
            CurrentLevelIndex = 0; // Default to 0 if no levels
        }
        Debug.Log($"Loaded progress. CurrentLevelIndex: {CurrentLevelIndex}");
    }

    public void SaveProgress()
    {
        PlayerPrefs.SetInt(PlayerPrefsLevelKey, CurrentLevelIndex);
        PlayerPrefs.Save();
        Debug.Log($"Saved progress. CurrentLevelIndex: {CurrentLevelIndex}");
    }
}
