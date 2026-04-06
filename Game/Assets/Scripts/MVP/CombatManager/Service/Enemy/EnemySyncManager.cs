using System;
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
        private const byte ENEMY_PLAYER_DAMAGE_REQUEST_EVENT = 170;
        private const byte ENEMY_PLAYER_DAMAGE_APPLIED_EVENT = 171;
        private const byte ENEMY_EXP_GRANTED_EVENT = 175;
        private const int DAMAGE_TOKEN_HISTORY_LIMIT = 128;

        private static EnemySyncManager instance;
        private static int localHitSequence;

        private readonly Dictionary<string, EnemyPresenter> enemiesByRuntimeId = new Dictionary<string, EnemyPresenter>();
        private readonly Dictionary<string, float> lastEnemyDamageByTarget = new Dictionary<string, float>();
        private readonly Dictionary<string, Dictionary<int, int>> enemyDamageContributionByRuntimeId = new Dictionary<string, Dictionary<int, int>>();
        private readonly Dictionary<string, Dictionary<int, int>> attackerLevelByRuntimeId = new Dictionary<string, Dictionary<int, int>>();
        private readonly HashSet<int> consumedEnemyDamageTokens = new HashSet<int>();
        private readonly Queue<int> enemyDamageTokenOrder = new Queue<int>();

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

        public bool TryGetEnemyByRuntimeId(string runtimeId, out EnemyPresenter enemy)
        {
            enemy = null;
            if (string.IsNullOrWhiteSpace(runtimeId))
                return false;

            if (enemiesByRuntimeId.TryGetValue(runtimeId, out EnemyPresenter cached) && cached != null)
            {
                enemy = cached;
                return true;
            }

            EnemyPresenter resolved = ResolveEnemy(runtimeId);
            if (resolved == null)
                return false;

            enemy = resolved;
            return true;
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

            int hitToken = BuildHitToken(attackerActorNumber);
            int attackerLevel = ResolveLocalPlayerLevel();

            if (IsAuthoritative)
            {
                ProcessHitRequest(runtimeId, damage, knockbackDir, knockbackForce, attackerActorNumber, hitToken, attackerLevel);
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
                attackerLevel,
            };

            RaiseEventOptions options = new RaiseEventOptions { Receivers = ReceiverGroup.MasterClient };
            PhotonNetwork.RaiseEvent(ENEMY_HIT_REQUEST_EVENT, payload, options, SendOptions.SendReliable);
        }

        public void RequestEnemyPlayerTouchDamage(
            EnemyPresenter enemy,
            int targetActorNumber,
            int damage,
            float knockbackForce,
            Vector2 enemyWorldPosition)
        {
            if (enemy == null || damage <= 0)
                return;

            string runtimeId = enemy.GetRuntimeEnemyId();
            if (string.IsNullOrWhiteSpace(runtimeId))
                return;

            RegisterEnemy(enemy);

            if (IsAuthoritative)
            {
                ProcessEnemyPlayerDamageRequest(runtimeId, targetActorNumber, damage, knockbackForce, enemyWorldPosition);
                return;
            }

            object[] payload =
            {
                runtimeId,
                targetActorNumber,
                damage,
                knockbackForce,
                enemyWorldPosition.x,
                enemyWorldPosition.y,
            };

            RaiseEventOptions options = new RaiseEventOptions { Receivers = ReceiverGroup.MasterClient };
            PhotonNetwork.RaiseEvent(ENEMY_PLAYER_DAMAGE_REQUEST_EVENT, payload, options, SendOptions.SendReliable);
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

                case ENEMY_PLAYER_DAMAGE_REQUEST_EVENT:
                    HandleEnemyPlayerDamageRequestEvent(photonEvent);
                    break;

                case ENEMY_PLAYER_DAMAGE_APPLIED_EVENT:
                    HandleEnemyPlayerDamageAppliedEvent(photonEvent);
                    break;

                case ENEMY_EXP_GRANTED_EVENT:
                    HandleEnemyExpGrantedEvent(photonEvent);
                    break;
            }
        }

        private void HandleHitRequestEvent(EventData photonEvent)
        {
            if (!IsAuthoritative)
                return;

            if (photonEvent.CustomData is not object[] payload || payload.Length < 8)
                return;

            string runtimeId = payload[0] as string ?? string.Empty;
            if (string.IsNullOrWhiteSpace(runtimeId))
                return;

            if (!TryGetInt(payload, 1, out int damage) ||
                !TryGetFloat(payload, 2, out float knockbackX) ||
                !TryGetFloat(payload, 3, out float knockbackY) ||
                !TryGetFloat(payload, 4, out float knockbackForce) ||
                !TryGetInt(payload, 5, out int attackerActorNumber) ||
                !TryGetInt(payload, 6, out int hitToken) ||
                !TryGetInt(payload, 7, out int attackerLevel))
            {
                return;
            }

            ProcessHitRequest(
                runtimeId,
                damage,
                new Vector2(knockbackX, knockbackY),
                knockbackForce,
                attackerActorNumber,
                hitToken,
                attackerLevel);
        }

        private void ProcessHitRequest(
            string runtimeId,
            int damage,
            Vector2 knockbackDir,
            float knockbackForce,
            int attackerActorNumber,
            int hitToken,
            int attackerLevel)
        {
            if (!IsAuthoritative || string.IsNullOrWhiteSpace(runtimeId) || damage <= 0)
                return;

            EnemyPresenter enemy = ResolveEnemy(runtimeId);
            if (enemy == null || !enemy.IsInitialized() || enemy.IsDead())
                return;

            RecordDamageContribution(runtimeId, attackerActorNumber, damage, attackerLevel);

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

        public void ProcessEnemyDeathReward(string runtimeId, int enemyLevel, int baseExp)
        {
            if (!IsAuthoritative || string.IsNullOrWhiteSpace(runtimeId) || baseExp <= 0)
                return;

            if (!enemyDamageContributionByRuntimeId.TryGetValue(runtimeId, out Dictionary<int, int> contributionMap) || contributionMap == null || contributionMap.Count == 0)
                return;

            int totalDamage = 0;
            foreach (KeyValuePair<int, int> entry in contributionMap)
            {
                if (entry.Value > 0)
                    totalDamage += entry.Value;
            }

            if (totalDamage <= 0)
            {
                ClearEnemyCombatTracking(runtimeId);
                return;
            }

            List<ExpShareCandidate> candidates = new List<ExpShareCandidate>();
            int awardedFloorTotal = 0;

            foreach (KeyValuePair<int, int> entry in contributionMap)
            {
                int actorNumber = entry.Key;
                int dealtDamage = Mathf.Max(0, entry.Value);
                if (dealtDamage <= 0)
                    continue;

                int playerLevel = ResolveAttackerLevel(runtimeId, actorNumber);
                float multiplier = Mathf.Clamp(1f + ((enemyLevel - playerLevel) * 0.1f), 0.5f, 2f);
                float playerScaledTotal = Mathf.Max(1f, baseExp * multiplier);

                float exactShare = playerScaledTotal * ((float)dealtDamage / totalDamage);
                int floorShare = Mathf.FloorToInt(exactShare);
                float remainder = exactShare - floorShare;

                candidates.Add(new ExpShareCandidate
                {
                    actorNumber = actorNumber,
                    floorShare = Mathf.Max(0, floorShare),
                    remainder = Mathf.Clamp01(remainder),
                });

                awardedFloorTotal += Mathf.Max(0, floorShare);
            }

            if (candidates.Count == 0)
            {
                ClearEnemyCombatTracking(runtimeId);
                return;
            }

            float expectedTotalFloat = 0f;
            foreach (ExpShareCandidate candidate in candidates)
            {
                int playerLevel = ResolveAttackerLevel(runtimeId, candidate.actorNumber);
                float multiplier = Mathf.Clamp(1f + ((enemyLevel - playerLevel) * 0.1f), 0.5f, 2f);
                expectedTotalFloat += Mathf.Max(1f, baseExp * multiplier) * (Mathf.Max(1, contributionMap[candidate.actorNumber]) / (float)totalDamage);
            }

            int expectedTotal = Mathf.Max(1, Mathf.RoundToInt(expectedTotalFloat));
            int remaining = Mathf.Max(0, expectedTotal - awardedFloorTotal);

            candidates.Sort((a, b) =>
            {
                int remainderCompare = b.remainder.CompareTo(a.remainder);
                if (remainderCompare != 0)
                    return remainderCompare;
                return a.actorNumber.CompareTo(b.actorNumber);
            });

            for (int i = 0; i < candidates.Count && remaining > 0; i++)
            {
                ExpShareCandidate updated = candidates[i];
                updated.floorShare += 1;
                candidates[i] = updated;
                remaining -= 1;
            }

            for (int i = 0; i < candidates.Count; i++)
            {
                ExpShareCandidate candidate = candidates[i];
                int expAward = Mathf.Max(0, candidate.floorShare);
                if (expAward <= 0)
                    continue;

                if (PhotonNetwork.IsConnected)
                {
                    object[] payload = { candidate.actorNumber, expAward, runtimeId };
                    RaiseEventOptions options = new RaiseEventOptions { Receivers = ReceiverGroup.All };
                    PhotonNetwork.RaiseEvent(ENEMY_EXP_GRANTED_EVENT, payload, options, SendOptions.SendReliable);
                }
                else
                {
                    ApplyExpAwardToLocalPlayer(expAward);
                }
            }

            ClearEnemyCombatTracking(runtimeId);
        }

        public void ClearEnemyRuntimeTracking(string runtimeId)
        {
            ClearEnemyCombatTracking(runtimeId);
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

        private void HandleEnemyPlayerDamageRequestEvent(EventData photonEvent)
        {
            if (!IsAuthoritative)
                return;

            if (photonEvent.CustomData is not object[] payload || payload.Length < 6)
                return;

            string runtimeId = payload[0] as string ?? string.Empty;
            if (string.IsNullOrWhiteSpace(runtimeId))
                return;

            if (!TryGetInt(payload, 1, out int targetActorNumber) ||
                !TryGetInt(payload, 2, out int damage) ||
                !TryGetFloat(payload, 3, out float knockbackForce) ||
                !TryGetFloat(payload, 4, out float enemyPosX) ||
                !TryGetFloat(payload, 5, out float enemyPosY))
            {
                return;
            }

            ProcessEnemyPlayerDamageRequest(
                runtimeId,
                targetActorNumber,
                damage,
                knockbackForce,
                new Vector2(enemyPosX, enemyPosY));
        }

        private void ProcessEnemyPlayerDamageRequest(
            string runtimeId,
            int targetActorNumber,
            int damage,
            float knockbackForce,
            Vector2 enemyWorldPosition)
        {
            if (!IsAuthoritative || string.IsNullOrWhiteSpace(runtimeId) || damage <= 0)
                return;

            EnemyPresenter enemy = ResolveEnemy(runtimeId);
            if (enemy == null || !enemy.IsInitialized() || enemy.IsDead())
                return;

            if (PhotonNetwork.IsConnected && targetActorNumber <= 0)
                return;

            if (PhotonNetwork.IsConnected && PhotonNetwork.CurrentRoom != null)
            {
                Player targetPlayer = PhotonNetwork.CurrentRoom.GetPlayer(targetActorNumber);
                if (targetPlayer == null)
                    return;
            }

            int appliedDamage = Mathf.Max(0, enemy.GetContactDamageAmount());
            if (appliedDamage <= 0)
                return;

            float appliedKnockbackForce = Mathf.Max(0f, enemy.GetContactKnockbackForce());
            float throttleWindow = Mathf.Max(0.05f, enemy.GetContactDamageThrottleTime());
            if (!CanApplyEnemyDamage(runtimeId, targetActorNumber, throttleWindow))
                return;

            int damageToken = BuildEnemyPlayerDamageToken(targetActorNumber);

            ApplyEnemyPlayerDamageToLocalTarget(
                targetActorNumber,
                appliedDamage,
                appliedKnockbackForce,
                enemyWorldPosition);

            if (!PhotonNetwork.IsConnected)
                return;

            object[] payload =
            {
                runtimeId,
                targetActorNumber,
                appliedDamage,
                appliedKnockbackForce,
                enemyWorldPosition.x,
                enemyWorldPosition.y,
                damageToken,
            };

            RaiseEventOptions options = new RaiseEventOptions { Receivers = ReceiverGroup.Others };
            PhotonNetwork.RaiseEvent(ENEMY_PLAYER_DAMAGE_APPLIED_EVENT, payload, options, SendOptions.SendReliable);
        }

        private void HandleEnemyPlayerDamageAppliedEvent(EventData photonEvent)
        {
            if (photonEvent.CustomData is not object[] payload || payload.Length < 7)
                return;

            if (!TryGetInt(payload, 1, out int targetActorNumber) ||
                !TryGetInt(payload, 2, out int damage) ||
                !TryGetFloat(payload, 3, out float knockbackForce) ||
                !TryGetFloat(payload, 4, out float enemyPosX) ||
                !TryGetFloat(payload, 5, out float enemyPosY) ||
                !TryGetInt(payload, 6, out int damageToken))
            {
                return;
            }

            if (!TryConsumeEnemyDamageToken(damageToken))
                return;

            ApplyEnemyPlayerDamageToLocalTarget(
                targetActorNumber,
                damage,
                knockbackForce,
                new Vector2(enemyPosX, enemyPosY));
        }

        private void ApplyEnemyPlayerDamageToLocalTarget(
            int targetActorNumber,
            int damage,
            float knockbackForce,
            Vector2 enemyWorldPosition)
        {
            if (damage <= 0)
                return;

            if (PhotonNetwork.IsConnected)
            {
                int localActorNumber = PhotonNetwork.LocalPlayer?.ActorNumber ?? -1;
                if (localActorNumber <= 0 || localActorNumber != targetActorNumber)
                    return;
            }

            if (!PlayerHealthPresenter.TryApplyDamageForActor(targetActorNumber, damage))
                return;

            PlayerKnockbackPresenter.TryApplyKnockbackForActor(targetActorNumber, enemyWorldPosition, knockbackForce);
        }

        private bool CanApplyEnemyDamage(string runtimeId, int targetActorNumber, float throttleWindow)
        {
            string key = runtimeId + ":" + targetActorNumber;
            float now = Time.time;

            if (lastEnemyDamageByTarget.TryGetValue(key, out float lastTime) && now - lastTime < throttleWindow)
                return false;

            lastEnemyDamageByTarget[key] = now;
            return true;
        }

        private bool TryConsumeEnemyDamageToken(int token)
        {
            if (token == 0)
                return false;

            if (!consumedEnemyDamageTokens.Add(token))
                return false;

            enemyDamageTokenOrder.Enqueue(token);
            while (enemyDamageTokenOrder.Count > DAMAGE_TOKEN_HISTORY_LIMIT)
            {
                int old = enemyDamageTokenOrder.Dequeue();
                consumedEnemyDamageTokens.Remove(old);
            }

            return true;
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

        private void HandleEnemyExpGrantedEvent(EventData photonEvent)
        {
            if (photonEvent.CustomData is not object[] payload || payload.Length < 3)
                return;

            if (!TryGetInt(payload, 0, out int targetActorNumber) ||
                !TryGetInt(payload, 1, out int expAward))
            {
                return;
            }

            if (PhotonNetwork.IsConnected)
            {
                int localActorNumber = PhotonNetwork.LocalPlayer?.ActorNumber ?? -1;
                if (localActorNumber <= 0 || localActorNumber != targetActorNumber)
                    return;
            }

            ApplyExpAwardToLocalPlayer(expAward);
        }

        private void ApplyExpAwardToLocalPlayer(int expAward)
        {
            if (expAward <= 0)
                return;

            StatsPresenter statsPresenter = FindObjectOfType<StatsPresenter>();
            if (statsPresenter == null)
                return;

            statsPresenter.AddExperienceFromHost(expAward);
        }

        private void RecordDamageContribution(string runtimeId, int attackerActorNumber, int damage, int attackerLevel)
        {
            if (string.IsNullOrWhiteSpace(runtimeId) || damage <= 0)
                return;

            int actorKey = attackerActorNumber;
            if (!PhotonNetwork.IsConnected && actorKey <= 0)
                actorKey = 0;

            if (!enemyDamageContributionByRuntimeId.TryGetValue(runtimeId, out Dictionary<int, int> contributionMap))
            {
                contributionMap = new Dictionary<int, int>();
                enemyDamageContributionByRuntimeId[runtimeId] = contributionMap;
            }

            contributionMap.TryGetValue(actorKey, out int currentDamage);
            contributionMap[actorKey] = currentDamage + damage;

            if (!attackerLevelByRuntimeId.TryGetValue(runtimeId, out Dictionary<int, int> levelMap))
            {
                levelMap = new Dictionary<int, int>();
                attackerLevelByRuntimeId[runtimeId] = levelMap;
            }

            levelMap[actorKey] = Mathf.Max(1, attackerLevel);
        }

        private int ResolveAttackerLevel(string runtimeId, int actorNumber)
        {
            if (attackerLevelByRuntimeId.TryGetValue(runtimeId, out Dictionary<int, int> levelMap) &&
                levelMap != null &&
                levelMap.TryGetValue(actorNumber, out int attackerLevel))
            {
                return Mathf.Max(1, attackerLevel);
            }

            return 1;
        }

        private int ResolveLocalPlayerLevel()
        {
            StatsPresenter statsPresenter = FindObjectOfType<StatsPresenter>();
            return statsPresenter != null ? Mathf.Max(1, statsPresenter.GetLevel()) : 1;
        }

        private void ClearEnemyCombatTracking(string runtimeId)
        {
            if (string.IsNullOrWhiteSpace(runtimeId))
                return;

            enemyDamageContributionByRuntimeId.Remove(runtimeId);
            attackerLevelByRuntimeId.Remove(runtimeId);
        }

        private struct ExpShareCandidate
        {
            public int actorNumber;
            public int floorShare;
            public float remainder;
        }

        private static int BuildHitToken(int attackerActorNumber)
        {
            unchecked
            {
                int normalizedActor = attackerActorNumber > 0
                    ? attackerActorNumber & 0x00000FFF
                    : 0;

                int sequence = ++localHitSequence;
                if (sequence <= 0)
                {
                    localHitSequence = 1;
                    sequence = 1;
                }

                return (normalizedActor << 20) | (sequence & 0x000FFFFF);
            }
        }

        private static int BuildEnemyPlayerDamageToken(int targetActorNumber)
        {
            unchecked
            {
                int normalizedTarget = targetActorNumber > 0
                    ? targetActorNumber & 0x00000FFF
                    : 0;

                int sequence = ++localHitSequence;
                if (sequence <= 0)
                {
                    localHitSequence = 1;
                    sequence = 1;
                }

                return (normalizedTarget << 20) | (sequence & 0x000FFFFF);
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
