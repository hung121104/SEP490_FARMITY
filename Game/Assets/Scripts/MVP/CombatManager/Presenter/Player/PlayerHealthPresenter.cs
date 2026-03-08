using UnityEngine;
using System.Collections;
using Photon.Pun;
using CombatManager.Model;
using CombatManager.Service;
using CombatManager.View;

namespace CombatManager.Presenter
{
    /// <summary>
    /// Presenter for Player Health system.
    /// Connects PlayerHealthModel and PlayerHealthService to PlayerHealthView.
    /// Handles initialization, health changes, and invulnerability.
    /// </summary>
    public class PlayerHealthPresenter : MonoBehaviour
    {
        [Header("Model")]
        [SerializeField] private PlayerHealthModel model = new PlayerHealthModel();

        [Header("Dependencies")]
        [SerializeField] private StatsPresenter statsPresenter;

        private IPlayerHealthService service;
        private IStatsService statsService;

        #region Unity Lifecycle

        private void Start()
        {
            StartCoroutine(DelayedInitialize());
        }

        #endregion

        #region Initialization

        private IEnumerator DelayedInitialize()
        {
            yield return new WaitForSeconds(0.5f);
            InitializeComponents();
        }

        private void InitializeComponents()
        {
            // Get StatsService
            if (statsPresenter == null)
            {
                statsPresenter = FindObjectOfType<StatsPresenter>();
            }

            if (statsPresenter == null)
            {
                Debug.LogError("[PlayerHealthPresenter] StatsPresenter not found!");
                enabled = false;
                return;
            }

            statsService = statsPresenter.GetService();
            if (statsService == null)
            {
                Debug.LogError("[PlayerHealthPresenter] StatsService not found!");
                enabled = false;
                return;
            }

            // Find local player entity
            GameObject playerObj = FindLocalPlayerEntity();
            if (playerObj == null)
            {
                Debug.LogError("[PlayerHealthPresenter] Local player entity not found!");
                enabled = false;
                return;
            }

            // Initialize service
            service = new PlayerHealthService(model);
            service.Initialize(playerObj.transform, statsService);

            // Notify view
            NotifyViewUpdate();

            Debug.Log("[PlayerHealthPresenter] Initialized successfully");
        }

        private GameObject FindLocalPlayerEntity()
        {
            // Try "Player" tag first (multiplayer spawn)
            foreach (GameObject go in GameObject.FindGameObjectsWithTag("Player"))
            {
                PhotonView pv = go.GetComponent<PhotonView>();
                if (pv != null && pv.IsMine)
                    return go;
            }

            // Fallback to "PlayerEntity" tag (test scenes)
            foreach (GameObject go in GameObject.FindGameObjectsWithTag("PlayerEntity"))
            {
                PhotonView pv = go.GetComponent<PhotonView>();
                if (pv != null && pv.IsMine)
                    return go;
            }

            // Last resort: find any GameObject named "PlayerEntity"
            GameObject fallback = GameObject.Find("PlayerEntity");
            if (fallback != null)
            {
                Debug.LogWarning("[PlayerHealthPresenter] Found PlayerEntity by name (not recommended for production)");
                return fallback;
            }

            return null;
        }

        #endregion

        #region Public API for External Systems

        public void ChangeHealth(int amount)
        {
            if (service == null || !service.IsInitialized())
                return;

            service.ChangeHealth(amount);
            NotifyViewUpdate();
        }

        public void RefreshHealthBar()
        {
            if (service == null || !service.IsInitialized())
                return;

            service.RefreshHealthBar();
            NotifyViewUpdate();
        }

        public void SetInvulnerable(float duration)
        {
            StartCoroutine(InvulnerabilityCoroutine(duration));
        }

        public void SetInvulnerable(bool invulnerable)
        {
            if (service != null)
            {
                service.SetInvulnerable(invulnerable);
            }
        }

        private IEnumerator InvulnerabilityCoroutine(float duration)
        {
            if (service != null)
            {
                service.SetInvulnerable(true);
                yield return new WaitForSeconds(duration);
                service.SetInvulnerable(false);
            }
        }

        public bool IsInvulnerable()
        {
            return service != null && service.IsInvulnerable();
        }

        #endregion

        #region View Update Notification

        private void NotifyViewUpdate()
        {
            PlayerHealthView view = GetComponent<PlayerHealthView>();
            if (view != null)
            {
                view.UpdateDisplay();
            }
        }

        #endregion

        #region Getters for View

        public int GetCurrentHealth() => service?.GetCurrentHealth() ?? 0;
        public int GetMaxHealth() => service?.GetMaxHealth() ?? 0;
        public float GetTargetHealthValue() => service?.GetTargetHealthValue() ?? 0f;
        public float GetEaseSpeed() => statsService?.GetEaseSpeed() ?? 1f;
        public bool IsInitialized() => service?.IsInitialized() ?? false;

        #endregion

        #region Public API for Other Systems

        public IPlayerHealthService GetService() => service;

        #endregion
    }
}