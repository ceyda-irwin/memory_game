using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

[RequireComponent(typeof(Image))]
public class Card : MonoBehaviour, IPointerClickHandler
{
    [Header("Visuals")]
    [SerializeField] private Image image;
    [SerializeField] private Sprite backSprite;

    [Header("State")]
    public int Id { get; private set; }
    private Sprite frontSprite;
    private bool isRevealed = false;
    private bool isMatched = false;
    private bool isLocked = false;
    public bool IsMatched => isMatched;

    private GameManager gm;

    private const float flipTime = 0.15f;
    private Coroutine flipCoroutine;

    private void Awake()
    {
        if (image == null) image = GetComponent<Image>();
        if (image == null) image = GetComponentInChildren<Image>();
    }

    public void Init(int id, Sprite front, Sprite back, GameManager manager)
    {
        if (front == null || back == null || manager == null)
        {
            Debug.LogError("Card Init: Invalid parameters provided");
            return;
        }

        Id = id;
        frontSprite = front;
        backSprite = back;
        gm = manager;

        isRevealed = false;
        isMatched = false;
        isLocked = false;

        if (image != null)
        {
            image.sprite = backSprite;
            image.raycastTarget = true; // re-enable taps on reuse
            
            // Reset color to full opacity
            Color color = image.color;
            color.a = 1f;
            image.color = color;
            
            // Reset scale to normal size
            transform.localScale = Vector3.one;
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (isLocked || isMatched || gm == null || gm.InputLocked) return;
        if (!isRevealed)
        {
            Reveal();
            gm.OnCardRevealed(this);
        }
    }

    public void Reveal()
    {
        if (isRevealed || isMatched) return;
        if (flipCoroutine != null) StopCoroutine(flipCoroutine);
        flipCoroutine = StartCoroutine(FlipRoutine(image.sprite, frontSprite));
        isRevealed = true;
    }

    public void Hide()
    {
        if (!isRevealed || isMatched) return;
        if (flipCoroutine != null) StopCoroutine(flipCoroutine);
        flipCoroutine = StartCoroutine(FlipRoutine(image.sprite, backSprite));
        isRevealed = false;
    }

    public void SetMatched()
    {
        if (isMatched) return;
        isMatched = true;
        if (image != null) image.raycastTarget = false; // don't accept taps anymore
        StartCoroutine(MatchedDisappear());
    }

    public void Lock(bool v) => isLocked = v;

    private IEnumerator FlipRoutine(Sprite from, Sprite to)
    {
        if (image == null) yield break;
        
        Vector3 s = transform.localScale;
        float t = 0f;
        while (t < flipTime)
        {
            t += Time.deltaTime;
            float k = t / flipTime;
            transform.localScale = new Vector3(Mathf.Lerp(1f, 0f, k), s.y, s.z);
            yield return null;
        }
        image.sprite = to;
        t = 0f;
        while (t < flipTime)
        {
            t += Time.deltaTime;
            float k = t / flipTime;
            transform.localScale = new Vector3(Mathf.Lerp(0f, 1f, k), s.y, s.z);
            yield return null;
        }
        transform.localScale = s;
        flipCoroutine = null;
    }

    private IEnumerator MatchedDisappear()
    {
        // First, do a quick pulse effect
        Vector3 originalScale = transform.localScale;
        float t = 0f, dur = 0.1f;
        
        // Scale up slightly
        while (t < dur)
        {
            t += Time.deltaTime;
            float k = t / dur;
            float scale = Mathf.Lerp(1f, 1.1f, k);
            transform.localScale = new Vector3(scale, scale, scale);
            yield return null;
        }
        
        // Then fade out and scale down
        t = 0f;
        dur = 0.3f; // Fade out duration
        
        Color originalColor = image.color;
        
        while (t < dur)
        {
            t += Time.deltaTime;
            float k = t / dur;
            
            // Fade out alpha
            Color currentColor = originalColor;
            currentColor.a = Mathf.Lerp(1f, 0f, k);
            image.color = currentColor;
            
            // Scale down
            float scale = Mathf.Lerp(1.1f, 0f, k);
            transform.localScale = new Vector3(scale, scale, scale);
            
            yield return null;
        }
        
        // Keep the card active but make it completely invisible and non-interactive
        // This prevents the GridLayoutGroup from repositioning other cards
        image.color = new Color(0, 0, 0, 0); // Completely transparent
        transform.localScale = Vector3.zero; // Scale to zero
        image.raycastTarget = false; // Disable interaction
    }
}
