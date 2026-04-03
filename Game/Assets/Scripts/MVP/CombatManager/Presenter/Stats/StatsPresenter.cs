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
    /// Presenter for Stats system.
    /// Connects StatsModel and StatsService to StatsView.
    /// Handles user input and updates the view.
    /// </summary>
    public class StatsPresenter : MonoBehaviour, IOnEventCallback
    {
        private const byte PLAYER_PROGRESSION_SYNC_EVENT = 177;
        private const string TRACE = "[PROGTRACE]";
        [Header("Model")]
        [SerializeField] private StatsModel model = new StatsModel();

        private IStatsService service;
        private bool suppressDirtySync;
        private bool progressionSyncDirty;
        private float nextProgressionSyncAt;
        private bool hasAppliedInitialRestore;

        [Header("Progression Sync")]
        [SerializeField] private float progressionSyncIntervalSeconds = 0.5f;
        [SerializeField] private float deferredRestoreMaxSeconds = 8f;
        [SerializeField] private float deferredRestoreRetryIntervalSeconds = 0.25f;

        private Coroutine deferredRestoreCoroutine;
        private static readonly Dictionary<string, PlayerProgressionSnapshot> CachedProgressionByAccount =
            new Dictionary<string, PlayerProgressionSnapshot>();

        #region Unity Lifecycle

        private void Awake()
        {
            // Initialize service with model
            service = new StatsService(model);

            Debug.Log("[StatsPresenter] Initialized");
        }

        private void OnEnable()
        {
            PhotonNetwork.AddCallbackTarget(this);
            WorldDataBootstrapper.OnWorldDataReady += HandleWorldDataReady;
        }

        private void Start()
        {
            RestoreProgressionFromPlayerData();
        }

        private void OnDisable()
        {
            PushFinalStateToMaster();
            PhotonNetwork.RemoveCallbackTarget(this);
            WorldDataBootstrapper.OnWorldDataReady -= HandleWorldDataReady;
            if (deferredRestoreCoroutine != null)
            {
                StopCoroutine(deferredRestoreCoroutine);
                deferredRestoreCoroutine = null;
            }
        }

        private void Update()
        {
            TryFlushProgressionSync();
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

            if (suppressDirtySync)
                return;

            MarkProgressionDirty();
        }

        #endregion

        #region Getters for View

        public int GetStrength() => service.GetStrength();
        public int GetVitality() => service.GetVitality();
        public int GetLevel() => service.GetLevel();
        public int GetCurrentExp() => service.GetCurrentExp();
        public int GetExpToNextLevel() => service.GetExpToNextLevel();
        public float GetExpProgress01() => service.GetExpProgress01();
        public int GetAttackDamage() => service.GetAttackDamage();
        public int GetMaxHealth() => service.GetMaxHealth();

        #endregion

        #region Progression API

        public int AddExperienceFromHost(int amount)
        {
            int levelsGained = service.AddExperience(amount);
            Debug.Log($"{TRACE} [StatsPresenter] AddExperienceFromHost amount={amount} => lv={service.GetLevel()} exp={service.GetCurrentExp()}/{service.GetExpToNextLevel()} str={service.GetStrength()} vit={service.GetVitality()} levelsGained={levelsGained}");
            if (levelsGained > 0)
            {
                GameEventBus.FireLevelReached(service.GetLevel(), levelsGained);

                PlayerHealthPresenter healthPresenter = FindObjectOfType<PlayerHealthPresenter>();
                if (healthPresenter != null)
                {
                    healthPresenter.RefreshHealthBar();
                }
            }

            NotifyViewUpdate();
            return levelsGained;
        }

        public void SetProgressionFromSave(int level, int currentExp, int expToNextLevel, int baseStrength, int baseVitality)
        {
            suppressDirtySync = true;
            service.SetProgressionState(level, currentExp, expToNextLevel);
            service.SetBaseStats(baseStrength, baseVitality);
            hasAppliedInitialRestore = true;
            NotifyViewUpdate();
            suppressDirtySync = false;
            Debug.Log($"{TRACE} [StatsPresenter] SetProgressionFromSave applied initial restore lv={service.GetLevel()} exp={service.GetCurrentExp()}/{service.GetExpToNextLevel()} str={service.GetStrength()} vit={service.GetVitality()}");
        }

        #endregion

        #region Public API for Other Systems

        public IStatsService GetService() => service;

        public void PushFinalStateToMaster()
        {
            string accountId = GetLocalAccountId();
            if (string.IsNullOrWhiteSpace(accountId) || service == null)
            {
                Debug.LogWarning($"{TRACE} [StatsPresenter] PushFinalStateToMaster skipped: accountId='{accountId}', hasService={service != null}");
                return;
            }

            PlayerProgressionSnapshot snapshot = BuildRuntimeSnapshot();
            Debug.Log($"{TRACE} [StatsPresenter] PushFinalStateToMaster accountId='{accountId}' isMaster={PhotonNetwork.IsMasterClient} lv={snapshot.level} exp={snapshot.currentExp}/{snapshot.expToNextLevel} str={snapshot.baseStrength} vit={snapshot.baseVitality}");
            CacheProgression(accountId, snapshot);
            UpdatePlayerDataProgression(accountId, snapshot);

            if (PhotonNetwork.IsMasterClient)
            {
                progressionSyncDirty = false;
                return;
            }

            if (!PhotonNetwork.IsConnected)
                return;

            RaiseProgressionSyncEvent(accountId, snapshot);
            progressionSyncDirty = false;
        }

        public static bool TryGetCachedProgression(string accountId, out PlayerProgressionSnapshot snapshot)
        {
            snapshot = default;
            if (string.IsNullOrWhiteSpace(accountId))
                return false;

            return CachedProgressionByAccount.TryGetValue(accountId, out snapshot);
        }

        public void OnEvent(EventData photonEvent)
        {
            if (photonEvent == null || photonEvent.Code != PLAYER_PROGRESSION_SYNC_EVENT)
                return;

            if (!PhotonNetwork.IsMasterClient)
                return;

            if (photonEvent.CustomData is not object[] payload || payload.Length < 6)
                return;

            string accountId = payload[0] as string ?? string.Empty;
            if (string.IsNullOrWhiteSpace(accountId))
                return;

            if (!TryGetInt(payload, 1, out int level) ||
                !TryGetInt(payload, 2, out int currentExp) ||
                !TryGetInt(payload, 3, out int expToNextLevel) ||
                !TryGetInt(payload, 4, out int baseStrength) ||
                !TryGetInt(payload, 5, out int baseVitality))
                return;

            Player sender = PhotonNetwork.CurrentRoom?.GetPlayer(photonEvent.Sender);
            if (sender == null)
                return;

            if (sender.CustomProperties.TryGetValue("accountId", out object raw) && raw is string senderAccountId &&
                !string.IsNullOrWhiteSpace(senderAccountId) && !string.Equals(senderAccountId, accountId, System.StringComparison.Ordinal))
                return;

            var snapshot = NormalizeSnapshot(new PlayerProgressionSnapshot
            {
                level = level,
                currentExp = currentExp,
                expToNextLevel = expToNextLevel,
                baseStrength = baseStrength,
                baseVitality = baseVitality,
            });

            Debug.Log($"{TRACE} [StatsPresenter] Master received progression event senderActor={photonEvent.Sender} accountId='{accountId}' lv={snapshot.level} exp={snapshot.currentExp}/{snapshot.expToNextLevel} str={snapshot.baseStrength} vit={snapshot.baseVitality}");

            CacheProgression(accountId, snapshot);
            UpdatePlayerDataProgression(accountId, snapshot);
        }

        #endregion

        private void RestoreProgressionFromPlayerData()
        {
            if (!PhotonNetwork.IsMasterClient)
            {
                // Non-master progression is restored from LoadPlayerData self-GET path.
                Debug.Log($"{TRACE} [StatsPresenter] Skip RestoreProgressionFromPlayerData on non-master; waiting for self-GET restore.");
                return;
            }

            string accountId = GetLocalAccountId();
            if (string.IsNullOrWhiteSpace(accountId) || PlayerDataManager.Instance == null)
            {
                Debug.LogWarning($"{TRACE} [StatsPresenter] Restore skipped: accountId='{accountId}', hasPlayerDataManager={PlayerDataManager.Instance != null}. Scheduling deferred restore.");
                ScheduleDeferredRestore();
                return;
            }

            List<PlayerData> list = PlayerDataManager.Instance.players;
            int idx = list.FindIndex(p => p.accountId == accountId);
            if (idx < 0)
            {
                Debug.LogWarning($"{TRACE} [StatsPresenter] Restore skipped: accountId='{accountId}' not found in PlayerDataManager. Scheduling deferred restore.");
                ScheduleDeferredRestore();
                return;
            }

            var pd = list[idx];
            Debug.Log($"{TRACE} [StatsPresenter] Restore from PlayerData accountId='{accountId}' lv={pd.level} exp={pd.currentExp}/{pd.expToNextLevel} str={pd.baseStrength} vit={pd.baseVitality}");
            ApplySnapshotToService(new PlayerProgressionSnapshot
            {
                level = pd.level,
                currentExp = pd.currentExp,
                expToNextLevel = pd.expToNextLevel,
                baseStrength = pd.baseStrength,
                baseVitality = pd.baseVitality,
            });
        }

        private void HandleWorldDataReady()
        {
            if (!PhotonNetwork.IsMasterClient)
                return;

            RestoreProgressionFromPlayerData();
        }

        private void ScheduleDeferredRestore()
        {
            if (!PhotonNetwork.IsMasterClient)
                return;

            if (deferredRestoreCoroutine != null)
                return;

            deferredRestoreCoroutine = StartCoroutine(DeferredRestoreProgressionFromPlayerData());
        }

        private IEnumerator DeferredRestoreProgressionFromPlayerData()
        {
            float deadline = Time.realtimeSinceStartup + Mathf.Max(1f, deferredRestoreMaxSeconds);
            float retryDelay = Mathf.Max(0.05f, deferredRestoreRetryIntervalSeconds);

            while (Time.realtimeSinceStartup < deadline)
            {
                string accountId = GetLocalAccountId();
                if (!string.IsNullOrWhiteSpace(accountId) && PlayerDataManager.Instance != null)
                {
                    List<PlayerData> list = PlayerDataManager.Instance.players;
                    int idx = list.FindIndex(p => p.accountId == accountId);
                    if (idx >= 0)
                    {
                        var pd = list[idx];
                        Debug.Log($"{TRACE} [StatsPresenter] Deferred restore hit accountId='{accountId}' lv={pd.level} exp={pd.currentExp}/{pd.expToNextLevel} str={pd.baseStrength} vit={pd.baseVitality}");
                        ApplySnapshotToService(new PlayerProgressionSnapshot
                        {
                            level = pd.level,
                            currentExp = pd.currentExp,
                            expToNextLevel = pd.expToNextLevel,
                            baseStrength = pd.baseStrength,
                            baseVitality = pd.baseVitality,
                        });
                        deferredRestoreCoroutine = null;
                        yield break;
                    }
                }

                yield return new WaitForSeconds(retryDelay);
            }

            deferredRestoreCoroutine = null;
        }

        private void ApplySnapshotToService(PlayerProgressionSnapshot snapshot)
        {
            if (service == null)
                return;

            PlayerProgressionSnapshot normalized = NormalizeSnapshot(snapshot);
            Debug.Log($"{TRACE} [StatsPresenter] ApplySnapshotToService lv={normalized.level} exp={normalized.currentExp}/{normalized.expToNextLevel} str={normalized.baseStrength} vit={normalized.baseVitality}");
            suppressDirtySync = true;
            service.SetProgressionState(normalized.level, normalized.currentExp, normalized.expToNextLevel);
            service.SetBaseStats(normalized.baseStrength, normalized.baseVitality);
            NotifyViewUpdate();
            suppressDirtySync = false;

            string accountId = GetLocalAccountId();
            if (!string.IsNullOrWhiteSpace(accountId))
            {
                CacheProgression(accountId, normalized);
                UpdatePlayerDataProgression(accountId, normalized);
            }
        }

        private void MarkProgressionDirty()
        {
            if (service == null)
                return;

            string accountId = GetLocalAccountId();
            if (string.IsNullOrWhiteSpace(accountId))
                return;

            if (!PhotonNetwork.IsMasterClient && !hasAppliedInitialRestore)
            {
                Debug.Log($"{TRACE} [StatsPresenter] MarkProgressionDirty suppressed on client until initial restore. accountId='{accountId}' lv={service.GetLevel()} exp={service.GetCurrentExp()}/{service.GetExpToNextLevel()}");
                return;
            }

            PlayerProgressionSnapshot snapshot = BuildRuntimeSnapshot();
            CacheProgression(accountId, snapshot);
            UpdatePlayerDataProgression(accountId, snapshot);
            Debug.Log($"{TRACE} [StatsPresenter] MarkProgressionDirty accountId='{accountId}' isMaster={PhotonNetwork.IsMasterClient} lv={snapshot.level} exp={snapshot.currentExp}/{snapshot.expToNextLevel} str={snapshot.baseStrength} vit={snapshot.baseVitality}");

            if (PhotonNetwork.IsMasterClient)
                return;

            progressionSyncDirty = true;
        }

        private void TryFlushProgressionSync()
        {
            if (!progressionSyncDirty)
                return;

            string accountId = GetLocalAccountId();
            if (string.IsNullOrWhiteSpace(accountId))
                return;

            if (!PhotonNetwork.IsConnected || PhotonNetwork.IsMasterClient)
            {
                progressionSyncDirty = false;
                return;
            }

            if (Time.time < nextProgressionSyncAt)
                return;

            var snapshot = BuildRuntimeSnapshot();
            Debug.Log($"{TRACE} [StatsPresenter] Client relay send -> master accountId='{accountId}' lv={snapshot.level} exp={snapshot.currentExp}/{snapshot.expToNextLevel} str={snapshot.baseStrength} vit={snapshot.baseVitality}");
            RaiseProgressionSyncEvent(accountId, snapshot);
            progressionSyncDirty = false;
            nextProgressionSyncAt = Time.time + Mathf.Max(0.1f, progressionSyncIntervalSeconds);
        }

        private PlayerProgressionSnapshot BuildRuntimeSnapshot()
        {
            return NormalizeSnapshot(new PlayerProgressionSnapshot
            {
                level = service.GetLevel(),
                currentExp = service.GetCurrentExp(),
                expToNextLevel = service.GetExpToNextLevel(),
                baseStrength = service.GetStrength(),
                baseVitality = service.GetVitality(),
            });
        }

        private void RaiseProgressionSyncEvent(string accountId, PlayerProgressionSnapshot snapshot)
        {
            if (!PhotonNetwork.IsConnected)
                return;

            object[] payload =
            {
                accountId,
                snapshot.level,
                snapshot.currentExp,
                snapshot.expToNextLevel,
                snapshot.baseStrength,
                snapshot.baseVitality,
            };

            RaiseEventOptions opts = new RaiseEventOptions { Receivers = ReceiverGroup.MasterClient };
            PhotonNetwork.RaiseEvent(PLAYER_PROGRESSION_SYNC_EVENT, payload, opts, SendOptions.SendReliable);
        }

        private static void CacheProgression(string accountId, PlayerProgressionSnapshot snapshot)
        {
            if (string.IsNullOrWhiteSpace(accountId))
                return;

            CachedProgressionByAccount[accountId] = NormalizeSnapshot(snapshot);
        }

        private static void UpdatePlayerDataProgression(string accountId, PlayerProgressionSnapshot snapshot)
        {
            if (PlayerDataManager.Instance == null || string.IsNullOrWhiteSpace(accountId))
                return;

            List<PlayerData> list = PlayerDataManager.Instance.players;
            int idx = list.FindIndex(p => p.accountId == accountId);
            if (idx < 0)
                return;

            var pd = list[idx];
            pd.level = snapshot.level;
            pd.currentExp = snapshot.currentExp;
            pd.expToNextLevel = snapshot.expToNextLevel;
            pd.baseStrength = snapshot.baseStrength;
            pd.baseVitality = snapshot.baseVitality;
            list[idx] = pd;
            Debug.Log($"{TRACE} [StatsPresenter] UpdatePlayerDataProgression applied accountId='{accountId}' lv={pd.level} exp={pd.currentExp}/{pd.expToNextLevel} str={pd.baseStrength} vit={pd.baseVitality}");
        }

        private static PlayerProgressionSnapshot NormalizeSnapshot(PlayerProgressionSnapshot snapshot)
        {
            return new PlayerProgressionSnapshot
            {
                level = Mathf.Max(1, snapshot.level),
                currentExp = Mathf.Max(0, snapshot.currentExp),
                expToNextLevel = Mathf.Max(1, snapshot.expToNextLevel),
                baseStrength = Mathf.Max(1, snapshot.baseStrength),
                baseVitality = Mathf.Max(1, snapshot.baseVitality),
            };
        }

        private string GetLocalAccountId()
        {
            if (PhotonNetwork.LocalPlayer != null &&
                PhotonNetwork.LocalPlayer.CustomProperties.TryGetValue("accountId", out object raw) &&
                raw is string accountId && !string.IsNullOrWhiteSpace(accountId))
            {
                return accountId;
            }

            return SessionManager.Instance != null && !string.IsNullOrWhiteSpace(SessionManager.Instance.UserId)
                ? SessionManager.Instance.UserId
                : null;
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
    }
}