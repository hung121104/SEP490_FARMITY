using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace CombatManager.View
{
    /// <summary>
    /// View for the weapon skill slot in hotbar.
    /// Separate from player skill slots.
    /// Shows current weapon skill icon, hotkey (R), cooldown fill.
    /// </summary>
    public class WeaponSkillSlotView : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Image skillIcon;
        [SerializeField] private Image cooldownFill;
        [SerializeField] private TextMeshProUGUI hotkeyLabel;
        [SerializeField] private Image background;

        [Header("Visual Settings")]
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color cooldownColor = new Color(0.3f, 0.3f, 0.3f, 1f);
        [SerializeField] private Color emptyColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
        [SerializeField] private Sprite emptyIcon;

        #region Unity Lifecycle

        private void Awake()
        {
            if (hotkeyLabel != null)
                hotkeyLabel.text = "R";

            if (cooldownFill != null)
                cooldownFill.fillAmount = 0f;

            SetEmpty();
        }

        #endregion

        #region Public API

        /// <summary>Load weapon skill into slot - show icon.</summary>
        public void SetSkill(Sprite icon)
        {
            if (skillIcon != null)
            {
                skillIcon.sprite = icon != null ? icon : emptyIcon;
                skillIcon.color = normalColor;
            }

            if (background != null)
                background.color = normalColor;
        }

        /// <summary>Clear slot - weapon unequipped.</summary>
        public void SetEmpty()
        {
            if (skillIcon != null)
            {
                skillIcon.sprite = emptyIcon;
                skillIcon.color = emptyColor;
            }

            if (background != null)
                background.color = emptyColor;

            if (cooldownFill != null)
                cooldownFill.fillAmount = 0f;
        }

        /// <summary>Update cooldown fill (0 = ready, 1 = full cooldown).</summary>
        public void UpdateCooldown(float fillAmount)
        {
            if (cooldownFill == null) return;

            cooldownFill.fillAmount = Mathf.Clamp01(fillAmount);

            if (skillIcon != null)
                skillIcon.color = fillAmount > 0f ? cooldownColor : normalColor;
        }

        /// <summary>Show/hide entire slot.</summary>
        public void SetVisible(bool visible)
        {
            gameObject.SetActive(visible);
        }

        #endregion
    }
}