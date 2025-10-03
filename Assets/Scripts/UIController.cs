using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class UIController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameManager gm;
    [SerializeField] private Canvas mainCanvas;
    [SerializeField] private GridLayoutGroup gameGrid;
    
    [Header("Screen Size Settings")]
    [SerializeField] private bool enableResponsiveSizing = true;
    [SerializeField] private float minCardSize = 60f;
    [SerializeField] private float maxCardSize = 120f;
    [SerializeField] private float screenPadding = 20f;
    [SerializeField] private float uiPadding = 10f;
    [SerializeField] private float resetButtonHeight = 50f;
    
    [Header("Aspect Ratio Settings")]
    [SerializeField] private bool adaptToAspectRatio = true;
    [SerializeField] private float targetAspectRatio = 16f / 9f;
    
    private RectTransform canvasRect;
    private Vector2 lastScreenSize;
    private bool isInitialized = false;

    private void Start()
    {
        InitializeReferences();
        SetupResponsiveUI();
    }

    private void InitializeReferences()
    {
        if (gm == null)
        {
#if UNITY_2023_1_OR_NEWER
            gm = Object.FindFirstObjectByType<GameManager>();
            if (gm == null) gm = Object.FindAnyObjectByType<GameManager>();
#else
            gm = Object.FindObjectOfType<GameManager>();
#endif
            if (gm == null)
            {
                Debug.LogError("UIController: GameManager not found!");
            }
        }

        if (mainCanvas == null)
            mainCanvas = GetComponentInParent<Canvas>();
        
        if (mainCanvas != null)
            canvasRect = mainCanvas.GetComponent<RectTransform>();
        
        if (gameGrid == null)
        {
#if UNITY_2023_1_OR_NEWER
            gameGrid = Object.FindFirstObjectByType<GridLayoutGroup>();
            if (gameGrid == null) gameGrid = Object.FindAnyObjectByType<GridLayoutGroup>();
#else
            gameGrid = Object.FindObjectOfType<GridLayoutGroup>();
#endif
        }
        
        
        isInitialized = true;
    }

    private void Update()
    {
        if (!enableResponsiveSizing || !isInitialized) return;
        
        Vector2 currentScreenSize = new Vector2(Screen.width, Screen.height);
        if (currentScreenSize != lastScreenSize)
        {
            lastScreenSize = currentScreenSize;
            SetupResponsiveUI();
        }
    }

    public void SetupResponsiveUI()
    {
        if (!isInitialized) return;
        
        // Handle safe area for mobile devices
        HandleSafeArea();
        
        // Adjust grid sizing based on screen size
        if (gameGrid != null)
        {
            AdjustGridSizing();
        }
        
        // Adjust UI scaling for different aspect ratios
        if (adaptToAspectRatio && mainCanvas != null)
        {
            AdjustCanvasScaling();
        }
    }

    private void HandleSafeArea()
    {
        if (canvasRect == null) return;
        
        Rect safeArea = Screen.safeArea;
        Vector2 anchorMin = safeArea.position;
        Vector2 anchorMax = safeArea.position + safeArea.size;
        
        anchorMin.x /= Screen.width;
        anchorMin.y /= Screen.height;
        anchorMax.x /= Screen.width;
        anchorMax.y /= Screen.height;
        
        canvasRect.anchorMin = anchorMin;
        canvasRect.anchorMax = anchorMax;
    }

    private void AdjustGridSizing()
    {
        if (gameGrid == null || canvasRect == null) return;
        
        Vector2 canvasSize = canvasRect.sizeDelta;
        float aspectRatio = canvasSize.x / canvasSize.y;
        
        // Calculate optimal card size based on screen dimensions
        float availableWidth = canvasSize.x - (screenPadding * 2);
        float availableHeight = canvasSize.y - (screenPadding * 2) - 100f - resetButtonHeight; // Reserve space for UI and reset button
        
        // Get grid dimensions from GameManager
        int rows = 4, cols = 4; // Default values
        if (gm != null)
        {
            rows = gm.Rows;
            cols = gm.Cols;
        }
        
        // Calculate card size based on available space
        float cardWidth = availableWidth / cols;
        float cardHeight = availableHeight / rows;
        float cardSize = Mathf.Min(cardWidth, cardHeight);
        
        // Clamp card size to reasonable bounds
        cardSize = Mathf.Clamp(cardSize, minCardSize, maxCardSize);
        
        // Apply spacing to prevent cards from touching
        float spacing = cardSize * 0.1f;
        gameGrid.cellSize = new Vector2(cardSize, cardSize);
        gameGrid.spacing = new Vector2(spacing, spacing);
        
        // Center the grid
        gameGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        gameGrid.constraintCount = cols;
        gameGrid.childAlignment = TextAnchor.MiddleCenter;
    }

    private void AdjustCanvasScaling()
    {
        if (mainCanvas == null) return;
        
        CanvasScaler scaler = mainCanvas.GetComponent<CanvasScaler>();
        if (scaler == null) return;
        
        float currentAspectRatio = (float)Screen.width / Screen.height;
        float aspectRatioDifference = Mathf.Abs(currentAspectRatio - targetAspectRatio);
        
        // If aspect ratio is very different, use scale with screen size
        if (aspectRatioDifference > 0.2f)
        {
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f; // Balance between width and height
        }
        else
        {
            // Use constant pixel size for similar aspect ratios
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
        }
    }

    public void SetResponsiveSizing(bool enabled)
    {
        enableResponsiveSizing = enabled;
        if (enabled)
        {
            SetupResponsiveUI();
        }
    }

    public void SetCardSizeLimits(float min, float max)
    {
        minCardSize = min;
        maxCardSize = max;
        if (enableResponsiveSizing)
        {
            SetupResponsiveUI();
        }
    }


    public void Restart()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
    }

    public void QuitGame() 
    {
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }
}
