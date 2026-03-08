using UnityEngine;
using UnityEngine.UI;
using CombatManager.Model;
using CombatManager.Service;
using CombatManager.View;

namespace CombatManager.Presenter
{
    /// <summary>
    /// Presenter for Combat Mode Indicator system.
    /// Connects CombatModeIndicatorModel and CombatModeIndicatorService to CombatModeIndicatorView.
    /// Subscribes to CombatModePresenter events and triggers view updates.
    /// </summary>
    public class CombatModeIndicatorPresenter : MonoBehaviour
    {
        [Header("Model")]
        [SerializeField] private CombatModeIndicatorModel model = new CombatModeIndicatorModel();

        [Header("UI References")]
        [SerializeField] private Image combatModeIcon;
        [SerializeField] private Image normalModeIcon;

        private ICombatModeIndicatorService service;

        #region Unity Lifecycle

        private void Start()
        {
            InitializeComponents();
            SubscribeToCombatModeEvents();
        }

        private void OnDestroy()
        {
            UnsubscribeFromCombatModeEvents();
        }

        #endregion

        #region Initialization

        private void InitializeComponents()
        {
            // Try to find icons if not assigned
            if (combatModeIcon == null)
            {
                combatModeIcon = FindIconByName("CombatModeIcon");
            }
            if (normalModeIcon == null)
            {
                normalModeIcon = FindIconByName("NormalModeIcon");
            }

            // Validate
            if (combatModeIcon == null || normalModeIcon == null)
            {
                Debug.LogError("[CombatModeIndicatorPresenter] UI icons not found! Assign manually or check Canvas structure.");
                enabled = false;
                return;
            }

            // Initialize service
            service = new CombatModeIndicatorService(model);
            service.Initialize(combatModeIcon, normalModeIcon);

            Debug.Log("[CombatModeIndicatorPresenter] Initialized successfully");
        }

        private Image FindIconByName(string iconName)
        {
            Canvas canvas = FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                Debug.LogWarning("[CombatModeIndicatorPresenter] Canvas not found");
                return null;
            }

            // Try direct child
            Transform iconTransform = canvas.transform.Find(iconName);
            if (iconTransform == null)
            {
                // Try inside ModeIndicatorContainer
                Transform container = canvas.transform.Find("ModeIndicatorContainer");
                if (container != null)
                {
                    iconTransform = container.Find(iconName);
                }
            }

            if (iconTransform != null)
            {
                return iconTransform.GetComponent<Image>();
            }

            Debug.LogWarning($"[CombatModeIndicatorPresenter] Icon '{iconName}' not found in Canvas");
            return null;
        }

        #endregion

        #region Combat Mode Events

        private void SubscribeToCombatModeEvents()
        {
            if (CombatModePresenter.Instance != null)
            {
                CombatModePresenter.Instance.RegisterCallback(OnCombatModeChanged);
                
                // Set initial state
                UpdateIndicator(CombatModePresenter.Instance.IsCombatModeActive());
            }
            else
            {
                Debug.LogWarning("[CombatModeIndicatorPresenter] CombatModePresenter.Instance not found");
            }
        }

        private void UnsubscribeFromCombatModeEvents()
        {
            if (CombatModePresenter.Instance != null)
            {
                CombatModePresenter.Instance.UnregisterCallback(OnCombatModeChanged);
            }
        }

        private void OnCombatModeChanged(bool isActive)
        {
            UpdateIndicator(isActive);
        }

        #endregion

        #region Public API

        public void UpdateIndicator(bool isCombatMode)
        {
            if (service == null || !service.IsInitialized())
                return;

            // Trigger animated fade via View
            CombatModeIndicatorView view = GetComponent<CombatModeIndicatorView>();
            if (view != null)
            {
                view.StartFadeAnimation(isCombatMode);
            }
            else
            {
                // Fallback: immediate update
                service.UpdateIndicatorImmediate(isCombatMode);
            }
        }

        #endregion

        #region Getters for View

        public bool IsInitialized() => service?.IsInitialized() ?? false;
        public Image GetCombatModeIcon() => service?.GetCombatModeIcon();
        public Image GetNormalModeIcon() => service?.GetNormalModeIcon();
        public float GetFadeDuration() => service?.GetFadeDuration() ?? 0.3f;
        public AnimationCurve GetFadeCurve() => service?.GetFadeCurve();

        #endregion
    }
}