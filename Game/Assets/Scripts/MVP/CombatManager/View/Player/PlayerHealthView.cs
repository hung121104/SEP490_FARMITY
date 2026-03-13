using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CombatManager.Presenter;

namespace CombatManager.View
{
    /// <summary>
    /// View for Player Health system UI.
    /// Displays health bar, ease animation, and health number.
    /// </summary>
    public class PlayerHealthView : MonoBehaviour
    {
        [Header("Presenter Reference")]
        [SerializeField] private PlayerHealthPresenter presenter;

        [Header("UI References")]
        [SerializeField] private Slider healthBar;
        [SerializeField] private Slider healthBarEase;
        [SerializeField] private TextMeshProUGUI healthText;

        #region Unity Lifecycle

        private void Update()
        {
            if (presenter == null || !presenter.IsInitialized())
                return;

            UpdateEaseAnimation();
        }

        #endregion

        #region Display Update

        public void UpdateDisplay()
        {
            if (presenter == null || !presenter.IsInitialized())
                return;

            UpdateHealthBars();
            UpdateHealthText();
        }

        private void UpdateHealthBars()
        {
            int currentHealth = presenter.GetCurrentHealth();
            int maxHealth = presenter.GetMaxHealth();

            // Update main health bar
            if (healthBar != null)
            {
                healthBar.maxValue = maxHealth;
                healthBar.value = currentHealth;
            }

            // Update ease health bar max value
            if (healthBarEase != null)
            {
                healthBarEase.maxValue = maxHealth;
            }
        }

        private void UpdateEaseAnimation()
        {
            if (healthBarEase == null)
                return;

            float targetValue = presenter.GetTargetHealthValue();
            float easeSpeed = presenter.GetEaseSpeed();

            healthBarEase.value = Mathf.Lerp(
                healthBarEase.value,
                targetValue,
                easeSpeed * Time.deltaTime
            );
        }

        private void UpdateHealthText()
        {
            if (healthText != null)
            {
                healthText.text = presenter.GetCurrentHealth().ToString();
            }
        }

        #endregion
    }
}