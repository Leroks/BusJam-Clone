using UnityEngine;
using UnityEngine.UI; // Still needed for Image, Button, GridLayoutGroup etc.
using System.Collections.Generic;
using System.IO;
using TMPro; // Added for TextMeshPro components

#if UNITY_EDITOR
using UnityEditor;
#endif

public class LevelEditorManager : MonoBehaviour
{
    [Header("Level Selection UI")]
    public TMP_Dropdown levelListDropdown; // Changed to TMP_Dropdown
    public Button createNewLevelButton;
    public Button renameLevelButton; // Functionality not yet implemented
    public Button globalValidationButton; // Functionality not yet implemented

    [Header("Level Properties UI")]
    public TMP_InputField levelNameInputField; // Changed to TMP_InputField (Displays ScriptableObject name, read-only for now)
    public TMP_InputField gridWidthInputField; // Changed to TMP_InputField
    public TMP_InputField gridHeightInputField; // Changed to TMP_InputField
    public TMP_InputField timerDurationInputField; // Changed to TMP_InputField

    [Header("Bus Spawn Queue UI")]
    public GameObject busSpawnQueueContainer; 
    public Button addBusToQueueButton;
    public GameObject busQueueItemPrefab; // Prefab: Button with Image and optional TMP_Text

    [Header("Grid UI")]
    public GameObject gridContainer;
    public GameObject gridCellPrefab; // Prefab: Button with Image

    [Header("Palette UI")]
    public GameObject paletteContainer;
    public GameObject paletteItemPrefab; // Prefab: Button with Image and TMP_Text

    [Header("Action Buttons UI")]
    public Button saveLevelButton;
    public Button testLevelButton; // Functionality not yet implemented
    public Button clearGridButton;

    // --- Data ---
    private List<LevelData> allLevels; 
    private LevelData currentSelectedLevel;
    private int currentEditingGridWidth = 4;
    private int currentEditingGridHeight = 4;

    public class EditorGridCell
    {
        public int x, y;
        public PassengerColor passengerColor;
        public Image uiImage;
        public Button uiButton;
    }
    private List<EditorGridCell> editorGridCells = new List<EditorGridCell>();

    private PassengerColor selectedPaletteColor = PassengerColor.Red; // Default to Red


    void Start()
    {
        LoadAllLevels();
        PopulateLevelListDropdown();
        SetupButtonListeners();
        InitializePalette();
        if (allLevels.Count == 0)
        {
            if (createNewLevelButton != null && createNewLevelButton.gameObject.activeInHierarchy) // Check if button is usable
            {
                CreateNewLevel(); 
            } else {
                Debug.LogWarning("No levels found and CreateNewLevel button is not available. Please create a level manually or ensure the button is set up.");
                // Handle UI state for no levels (e.g. disable most controls)
            }
        }
        else
        {
            OnLevelSelected(0); 
        }
    }

    void LoadAllLevels()
    {
        allLevels = new List<LevelData>();
        LevelData[] loadedLevels = Resources.LoadAll<LevelData>("Levels");
        allLevels.AddRange(loadedLevels);
        // Sort levels by name or ID if desired
        // allLevels.Sort((a, b) => a.name.CompareTo(b.name)); 
        Debug.Log($"Loaded {allLevels.Count} levels.");
    }

    void PopulateLevelListDropdown()
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

        // Refresh visual for current selection if list was repopulated
        if (currentSelectedLevel != null) {
            int currentIndex = allLevels.FindIndex(l => l == currentSelectedLevel);
            if (currentIndex != -1) {
                levelListDropdown.SetValueWithoutNotify(currentIndex);
            } else if (allLevels.Count > 0) {
                 levelListDropdown.SetValueWithoutNotify(0); // Default to first if current is no longer in list
                 OnLevelSelected(0);
            }
        } else if (allLevels.Count > 0) {
            levelListDropdown.SetValueWithoutNotify(0);
            OnLevelSelected(0);
        }
    }

    void OnLevelSelected(int index)
    {
        if (allLevels == null || allLevels.Count == 0) {
             currentSelectedLevel = null;
        } else if (index < 0 || index >= allLevels.Count) {
            currentSelectedLevel = allLevels[0]; // Default to first if out of bounds
            if(levelListDropdown != null) levelListDropdown.SetValueWithoutNotify(0);
        } else {
            currentSelectedLevel = allLevels[index];
        }
        DisplayLevelData();
    }

    void DisplayLevelData()
    {
        if (currentSelectedLevel == null)
        {
            if (levelNameInputField != null) levelNameInputField.text = "N/A";
            if (timerDurationInputField != null) timerDurationInputField.text = "";
            if (gridWidthInputField != null) gridWidthInputField.text = currentEditingGridWidth.ToString();
            if (gridHeightInputField != null) gridHeightInputField.text = currentEditingGridHeight.ToString();
            // Disable most input fields if no level is loaded
            SetUIInteractable(false); 
            RebuildEditorGrid(); 
            UpdateBusSpawnQueueUI();
            return;
        }
        SetUIInteractable(true);

        if (levelNameInputField != null) {
            levelNameInputField.text = currentSelectedLevel.name;
            levelNameInputField.interactable = false; // Name is usually managed by asset renaming
        }
        if (timerDurationInputField != null) timerDurationInputField.text = currentSelectedLevel.timerDuration.ToString();
        if (gridWidthInputField != null) gridWidthInputField.text = currentSelectedLevel.gridWidth.ToString();
        if (gridHeightInputField != null) gridHeightInputField.text = currentSelectedLevel.gridHeight.ToString();

        currentEditingGridWidth = currentSelectedLevel.gridWidth;
        currentEditingGridHeight = currentSelectedLevel.gridHeight;
        
        RebuildEditorGrid();
        UpdateBusSpawnQueueUI();
    }

    void SetUIInteractable(bool isInteractable)
    {
        if (timerDurationInputField != null) timerDurationInputField.interactable = isInteractable;
        if (gridWidthInputField != null) gridWidthInputField.interactable = isInteractable;
        if (gridHeightInputField != null) gridHeightInputField.interactable = isInteractable;
        if (saveLevelButton != null) saveLevelButton.interactable = isInteractable;
        if (clearGridButton != null) clearGridButton.interactable = isInteractable;
        if (addBusToQueueButton != null) addBusToQueueButton.interactable = isInteractable;
        // Palette and grid cells might also need their interactability managed
    }
    
    void RebuildEditorGrid()
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
                    else cell.passengerColor = PassengerColor.Red; // Default to Red
                }
                else cell.passengerColor = PassengerColor.Red; // Default to Red
                
                UpdateCellVisual(cell);
                editorGridCells.Add(cell);
            }
        }
    }

    void OnGridCellClicked(int x, int y)
    {
        if (currentSelectedLevel == null) return; // Don't allow editing if no level loaded

        EditorGridCell clickedCell = editorGridCells.Find(cell => cell.x == x && cell.y == y);
        if (clickedCell != null)
        {
            clickedCell.passengerColor = selectedPaletteColor;
            UpdateCellVisual(clickedCell);
        }
    }

    void UpdateCellVisual(EditorGridCell cell)
    {
        if (cell.uiImage != null)
        {
            cell.uiImage.color = GetUnityColorForPassenger(cell.passengerColor);
        }
    }

    void InitializePalette()
    {
        if (paletteContainer == null || paletteItemPrefab == null) return;
        foreach (Transform child in paletteContainer.transform) Destroy(child.gameObject);

        // Create buttons for all defined passenger colors
        foreach (PassengerColor color in System.Enum.GetValues(typeof(PassengerColor)))
        {
            CreatePaletteButtonTMP(color);
        }
        // Set initial selected palette color (e.g. Red)
        SelectPaletteColor(PassengerColor.Red);
    }

    void CreatePaletteButtonTMP(PassengerColor color)
    {
        GameObject paletteButtonGO = Instantiate(paletteItemPrefab, paletteContainer.transform);
        Image buttonImage = paletteButtonGO.GetComponent<Image>();
        if(buttonImage != null) buttonImage.color = GetUnityColorForPassenger(color);
        
        Button button = paletteButtonGO.GetComponent<Button>();
        if(button != null) button.onClick.AddListener(() => SelectPaletteColor(color));
        
        TMP_Text buttonText = paletteButtonGO.GetComponentInChildren<TMP_Text>();
        if (buttonText != null) buttonText.text = color.ToString();
    }

    void SelectPaletteColor(PassengerColor color)
    {
        selectedPaletteColor = color;
        // Optionally highlight selected palette button
    }
    
    Color GetUnityColorForPassenger(PassengerColor pc)
    {
        switch (pc)
        {
            case PassengerColor.Red: return Color.red;
            case PassengerColor.Green: return Color.green;
            case PassengerColor.Blue: return Color.blue;
            case PassengerColor.Yellow: return Color.yellow;
            case PassengerColor.Purple: return new Color(0.5f, 0f, 0.5f); // Magenta-like
            case PassengerColor.Orange: return new Color(1f, 0.5f, 0f);
            case PassengerColor.Black: return Color.black;
            // Removed PassengerColor.None case
            default: return Color.grey; // Fallback for unmapped colors
        }
    }

    void UpdateBusSpawnQueueUI()
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
            
            // TMP_Text itemText = itemGO.GetComponentInChildren<TMP_Text>();
            // if(itemText != null) itemText.text = busData.color.ToString();
            // Add button to remove or change color later
        }
    }

    void AddBusToQueue()
    {
        if (currentSelectedLevel == null) return;
        // Default to the first color in the enum (which should be Red now)
        // or a specific default like PassengerColor.Red
        PassengerColor newBusColor = PassengerColor.Red; 
        if (System.Enum.GetValues(typeof(PassengerColor)).Length > 0)
        {
            // Ensure there's at least one color defined.
            // If PassengerColor.Red is not guaranteed to be first, explicitly set it.
            bool foundRed = false;
            foreach(PassengerColor c in System.Enum.GetValues(typeof(PassengerColor))) {
                if (c == PassengerColor.Red) {
                    newBusColor = PassengerColor.Red;
                    foundRed = true;
                    break;
                }
            }
            if (!foundRed) { // If Red is not in enum (should not happen), pick first available
                 newBusColor = (PassengerColor)System.Enum.GetValues(typeof(PassengerColor)).GetValue(0);
            }
        } else {
            Debug.LogError("No PassengerColors defined in enum!");
            return; // Cannot add a bus without a color
        }
        currentSelectedLevel.busConfigurations.Add(new BusData(newBusColor));
        UpdateBusSpawnQueueUI();
    }
    
    void SetupButtonListeners()
    {
        if (createNewLevelButton != null) createNewLevelButton.onClick.AddListener(CreateNewLevel);
        if (saveLevelButton != null) saveLevelButton.onClick.AddListener(SaveCurrentLevel);
        if (gridWidthInputField != null) gridWidthInputField.onEndEdit.AddListener(OnGridDimensionChanged);
        if (gridHeightInputField != null) gridHeightInputField.onEndEdit.AddListener(OnGridDimensionChanged);
        if (addBusToQueueButton != null) addBusToQueueButton.onClick.AddListener(AddBusToQueue);
        if (clearGridButton != null) clearGridButton.onClick.AddListener(ClearGrid);
    }

    void OnGridDimensionChanged(string val) 
    {
        RebuildEditorGrid();
    }

    void ClearGrid()
    {
        if (currentSelectedLevel == null) return;
        foreach(var cell in editorGridCells)
        {
            cell.passengerColor = PassengerColor.Red; // Default to Red
            UpdateCellVisual(cell);
        }
    }

    public void SaveCurrentLevel()
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
            currentSelectedLevel.standardGridPassengers.Add(PassengerColor.Red); // Initialize with Red
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
#else
        Debug.LogWarning("Saving ScriptableObjects at runtime is complex. This feature is primarily for editor use.");
#endif
    }

    public void CreateNewLevel()
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
        // Defaults are set in LevelData class definition
        // newLevel.timerDuration = 30f; 
        // newLevel.gridWidth = 4; 
        // newLevel.gridHeight = 4; // Defaults are set in LevelData
        newLevel.standardGridPassengers = new List<PassengerColor>();
        for(int i = 0; i < newLevel.gridWidth * newLevel.gridHeight; i++) {
            newLevel.standardGridPassengers.Add(PassengerColor.Red); // Default new level cells to Red
        }
        newLevel.busConfigurations = new List<BusData>();

        string assetPath = $"{basePath}/{newLevel.name}.asset";
        AssetDatabase.CreateAsset(newLevel, assetPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh(); 

        allLevels.Add(newLevel);
        // Sort again if you want new levels to appear in a sorted order immediately
        // allLevels.Sort((a, b) => a.name.CompareTo(b.name));
        PopulateLevelListDropdown(); 
        
        int newIndex = allLevels.FindIndex(l => l == newLevel);
        if (newIndex != -1) {
            levelListDropdown.value = newIndex; 
            // OnLevelSelected will be called by dropdown's onValueChanged
        }
        Debug.Log($"Created new level: {assetPath}");
#else
        Debug.LogError("Creating ScriptableObjects at runtime is not supported this way.");
#endif
    }
}
