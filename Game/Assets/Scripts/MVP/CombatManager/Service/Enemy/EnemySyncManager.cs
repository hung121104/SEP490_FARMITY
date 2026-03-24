using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;
using CombatManager.Presenter;

namespace CombatManager.Service
{
    /// <summary>
    /// Centralized host-authoritative enemy combat sync pipeline.
    /// Request -> Master apply -> Broadcast authoritative outcome.
    /// </summary>
    public class EnemySyncManager : MonoBehaviourPunCallbacks, IOnEventCallback
    {
        private const byte ENEMY_HIT_REQUEST_EVENT = 168;
        private const byte ENEMY_HIT_APPLIED_EVENT = 169;

        private static EnemySyncManager instance;

        private readonly Dictionary<string, EnemyPresenter> enemiesByRuntimeId = new Dictionary<string, EnemyPresenter>();
        private readonly HashSet<string> processingHits = new HashSet<string>();

        public static EnemySyncManager Instance
        {
            get
            {
                if (instance != null)
                    return instance;

                instance = FindObjectOfType<EnemySyncManager>();
                if (instance != null)
                    return instance;

                GameObject go = new GameObject("EnemySyncManager");
                instance = go.AddComponent<EnemySyncManager>();
                DontDestroyOnLoad(go);
                return instance;
            }
        }

        private bool IsAuthoritative => !PhotonNetwork.IsConnected || PhotonNetwork.IsMasterClient;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnEnable()
        {
            PhotonNetwork.AddCallbackTarget(this);
        }

        private void OnDisable()
        {
            PhotonNetwork.RemoveCallbackTarget(this);
        }

        public void RegisterEnemy(EnemyPresenter enemy)
        {
            if (enemy == null)
                return;

            string runtimeId = enemy.GetRuntimeEnemyId();
            if (string.IsNullOrWhiteSpace(runtimeId))
                return;

            enemiesByRuntimeId[runtimeId] = enemy;
        }

        public void UnregisterEnemy(EnemyPresenter enemy)
        {
            if (enemy == null)
                return;

            string runtimeId = enemy.GetRuntimeEnemyId();
            if (string.IsNullOrWhiteSpace(runtimeId))
                return;

            if (enemiesByRuntimeId.TryGetValue(runtimeId, out EnemyPresenter existing) && existing == enemy)
                enemiesByRuntimeId.Remove(runtimeId);
        }

        public void RequestEnemyHit(EnemyPresenter enemy, int damage, Vector2 knockbackDir, float knockbackForce)
        {
            if (enemy == null || damage <= 0)
                return;

            string runtimeId = enemy.GetRuntimeEnemyId();
            if (string.IsNullOrWhiteSpace(runtimeId))
                return;

            RegisterEnemy(enemy);

            int attackerActorNumber = PhotonNetwork.IsConnected
                ? PhotonNetwork.LocalPlayer?.ActorNumber ?? -1
                : -1;

            int hitToken = BuildHitToken(attackerActorNumber, runtimeId);

            if (IsAuthoritative)
            {
                ProcessHitRequest(runtimeId, damage, knockbackDir, knockbackForce, attackerActorNumber, hitToken);
                return;
            }

            object[] payload =
            {
                runtimeId,
                damage,
                knockbackDir.x,
                knockbackDir.y,
                knockbackForce,
                attackerActorNumber,
                hitToken,
            };

            RaiseEventOptions options = new RaiseEventOptions { Receivers = ReceiverGroup.MasterClient };
            PhotonNetwork.RaiseEvent(ENEMY_HIT_REQUEST_EVENT, payload, options, SendOptions.SendReliable);
        }

        public void OnEvent(EventData photonEvent)
        {
            switch (photonEvent.Code)
            {
                case ENEMY_HIT_REQUEST_EVENT:
                    HandleHitRequestEvent(photonEvent);
                    break;

                case ENEMY_HIT_APPLIED_EVENT:
                    HandleHitAppliedEvent(photonEvent);
                    break;
            }
        }

        private void HandleHitRequestEvent(EventData photonEvent)
        {
            if (!IsAuthoritative)
                return;

            if (photonEvent.CustomData is not object[] payload || payload.Length < 7)
                return;

            string runtimeId = payload[0] as string ?? string.Empty;
            if (string.IsNullOrWhiteSpace(runtimeId))
                return;

            if (!TryGetInt(payload, 1, out int damage) ||
                !TryGetFloat(payload, 2, out float knockbackX) ||
                !TryGetFloat(payload, 3, out float knockbackY) ||
                !TryGetFloat(payload, 4, out float knockbackForce) ||
                !TryGetInt(payload, 5, out int attackerActorNumber) ||
                !TryGetInt(payload, 6, out int hitToken))
            {
                return;
            }

            ProcessHitRequest(
                runtimeId,
                damage,
                new Vector2(knockbackX, knockbackY),
                knockbackForce,
                attackerActorNumber,
                hitToken);
        }

        private void ProcessHitRequest(
            string runtimeId,
            int damage,
            Vector2 knockbackDir,
            float knockbackForce,
            int attackerActorNumber,
            int hitToken)
        {
            if (!IsAuthoritative || string.IsNullOrWhiteSpace(runtimeId) || damage <= 0)
                return;

            if (processingHits.Contains(runtimeId))
                return;

            EnemyPresenter enemy = ResolveEnemy(runtimeId);
            if (enemy == null || !enemy.IsInitialized() || enemy.IsDead())
                return;

            processingHits.Add(runtimeId);
            try
            {
                enemy.ApplyAuthoritativeHit(damage, knockbackDir, knockbackForce, hitToken, attackerActorNumber);

                int newHp = enemy.GetCurrentHealth();
                int maxHp = enemy.GetMaxHealth();
                bool isDead = newHp <= 0;

                object[] payload =
                {
                    runtimeId,
                    newHp,
                    maxHp,
                    knockbackDir.x,
                    knockbackDir.y,
                    knockbackForce,
                    damage,
                    hitToken,
                    isDead,
                };

                if (PhotonNetwork.IsConnected)
                {
                    RaiseEventOptions options = new RaiseEventOptions { Receivers = ReceiverGroup.Others };
                    PhotonNetwork.RaiseEvent(ENEMY_HIT_APPLIED_EVENT, payload, options, SendOptions.SendReliable);
                }
            }
            finally
            {
                processingHits.Remove(runtimeId);
            }
        }

        private void HandleHitAppliedEvent(EventData photonEvent)
        {
            if (photonEvent.CustomData is not object[] payload || payload.Length < 9)
                return;

            string runtimeId = payload[0] as string ?? string.Empty;
            if (string.IsNullOrWhiteSpace(runtimeId))
                return;

            if (!TryGetInt(payload, 1, out int newHp) ||
                !TryGetInt(payload, 2, out int maxHp) ||
                !TryGetFloat(payload, 3, out float knockbackX) ||
                !TryGetFloat(payload, 4, out float knockbackY) ||
                !TryGetFloat(payload, 5, out float knockbackForce) ||
                !TryGetInt(payload, 6, out int damage) ||
                !TryGetInt(payload, 7, out int hitToken) ||
                !TryGetBool(payload, 8, out bool isDead))
            {
                return;
            }

            EnemyPresenter enemy = ResolveEnemy(runtimeId);
            if (enemy == null || !enemy.IsInitialized())
                return;

            enemy.ApplyReplicatedHitState(
                newHp,
                maxHp,
                new Vector2(knockbackX, knockbackY),
                knockbackForce,
                damage,
                hitToken,
                isDead);
        }

        private EnemyPresenter ResolveEnemy(string runtimeId)
        {
            if (enemiesByRuntimeId.TryGetValue(runtimeId, out EnemyPresenter cached) && cached != null)
                return cached;

            EnemyPresenter[] allEnemies = FindObjectsOfType<EnemyPresenter>(true);
            for (int i = 0; i < allEnemies.Length; i++)
            {
                EnemyPresenter enemy = allEnemies[i];
                if (enemy == null)
                    continue;

                if (enemy.GetRuntimeEnemyId() == runtimeId)
                {
                    RegisterEnemy(enemy);
                    return enemy;
                }
            }

            return null;
        }

        private static int BuildHitToken(int attackerActorNumber, string runtimeId)
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + attackerActorNumber;
                hash = hash * 31 + (runtimeId?.GetHashCode() ?? 0);
                hash = hash * 31 + PhotonNetwork.ServerTimestamp;
                return hash;
            }
        }

        private static bool TryGetFloat(object[] payload, int index, out float value)
        {
            value = 0f;
            if (index < 0 || index >= payload.Length || payload[index] == null)
                return false;

            if (payload[index] is float f)
            {
                value = f;
                return true;
            }

            if (payload[index] is int i)
            {
                value = i;
                return true;
            }

            return false;
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

            if (payload[index] is byte b)
            {
                value = b;
                return true;
            }

            return false;
        }

        private static bool TryGetBool(object[] payload, int index, out bool value)
        {
            value = false;
            if (index < 0 || index >= payload.Length || payload[index] == null)
                return false;

            if (payload[index] is bool b)
            {
                value = b;
                return true;
            }

            return false;
        }
    }
}
