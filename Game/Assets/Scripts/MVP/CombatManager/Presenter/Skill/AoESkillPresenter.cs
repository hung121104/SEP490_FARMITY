using System.Collections;
using System.Collections.Generic;
using CombatManager.Model;
using CombatManager.Service;
using CombatManager.View;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

namespace CombatManager.Presenter
{
    /// <summary>
    /// Handles all AoE-type skills (SkillCategory.AoE).
    /// Casts at clamped mouse position, shows circle indicator, and applies damage at animation event timing.
    /// </summary>
    public class AoESkillPresenter : SkillPatternPresenter, IOnEventCallback
    {
        private const byte SKILL_AOE_VFX_EVENT = 165;

        public static AoESkillPresenter Instance { get; private set; }

        [Header("Runtime Prefabs")]
        [SerializeField] private GameObject baseAoePrefab;
        [Tooltip("The AoE prefab radius in world units when localScale = 1. Used to scale VFX to aoeRadius.")]
        [SerializeField] private float aoeVisualRadiusAtScaleOne = 1f;

        private readonly List<Collider2D> activeAttackTargets = new List<Collider2D>(32);
        private readonly HashSet<EnemyPresenter> uniqueEnemies = new HashSet<EnemyPresenter>();

        private SkillData currentSkillData;
        private GameObject activeAoeInstance;
        private Vector3 pendingImpactPosition;
        private int pendingImpactDamage;
        private AoEAttackHitbox activeAoeHitbox;

        protected override void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }

            Instance = this;
            base.Awake();
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        private void OnEnable()
        {
            PhotonNetwork.AddCallbackTarget(this);
        }

        private void OnDisable()
        {
            PhotonNetwork.RemoveCallbackTarget(this);
        }

        public void SetSkillData(SkillData skillData)
        {
            currentSkillData = skillData;

            if (skillData != null)
            {
                skillCooldown = skillData.cooldown;
                skillTier = skillData.diceTier;
                skillMultiplier = skillData.skillMultiplier;
                SyncModelFromSkillData();

                Debug.Log($"[AoESkillPresenter] SkillData set: {skillData.skillName}");
            }
        }

        public SkillData GetCurrentSkillData() => currentSkillData;

        protected override SkillIndicatorData GetIndicatorData()
        {
            if (currentSkillData == null)
                return null;

            return SkillIndicatorData.Circle(
                Mathf.Max(0.1f, currentSkillData.aoeRadius),
                Mathf.Max(0.1f, currentSkillData.aoeCastRange));
        }

        protected override IEnumerator OnExecute(int finalDamage, Vector3 direction)
        {
            if (currentSkillData == null)
            {
                Debug.LogWarning("[AoESkillPresenter] No SkillData assigned!");
                yield break;
            }

            if (playerTransform == null)
            {
                Debug.LogWarning("[AoESkillPresenter] Player transform missing.");
                yield break;
            }

            pendingImpactPosition = GetClampedMouseCastPosition(Mathf.Max(0.1f, currentSkillData.aoeCastRange));
            pendingImpactDamage = finalDamage;

            SpawnLocalAoeVfx(pendingImpactPosition);
            BroadcastAoeSkillVfx(pendingImpactPosition);

            float fallbackDuration = Mathf.Max(0.05f, currentSkillData.aoeVfxDuration);
            yield return new WaitForSeconds(fallbackDuration);
        }

        public void OnEvent(EventData photonEvent)
        {
            if (photonEvent.Code != SKILL_AOE_VFX_EVENT)
                return;

            if (photonEvent.CustomData is not object[] payload || payload.Length < 8)
                return;

            if (!TryGetPayloadInt(payload, 0, out int sourceActor) ||
                !TryGetPayloadFloat(payload, 2, out float posX) ||
                !TryGetPayloadFloat(payload, 3, out float posY) ||
                !TryGetPayloadFloat(payload, 4, out float posZ) ||
                !TryGetPayloadFloat(payload, 5, out float radius) ||
                !TryGetPayloadFloat(payload, 6, out float duration))
            {
                return;
            }

            if (sourceActor == (PhotonNetwork.LocalPlayer?.ActorNumber ?? -1))
                return;

            string skillVisualConfigId = payload[1] as string ?? string.Empty;
            string skillId = payload[7] as string ?? string.Empty;

            SpawnRemoteAoeVfx(new Vector3(posX, posY, posZ), radius, duration, skillVisualConfigId, skillId);
        }

        protected override void OnStart() =>
            Debug.Log("[AoESkillPresenter] Ready!");

        protected override void OnChargeStart() =>
            Debug.Log($"[AoESkillPresenter] Charging: {currentSkillData?.skillName}");

        protected override void OnAttackStart() =>
            Debug.Log($"[AoESkillPresenter] Casting: {currentSkillData?.skillName}");

        protected override void OnAttackEnd() =>
            Debug.Log($"[AoESkillPresenter] Done: {currentSkillData?.skillName}");

        protected override void OnSkillCancelled() =>
            Debug.Log($"[AoESkillPresenter] Cancelled: {currentSkillData?.skillName}");

        private void SpawnLocalAoeVfx(Vector3 castPosition)
        {
            if (baseAoePrefab == null)
            {
                Debug.LogWarning("[AoESkillPresenter] baseAoePrefab is not assigned.");
                return;
            }

            float radius = Mathf.Max(0.1f, currentSkillData.aoeRadius);
            float duration = Mathf.Max(0.05f, currentSkillData.aoeVfxDuration);

            activeAoeInstance = Instantiate(baseAoePrefab, castPosition, Quaternion.identity);
            ApplySkillTint(activeAoeInstance, currentSkillData.skillVisualConfigId);
            ApplyAoeScale(activeAoeInstance, radius);

            activeAoeHitbox = activeAoeInstance.GetComponent<AoEAttackHitbox>();
            if (activeAoeHitbox == null)
            {
                activeAoeHitbox = activeAoeInstance.AddComponent<AoEAttackHitbox>();
            }

            AoESkillImpactEventRelay relay = activeAoeInstance.GetComponent<AoESkillImpactEventRelay>();
            if (relay == null)
                relay = activeAoeInstance.AddComponent<AoESkillImpactEventRelay>();

            relay.Initialize(OnAoeImpactAnimationEvent);

            Destroy(activeAoeInstance, duration + 0.1f);
        }

        private void OnAoeImpactAnimationEvent()
        {
            if (currentSkillData == null || playerTransform == null)
                return;

            if (activeAoeHitbox == null)
            {
                Debug.LogWarning("[AoESkillPresenter] Impact skipped: active AoE hitbox missing.");
                return;
            }

            activeAoeHitbox.CollectOverlappingEnemies(activeAttackTargets);
            uniqueEnemies.Clear();

            for (int i = 0; i < activeAttackTargets.Count; i++)
            {
                Collider2D hit = activeAttackTargets[i];
                if (hit == null)
                    continue;

                EnemyPresenter enemyPresenter = hit.GetComponent<EnemyPresenter>()
                    ?? hit.GetComponentInParent<EnemyPresenter>()
                    ?? hit.GetComponentInChildren<EnemyPresenter>();

                if (enemyPresenter == null)
                    continue;

                if (!uniqueEnemies.Add(enemyPresenter))
                    continue;

                Vector2 knockbackDir = (enemyPresenter.transform.position - pendingImpactPosition).normalized;
                if (knockbackDir.sqrMagnitude < 0.0001f)
                    knockbackDir = Vector2.up;

                EnemySyncManager.Instance.RequestEnemyHit(
                    enemyPresenter,
                    pendingImpactDamage,
                    knockbackDir,
                    currentSkillData.slashKnockbackForce);
            }

            Debug.Log($"[AoESkillPresenter] Impact applied at {pendingImpactPosition} hits={uniqueEnemies.Count} damage={pendingImpactDamage}");
        }

        private Vector3 GetClampedMouseCastPosition(float maxRange)
        {
            Camera cam = mainCamera != null ? mainCamera : Camera.main;
            Vector3 fallback = playerTransform.position;

            if (cam == null)
                return fallback;

            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            Plane plane = new Plane(Vector3.forward, playerTransform.position);
            if (!plane.Raycast(ray, out float dist))
                return fallback;

            Vector3 mouseWorld = ray.GetPoint(dist);
            Vector3 fromPlayer = mouseWorld - playerTransform.position;
            fromPlayer.z = 0f;

            if (fromPlayer.sqrMagnitude <= maxRange * maxRange)
                return new Vector3(mouseWorld.x, mouseWorld.y, playerTransform.position.z);

            Vector3 clamped = playerTransform.position + fromPlayer.normalized * maxRange;
            return new Vector3(clamped.x, clamped.y, playerTransform.position.z);
        }

        private static void ApplySkillTint(GameObject target, string skillVisualConfigId)
        {
            if (target == null || string.IsNullOrWhiteSpace(skillVisualConfigId))
                return;

            SkillVfxCatalogManager catalog = SkillVfxCatalogManager.Instance;
            if (catalog == null)
                return;

            if (!catalog.TryGetPrimaryTint(skillVisualConfigId, out Color tint))
            {
                Debug.LogWarning($"[AoESkillPresenter] Missing or invalid tint config '{skillVisualConfigId}'.");
                return;
            }

            SpriteRenderer[] renderers = target.GetComponentsInChildren<SpriteRenderer>(true);
            foreach (SpriteRenderer renderer in renderers)
                renderer.color = tint;

            ParticleSystem[] particles = target.GetComponentsInChildren<ParticleSystem>(true);
            foreach (ParticleSystem particle in particles)
            {
                var main = particle.main;
                main.startColor = tint;
            }
        }

        private void ApplyAoeScale(GameObject target, float radius)
        {
            if (target == null)
                return;

            float reference = Mathf.Max(0.01f, aoeVisualRadiusAtScaleOne);
            float scale = radius / reference;
            target.transform.localScale = new Vector3(scale, scale, 1f);
        }

        private void BroadcastAoeSkillVfx(Vector3 castPosition)
        {
            if (!PhotonNetwork.IsConnected || currentSkillData == null)
                return;

            int actorNumber = PhotonNetwork.LocalPlayer?.ActorNumber ?? -1;
            if (actorNumber <= 0)
                return;

            object[] payload =
            {
                actorNumber,
                currentSkillData.skillVisualConfigId ?? string.Empty,
                castPosition.x,
                castPosition.y,
                castPosition.z,
                Mathf.Max(0.1f, currentSkillData.aoeRadius),
                Mathf.Max(0.05f, currentSkillData.aoeVfxDuration),
                currentSkillData.skillId ?? string.Empty
            };

            RaiseEventOptions options = new RaiseEventOptions { Receivers = ReceiverGroup.Others };
            PhotonNetwork.RaiseEvent(SKILL_AOE_VFX_EVENT, payload, options, SendOptions.SendUnreliable);
        }

        private void SpawnRemoteAoeVfx(Vector3 castPosition, float radius, float duration, string skillVisualConfigId, string skillId)
        {
            if (baseAoePrefab == null)
                return;

            GameObject aoeVfx = Instantiate(baseAoePrefab, castPosition, Quaternion.identity);
            ApplySkillTint(aoeVfx, skillVisualConfigId);
            ApplyAoeScale(aoeVfx, Mathf.Max(0.1f, radius));

            if (aoeVfx.GetComponent<AoEAttackHitbox>() == null)
            {
                aoeVfx.AddComponent<AoEAttackHitbox>();
            }

            Destroy(aoeVfx, Mathf.Max(0.05f, duration) + 0.1f);

            if (!string.IsNullOrWhiteSpace(skillId))
                Debug.Log($"[AoESkillPresenter] Remote AoE VFX spawned for skill '{skillId}'.");
        }

        private void SyncModelFromSkillData()
        {
            model.skillCooldown = skillCooldown;
            model.skillTier = skillTier;
            model.skillMultiplier = skillMultiplier;
        }

        private static bool TryGetPayloadInt(object[] payload, int index, out int value)
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

        private static bool TryGetPayloadFloat(object[] payload, int index, out float value)
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
    }
}
