using UnityEngine;
using CombatManager.Model;
using CombatManager.Service;
using CombatManager.View;

namespace CombatManager.Presenter
{
    /// <summary>
    /// Presenter for Stats system.
    /// Connects StatsModel and StatsService to StatsView.
    /// Handles user input and updates the view.
    /// </summary>
    public class StatsPresenter : MonoBehaviour
    {
        [Header("Model")]
        [SerializeField] private StatsModel model = new StatsModel();

        private IStatsService service;

        #region Unity Lifecycle

        private void Awake()
        {
            // Initialize service with model
            service = new StatsService(model);
            Debug.Log("[StatsPresenter] Initialized");
        }

        #endregion

        #region Public API for View

        // Called by View buttons
        public void OnIncreaseStrength()
        {
            if (service.IncreaseTempStrength())
            {
                NotifyViewUpdate();
            }
        }

        public void OnDecreaseStrength()
        {
            if (service.DecreaseTempStrength())
            {
                NotifyViewUpdate();
            }
        }

        public void OnIncreaseVitality()
        {
            if (service.IncreaseTempVitality())
            {
                NotifyViewUpdate();
            }
        }

        public void OnDecreaseVitality()
        {
            if (service.DecreaseTempVitality())
            {
                NotifyViewUpdate();
            }
        }

        public void OnApplyStats()
        {
            service.ApplyStats();
            NotifyViewUpdate();
            
            // ===== NEW: Notify PlayerHealth to refresh =====
            PlayerHealthPresenter healthPresenter = FindObjectOfType<PlayerHealthPresenter>();
            if (healthPresenter != null)
            {
                healthPresenter.RefreshHealthBar();
            }
        }

        public void OnCancelStats()
        {
            service.CancelStats();
            NotifyViewUpdate();
        }

        public void OnAddPoints(int amount)
        {
            service.AddPoints(amount);
            NotifyViewUpdate();
        }

        #endregion

        #region View Update Notification

        private void NotifyViewUpdate()
        {
            // Find and update the view
            StatsView view = GetComponent<StatsView>();
            if (view != null)
            {
                view.UpdateDisplay();
            }
        }

        #endregion

        #region Getters for View

        public int GetTempStrength() => service.GetTempStrength();
        public int GetTempVitality() => service.GetTempVitality();
        public int GetCurrentPoints() => service.GetCurrentPoints();
        public int GetAttackDamage() => service.GetAttackDamage();
        public int GetMaxHealth() => service.GetMaxHealth();

        #endregion

        #region Public API for Other Systems

        public IStatsService GetService() => service;

        #endregion

        // For testing
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.P))
            {
                OnAddPoints(5);
            }
        }
    }
}