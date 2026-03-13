using System.Collections.Generic;
using UnityEngine;
using TMPro;

/// <summary>
/// Reusable drop-in class for managing floating interaction prompts.
/// Automatically repositions the text left/right relative to the target to avoid overlapping.
/// Now relies on both Mouse Hover and Player proximity.
/// </summary>
public class DynamicInteractionPrompt : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("The text to show when the player is near and hovering.")]
    public string interactionPrompt = "Click to Interact";

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
    private bool _isMouseHovering = false;
    private Collider2D _triggerCollider;

    private bool PlayerInRange => _overlapCount > 0;

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
        yield return null;
        yield return null;

        EvaluateVisibility();
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
        if (_targetPlayer == null) return;
        
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

    // ── Xử lý tầm nhìn của Player (Collider) ───────

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("PlayerEntity"))
        {
            _overlapCount++;

            if (_targetPlayer == null)
            {
                _targetPlayer = collision.transform;
            }

            EvaluateVisibility();
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
        else
        {
            EvaluateVisibility();
        }
    }

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
                EvaluateVisibility();
                yield break;
            }
        }

        EvaluateVisibility();
    }

    // ── Xử lý chuột (Hover) ───────

    private void OnMouseEnter()
    {
        _isMouseHovering = true;
        EvaluateVisibility();
    }

    private void OnMouseExit()
    {
        _isMouseHovering = false;
        EvaluateVisibility();
    }

    // ── Cập nhật trạng thái hiển thị UI ───────

    private void EvaluateVisibility()
    {
        bool shouldShow = PlayerInRange && _isMouseHovering;

        if (shouldShow && !_isShowing)
        {
            if (showDebugLogs) Debug.Log("[DynamicPrompt] Showing interaction prompt.");
            if (promptText != null)
            {
                promptText.text = interactionPrompt;
                promptText.enabled = true;
            }
            _isShowing = true;
            UpdatePosition(); // Instantly update position to prevent 1-frame jitter
        }
        else if (!shouldShow && _isShowing)
        {
            if (showDebugLogs) Debug.Log("[DynamicPrompt] Hiding interaction prompt.");
            _isShowing = false;
            
            // Only clear target if the player actually left
            if (!PlayerInRange) _targetPlayer = null;
            
            if (promptText != null) promptText.enabled = false;
        }
    }

    private void OnDisable()
    {
        // Purposely do not set promptText.enabled = false here!
        // This preserves the state across 1-frame ObjectPool chunk refreshes
        // to prevent UI flickering.
        _overlapCount = 0;
        _isMouseHovering = false;
        _isShowing = false;
        _targetPlayer = null;
    }
}
