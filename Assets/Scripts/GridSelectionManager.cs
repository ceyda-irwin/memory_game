using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

[System.Serializable]
public class GridSize
{
    public int rows;
    public int cols;
    public string displayName;
    public string description;
    
    public GridSize(int r, int c, string name, string desc)
    {
        rows = r;
        cols = c;
        displayName = name;
        description = desc;
    }
}

public class GridSelectionManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Transform buttonContainer;
    [SerializeField] private Button backButton;
    [SerializeField] private Button startGameButton;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private Button gridButtonPrefab;
    
    [Header("Grid Options")]
    [SerializeField] private GridSize[] availableGridSizes = new GridSize[]
    {
        new GridSize(4, 4, "4x4", "Klasik - 16 kart"),
        new GridSize(5, 4, "5x4", "Orta - 20 kart"),
        new GridSize(6, 4, "6x4", "Zor - 24 kart"),
        new GridSize(5, 6, "5x6", "Çok Zor - 30 kart"),
        new GridSize(6, 6, "6x6", "Uzman - 36 kart")
    };
    
    [Header("Settings")]
    [SerializeField] private Color normalButtonColor = Color.white;
    [SerializeField] private Color selectedButtonColor = Color.green;
    [SerializeField] private Color hoverButtonColor = Color.yellow;
    
    private GridSize selectedGridSize;
    private Button[] gridButtons;
    
    private void Start()
    {
        InitializeUI();
        CreateGridButtons();
        SelectDefaultGrid();
        SetupStartGameButton();
    }
    
    private void InitializeUI()
    {
        if (descriptionText != null)
            descriptionText.text = "Grid Boyutu Seçin";
        
        if (backButton != null)
            backButton.onClick.AddListener(GoBack);   
    }
    
    private void SetupStartGameButton()
    {
        if (startGameButton != null)
        {
            startGameButton.onClick.RemoveAllListeners();
            startGameButton.onClick.AddListener(StartGame);
            Debug.Log("Start Game button connected successfully!");
        }
        else
        {
            Debug.LogWarning("Start Game button not assigned in Inspector!");
        }
    }
    
    private void CreateGridButtons()
    {
        if (buttonContainer == null || gridButtonPrefab == null)
        {
            Debug.LogError("GridSelectionManager: Button container or prefab not assigned!");
            return;
        }
        
        // Clear existing buttons
        foreach (Transform child in buttonContainer)
        {
            if (Application.isPlaying)
                Destroy(child.gameObject);
            else
                DestroyImmediate(child.gameObject);
        }
        
        gridButtons = new Button[availableGridSizes.Length];
        
        for (int i = 0; i < availableGridSizes.Length; i++)
        {
            GridSize gridSize = availableGridSizes[i];
            
            // Create button
            Button button = Instantiate(gridButtonPrefab, buttonContainer);
            button.name = $"GridButton_{gridSize.rows}x{gridSize.cols}";
            
            // Setup button text
            TMP_Text buttonText = button.GetComponentInChildren<TMP_Text>();
            if (buttonText != null)
            {
                buttonText.text = $"{gridSize.displayName}\n{gridSize.description}";
            }
            
            // Setup button colors
            ColorBlock colors = button.colors;
            colors.normalColor = normalButtonColor;
            colors.selectedColor = selectedButtonColor;
            colors.highlightedColor = hoverButtonColor;
            button.colors = colors;
            
            // Add click listener
            int index = i; // Capture for closure
            button.onClick.AddListener(() => SelectGridSize(index));
            
            // Add hover effects
            AddHoverEffects(button);
            
            gridButtons[i] = button;
        }
    }
    
    private void AddHoverEffects(Button button)
    {
        // Add hover sound or visual effects here if needed
        // For now, we'll just use the color changes
    }
    
    private void SelectDefaultGrid()
    {
        if (availableGridSizes.Length > 0)
        {
            SelectGridSize(0); // Select first grid by default
        }
    }
    
    public void SelectGridSize(int index)
    {
        if (index < 0 || index >= availableGridSizes.Length)
        {
            Debug.LogError($"Invalid grid size index: {index}");
            return;
        }
        
        selectedGridSize = availableGridSizes[index];
        
        // Update button visuals
        UpdateButtonVisuals();
        
        // Update description
        if (descriptionText != null)
        {
            descriptionText.text = $"Seçilen: {selectedGridSize.displayName} - {selectedGridSize.description}";
        }
        
        Debug.Log($"Selected grid size: {selectedGridSize.displayName}");
    }
    
    private void UpdateButtonVisuals()
    {
        for (int i = 0; i < gridButtons.Length; i++)
        {
            if (gridButtons[i] != null)
            {
                bool isSelected = (availableGridSizes[i].rows == selectedGridSize.rows && 
                                 availableGridSizes[i].cols == selectedGridSize.cols);
                
                // Update button appearance
                Image buttonImage = gridButtons[i].GetComponent<Image>();
                if (buttonImage != null)
                {
                    buttonImage.color = isSelected ? selectedButtonColor : normalButtonColor;
                }
            }
        }
    }
    
    public void StartGame()
    {
        if (selectedGridSize == null)
        {
            Debug.LogError("No grid size selected!");
            return;
        }
        
        // Save selected grid size to PlayerPrefs
        PlayerPrefs.SetInt("SelectedRows", selectedGridSize.rows);
        PlayerPrefs.SetInt("SelectedCols", selectedGridSize.cols);
        PlayerPrefs.Save();
        
        // Load game scene
        LoadGameScene();
    }
    
    private void LoadGameScene()
    {
        // Assuming the game scene is at build index 1
        // You can change this to match your scene setup
        SceneManager.LoadScene(1);
    }
    
    public void GoBack()
    {
        // Load main menu or previous scene
        // Assuming main menu is at build index 0
        SceneManager.LoadScene(0);
    }
    
    public void SetGridSizes(GridSize[] newGridSizes)
    {
        availableGridSizes = newGridSizes;
        if (Application.isPlaying)
        {
            CreateGridButtons();
            SelectDefaultGrid();
        }
    }
    
    public GridSize GetSelectedGridSize()
    {
        return selectedGridSize;
    }
    
    // Static method to get saved grid size
    public static GridSize GetSavedGridSize()
    {
        int rows = PlayerPrefs.GetInt("SelectedRows", 4);
        int cols = PlayerPrefs.GetInt("SelectedCols", 4);
        return new GridSize(rows, cols, $"{rows}x{cols}", "");
    }
}
