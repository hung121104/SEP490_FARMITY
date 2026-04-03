using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;
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
    public class PlayerHealthPresenter : MonoBehaviour, IOnEventCallback
    {
        private const byte PLAYER_HEALTH_SYNC_EVENT = 176;
        private const string TRACE = "[HPTRACE]";

        [Header("Model")]
        [SerializeField] private PlayerHealthModel model = new PlayerHealthModel();

        [Header("Dependencies")]
        [SerializeField] private StatsPresenter statsPresenter;

        private IPlayerHealthService service;
        private IStatsService statsService;
        private bool suppressDirtySync;
        private Coroutine deferredRestoreCoroutine;
        private bool hasAppliedInitialRestore;

        private static readonly Dictionary<int, PlayerHealthPresenter> PresentersByActor = new Dictionary<int, PlayerHealthPresenter>();
        private static readonly Dictionary<int, int> CachedHealthByActor = new Dictionary<int, int>();
        private int registeredActorNumber = -1;
        private bool healthSyncDirty;
        private float nextHealthSyncAt;
        private bool hasPendingRpcRestoreHealth;
        private int pendingRpcRestoreHealth;

        [Header("Health Sync")]
        [SerializeField] private float healthSyncIntervalSeconds = 0.4f;
        [SerializeField] private float deferredRestoreMaxSeconds = 8f;
        [SerializeField] private float deferredRestoreRetryIntervalSeconds = 0.25f;

        #region Unity Lifecycle

        private void Start()
        {
            StartCoroutine(DelayedInitialize());
        }

        private void OnEnable()
        {
            PlayerRegistry.OnLocalPlayerSpawned += HandleLocalPlayerSpawned;
            WorldDataBootstrapper.OnWorldDataReady += HandleWorldDataReady;
            PhotonNetwork.AddCallbackTarget(this);
        }

        private void OnDisable()
        {
            PushFinalStateToMaster();
            PhotonNetwork.RemoveCallbackTarget(this);
            PlayerRegistry.OnLocalPlayerSpawned -= HandleLocalPlayerSpawned;
            WorldDataBootstrapper.OnWorldDataReady -= HandleWorldDataReady;
            if (deferredRestoreCoroutine != null)
            {
                StopCoroutine(deferredRestoreCoroutine);
                deferredRestoreCoroutine = null;
            }
            UnregisterActorBinding();
        }

        private void Update()
        {
            if (service == null || !service.IsInitialized())
                return;

            if (service.TickPassiveRegeneration(Time.deltaTime))
            {
                MarkHealthDirty();
                NotifyViewUpdate();
            }

            TryFlushHealthSync();
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

        public static bool TryGetCachedHealthForActor(int actorNumber, out int currentHealth)
        {
            currentHealth = 0;
            if (actorNumber <= 0)
                return false;

            return CachedHealthByActor.TryGetValue(actorNumber, out currentHealth);
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
            CacheCurrentHealth(registeredActorNumber, service?.GetCurrentHealth() ?? 0);
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

            if (hasPendingRpcRestoreHealth)
            {
                Debug.Log($"{TRACE} [PlayerHealthPresenter] Applying pending RPC restore health={pendingRpcRestoreHealth}");
                suppressDirtySync = true;
                service.SetCurrentHealth(Mathf.Max(0, pendingRpcRestoreHealth));
                suppressDirtySync = false;
                hasPendingRpcRestoreHealth = false;
                hasAppliedInitialRestore = true;
            }

            RegisterActorBinding(playerObj);
            bool restored = TryRestoreCurrentHealthFromSavedCharacterData();
            if (!restored)
                ScheduleDeferredRestore("bind");
            MarkHealthDirty();
            NotifyViewUpdate();
        }

        private void HandleWorldDataReady()
        {
            if (service == null || !service.IsInitialized() || hasAppliedInitialRestore)
                return;

            Debug.Log($"{TRACE} [PlayerHealthPresenter] WorldDataReady received. Retrying health restore.");
            if (TryRestoreCurrentHealthFromSavedCharacterData())
            {
                hasAppliedInitialRestore = true;
                NotifyViewUpdate();
                MarkHealthDirty();
                return;
            }

            ScheduleDeferredRestore("world-data-ready-event");
        }

        private void ScheduleDeferredRestore(string reason)
        {
            if (hasAppliedInitialRestore || deferredRestoreCoroutine != null)
                return;

            Debug.Log($"{TRACE} [PlayerHealthPresenter] Scheduling deferred restore. reason={reason}");
            deferredRestoreCoroutine = StartCoroutine(DeferredRestoreHealthFromPlayerData(reason));
        }

        private IEnumerator DeferredRestoreHealthFromPlayerData(string reason)
        {
            float deadline = Time.realtimeSinceStartup + Mathf.Max(1f, deferredRestoreMaxSeconds);
            float retryDelay = Mathf.Max(0.05f, deferredRestoreRetryIntervalSeconds);

            while (Time.realtimeSinceStartup < deadline)
            {
                if (TryRestoreCurrentHealthFromSavedCharacterData())
                {
                    hasAppliedInitialRestore = true;
                    Debug.Log($"{TRACE} [PlayerHealthPresenter] Deferred restore success. reason={reason}");
                    NotifyViewUpdate();
                    MarkHealthDirty();
                    deferredRestoreCoroutine = null;
                    yield break;
                }

                yield return new WaitForSeconds(retryDelay);
            }

            Debug.LogWarning($"{TRACE} [PlayerHealthPresenter] Deferred restore timed out. reason={reason}");
            deferredRestoreCoroutine = null;
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

            MarkHealthDirty();

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
            MarkHealthDirty();
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

        public void PushFinalStateToMaster()
        {
            if (service == null || !service.IsInitialized())
                return;

            if (!IsLocalOwnedPlayer())
                return;

            string accountId = GetBoundAccountId();
            int health = service.GetCurrentHealth();
            Debug.Log($"{TRACE} [PlayerHealthPresenter] PushFinalStateToMaster accountId='{accountId}' health={health} isMaster={PhotonNetwork.IsMasterClient}");

            if (registeredActorNumber > 0)
                CacheCurrentHealth(registeredActorNumber, health);

            if (PhotonNetwork.IsMasterClient)
            {
                UpdatePlayerDataHealth(accountId, health);
                healthSyncDirty = false;
                return;
            }

            if (!PhotonNetwork.IsConnected)
                return;

            RaiseHealthSyncEvent(accountId, health);
            healthSyncDirty = false;
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

        public void OnEvent(EventData photonEvent)
        {
            if (photonEvent == null || photonEvent.Code != PLAYER_HEALTH_SYNC_EVENT)
                return;

            if (!PhotonNetwork.IsMasterClient)
                return;

            if (photonEvent.CustomData is not object[] payload || payload.Length < 2)
                return;

            string accountId = payload[0] as string ?? string.Empty;
            if (string.IsNullOrWhiteSpace(accountId) || !TryGetInt(payload, 1, out int currentHealth))
                return;

            Debug.Log($"{TRACE} [PlayerHealthPresenter] Master received health event senderActor={photonEvent.Sender} accountId='{accountId}' health={currentHealth}");

            Player sender = PhotonNetwork.CurrentRoom?.GetPlayer(photonEvent.Sender);
            if (sender == null)
                return;

            if (sender.CustomProperties.TryGetValue("accountId", out object raw) && raw is string senderAccountId &&
                !string.IsNullOrWhiteSpace(senderAccountId) && !string.Equals(senderAccountId, accountId, System.StringComparison.Ordinal))
            {
                return;
            }

            CacheCurrentHealth(sender.ActorNumber, currentHealth);
            UpdatePlayerDataHealth(accountId, currentHealth);
        }

        [PunRPC]
        private void RPC_RestoreHealthFromMaster(int restoredHealth)
        {
            int normalized = Mathf.Max(0, restoredHealth);
            Debug.Log($"{TRACE} [PlayerHealthPresenter] RPC_RestoreHealthFromMaster received health={normalized} isMine={IsLocalOwnedPlayer()}");

            if (!IsLocalOwnedPlayer())
                return;

            if (service == null || !service.IsInitialized())
            {
                hasPendingRpcRestoreHealth = true;
                pendingRpcRestoreHealth = normalized;
                Debug.Log($"{TRACE} [PlayerHealthPresenter] Service not ready; queued RPC health restore={normalized}");
                return;
            }

            suppressDirtySync = true;
            service.SetCurrentHealth(normalized);
            suppressDirtySync = false;
            hasAppliedInitialRestore = true;
            NotifyViewUpdate();
            MarkHealthDirty();
            Debug.Log($"{TRACE} [PlayerHealthPresenter] RPC health restore applied health={normalized}");
        }

        private bool TryRestoreCurrentHealthFromSavedCharacterData()
        {
            if (service == null || !service.IsInitialized() || PlayerDataManager.Instance == null)
            {
                Debug.LogWarning($"{TRACE} [PlayerHealthPresenter] Fallback skipped: service initialized={service != null && service.IsInitialized()}, hasPlayerDataManager={PlayerDataManager.Instance != null}");
                return false;
            }

            string accountId = GetBoundAccountId();
            if (string.IsNullOrWhiteSpace(accountId))
            {
                Debug.LogWarning($"{TRACE} [PlayerHealthPresenter] Fallback skipped: accountId missing.");
                return false;
            }

            List<PlayerData> list = PlayerDataManager.Instance.players;
            int idx = list.FindIndex(p => p.accountId == accountId);
            if (idx < 0)
            {
                Debug.LogWarning($"{TRACE} [PlayerHealthPresenter] Fallback skipped: accountId='{accountId}' not found in PlayerDataManager.");
                return false;
            }

            PlayerData data = list[idx];
            if (data.currentHealth < 0f)
            {
                Debug.LogWarning($"{TRACE} [PlayerHealthPresenter] Fallback found accountId='{accountId}' but currentHealth={data.currentHealth} (<0), keeping current runtime value={service.GetCurrentHealth()}.");
                return false;
            }

            Debug.Log($"{TRACE} [PlayerHealthPresenter] Fallback applying PlayerData currentHealth={data.currentHealth} for accountId='{accountId}'.");
            service.SetCurrentHealth(Mathf.RoundToInt(data.currentHealth));
            hasAppliedInitialRestore = true;
            return true;
        }

        private void MarkHealthDirty()
        {
            if (suppressDirtySync)
                return;

            int currentHealth = service != null ? service.GetCurrentHealth() : 0;
            Debug.Log($"{TRACE} [PlayerHealthPresenter] MarkHealthDirty currentHealth={currentHealth}, isMaster={PhotonNetwork.IsMasterClient}");

            // Non-host clients must not publish startup max-health before restore is applied.
            if (!PhotonNetwork.IsMasterClient && !hasAppliedInitialRestore)
            {
                Debug.Log($"{TRACE} [PlayerHealthPresenter] MarkHealthDirty suppressed on client until initial restore completes. currentHealth={currentHealth}");
                return;
            }

            if (registeredActorNumber > 0)
                CacheCurrentHealth(registeredActorNumber, currentHealth);

            if (PhotonNetwork.IsMasterClient)
            {
                UpdatePlayerDataHealth(GetBoundAccountId(), currentHealth);
                return;
            }

            healthSyncDirty = true;
        }

        private void TryFlushHealthSync()
        {
            if (!healthSyncDirty)
                return;

            if (service == null || !service.IsInitialized())
                return;

            if (!IsLocalOwnedPlayer())
                return;

            int currentHealth = service.GetCurrentHealth();
            string accountId = GetBoundAccountId();
            if (string.IsNullOrWhiteSpace(accountId))
                return;

            if (!PhotonNetwork.IsConnected || PhotonNetwork.IsMasterClient)
            {
                UpdatePlayerDataHealth(accountId, currentHealth);
                healthSyncDirty = false;
                return;
            }

            if (Time.time < nextHealthSyncAt)
                return;

            Debug.Log($"{TRACE} [PlayerHealthPresenter] Client relay send -> master accountId='{accountId}' health={currentHealth}");
            RaiseHealthSyncEvent(accountId, currentHealth);
            healthSyncDirty = false;
            nextHealthSyncAt = Time.time + Mathf.Max(0.1f, healthSyncIntervalSeconds);
        }

        private static void CacheCurrentHealth(int actorNumber, int currentHealth)
        {
            if (actorNumber <= 0)
                return;

            CachedHealthByActor[actorNumber] = Mathf.Max(0, currentHealth);
        }

        private void RaiseHealthSyncEvent(string accountId, int currentHealth)
        {
            if (!PhotonNetwork.IsConnected)
                return;

            object[] payload = { accountId, Mathf.Max(0, currentHealth) };
            RaiseEventOptions opts = new RaiseEventOptions { Receivers = ReceiverGroup.MasterClient };
            PhotonNetwork.RaiseEvent(PLAYER_HEALTH_SYNC_EVENT, payload, opts, SendOptions.SendReliable);
        }

        private bool IsLocalOwnedPlayer()
        {
            PhotonView pv = model?.playerEntity != null
                ? model.playerEntity.GetComponent<PhotonView>() ?? model.playerEntity.GetComponentInParent<PhotonView>()
                : null;

            if (!PhotonNetwork.IsConnected)
                return pv == null || pv.IsMine;

            return pv != null && pv.IsMine;
        }

        private string GetBoundAccountId()
        {
            if (model?.playerEntity == null)
                return null;

            PhotonView pv = model.playerEntity.GetComponent<PhotonView>() ?? model.playerEntity.GetComponentInParent<PhotonView>();
            if (pv?.Owner == null)
                return null;

            if (pv.Owner.CustomProperties.TryGetValue("accountId", out object raw) && raw is string accountId && !string.IsNullOrWhiteSpace(accountId))
                return accountId;

            return string.IsNullOrWhiteSpace(pv.Owner.UserId) ? null : pv.Owner.UserId;
        }

        private static void UpdatePlayerDataHealth(string accountId, int currentHealth)
        {
            if (PlayerDataManager.Instance == null || string.IsNullOrWhiteSpace(accountId))
            {
                Debug.LogWarning($"{TRACE} [PlayerHealthPresenter] UpdatePlayerDataHealth skipped: hasPlayerDataManager={PlayerDataManager.Instance != null}, accountId='{accountId}'");
                return;
            }

            List<PlayerData> list = PlayerDataManager.Instance.players;
            int idx = list.FindIndex(p => p.accountId == accountId);
            if (idx < 0)
            {
                Debug.LogWarning($"{TRACE} [PlayerHealthPresenter] UpdatePlayerDataHealth skipped: accountId='{accountId}' not found in PlayerDataManager.");
                return;
            }

            PlayerData pd = list[idx];
            pd.currentHealth = Mathf.Max(0, currentHealth);
            list[idx] = pd;
            Debug.Log($"{TRACE} [PlayerHealthPresenter] UpdatePlayerDataHealth applied accountId='{accountId}' health={pd.currentHealth}");
        }

        private static bool TryGetInt(object[] payload, int index, out int value)
        {
            value = 0;
            if (index < 0 || index >= payload.Length || payload[index] == null)
                return false;

            if (payload[index] is int i)
            {
                value = i;
                return true;
            }

            if (payload[index] is float f)
            {
                value = Mathf.RoundToInt(f);
                return true;
            }

            return false;
        }

        #endregion
    }
}