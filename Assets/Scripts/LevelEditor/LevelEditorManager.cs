using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.IO;
using TMPro;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class LevelEditorManager : MonoBehaviour
{
    [Header("Level Selection UI")]
    public TMP_Dropdown levelListDropdown;
    public Button createNewLevelButton;

    [Header("Level Properties UI")]
    public TMP_InputField levelNameInputField;
    public TMP_InputField gridWidthInputField;
    public TMP_InputField gridHeightInputField;
    public TMP_InputField timerDurationInputField;

    [Header("Bus Spawn Queue UI")]
    public GameObject busSpawnQueueContainer; 
    public Button addBusToQueueButton;
    public GameObject busQueueItemPrefab;

    [Header("Grid UI")]
    public GameObject gridContainer;
    public GameObject gridCellPrefab;

    [Header("Palette UI")]
    public GameObject paletteContainer;
    public GameObject paletteItemPrefab;

    [Header("Action Buttons UI")]
    public Button saveLevelButton;
    public Button clearGridButton;

    // Data
    private List<LevelData> allLevels; 
    private LevelData currentSelectedLevel;
    private int currentEditingGridWidth = 4;
    private int currentEditingGridHeight = 4;

    private class EditorGridCell
    {
        public int x, y;
        public PassengerColor passengerColor;
        public Image uiImage;
        public Button uiButton;
    }
    private List<EditorGridCell> editorGridCells = new List<EditorGridCell>();

    private PassengerColor selectedPaletteColor = PassengerColor.Red; // Default


    private void Start()
    {
        LoadAllLevels();
        PopulateLevelListDropdown();
        SetupButtonListeners();
        InitializePalette();
        if (allLevels.Count == 0)
        {
            if (createNewLevelButton != null && createNewLevelButton.gameObject.activeInHierarchy)
            {
                CreateNewLevel(); 
            } else {
                Debug.LogWarning("error");
            }
        }
        else
        {
            OnLevelSelected(0); 
        }
    }

    private void LoadAllLevels()
    {
        allLevels = new List<LevelData>();
        LevelData[] loadedLevels = Resources.LoadAll<LevelData>("Levels");
        allLevels.AddRange(loadedLevels);
        Debug.Log($"Loaded {allLevels.Count} levels.");
    }

    private void PopulateLevelListDropdown()
    {
        if (levelListDropdown == null) return;
        levelListDropdown.ClearOptions();
        List<TMP_Dropdown.OptionData> options = new List<TMP_Dropdown.OptionData>();
        foreach (var level in allLevels)
        {
            options.Add(new TMP_Dropdown.OptionData(level.name));
        }
        levelListDropdown.AddOptions(options);
        
        levelListDropdown.onValueChanged.RemoveAllListeners(); 
        levelListDropdown.onValueChanged.AddListener(OnLevelSelected);
        
        if (currentSelectedLevel != null) {
            int currentIndex = allLevels.FindIndex(l => l == currentSelectedLevel);
            if (currentIndex != -1) {
                levelListDropdown.SetValueWithoutNotify(currentIndex);
            } else if (allLevels.Count > 0) {
                 levelListDropdown.SetValueWithoutNotify(0);
                 OnLevelSelected(0);
            }
        } else if (allLevels.Count > 0) {
            levelListDropdown.SetValueWithoutNotify(0);
            OnLevelSelected(0);
        }
    }

    private void OnLevelSelected(int index)
    {
        if (allLevels == null || allLevels.Count == 0) {
             currentSelectedLevel = null;
        } else if (index < 0 || index >= allLevels.Count) {
            currentSelectedLevel = allLevels[0];
            if(levelListDropdown != null) levelListDropdown.SetValueWithoutNotify(0);
        } else {
            currentSelectedLevel = allLevels[index];
        }
        DisplayLevelData();
    }

    private void DisplayLevelData()
    {
        if (currentSelectedLevel == null)
        {
            if (levelNameInputField != null) levelNameInputField.text = "N/A";
            if (timerDurationInputField != null) timerDurationInputField.text = "";
            if (gridWidthInputField != null) gridWidthInputField.text = currentEditingGridWidth.ToString();
            if (gridHeightInputField != null) gridHeightInputField.text = currentEditingGridHeight.ToString();
            SetUIInteractable(false); 
            RebuildEditorGrid(); 
            UpdateBusSpawnQueueUI();
            return;
        }
        SetUIInteractable(true);

        if (levelNameInputField != null) {
            levelNameInputField.text = currentSelectedLevel.name;
            levelNameInputField.interactable = false;
        }
        if (timerDurationInputField != null) timerDurationInputField.text = currentSelectedLevel.timerDuration.ToString();
        if (gridWidthInputField != null) gridWidthInputField.text = currentSelectedLevel.gridWidth.ToString();
        if (gridHeightInputField != null) gridHeightInputField.text = currentSelectedLevel.gridHeight.ToString();

        currentEditingGridWidth = currentSelectedLevel.gridWidth;
        currentEditingGridHeight = currentSelectedLevel.gridHeight;
        
        RebuildEditorGrid();
        UpdateBusSpawnQueueUI();
    }

    private void SetUIInteractable(bool isInteractable)
    {
        if (timerDurationInputField != null) timerDurationInputField.interactable = isInteractable;
        if (gridWidthInputField != null) gridWidthInputField.interactable = isInteractable;
        if (gridHeightInputField != null) gridHeightInputField.interactable = isInteractable;
        if (saveLevelButton != null) saveLevelButton.interactable = isInteractable;
        if (clearGridButton != null) clearGridButton.interactable = isInteractable;
        if (addBusToQueueButton != null) addBusToQueueButton.interactable = isInteractable;
    }
    
    private void RebuildEditorGrid()
    {
        if (gridContainer == null || gridCellPrefab == null) return;

        foreach (Transform child in gridContainer.transform) Destroy(child.gameObject);
        editorGridCells.Clear();

        if (!int.TryParse(gridWidthInputField.text, out currentEditingGridWidth) || currentEditingGridWidth <= 0) {
            currentEditingGridWidth = (currentSelectedLevel != null) ? currentSelectedLevel.gridWidth : 4;
            if (gridWidthInputField != null) gridWidthInputField.text = currentEditingGridWidth.ToString();
        }
        if (!int.TryParse(gridHeightInputField.text, out currentEditingGridHeight) || currentEditingGridHeight <= 0) {
            currentEditingGridHeight = (currentSelectedLevel != null) ? currentSelectedLevel.gridHeight : 4;
            if (gridHeightInputField != null) gridHeightInputField.text = currentEditingGridHeight.ToString();
        }
        
        GridLayoutGroup gridLayout = gridContainer.GetComponent<GridLayoutGroup>();
        if (gridLayout != null) gridLayout.constraintCount = currentEditingGridWidth;

        for (int y = 0; y < currentEditingGridHeight; y++)
        {
            for (int x = 0; x < currentEditingGridWidth; x++)
            {
                GameObject cellGO = Instantiate(gridCellPrefab, gridContainer.transform);
                Image cellImage = cellGO.GetComponent<Image>();
                Button cellButton = cellGO.GetComponent<Button>();
                
                EditorGridCell cell = new EditorGridCell { x = x, y = y, uiImage = cellImage, uiButton = cellButton };
                
                if (cellButton != null)
                {
                    int localX = x; 
                    int localY = y;
                    cellButton.onClick.AddListener(() => OnGridCellClicked(localX, localY));
                }

                if (currentSelectedLevel != null && currentSelectedLevel.standardGridPassengers != null)
                {
                    int index = y * currentSelectedLevel.gridWidth + x; 
                    if (x < currentSelectedLevel.gridWidth && y < currentSelectedLevel.gridHeight &&
                        index < currentSelectedLevel.standardGridPassengers.Count)
                    {
                        cell.passengerColor = currentSelectedLevel.standardGridPassengers[index];
                    }
                    else cell.passengerColor = PassengerColor.Red;
                }
                else cell.passengerColor = PassengerColor.Red;
                
                UpdateCellVisual(cell);
                editorGridCells.Add(cell);
            }
        }
    }

    private void OnGridCellClicked(int x, int y)
    {
        if (currentSelectedLevel == null) return;

        EditorGridCell clickedCell = editorGridCells.Find(cell => cell.x == x && cell.y == y);
        if (clickedCell != null)
        {
            clickedCell.passengerColor = selectedPaletteColor;
            UpdateCellVisual(clickedCell);
        }
    }

    private void UpdateCellVisual(EditorGridCell cell)
    {
        if (cell.uiImage != null)
        {
            cell.uiImage.color = GetUnityColorForPassenger(cell.passengerColor);
        }
    }

    private void InitializePalette()
    {
        if (paletteContainer == null || paletteItemPrefab == null) return;
        foreach (Transform child in paletteContainer.transform) Destroy(child.gameObject);
        
        foreach (PassengerColor color in System.Enum.GetValues(typeof(PassengerColor)))
        {
            CreatePaletteButtonTMP(color);
        }
        SelectPaletteColor(PassengerColor.Red);
    }

    private void CreatePaletteButtonTMP(PassengerColor color)
    {
        GameObject paletteButtonGO = Instantiate(paletteItemPrefab, paletteContainer.transform);
        Image buttonImage = paletteButtonGO.GetComponent<Image>();
        if(buttonImage != null) buttonImage.color = GetUnityColorForPassenger(color);
        
        Button button = paletteButtonGO.GetComponent<Button>();
        if(button != null) button.onClick.AddListener(() => SelectPaletteColor(color));
        
        TMP_Text buttonText = paletteButtonGO.GetComponentInChildren<TMP_Text>();
        if (buttonText != null) buttonText.text = color.ToString();
    }

    private void SelectPaletteColor(PassengerColor color)
    {
        selectedPaletteColor = color;
    }
    
    Color GetUnityColorForPassenger(PassengerColor pc)
    {
        switch (pc)
        {
            case PassengerColor.Red: return Color.red;
            case PassengerColor.Green: return Color.green;
            case PassengerColor.Blue: return Color.blue;
            case PassengerColor.Yellow: return Color.yellow;
            case PassengerColor.Purple: return new Color(0.5f, 0f, 0.5f);
            case PassengerColor.Orange: return new Color(1f, 0.5f, 0f);
            case PassengerColor.Black: return Color.black;
            default: return Color.grey;
        }
    }

    private void UpdateBusSpawnQueueUI()
    {
        if (busSpawnQueueContainer == null || busQueueItemPrefab == null) return;
        foreach (Transform child in busSpawnQueueContainer.transform) Destroy(child.gameObject);

        if (currentSelectedLevel == null || currentSelectedLevel.busConfigurations == null) return;

        for(int i=0; i < currentSelectedLevel.busConfigurations.Count; i++)
        {
            BusData busData = currentSelectedLevel.busConfigurations[i];
            GameObject itemGO = Instantiate(busQueueItemPrefab, busSpawnQueueContainer.transform);
            Image itemImage = itemGO.GetComponent<Image>();
            if(itemImage != null) itemImage.color = GetUnityColorForPassenger(busData.color);
            
            Button itemButton = itemGO.GetComponent<Button>();
            if (itemButton != null)
            {
                int busIndex = i;
                itemButton.onClick.AddListener(() => OnBusQueueItemClicked(busIndex, itemImage));
            }
        }
    }

    private void OnBusQueueItemClicked(int busIndex, Image busItemImage)
    {
        if (currentSelectedLevel == null || busIndex < 0 || busIndex >= currentSelectedLevel.busConfigurations.Count)
        {
            Debug.LogError("Invalid bus item clicked or no level selected.");
            return;
        }
        
        currentSelectedLevel.busConfigurations[busIndex].color = selectedPaletteColor;
        
        if (busItemImage != null)
        {
            busItemImage.color = GetUnityColorForPassenger(selectedPaletteColor);
        }
    }

    private void AddBusToQueue()
    {
        if (currentSelectedLevel == null) return;
        PassengerColor newBusColor = PassengerColor.Red; // Default red
        if (System.Enum.GetValues(typeof(PassengerColor)).Length > 0)
        {
            bool foundRed = false;
            foreach(PassengerColor c in System.Enum.GetValues(typeof(PassengerColor))) {
                if (c == PassengerColor.Red) {
                    newBusColor = PassengerColor.Red;
                    foundRed = true;
                    break;
                }
            }
            if (!foundRed) {
                 newBusColor = (PassengerColor)System.Enum.GetValues(typeof(PassengerColor)).GetValue(0);
            }
        } else {
            Debug.LogError("No PassengerColors");
            return;
        }
        currentSelectedLevel.busConfigurations.Add(new BusData(newBusColor));
        UpdateBusSpawnQueueUI();
    }
    
    private void SetupButtonListeners()
    {
        if (createNewLevelButton != null) createNewLevelButton.onClick.AddListener(CreateNewLevel);
        if (saveLevelButton != null) saveLevelButton.onClick.AddListener(SaveCurrentLevel);
        if (gridWidthInputField != null) gridWidthInputField.onEndEdit.AddListener(OnGridDimensionChanged);
        if (gridHeightInputField != null) gridHeightInputField.onEndEdit.AddListener(OnGridDimensionChanged);
        if (addBusToQueueButton != null) addBusToQueueButton.onClick.AddListener(AddBusToQueue);
        if (clearGridButton != null) clearGridButton.onClick.AddListener(ClearGrid);
    }

    private void OnGridDimensionChanged(string val) 
    {
        RebuildEditorGrid();
    }

    private void ClearGrid()
    {
        if (currentSelectedLevel == null) return;
        foreach(var cell in editorGridCells)
        {
            cell.passengerColor = PassengerColor.Red;
            UpdateCellVisual(cell);
        }
    }

    private void SaveCurrentLevel()
    {
        if (currentSelectedLevel == null)
        {
            Debug.LogError("No level selected to save.");
            return;
        }

        if (timerDurationInputField != null && float.TryParse(timerDurationInputField.text, out float duration)) 
            currentSelectedLevel.timerDuration = duration;
        
        if (gridWidthInputField != null && int.TryParse(gridWidthInputField.text, out int width) && width > 0)
            currentSelectedLevel.gridWidth = width;
        if (gridHeightInputField != null && int.TryParse(gridHeightInputField.text, out int height) && height > 0)
            currentSelectedLevel.gridHeight = height;

        currentSelectedLevel.standardGridPassengers.Clear();
        int totalCells = currentSelectedLevel.gridWidth * currentSelectedLevel.gridHeight;
        for (int i = 0; i < totalCells; i++)
        {
            currentSelectedLevel.standardGridPassengers.Add(PassengerColor.Red);
        }

        foreach (var cell in editorGridCells)
        {
            if (cell.x < currentSelectedLevel.gridWidth && cell.y < currentSelectedLevel.gridHeight)
            {
                int index = cell.y * currentSelectedLevel.gridWidth + cell.x;
                currentSelectedLevel.standardGridPassengers[index] = cell.passengerColor;
            }
        }
        
#if UNITY_EDITOR
        EditorUtility.SetDirty(currentSelectedLevel);
        AssetDatabase.SaveAssets();
        Debug.Log($"Level {currentSelectedLevel.name} saved with Grid: {currentSelectedLevel.gridWidth}x{currentSelectedLevel.gridHeight}.");
#endif
    }

    private void CreateNewLevel()
    {
#if UNITY_EDITOR
        LevelData newLevel = ScriptableObject.CreateInstance<LevelData>();
        
        int newLevelId = 1;
        string basePath = "Assets/Resources/Levels";
        if (!Directory.Exists(basePath)) {
            Directory.CreateDirectory(basePath);
        }

        while (AssetDatabase.LoadAssetAtPath<LevelData>($"{basePath}/Level_{newLevelId}.asset") != null)
        {
            newLevelId++;
        }
        newLevel.name = $"Level_{newLevelId}"; 
        newLevel.levelId = newLevelId; 
        newLevel.standardGridPassengers = new List<PassengerColor>();
        for(int i = 0; i < newLevel.gridWidth * newLevel.gridHeight; i++) {
            newLevel.standardGridPassengers.Add(PassengerColor.Red);
        }
        newLevel.busConfigurations = new List<BusData>();

        string assetPath = $"{basePath}/{newLevel.name}.asset";
        AssetDatabase.CreateAsset(newLevel, assetPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh(); 

        allLevels.Add(newLevel);
        PopulateLevelListDropdown(); 
        
        int newIndex = allLevels.FindIndex(l => l == newLevel);
        if (newIndex != -1) {
            levelListDropdown.value = newIndex; 
        }
        Debug.Log($"Created new level: {assetPath}");
#endif
    }
}
