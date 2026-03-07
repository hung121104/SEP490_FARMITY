using UnityEngine;
using CombatManager.Model;
using CombatManager.Service;
using CombatManager.View;

namespace CombatManager.Presenter
{
    /// <summary>
    /// Presenter for Combat Mode system.
    /// Phase 4: Combat mode now driven by weapon equip/unequip.
    /// Alt key removed. SetCombatMode() called by WeaponEquipPresenter.
    /// </summary>
    public class CombatModePresenter : MonoBehaviour
    {
        public static CombatModePresenter Instance { get; private set; }

        [Header("Model")]
        [SerializeField] private CombatModeModel model = new CombatModeModel();

        private CombatModeService service;

        public static event System.Action<bool> OnCombatModeChanged;

        #region Unity Lifecycle

        private void Awake()
        {
            if (Instance == null)
                Instance = this;
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
            // ✅ Phase 4: Only allow debug toggle if explicitly enabled in model
            // In production: enableDebugToggle = false → this does nothing
            if (model.enableDebugToggle && Input.GetKeyDown(model.debugToggleKey))
            {
                Debug.LogWarning("[CombatModePresenter] DEBUG toggle used! " +
                                 "Disable enableDebugToggle in production.");
                ToggleCombatMode();
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        #endregion

        #region Initialization

        private void InitializeService()
        {
            service = new CombatModeService(model);
            service.RegisterOnCombatModeChanged(OnModeChanged);
            Debug.Log("[CombatModePresenter] Initialized - Combat mode driven by weapon equip");
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
            OnCombatModeChanged?.Invoke(isActive);
        }

        #endregion

        #region View Update

        private void NotifyViewUpdate()
        {
            CombatModeView view = GetComponent<CombatModeView>();
            if (view != null)
                view.UpdateDisplay();
        }

        #endregion

        #region Event Registration

        public ICombatModeService GetService() => service;

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