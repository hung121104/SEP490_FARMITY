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
        private bool hasCompletedFirstBind;
        private bool forceFullHealthOnNextRestore;

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

            // A later spawn notification after first successful bind is treated as a respawn.
            if (hasCompletedFirstBind)
            {
                forceFullHealthOnNextRestore = true;
                Debug.LogWarning($"{TRACE} [PlayerHealthPresenter] Respawn detected via PlayerRegistry. Next restore will force full health from stats max.");
            }

            BindToPlayer(playerTransform.gameObject);
        }

        private void BindToPlayer(GameObject playerObj)
        {
            if (playerObj == null)
                return;

            // New bind (especially respawn) must re-run initial-restore gating from scratch.
            hasAppliedInitialRestore = false;

            if (service == null)
                service = new PlayerHealthService(model);

            service.Initialize(playerObj.transform, statsService);
            Debug.Log($"{TRACE} [PlayerHealthPresenter] BindToPlayer initialized runtime health={service.GetCurrentHealth()}/{service.GetMaxHealth()} accountId='{GetBoundAccountId()}'");

            if (hasPendingRpcRestoreHealth)
            {
                if (forceFullHealthOnNextRestore)
                {
                    Debug.LogWarning($"{TRACE} [PlayerHealthPresenter] Dropping pending saved-health restore={pendingRpcRestoreHealth} due to respawn full-health policy.");
                    hasPendingRpcRestoreHealth = false;
                }

                if (hasPendingRpcRestoreHealth)
                {
                    Debug.Log($"{TRACE} [PlayerHealthPresenter] Found pending restore on bind health={pendingRpcRestoreHealth}. Trying guarded apply.");
                    if (TryApplyHealthRestoreWithGuards(pendingRpcRestoreHealth, "pending-on-bind"))
                        hasPendingRpcRestoreHealth = false;
                }
            }

            RegisterActorBinding(playerObj);
            bool restored = TryRestoreCurrentHealthFromSavedCharacterData();
            if (!restored || hasPendingRpcRestoreHealth)
                ScheduleDeferredRestore("bind");
            MarkHealthDirty();
            NotifyViewUpdate();
            hasCompletedFirstBind = true;
        }

        private void HandleWorldDataReady()
        {
            if (service == null || !service.IsInitialized() || hasAppliedInitialRestore)
                return;

            Debug.Log($"{TRACE} [PlayerHealthPresenter] WorldDataReady received. Retrying health restore.");
            if (hasPendingRpcRestoreHealth && TryApplyHealthRestoreWithGuards(pendingRpcRestoreHealth, "pending-on-world-data-ready"))
            {
                hasPendingRpcRestoreHealth = false;
                NotifyViewUpdate();
                MarkHealthDirty();
                return;
            }

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
                if (hasPendingRpcRestoreHealth && TryApplyHealthRestoreWithGuards(pendingRpcRestoreHealth, $"pending-deferred-{reason}"))
                {
                    hasPendingRpcRestoreHealth = false;
                    hasAppliedInitialRestore = true;
                    Debug.Log($"{TRACE} [PlayerHealthPresenter] Deferred pending restore success. reason={reason}");
                    NotifyViewUpdate();
                    MarkHealthDirty();
                    deferredRestoreCoroutine = null;
                    yield break;
                }

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
            hasAppliedInitialRestore = true;
            MarkHealthDirty();
            deferredRestoreCoroutine = null;
        }

        #endregion

        #region Public API for External Systems

        public void ChangeHealth(int amount)
        {
            if (service == null || !service.IsInitialized())
                return;

            int beforeHealth = service.GetCurrentHealth();
            int beforeMax = service.GetMaxHealth();
            service.ChangeHealth(amount);
            int afterHealth = service.GetCurrentHealth();
            int afterMax = service.GetMaxHealth();

            if (amount < 0 && afterHealth <= 0)
            {
                Debug.LogWarning($"{TRACE} [PlayerHealthPresenter] Defeat detected. before={beforeHealth}/{beforeMax} after={afterHealth}/{afterMax} accountId='{GetBoundAccountId()}' isMaster={PhotonNetwork.IsMasterClient}");
            }

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
            int currentHealth = service.GetCurrentHealth();
            int healthToPersist = GetPersistableHealthValue(currentHealth, "PushFinalStateToMaster");
            int maxHealth = Mathf.Max(1, service.GetMaxHealth());

            Debug.Log($"{TRACE} [PlayerHealthPresenter] PushFinalStateToMaster accountId='{accountId}' current={currentHealth} max={maxHealth} persisted={healthToPersist} isMaster={PhotonNetwork.IsMasterClient}");

            if (registeredActorNumber > 0)
                CacheCurrentHealth(registeredActorNumber, healthToPersist);

            if (PhotonNetwork.IsMasterClient)
            {
                UpdatePlayerDataHealth(accountId, healthToPersist);
                healthSyncDirty = false;
                return;
            }

            if (!PhotonNetwork.IsConnected)
                return;

            RaiseHealthSyncEvent(accountId, healthToPersist);
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

        public void SetHealthFromSave(int restoredHealth)
        {
            int normalized = Mathf.Max(0, restoredHealth);

            if (service == null || !service.IsInitialized())
            {
                hasPendingRpcRestoreHealth = true;
                pendingRpcRestoreHealth = normalized;
                Debug.Log($"{TRACE} [PlayerHealthPresenter] SetHealthFromSave queued; service not ready. health={normalized}");
                return;
            }

            if (deferredRestoreCoroutine != null)
            {
                StopCoroutine(deferredRestoreCoroutine);
                deferredRestoreCoroutine = null;
            }

            if (TryApplyHealthRestoreWithGuards(normalized, "rpc"))
            {
                hasPendingRpcRestoreHealth = false;
                NotifyViewUpdate();
                MarkHealthDirty();
                return;
            }

            hasPendingRpcRestoreHealth = true;
            pendingRpcRestoreHealth = normalized;
            Debug.LogWarning($"{TRACE} [PlayerHealthPresenter] SetHealthFromSave deferred; waiting progression readiness. queuedHealth={normalized}");
            ScheduleDeferredRestore("rpc-wait-progression");
        }

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

            if (currentHealth <= 0)
            {
                Debug.LogWarning($"{TRACE} [PlayerHealthPresenter] Ignoring relay health=0 for accountId='{accountId}' senderActor={photonEvent.Sender}. Waiting for respawn-safe health update.");
                return;
            }

            CacheCurrentHealth(sender.ActorNumber, currentHealth);
            UpdatePlayerDataHealth(accountId, currentHealth);
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

            int restoredHealth = Mathf.RoundToInt(data.currentHealth);
            Debug.Log($"{TRACE} [PlayerHealthPresenter] Fallback attempting apply PlayerData currentHealth={restoredHealth} for accountId='{accountId}'.");
            return TryApplyHealthRestoreWithGuards(restoredHealth, "player-data-fallback");
        }

        private bool TryApplyHealthRestoreWithGuards(int restoredHealth, string source)
        {
            if (service == null || !service.IsInitialized())
                return false;

            if (!CanApplyHealthRestoreNow(out string waitReason))
            {
                Debug.LogWarning($"{TRACE} [PlayerHealthPresenter] Skip apply health restore yet. source={source} reason={waitReason} pendingHealth={restoredHealth}");
                return false;
            }

            int normalizedRaw = Mathf.Max(0, restoredHealth);
            int statsMax = statsService != null ? Mathf.Max(1, statsService.GetMaxHealth()) : Mathf.Max(1, service.GetMaxHealth());
            bool usedRespawnFullHealth = false;

            if (forceFullHealthOnNextRestore)
            {
                usedRespawnFullHealth = true;
                normalizedRaw = statsMax;
            }

            bool usedStatsMaxFallback = ShouldUseStatsMaxForZeroInitialRestore(normalizedRaw);
            int normalized = usedStatsMaxFallback ? statsMax : normalizedRaw;
            int beforeCurrent = service.GetCurrentHealth();

            suppressDirtySync = true;
            service.SetMaxHealth(statsMax);
            service.SetCurrentHealth(normalized);
            suppressDirtySync = false;

            int afterCurrent = service.GetCurrentHealth();
            int afterMax = service.GetMaxHealth();
            hasAppliedInitialRestore = true;
            if (usedRespawnFullHealth)
                forceFullHealthOnNextRestore = false;

            Debug.Log($"{TRACE} [PlayerHealthPresenter] Applied health restore source={source} requestedRaw={restoredHealth} normalizedRaw={normalizedRaw} applied={normalized} usedRespawnFullHealth={usedRespawnFullHealth} usedStatsMaxFallback={usedStatsMaxFallback} beforeCurrent={beforeCurrent} afterCurrent={afterCurrent} max={afterMax} level={(statsPresenter != null ? statsPresenter.GetLevel() : -1)} exp={(statsPresenter != null ? statsPresenter.GetCurrentExp() : -1)}");
            return true;
        }

        private bool ShouldUseStatsMaxForZeroInitialRestore(int normalizedRaw)
        {
            if (normalizedRaw > 0)
                return false;

            if (hasAppliedInitialRestore)
                return false;

            if (statsPresenter == null)
                return false;

            int level = Mathf.Max(1, statsPresenter.GetLevel());
            int currentExp = Mathf.Max(0, statsPresenter.GetCurrentExp());
            int currentHealth = service != null ? service.GetCurrentHealth() : 0;
            int currentMax = service != null ? Mathf.Max(1, service.GetMaxHealth()) : 1;

            bool currentlyAtFreshInitHealth = currentHealth >= currentMax;

            if (currentlyAtFreshInitHealth)
            {
                Debug.LogWarning($"{TRACE} [PlayerHealthPresenter] Zero-health initial restore treated as respawn/default dead snapshot. Bootstrapping to stats max. lv={level} exp={currentExp} current={currentHealth}/{currentMax}");
                return true;
            }

            return false;
        }

        private bool CanApplyHealthRestoreNow(out string reason)
        {
            reason = "ok";

            if (statsService == null)
            {
                reason = "stats-service-missing";
                return false;
            }

            if (!PhotonNetwork.IsMasterClient)
                return true;

            if (PlayerDataManager.Instance == null)
            {
                reason = "player-data-manager-missing";
                return false;
            }

            string accountId = GetBoundAccountId();
            if (string.IsNullOrWhiteSpace(accountId))
            {
                reason = "account-id-missing";
                return false;
            }

            List<PlayerData> list = PlayerDataManager.Instance.players;
            int idx = list.FindIndex(p => p.accountId == accountId);
            if (idx < 0)
            {
                reason = $"account-not-found:{accountId}";
                return false;
            }

            int expectedLevel = Mathf.Max(1, list[idx].level);
            int runtimeLevel = statsPresenter != null ? Mathf.Max(1, statsPresenter.GetLevel()) : 1;
            if (runtimeLevel < expectedLevel)
            {
                reason = $"progression-not-ready expectedLv={expectedLevel} runtimeLv={runtimeLevel}";
                return false;
            }

            return true;
        }

        private void MarkHealthDirty()
        {
            if (suppressDirtySync)
                return;

            int currentHealth = service != null ? service.GetCurrentHealth() : 0;
            int persistableHealth = GetPersistableHealthValue(currentHealth, "MarkHealthDirty");
            Debug.Log($"{TRACE} [PlayerHealthPresenter] MarkHealthDirty currentHealth={currentHealth} persistable={persistableHealth}, isMaster={PhotonNetwork.IsMasterClient}");

            // Neither host nor client should publish startup max-health before restore is applied.
            if (!hasAppliedInitialRestore)
            {
                Debug.Log($"{TRACE} [PlayerHealthPresenter] MarkHealthDirty suppressed until initial restore completes. currentHealth={currentHealth} isMaster={PhotonNetwork.IsMasterClient}");
                return;
            }

            if (registeredActorNumber > 0)
                CacheCurrentHealth(registeredActorNumber, persistableHealth);

            if (PhotonNetwork.IsMasterClient)
            {
                UpdatePlayerDataHealth(GetBoundAccountId(), persistableHealth);
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
            int persistableHealth = GetPersistableHealthValue(currentHealth, "TryFlushHealthSync");
            string accountId = GetBoundAccountId();
            if (string.IsNullOrWhiteSpace(accountId))
                return;

            if (!PhotonNetwork.IsConnected || PhotonNetwork.IsMasterClient)
            {
                UpdatePlayerDataHealth(accountId, persistableHealth);
                healthSyncDirty = false;
                return;
            }

            if (Time.time < nextHealthSyncAt)
                return;

            Debug.Log($"{TRACE} [PlayerHealthPresenter] Client relay send -> master accountId='{accountId}' health={persistableHealth} (runtimeCurrent={currentHealth})");
            RaiseHealthSyncEvent(accountId, persistableHealth);
            healthSyncDirty = false;
            nextHealthSyncAt = Time.time + Mathf.Max(0.1f, healthSyncIntervalSeconds);
        }

        private int GetPersistableHealthValue(int runtimeCurrentHealth, string source)
        {
            int normalizedCurrent = Mathf.Max(0, runtimeCurrentHealth);
            int maxHealth = service != null ? Mathf.Max(1, service.GetMaxHealth()) : Mathf.Max(1, statsService != null ? statsService.GetMaxHealth() : 1);

            if (normalizedCurrent <= 0)
            {
                Debug.LogWarning($"{TRACE} [PlayerHealthPresenter] {source} replaced persist health 0 with max={maxHealth} to keep respawn-safe save state.");
                return maxHealth;
            }

            return normalizedCurrent;
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