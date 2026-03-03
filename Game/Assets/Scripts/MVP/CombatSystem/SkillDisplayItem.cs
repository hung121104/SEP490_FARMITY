using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

/// <summary>
/// Prefab component that displays a single skill.
/// Instantiated by SkillManagementPanel for each skill in database.
/// Handles skill info display and drag-and-drop interaction.
/// 
/// Drag Logic: When dragging, hides visual, shows drag preview.
/// On drop, returns to original position (no permanent move yet).
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
    private Vector3 originalPosition;
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
        RectTransform rect = GetComponent<RectTransform>();
        if (rect != null)
        {
            originalPosition = rect.localPosition;
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

    #region Drag Implementation (Based on InventorySlotView)

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (skillData == null)
        {
            Debug.LogWarning("[SkillDisplayItem] Cannot drag - skill data is null");
            return;
        }

        isDragging = true;
        Debug.Log($"[SkillDisplayItem] Begin drag: {skillData.skillName}");

        // Reduce opacity to show it's being dragged
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0.6f;
            canvasGroup.blocksRaycasts = false; // Allow dropping on other elements
        }

        // Hide the skill visuals
        SetSkillVisuals(false);

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
        RectTransform rect = GetComponent<RectTransform>();
        if (rect != null)
        {
            rect.position = eventData.position;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!isDragging)
            return;

        isDragging = false;
        Debug.Log($"[SkillDisplayItem] End drag: {skillData.skillName}");

        // Return to original position
        RectTransform rect = GetComponent<RectTransform>();
        if (rect != null)
        {
            rect.localPosition = originalPosition;
        }

        // Restore opacity
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = true;
        }

        // Restore visuals
        if (skillData != null)
        {
            SetSkillVisuals(true);
        }

        // Notify SkillManagementPanel that drag ended
        if (SkillManagementPanel.Instance != null)
        {
            SkillManagementPanel.Instance.OnSkillEndDrag(this);
        }
    }

    /// <summary>
    /// Show or hide the skill visuals (icon, name, description).
    /// Used during drag to hide card content.
    /// </summary>
    private void SetSkillVisuals(bool visible)
    {
        if (skillIconImage != null)
            skillIconImage.enabled = visible;

        if (skillNameText != null)
            skillNameText.enabled = visible;

        if (skillDescriptionText != null)
            skillDescriptionText.enabled = visible;
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

            // Return to original position
            RectTransform rect = GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.localPosition = originalPosition;
            }

            // Restore opacity
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.blocksRaycasts = true;
            }

            // Restore visuals
            if (skillData != null)
            {
                SetSkillVisuals(true);
            }

            Debug.Log($"[SkillDisplayItem] Force reset state for: {skillData?.skillName}");
        }
    }

    #endregion
}