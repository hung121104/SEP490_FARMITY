using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Prefab component that displays a single skill.
/// Instantiated by SkillManagementPanel for each skill in database.
/// Handles skill info display and future drag-and-drop interaction.
/// </summary>
public class SkillDisplayItem : MonoBehaviour
{
    #region Editor Fields

    [Header("UI Elements - Auto-found from children")]
    private Image skillIconImage;
    private TextMeshProUGUI skillNameText;
    private TextMeshProUGUI skillDescriptionText;
    private Button selectButton;

    #endregion

    #region Private Fields

    private SkillData skillData;

    #endregion

    #region Initialization

    public void Initialize(SkillData data)
    {
        skillData = data;
        FindUIElements();
        PopulateUI();
        SetupButton();
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

    #endregion
}