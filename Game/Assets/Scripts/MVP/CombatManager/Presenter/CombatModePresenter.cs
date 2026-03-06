using UnityEngine;
using CombatManager.Model;
using CombatManager.Service;
using CombatManager.View;

namespace CombatManager.Presenter
{
    /// <summary>
    /// Presenter for Combat Mode system.
    /// Connects CombatModeModel and CombatModeService to CombatModeView.
    /// Acts as singleton for easy access by other combat systems.
    /// </summary>
    public class CombatModePresenter : MonoBehaviour
    {
        // Singleton instance for easy access
        public static CombatModePresenter Instance { get; private set; }

        [Header("Model")]
        [SerializeField] private CombatModeModel model = new CombatModeModel();

        private CombatModeService service;

        // Static event for backward compatibility with old CombatModeManager
        public static event System.Action<bool> OnCombatModeChanged;

        #region Unity Lifecycle

        private void Awake()
        {
            // Singleton setup
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Debug.LogWarning("[CombatModePresenter] Duplicate instance found, destroying");
                Destroy(gameObject);
                return;
            }

            InitializeService();
        }

        private void Update()
        {
            CheckToggleInput();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        #endregion

        #region Initialization

        private void InitializeService()
        {
            service = new CombatModeService(model);

            // Subscribe to service events and forward to static event
            service.RegisterOnCombatModeChanged(OnModeChanged);

            Debug.Log("[CombatModePresenter] Initialized");
        }

        #endregion

        #region Input Handling

        private void CheckToggleInput()
        {
            if (Input.GetKeyDown(model.combatModeToggleKey))
            {
                ToggleCombatMode();
            }
        }

        #endregion

        #region Public API

        public void ToggleCombatMode()
        {
            service?.ToggleCombatMode();
            NotifyViewUpdate();
        }

        public void SetCombatMode(bool isActive)
        {
            service?.SetCombatMode(isActive);
            NotifyViewUpdate();
        }

        public bool IsCombatModeActive()
        {
            return service?.IsCombatModeActive() ?? false;
        }

        #endregion

        #region Event Handling

        private void OnModeChanged(bool isActive)
        {
            // Forward to static event (for backward compatibility)
            OnCombatModeChanged?.Invoke(isActive);
        }

        #endregion

        #region View Update Notification

        private void NotifyViewUpdate()
        {
            CombatModeView view = GetComponent<CombatModeView>();
            if (view != null)
            {
                view.UpdateDisplay();
            }
        }

        #endregion

        #region Public API for Other Systems

        public ICombatModeService GetService() => service;

        // Event registration for other systems
        public void RegisterCallback(System.Action<bool> callback)
        {
            service?.RegisterOnCombatModeChanged(callback);
        }

        public void UnregisterCallback(System.Action<bool> callback)
        {
            service?.UnregisterOnCombatModeChanged(callback);
        }

        #endregion
    }
}