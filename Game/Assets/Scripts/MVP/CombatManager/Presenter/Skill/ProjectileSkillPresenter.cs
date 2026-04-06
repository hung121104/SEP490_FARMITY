using UnityEngine;
using System.Collections;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;
using CombatManager.Model;
using CombatManager.Model;

namespace CombatManager.Presenter
{
    /// <summary>
    /// Handles ALL projectile-type skills (SkillCategory.Projectile).
    /// Replaces: AirSlashPresenter + WeaponSkillStaffSpecial.
    /// Reads all settings from SkillData SO - no hardcoded values.
    /// Attach ONE instance to CombatSystem GameObject.
    /// SkillHotbarPresenter finds this by SkillCategory.Projectile.
    /// </summary>
    public class ProjectileSkillPresenter : SkillPatternPresenter, IOnEventCallback
    {
        private const byte SKILL_PROJECTILE_VFX_EVENT = 164;

        public static ProjectileSkillPresenter Instance { get; private set; }

        [Header("Runtime Prefabs")]
        [SerializeField] private GameObject baseProjectilePrefab;

        // Current skill data being executed (set by SkillHotbarPresenter)
        private SkillData currentSkillData;

        #region Unity Lifecycle

        protected override void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            base.Awake();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void OnEnable()
        {
            PhotonNetwork.AddCallbackTarget(this);
        }

        private void OnDisable()
        {
            PhotonNetwork.RemoveCallbackTarget(this);
        }

        #endregion

        #region Public API - Called by SkillHotbarPresenter

        /// <summary>
        /// Set which skill data to use before triggering.
        /// Called by SkillHotbarPresenter before TriggerSkill().
        /// </summary>
        public void SetSkillData(SkillData skillData)
        {
            currentSkillData = skillData;

            if (skillData != null)
            {
                // ✅ Override base SkillPresenter settings from SO data
                skillCooldown   = skillData.cooldown;
                skillTier       = skillData.diceTier;
                skillMultiplier = skillData.skillMultiplier;

                // Re-sync model with new values
                SyncModelFromSkillData();

                Debug.Log($"[ProjectileSkillPresenter] SkillData set: {skillData.skillName}");
            }
        }

        public SkillData GetCurrentSkillData() => currentSkillData;

        #endregion

        #region SkillPresenter Abstract Implementation

        protected override SkillIndicatorData GetIndicatorData()
        {
            if (currentSkillData == null) return null;
            return SkillIndicatorData.Arrow(currentSkillData.projectileRange);
        }

        protected override IEnumerator OnExecute(int finalDamage, Vector3 direction)
        {
            if (currentSkillData == null)
            {
                Debug.LogWarning("[ProjectileSkillPresenter] No SkillData assigned!");
                yield break;
            }

            FireProjectile(finalDamage, direction);
            BroadcastProjectileSkillVfx(direction);
            yield return new WaitForSeconds(0.1f);
        }

        #endregion

        #region Projectile Logic

        private void FireProjectile(int damage, Vector3 direction)
        {
            if (baseProjectilePrefab == null)
            {
                Debug.LogWarning($"[ProjectileSkillPresenter] " +
                                 $"baseProjectilePrefab is not assigned in presenter.");
                return;
            }

            if (playerTransform == null)
            {
                Debug.LogWarning("[ProjectileSkillPresenter] playerTransform is null!");
                return;
            }

            GameObject projectileGO = Instantiate(
                baseProjectilePrefab,
                playerTransform.position,
                Quaternion.identity
            );

            ApplySkillTint(projectileGO, currentSkillData.skillVisualConfigId);

            ProjectileModel projectileModel = new ProjectileModel
            {
                direction       = direction.normalized,
                speed           = currentSkillData.projectileSpeed,
                maxRange        = currentSkillData.projectileRange,
                damage          = damage,
                knockbackForce  = currentSkillData.projectileKnockback,
                enemyLayers     = enemyLayers,
                playerTransform = playerTransform
            };

            ProjectilePresenter projectilePresenter =
                projectileGO.GetComponent<ProjectilePresenter>();

            if (projectilePresenter == null)
            {
                Debug.LogWarning("[ProjectileSkillPresenter] " +
                                 "ProjectilePresenter missing on prefab!");
                Destroy(projectileGO);
                return;
            }

            projectilePresenter.Initialize(projectileModel);

            Debug.Log($"[ProjectileSkillPresenter] Fired! " +
                      $"Skill={currentSkillData.skillName} | " +
                      $"Damage={damage} | Dir={direction} | " +
                      $"Speed={currentSkillData.projectileSpeed} | " +
                      $"Range={currentSkillData.projectileRange}");
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
                Debug.LogWarning(
                    $"[ProjectileSkillPresenter] Missing or invalid tint config '{skillVisualConfigId}'.");
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

        public void OnEvent(EventData photonEvent)
        {
            if (photonEvent.Code != SKILL_PROJECTILE_VFX_EVENT)
                return;

            if (photonEvent.CustomData is not object[] payload || payload.Length < 11)
                return;

            if (!TryGetPayloadInt(payload, 0, out int sourceActor) ||
                !TryGetPayloadFloat(payload, 2, out float posX) ||
                !TryGetPayloadFloat(payload, 3, out float posY) ||
                !TryGetPayloadFloat(payload, 4, out float posZ) ||
                !TryGetPayloadFloat(payload, 5, out float dirX) ||
                !TryGetPayloadFloat(payload, 6, out float dirY) ||
                !TryGetPayloadFloat(payload, 7, out float dirZ) ||
                !TryGetPayloadFloat(payload, 8, out float speed) ||
                !TryGetPayloadFloat(payload, 9, out float range))
            {
                return;
            }

            if (sourceActor == (PhotonNetwork.LocalPlayer?.ActorNumber ?? -1))
                return;

            string skillVisualConfigId = payload[1] as string ?? string.Empty;
            string skillId = payload[10] as string ?? string.Empty;

            SpawnRemoteProjectileVfx(
                new Vector3(posX, posY, posZ),
                new Vector3(dirX, dirY, dirZ).normalized,
                speed,
                range,
                skillVisualConfigId,
                skillId);
        }

        private void BroadcastProjectileSkillVfx(Vector3 direction)
        {
            if (!PhotonNetwork.IsConnected || currentSkillData == null || playerTransform == null)
                return;

            int actorNumber = PhotonNetwork.LocalPlayer?.ActorNumber ?? -1;
            if (actorNumber <= 0)
                return;

            Vector3 spawnPos = playerTransform.position;
            Vector3 dir = direction.normalized;
            if (dir.sqrMagnitude < 0.0001f)
                return;

            object[] payload =
            {
                actorNumber,
                currentSkillData.skillVisualConfigId ?? string.Empty,
                spawnPos.x,
                spawnPos.y,
                spawnPos.z,
                dir.x,
                dir.y,
                dir.z,
                currentSkillData.projectileSpeed,
                currentSkillData.projectileRange,
                currentSkillData.skillId ?? string.Empty
            };

            RaiseEventOptions options = new RaiseEventOptions { Receivers = ReceiverGroup.Others };
            PhotonNetwork.RaiseEvent(SKILL_PROJECTILE_VFX_EVENT, payload, options, SendOptions.SendUnreliable);
        }

        private void SpawnRemoteProjectileVfx(
            Vector3 spawnPos,
            Vector3 direction,
            float speed,
            float range,
            string skillVisualConfigId,
            string skillId)
        {
            if (baseProjectilePrefab == null)
                return;

            GameObject projectileGO = Instantiate(baseProjectilePrefab, spawnPos, Quaternion.identity);
            ApplySkillTint(projectileGO, skillVisualConfigId);

            ProjectilePresenter projectilePresenter = projectileGO.GetComponent<ProjectilePresenter>();
            if (projectilePresenter == null)
            {
                Destroy(projectileGO);
                return;
            }

            ProjectileModel projectileModel = new ProjectileModel
            {
                direction = direction,
                speed = speed,
                maxRange = range,
                damage = 0,
                knockbackForce = 0f,
                enemyLayers = 0,
                playerTransform = transform
            };

            projectilePresenter.Initialize(projectileModel);

            if (!string.IsNullOrWhiteSpace(skillId))
            {
                Debug.Log($"[ProjectileSkillPresenter] Remote projectile VFX spawned for skill '{skillId}'.");
            }
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

        #endregion

        #region Virtual Overrides

        protected override void OnStart() =>
            Debug.Log("[ProjectileSkillPresenter] Ready!");

        protected override void OnChargeStart() =>
            Debug.Log($"[ProjectileSkillPresenter] Charging: {currentSkillData?.skillName}");

        protected override void OnAttackStart() =>
            Debug.Log($"[ProjectileSkillPresenter] Firing: {currentSkillData?.skillName}");

        protected override void OnAttackEnd() =>
            Debug.Log($"[ProjectileSkillPresenter] Done: {currentSkillData?.skillName}");

        protected override void OnSkillCancelled() =>
            Debug.Log($"[ProjectileSkillPresenter] Cancelled: {currentSkillData?.skillName}");

        #endregion

        #region Private Helpers

        private void SyncModelFromSkillData()
        {
            model.skillCooldown   = skillCooldown;
            model.skillTier       = skillTier;
            model.skillMultiplier = skillMultiplier;
        }

        #endregion
    }
}
