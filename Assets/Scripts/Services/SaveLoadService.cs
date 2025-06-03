using UnityEngine;

public class GameSaveData
{
    public int currentLevelIndex;
    public float remainingTime;
    public bool isInProgress;
}

public class SaveLoadService
{
    private const string SaveKey = "GameSaveData";

    public void SaveGame(GameSaveData data)
    {
        string json = JsonUtility.ToJson(data);
        PlayerPrefs.SetString(SaveKey, json);
        PlayerPrefs.Save();
        Debug.Log("Game Saved: " + json);
    }

    public GameSaveData LoadGame()
    {
        if (PlayerPrefs.HasKey(SaveKey))
        {
            string json = PlayerPrefs.GetString(SaveKey);
            GameSaveData data = JsonUtility.FromJson<GameSaveData>(json);
            Debug.Log("Game Loaded: " + json);
            return data;
        }
        Debug.Log("No save data found.");
        return null;
    }
}
