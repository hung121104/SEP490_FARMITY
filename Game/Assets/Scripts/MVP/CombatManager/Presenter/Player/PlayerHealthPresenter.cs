using UnityEngine;
using System.Collections;
using System.Collections.Generic;
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

        private static readonly Dictionary<int, PlayerHealthPresenter> PresentersByActor = new Dictionary<int, PlayerHealthPresenter>();
        private int registeredActorNumber = -1;

        #region Unity Lifecycle

        private void Start()
        {
            StartCoroutine(DelayedInitialize());
        }

        private void OnEnable()
        {
            PlayerRegistry.OnLocalPlayerSpawned += HandleLocalPlayerSpawned;
        }

        private void OnDisable()
        {
            PlayerRegistry.OnLocalPlayerSpawned -= HandleLocalPlayerSpawned;
            UnregisterActorBinding();
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
                Debug.LogWarning("[PlayerHealthPresenter] Local player entity not found yet. Waiting for spawn event.");
                return;
            }

            BindToPlayer(playerObj);

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

        /// <summary>Returns the local PlayerHealthPresenter (the one tracking this client's player).</summary>
        public static PlayerHealthPresenter FindLocal()
        {
            // FindObjectsByType avoids the obsolete FindObjectsOfType warning.
            foreach (var presenter in UnityEngine.Object.FindObjectsByType<PlayerHealthPresenter>(
                         UnityEngine.FindObjectsSortMode.None))
            {
                if (presenter.model?.playerEntity != null &&
                    presenter.model.playerEntity.GetComponent<PhotonView>() is PhotonView pv &&
                    pv.IsMine)
                    return presenter;
            }
            // Fallback: if only one presenter exists (single-player / host)
            var all = UnityEngine.Object.FindObjectsByType<PlayerHealthPresenter>(UnityEngine.FindObjectsSortMode.None);
            return all.Length == 1 ? all[0] : null;
        }

        public static bool TryApplyDamageForActor(int actorNumber, int damageAmount)
        {
            if (damageAmount <= 0)
                return false;

            PlayerHealthPresenter presenter = null;

            if (PhotonNetwork.IsConnected)
            {
                if (actorNumber <= 0)
                    return false;

                PresentersByActor.TryGetValue(actorNumber, out presenter);

                if (presenter == null || !presenter.IsInitialized())
                    return false;
            }
            else
            {
                if (actorNumber > 0)
                    PresentersByActor.TryGetValue(actorNumber, out presenter);

                if (presenter == null)
                    presenter = FindLocal();

                if (presenter == null || !presenter.IsInitialized())
                    return false;
            }

            presenter.ChangeHealth(-Mathf.Abs(damageAmount));
            return true;
        }

        private void RegisterActorBinding(GameObject playerObj)
        {
            if (playerObj == null)
                return;

            UnregisterActorBinding();

            PhotonView pv = playerObj.GetComponent<PhotonView>();
            if (pv == null || pv.OwnerActorNr <= 0)
                return;

            registeredActorNumber = pv.OwnerActorNr;
            PresentersByActor[registeredActorNumber] = this;
        }

        private void UnregisterActorBinding()
        {
            if (registeredActorNumber <= 0)
                return;

            if (PresentersByActor.TryGetValue(registeredActorNumber, out PlayerHealthPresenter existing) && existing == this)
                PresentersByActor.Remove(registeredActorNumber);

            registeredActorNumber = -1;
        }

        private void HandleLocalPlayerSpawned(Transform playerTransform)
        {
            if (playerTransform == null)
                return;

            if (statsService == null)
            {
                // Initialization not finished yet; delayed init will bind later.
                return;
            }

            BindToPlayer(playerTransform.gameObject);
        }

        private void BindToPlayer(GameObject playerObj)
        {
            if (playerObj == null)
                return;

            if (service == null)
                service = new PlayerHealthService(model);

            service.Initialize(playerObj.transform, statsService);
            RegisterActorBinding(playerObj);
            NotifyViewUpdate();
        }

        #endregion

        #region Public API for External Systems

        public void ChangeHealth(int amount)
        {
            if (service == null || !service.IsInitialized())
                return;

            int beforeHealth = service.GetCurrentHealth();
            service.ChangeHealth(amount);
            int afterHealth = service.GetCurrentHealth();

            TrySpawnHealthPopup(afterHealth - beforeHealth);
            NotifyViewUpdate();
        }

        private void TrySpawnHealthPopup(int healthDelta)
        {
            if (healthDelta == 0)
                return;

            Transform playerEntity = service?.GetPlayerEntity();
            if (playerEntity == null)
                return;

            if (healthDelta > 0)
            {
                DamagePopupPresenter.Spawn(playerEntity.position, healthDelta, PopupType.Heal);
                return;
            }

            DamagePopupPresenter.Spawn(playerEntity.position, Mathf.Abs(healthDelta));
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