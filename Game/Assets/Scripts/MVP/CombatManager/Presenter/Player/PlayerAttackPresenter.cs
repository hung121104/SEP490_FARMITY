using UnityEngine;
using Photon.Pun;
using System.Collections;
using CombatManager.Model;
using CombatManager.Service;
using CombatManager.View;
using CombatManager.SO;

namespace CombatManager.Presenter
{
    public class PlayerAttackPresenter : MonoBehaviour
    {
        [Header("Model")]
        [SerializeField] private PlayerAttackModel model = new PlayerAttackModel();

        [Header("VFX Prefabs - Melee")]
        [SerializeField] private GameObject stabVFXPrefab;
        [SerializeField] private GameObject horizontalVFXPrefab;
        [SerializeField] private GameObject verticalVFXPrefab;
        [SerializeField] private GameObject damagePopupPrefab;

        [Header("Staff Projectile Prefab")]
        [SerializeField] private GameObject staffProjectilePrefab;

        [Header("Combat Settings")]
        [SerializeField] private LayerMask enemyLayers;

        [Header("VFX Position Offsets")]
        [SerializeField] private Vector2 stabPositionOffset = Vector2.zero;
        [SerializeField] private Vector2 horizontalPositionOffset = Vector2.zero;
        [SerializeField] private Vector2 verticalPositionOffset = Vector2.zero;

        [Header("VFX Spawn Settings")]
        [SerializeField] private float vfxSpawnOffset = 1f;

        [Header("Dependencies")]
        [SerializeField] private StatsPresenter statsPresenter;
        [SerializeField] private PlayerPointerPresenter pointerPresenter;

        private IPlayerAttackService service;
        private IStatsService statsService;
        private IDamageCalculatorService damageCalculator;

        private Transform localPlayerTransform;

        // ✅ Cache current weapon for GetCooldownPercent()
        private WeaponDataSO currentWeaponCache;

        #region Unity Lifecycle

        private void Start()
        {
            StartCoroutine(DelayedInitialize());
        }

        private void Update()
        {
            if (service == null || !service.IsInitialized())
                return;

            service.UpdateTimers(Time.deltaTime);
            CheckAttackInput();
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
            if (statsPresenter == null)
                statsPresenter = FindObjectOfType<StatsPresenter>();

            if (pointerPresenter == null)
                pointerPresenter = FindObjectOfType<PlayerPointerPresenter>();

            if (statsPresenter == null)
            {
                Debug.LogError("[PlayerAttackPresenter] StatsPresenter not found!");
                enabled = false;
                return;
            }

            if (pointerPresenter == null)
            {
                Debug.LogError("[PlayerAttackPresenter] PlayerPointerPresenter not found!");
                enabled = false;
                return;
            }

            statsService = statsPresenter.GetService();
            damageCalculator = new DamageCalculatorService();

            GameObject playerObj = FindLocalPlayerEntity();
            if (playerObj == null)
            {
                Debug.LogError("[PlayerAttackPresenter] Local player not found!");
                enabled = false;
                return;
            }

            localPlayerTransform = playerObj.transform;

            Transform centerPoint = playerObj.transform.Find("CenterPoint");
            if (centerPoint == null)
                centerPoint = playerObj.transform;

            model.stabPositionOffset = stabPositionOffset;
            model.horizontalPositionOffset = horizontalPositionOffset;
            model.verticalPositionOffset = verticalPositionOffset;
            model.vfxSpawnOffset = vfxSpawnOffset;

            service = new PlayerAttackService(model);
            service.Initialize(
                playerObj.transform,
                centerPoint,
                stabVFXPrefab,
                horizontalVFXPrefab,
                verticalVFXPrefab,
                damagePopupPrefab,
                enemyLayers
            );

            Debug.Log("[PlayerAttackPresenter] Initialized successfully");
        }

        private GameObject FindLocalPlayerEntity()
        {
            foreach (GameObject go in GameObject.FindGameObjectsWithTag("Player"))
            {
                PhotonView pv = go.GetComponent<PhotonView>();
                if (pv != null && pv.IsMine) return go;
            }

            foreach (GameObject go in GameObject.FindGameObjectsWithTag("PlayerEntity"))
            {
                PhotonView pv = go.GetComponent<PhotonView>();
                if (pv != null && pv.IsMine) return go;
            }

            GameObject fallback = GameObject.Find("PlayerEntity");
            if (fallback != null)
            {
                Debug.LogWarning("[PlayerAttackPresenter] Found PlayerEntity by name");
                return fallback;
            }

            return null;
        }

        #endregion

        #region Input Handling

        private void CheckAttackInput()
        {
            if (CombatModePresenter.Instance == null ||
                !CombatModePresenter.Instance.IsCombatModeActive())
                return;

            if (WeaponEquipPresenter.Instance == null ||
                !WeaponEquipPresenter.Instance.IsWeaponEquipped())
                return;

            if (Input.GetMouseButtonDown(0) && service.CanAttack())
                ExecuteAttack();
        }

        #endregion

        #region Attack Execution

        private void ExecuteAttack()
        {
            if (service == null || !service.IsInitialized()) return;

            var currentWeapon = WeaponEquipPresenter.Instance?.GetCurrentWeapon();
            if (currentWeapon == null)
            {
                Debug.LogWarning("[PlayerAttackPresenter] No weapon equipped - attack blocked!");
                return;
            }

            // ✅ Cache for GetCooldownPercent()
            currentWeaponCache = currentWeapon;

            int strength    = statsService.GetAttackDamage();
            int weaponDamage = currentWeapon.damage;
            int baseDamage  = damageCalculator.CalculateBasicAttackDamage(strength, weaponDamage);

            switch (currentWeapon.weaponType)
            {
                case WeaponType.Staff:
                    ExecuteStaffAttack(baseDamage, currentWeapon);
                    break;

                case WeaponType.Sword:
                case WeaponType.Spear:
                default:
                    ExecuteMeleeAttack(baseDamage, currentWeapon);
                    break;
            }

            if (WeaponAnimationPresenter.Instance != null &&
                WeaponAnimationPresenter.Instance.IsWeaponActive())
                WeaponAnimationPresenter.Instance.PlayAttackAnimation();

            service.ExecuteAttack();

            // ✅ Cooldown now from weapon, not statsService
            service.SetAttackCooldown(currentWeapon.attackCooldown);
        }

        #endregion

        #region Melee Attack

        private void ExecuteMeleeAttack(int baseDamage, WeaponDataSO currentWeapon)
        {
            int comboStep = service.GetCurrentComboStep();
            GameObject vfxPrefab = service.GetVFXPrefab(comboStep);
            float vfxDuration = service.GetVFXDuration(comboStep);

            if (vfxPrefab == null)
            {
                Debug.LogWarning($"[PlayerAttackPresenter] VFX prefab missing for combo step {comboStep}");
                return;
            }

            int finalDamage = service.CalculateDamage(comboStep, baseDamage);

            // ✅ Knockback now from weapon, not statsService
            float knockback = currentWeapon.knockbackForce;

            SpawnSlashVFX(vfxPrefab, vfxDuration, finalDamage, knockback, comboStep);

            Debug.Log($"[PlayerAttackPresenter] Melee | Step={comboStep} | " +
                      $"Str={statsService.GetAttackDamage()} + WeaponDmg={currentWeapon.damage} " +
                      $"= Base={baseDamage} → Final={finalDamage} | " +
                      $"Knockback={knockback} | Weapon={currentWeapon.weaponName}");
        }

        private void SpawnSlashVFX(GameObject vfxPrefab, float duration,
                                    int damage, float knockback, int comboStep)
        {
            Transform centerPoint = service.GetCenterPoint();
            Vector3 pointerDirection = pointerPresenter.GetPointerDirection();

            float spawnOffset = service.GetVFXSpawnOffset();
            Vector3 spawnPosition = centerPoint.position + pointerDirection * spawnOffset;

            Vector2 positionOffset = service.GetPositionOffset(comboStep);
            spawnPosition += (Vector3)positionOffset;
            spawnPosition.z = centerPoint.position.z;

            float angle = Mathf.Atan2(pointerDirection.y, pointerDirection.x) * Mathf.Rad2Deg;
            Quaternion rotation = Quaternion.Euler(0f, 0f, angle);

            GameObject vfxInstance = Instantiate(vfxPrefab, spawnPosition, rotation);

            if (pointerDirection.x < 0)
            {
                Vector3 scale = vfxInstance.transform.localScale;
                scale.y *= -1;
                vfxInstance.transform.localScale = scale;
            }

            SlashHitboxPresenter hitboxPresenter = vfxInstance.GetComponent<SlashHitboxPresenter>();
            if (hitboxPresenter == null)
                hitboxPresenter = vfxInstance.AddComponent<SlashHitboxPresenter>();

            hitboxPresenter.Initialize(
                damage,
                knockback,
                service.GetEnemyLayers(),
                service.GetPlayerTransform(),
                service.GetDamagePopupPrefab(),
                duration
            );

            Debug.Log($"[PlayerAttackPresenter] Melee VFX spawned at {spawnPosition}, angle={angle}°");
        }

        #endregion

        #region Staff Projectile Attack

        private void ExecuteStaffAttack(int baseDamage, WeaponDataSO currentWeapon)
        {
            if (staffProjectilePrefab == null)
            {
                Debug.LogWarning("[PlayerAttackPresenter] Staff projectile prefab not assigned!");
                return;
            }

            if (localPlayerTransform == null)
            {
                Debug.LogWarning("[PlayerAttackPresenter] Local player transform missing!");
                return;
            }

            // TODO: Staff combo system - currently single shot per click
            // Future: add staffComboSteps[] for multi-projectile patterns

            Vector3 direction = pointerPresenter.GetPointerDirection().normalized;

            GameObject projectileGO = Instantiate(
                staffProjectilePrefab,
                localPlayerTransform.position,
                Quaternion.identity
            );

            // ✅ All projectile stats now read from WeaponDataSO
            ProjectileModel projectileModel = new ProjectileModel
            {
                direction       = direction,
                speed           = currentWeapon.projectileSpeed,
                maxRange        = currentWeapon.projectileRange,
                damage          = baseDamage,
                knockbackForce  = currentWeapon.projectileKnockback,
                enemyLayers     = enemyLayers,
                playerTransform = localPlayerTransform
            };

            ProjectilePresenter projectilePresenter =
                projectileGO.GetComponent<ProjectilePresenter>();

            if (projectilePresenter == null)
            {
                Debug.LogWarning("[PlayerAttackPresenter] ProjectilePresenter " +
                                 "missing on NA_Staff prefab!");
                Destroy(projectileGO);
                return;
            }

            projectilePresenter.Initialize(projectileModel);

            Debug.Log($"[PlayerAttackPresenter] Staff fired! " +
                      $"Damage={baseDamage} | Dir={direction} | " +
                      $"Speed={currentWeapon.projectileSpeed} | " +
                      $"Range={currentWeapon.projectileRange} | " +
                      $"Knockback={currentWeapon.projectileKnockback} | " +
                      $"Weapon={currentWeapon.weaponName}");
        }

        #endregion

        #region Getters

        public bool IsInitialized() => service?.IsInitialized() ?? false;
        public bool CanAttack() => service?.CanAttack() ?? false;
        public int GetCurrentComboStep() => service?.GetCurrentComboStep() ?? 0;

        public float GetCooldownPercent()
        {
            if (service == null) return 0f;

            // ✅ Use weapon cooldown as reference, fallback to statsService
            float cooldownRef = currentWeaponCache != null
                ? currentWeaponCache.attackCooldown
                : statsService?.GetCooldownTime() ?? 1f;

            return service.GetCooldownPercent(cooldownRef);
        }

        public IPlayerAttackService GetService() => service;

        #endregion
    }
}