using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

/// <summary>
/// Prefab component that displays a single skill.
/// Instantiated by SkillManagementPanel for each skill in database.
/// Handles skill info display and drag-and-drop interaction.
/// 
/// Drag Logic: When dragging, shows the skill card (background, icon, name)
/// but hides description. Creates a draggable preview of the skill.
/// On drop, returns to original position and rebuilds grid layout.
/// Based on InventorySlotView drag pattern.
/// </summary>
public class SkillDisplayItem : MonoBehaviour,
    IBeginDragHandler,
    IDragHandler,
    IEndDragHandler,
    IPointerClickHandler
{
    #region Editor Fields

    [Header("UI Elements - Auto-found from children")]
    private Image skillIconImage;
    private TextMeshProUGUI skillNameText;
    private TextMeshProUGUI skillDescriptionText;
    private Button selectButton;
    private Image backgroundImage;

    #endregion

    #region Private Fields

    private SkillData skillData;
    private CanvasGroup canvasGroup;
    private RectTransform rectTransform;
    private Vector3 originalPosition;
    private Transform gridParent;
    private bool isDragging = false;

    #endregion

    #region Initialization

    public void Initialize(SkillData data)
    {
        skillData = data;
        FindUIElements();
        SetupCanvasGroup();
        PopulateUI();
        SetupButton();
        StoreOriginalPosition();
    }

    private void FindUIElements()
    {
        // Auto-find UI elements from children
        // Naming convention: SkillIcon, SkillName, SkillDescription, SelectButton

        foreach (Transform child in transform)
        {
            Image img = child.GetComponent<Image>();
            TextMeshProUGUI txt = child.GetComponent<TextMeshProUGUI>();
            Button btn = child.GetComponent<Button>();

            if (child.name.Contains("Icon") && img != null)
                skillIconImage = img;
            else if (child.name.Contains("Name") && txt != null)
                skillNameText = txt;
            else if (child.name.Contains("Description") && txt != null)
                skillDescriptionText = txt;
            else if (child.name.Contains("Select") && btn != null)
                selectButton = btn;
        }

        // Get background image from root
        backgroundImage = GetComponent<Image>();
    }

    private void SetupCanvasGroup()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
            Debug.Log($"[SkillDisplayItem] Added CanvasGroup to {skillData?.skillName ?? "Unknown"}");
        }
    }

    private void StoreOriginalPosition()
    {
        rectTransform = GetComponent<RectTransform>();
        gridParent = transform.parent;
        
        if (rectTransform != null)
        {
            originalPosition = rectTransform.localPosition;
            Debug.Log($"[SkillDisplayItem] Stored original position for {skillData?.skillName}: {originalPosition}");
        }
    }

    private void PopulateUI()
    {
        if (skillData == null) return;

        // Set icon sprite only (no color changes)
        if (skillIconImage != null)
        {
            skillIconImage.sprite = skillData.skillIcon;
        }

        // Set name text
        if (skillNameText != null)
            skillNameText.text = skillData.skillName;

        // Set description text
        if (skillDescriptionText != null)
            skillDescriptionText.text = skillData.skillDescription;
    }

    private void SetupButton()
    {
        if (selectButton != null)
        {
            selectButton.onClick.AddListener(OnSkillSelected);
        }
    }

    #endregion

    #region Drag Implementation

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (skillData == null)
        {
            Debug.LogWarning("[SkillDisplayItem] Cannot drag - skill data is null");
            return;
        }

        isDragging = true;
        Debug.Log($"[SkillDisplayItem] Begin drag: {skillData.skillName}");

        // Reduce opacity slightly to show it's being dragged
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0.85f;
            canvasGroup.blocksRaycasts = false;
        }

        // Hide only the description (keep background, icon, name visible)
        HideDescription();

        // Notify SkillManagementPanel that drag started
        if (SkillManagementPanel.Instance != null)
        {
            SkillManagementPanel.Instance.OnSkillBeginDrag(this);
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging)
            return;

        // Move this skill card to follow mouse
        if (rectTransform != null)
        {
            rectTransform.position = eventData.position;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!isDragging)
            return;

        isDragging = false;
        Debug.Log($"[SkillDisplayItem] End drag: {skillData.skillName}");

        // Enable raycast BEFORE resetting position
        if (canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = true;
        }

        // Return to original position
        if (rectTransform != null)
        {
            rectTransform.localPosition = originalPosition;
        }

        // Restore opacity
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
        }

        // Restore description visibility
        if (skillData != null)
        {
            ShowDescription();
        }

        // Rebuild layout for grid to recalculate positions
        if (gridParent != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(gridParent as RectTransform);
            Debug.Log($"[SkillDisplayItem] Grid layout rebuilt");
        }

        // Notify SkillManagementPanel that drag ended
        if (SkillManagementPanel.Instance != null)
        {
            SkillManagementPanel.Instance.OnSkillEndDrag(this);
        }

        Debug.Log($"[SkillDisplayItem] ✓ Restored to original state: {skillData.skillName}");
    }

    /// <summary>
    /// Hide only the description text during drag.
    /// Background, icon, and name remain visible.
    /// </summary>
    private void HideDescription()
    {
        if (skillDescriptionText != null)
        {
            skillDescriptionText.enabled = false;
            Debug.Log($"[SkillDisplayItem] Description hidden for: {skillData?.skillName}");
        }
    }

    /// <summary>
    /// Show the description text after drag ends.
    /// </summary>
    private void ShowDescription()
    {
        if (skillDescriptionText != null)
        {
            skillDescriptionText.enabled = true;
            Debug.Log($"[SkillDisplayItem] Description shown for: {skillData?.skillName}");
        }
    }

    #endregion

    #region Click Handler

    public void OnPointerClick(PointerEventData eventData)
    {
        if (isDragging)
            return;

        OnSkillSelected();
    }

    #endregion

    #region Interaction

    private void OnSkillSelected()
    {
        if (skillData == null) return;

        Debug.Log($"[SkillDisplayItem] Selected skill: {skillData.skillName}");
        
        // TODO: Open skill details window, or trigger drag-and-drop assignment
    }

    #endregion

    #region Public API

    public SkillData GetSkillData() => skillData;

    public bool IsDragging => isDragging;

    /// <summary>
    /// Force reset drag state (called when drag is interrupted).
    /// </summary>
    public void ForceResetState()
    {
        if (isDragging)
        {
            isDragging = false;

            // Enable raycast first
            if (canvasGroup != null)
            {
                canvasGroup.blocksRaycasts = true;
            }

            // Return to original position
            if (rectTransform != null)
            {
                rectTransform.localPosition = originalPosition;
            }

            // Restore opacity
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
            }

            // Restore description
            ShowDescription();

            // Rebuild grid layout
            if (gridParent != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(gridParent as RectTransform);
            }

            Debug.Log($"[SkillDisplayItem] Force reset state for: {skillData?.skillName}");
        }
    }

    #endregion
}