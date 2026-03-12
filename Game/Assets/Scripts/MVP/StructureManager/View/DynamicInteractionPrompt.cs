using System.Collections.Generic;
using UnityEngine;
using TMPro;

/// <summary>
/// Reusable drop-in class for managing floating interaction prompts (e.g. "Press E to Craft").
/// Automatically repositions the text left/right relative to the target to avoid overlapping.
/// </summary>
public class DynamicInteractionPrompt : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("The text to show when the player is near.")]
    public string interactionPrompt = "Press E to Interact";

    [Header("UI Component")]
    [Tooltip("Text element to display the interaction prompt")]
    public TextMeshProUGUI promptText;

    [Tooltip("X offset from center. Flips automatically based on player position.")]
    public Vector3 promptOffset = new Vector3(1f, 1f, 0f);

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = false;

    private Transform _targetPlayer;
    private bool _isShowing = false;
    private int _overlapCount = 0;
    private Collider2D _triggerCollider;

    private void Awake()
    {
        // Find existing collider on the same object, or the parent if attached to a child anchor.
        _triggerCollider = GetComponent<Collider2D>();
        if (_triggerCollider == null)
        {
            _triggerCollider = GetComponentInParent<Collider2D>();
        }
    }

    private void Start()
    {
        if (promptText != null)
        {
            promptText.text = interactionPrompt;
            promptText.enabled = false;
        }
    }

    private void OnEnable()
    {
        StartCoroutine(VisibilityCheckRoutine());
    }

    private System.Collections.IEnumerator VisibilityCheckRoutine()
    {
        // Wait 2 frames for physics engine to catch up and trigger OnTriggerEnter2D
        // in case the object was spawned from a pool directly over the player.
        yield return null;
        yield return null;

        if (_overlapCount == 0 && promptText != null)
        {
            promptText.enabled = false;
        }
    }

    private void Update()
    {
        // Smoothly and constantly update the UI position without disrupting Physics bounds
        if (_isShowing && promptText != null && promptText.enabled && _targetPlayer != null)
        {
            UpdatePosition();
        }
    }

    private void UpdatePosition()
    {
        float targetX = _targetPlayer.position.x;
        float selfX = transform.position.x;
        
        // If target is on the left (-), put prompt on the left (-)
        float signOffset = (targetX < selfX) ? -1f : 1f;

        promptText.rectTransform.localPosition = new Vector3(
            Mathf.Abs(promptOffset.x) * signOffset, 
            promptOffset.y, 
            promptOffset.z
        );
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("PlayerEntity"))
        {
            _overlapCount++;

            if (_targetPlayer == null)
            {
                _targetPlayer = collision.transform;
            }

            if (_overlapCount == 1 && promptText != null)
            {
                if (showDebugLogs) Debug.Log("[DynamicPrompt] Showing interaction prompt.");
                promptText.text = interactionPrompt;
                promptText.enabled = true;
                _isShowing = true;
                UpdatePosition(); // Instantly update position to prevent 1-frame jitter
            }
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (!collision.CompareTag("PlayerEntity")) return;

        _overlapCount = Mathf.Max(0, _overlapCount - 1);

        if (_overlapCount == 0)
        {
            // Guard: if the GameObject is being deactivated (e.g. returned to pool),
            // this is a false exit caused by SetActive(false). Ignore it.
            if (!gameObject.activeInHierarchy) return;
            
            StartCoroutine(ConfirmPlayerExited());
        }
    }

    /// <summary>
    /// Wait one frame then perform a physics query to verify the player
    /// has actually left the trigger zone. Prevents false UI closing
    /// when the Rigidbody2D briefly sleeps.
    /// </summary>
    private System.Collections.IEnumerator ConfirmPlayerExited()
    {
        yield return null;

        if (_triggerCollider == null) yield break;

        var results = new List<Collider2D>();
        var filter = new ContactFilter2D { useTriggers = true };
        Physics2D.OverlapCollider(_triggerCollider, filter, results);

        foreach (var col in results)
        {
            if (col.CompareTag("PlayerEntity"))
            {
                // False exit — player is still inside.
                _overlapCount++;
                if (promptText != null) promptText.enabled = true;
                yield break;
            }
        }

        if (showDebugLogs) Debug.Log("[DynamicPrompt] Hiding interaction prompt.");
        
        _isShowing = false;
        _targetPlayer = null;
        if (promptText != null) promptText.enabled = false;
    }

    private void OnDisable()
    {
        // Purposely do not set promptText.enabled = false here!
        // This preserves the state across 1-frame ObjectPool chunk refreshes
        // to prevent UI flickering.
        _overlapCount = 0;
        _isShowing = false;
        _targetPlayer = null;
    }
}
