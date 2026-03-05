using UnityEngine;
using TMPro;
using CombatManager.Model;

namespace CombatManager.View
{
    /// <summary>
    /// View for individual dice roll display.
    /// Handles text update, show/hide on spawned dice prefab.
    /// Uses PopupDamage-style animation (already on prefab).
    /// </summary>
    public class RollDisplayView : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI numberText;

        #region Unity Lifecycle

        private void Awake()
        {
            // Auto-find text if not assigned
            if (numberText == null)
            {
                numberText = GetComponentInChildren<TextMeshProUGUI>();
            }
        }

        #endregion

        #region Display Update

        public void UpdateDisplay(int value)
        {
            if (numberText == null)
                return;

            numberText.text = value.ToString();
            numberText.color = Color.white;
        }

        public void UpdateDisplay(int value, Color color)
        {
            if (numberText == null)
                return;

            numberText.text = value.ToString();
            numberText.color = color;
        }

        #endregion

        #region Visibility

        public void Show()
        {
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        #endregion
    }
}