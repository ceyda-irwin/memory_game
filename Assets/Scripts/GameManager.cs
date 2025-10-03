using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    [Header("Board")]
    [SerializeField] private GridLayoutGroup grid;
    [SerializeField] private Card cardPrefab;
    [SerializeField] private int rows = 4;
    [SerializeField] private int cols = 4;
    
    // Public properties for UI controller access
    public int Rows => rows;
    public int Cols => cols;
    public GridLayoutGroup Grid => grid;

    [Header("Sprites")]
    [SerializeField] private Sprite cardBack;
    [SerializeField] private List<Sprite> cardFronts; // assign at least (rows*cols)/2

    [Header("UI")]
    [SerializeField] private TMP_Text movesText;
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private TMP_Text bestText;
    [SerializeField] private Button resetButton;
    [SerializeField] private Button gridSelectionButton;

    [Header("Timing")]
    [SerializeField] private float revealSettleDelay = 0.2f; // wait after second reveal before evaluating
    [SerializeField] private float mismatchHideDelay = 0.8f; // extra time to show mismatched cards before hiding

    public bool InputLocked { get; private set; }

    private readonly List<Card> spawned = new List<Card>();
    private Card first, second;
    private int moves;
    private float timer;
    private bool running;

    private const string BEST_KEY = "MM_BEST_TIME_"; // suffix with size

    private void Start()
    {
        // Load saved grid size or use default
        LoadGridSize();
        
        SetupBoard(rows, cols);
        ResetUI();
        running = true;
        
        // Setup reset button
        SetupResetButton();
        
        // Notify UI controller that board is ready
        NotifyUIUpdate();
    }
    
    private void LoadGridSize()
    {
        // Try to load saved grid size from GridSelectionManager
        int savedRows = PlayerPrefs.GetInt("SelectedRows", -1);
        int savedCols = PlayerPrefs.GetInt("SelectedCols", -1);
        
        if (savedRows > 0 && savedCols > 0)
        {
            rows = savedRows;
            cols = savedCols;
            Debug.Log($"Loaded grid size: {rows}x{cols}");
        }
        else
        {
            Debug.Log($"Using default grid size: {rows}x{cols}");
        }
    }
    
    private void SetupResetButton()
    {
        if (resetButton != null)
        {
            resetButton.onClick.RemoveAllListeners();
            resetButton.onClick.AddListener(ResetGame);
        }
        
        if (gridSelectionButton != null)
        {
            gridSelectionButton.onClick.RemoveAllListeners();
            gridSelectionButton.onClick.AddListener(GoToGridSelection);
        }
    }
    
    private void NotifyUIUpdate()
    {
#if UNITY_2023_1_OR_NEWER
        UIController uiController = Object.FindFirstObjectByType<UIController>();
        if (uiController == null) uiController = Object.FindAnyObjectByType<UIController>();
#else
        UIController uiController = Object.FindObjectOfType<UIController>();
#endif
        if (uiController != null)
        {
            // Trigger UI update after a short delay to ensure everything is set up
            StartCoroutine(DelayedUIUpdate(uiController));
        }
    }
    
    private System.Collections.IEnumerator DelayedUIUpdate(UIController uiController)
    {
        yield return new WaitForEndOfFrame();
        uiController.SetupResponsiveUI();
    }

    private void Update()
    {
        if (!running) return;
        timer += Time.deltaTime;
        if (timerText != null) timerText.text = $"Time: {FormatTime(timer)}";
    }

    private void ResetUI()
    {
        moves = 0;
        if (movesText != null) movesText.text = $"Moves: {moves}";
        timer = 0f;
        if (timerText != null) timerText.text = "Time: 00:00";

        string k = BEST_KEY + rows + "x" + cols;
        float best = PlayerPrefs.GetFloat(k, -1f);
        if (bestText != null) bestText.text = best < 0 ? "Best: —" : $"Best: {FormatTime(best)}";
    }

    private void SetupBoard(int r, int c)
    {
        if ((r * c) % 2 != 0)
        {
            Debug.LogError("Rows*Cols must be even.");
            return;
        }

        // Clear previous
        foreach (var card in spawned)
            if (card) Destroy(card.gameObject);
        spawned.Clear();

        for (int i = grid.transform.childCount - 1; i >= 0; i--)
            Destroy(grid.transform.GetChild(i).gameObject);

        int pairCount = (r * c) / 2;
        if (cardFronts == null || cardFronts.Count < pairCount)
        {
            Debug.LogError($"Not enough front sprites. Need {pairCount}, have {cardFronts?.Count ?? 0}.");
            return;
        }
        if (cardPrefab == null || grid == null || cardBack == null)
        {
            Debug.LogError("Assign grid, cardPrefab, and cardBack in the inspector.");
            return;
        }

        // Build deck: two of each id
        var deck = new List<(int id, Sprite front)>(r * c);
        for (int i = 0; i < pairCount; i++)
        {
            deck.Add((i, cardFronts[i]));
            deck.Add((i, cardFronts[i]));
        }
        Shuffle(deck);

        // Instantiate
        foreach (var entry in deck)
        {
            var card = Instantiate(cardPrefab, grid.transform);
            card.Init(entry.id, entry.front, cardBack, this);
            spawned.Add(card);
        }
    }

    public void OnCardRevealed(Card card)
    {
        if (card == null) return;
        
        if (first == null)
        {
            first = card;
            return;
        }

        if (second == null && card != first)
        {
            second = card;
            // Prevent any third tap before the coroutine starts
            LockInput(true);
            StartCoroutine(EvaluatePair());
        }
    }

    private IEnumerator EvaluatePair()
    {
        moves++;
        if (movesText != null) movesText.text = $"Moves: {moves}";
        // Allow the second card's reveal animation to complete
        yield return new WaitForSeconds(revealSettleDelay);

        if (first == null || second == null)
        {
            first = null;
            second = null;
            LockInput(false);
            yield break;
        }
        if (first.Id == second.Id)
        {
            first.SetMatched();
            second.SetMatched();
        }
        else
        {
            // Keep the mismatched cards visible for a bit before hiding
            Debug.Log($"Mismatch: {first.Id} vs {second.Id}. Hiding after {mismatchHideDelay}s");
            yield return new WaitForSeconds(mismatchHideDelay);
            first.Hide();
            second.Hide();
        }

        first = null;
        second = null;

        LockInput(false);

        if (AllMatched())
        {
            running = false;
            TrySaveBestTime();
            Debug.Log("You win!");
        }
    }

    private void LockInput(bool v)
    {
        InputLocked = v;
        foreach (var c in spawned) 
            if (c != null) c.Lock(v);
    }

    private bool AllMatched()
    {
        for (int i = 0; i < spawned.Count; i++)
            if (!spawned[i].IsMatched) return false;
        return true;
    }

    private void TrySaveBestTime()
    {
        string k = BEST_KEY + rows + "x" + cols;
        float best = PlayerPrefs.GetFloat(k, -1f);
        if (best < 0 || timer < best)
        {
            PlayerPrefs.SetFloat(k, timer);
            PlayerPrefs.Save();
            if (bestText != null) bestText.text = $"Best: {FormatTime(timer)}";
        }
        else
        {
            if (bestText != null) bestText.text = $"Best: {FormatTime(best)}";
        }
    }

    private static void Shuffle<T>(IList<T> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int j = Random.Range(i, list.Count);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    public void ResetGame()
    {
        // Stop any running coroutines
        StopAllCoroutines();
        
        // Reset game state
        first = null;
        second = null;
        moves = 0;
        timer = 0f;
        running = true;
        InputLocked = false;
        
        // Clear existing cards
        foreach (var card in spawned)
            if (card) Destroy(card.gameObject);
        spawned.Clear();
        
        if (grid != null)
        {
            for (int i = grid.transform.childCount - 1; i >= 0; i--)
                Destroy(grid.transform.GetChild(i).gameObject);
        }
        
        // Reset UI
        ResetUI();
        
        // Setup new board
        SetupBoard(rows, cols);
        
        // Setup reset button again
        SetupResetButton();
        
        // Notify UI controller to update layout
        NotifyUIUpdate();
        
        Debug.Log("Game reset!");
    }
    
    public void ChangeGridSize(int newRows, int newCols)
    {
        if (newRows <= 0 || newCols <= 0)
        {
            Debug.LogError($"Invalid grid size: {newRows}x{newCols}");
            return;
        }
        
        if ((newRows * newCols) % 2 != 0)
        {
            Debug.LogError($"Grid size must have even number of cards: {newRows}x{newCols}");
            return;
        }
        
        rows = newRows;
        cols = newCols;
        
        // Save the new grid size
        PlayerPrefs.SetInt("SelectedRows", rows);
        PlayerPrefs.SetInt("SelectedCols", cols);
        PlayerPrefs.Save();
        
        // Reset and setup new board
        ResetGame();
        
        Debug.Log($"Grid size changed to: {rows}x{cols}");
    }
    
    public void GoToGridSelection()
    {
        // Load grid selection scene
        // Assuming grid selection scene is at build index 0
        SceneManager.LoadScene(0);
    }
    
    private static string FormatTime(float t)
    {
        int m = Mathf.FloorToInt(t / 60f);
        int s = Mathf.FloorToInt(t % 60f);
        return $"{m:00}:{s:00}";
    }
}
