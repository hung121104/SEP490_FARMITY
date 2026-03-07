using UnityEngine;
using Photon.Pun;
using System.Collections;
using CombatManager.Model;
using CombatManager.Service;
using CombatManager.View;

namespace CombatManager.Presenter
{
    public class PlayerAttackPresenter : MonoBehaviour
    {
        [Header("Model")]
        [SerializeField] private PlayerAttackModel model = new PlayerAttackModel();

        [Header("VFX Prefabs")]
        [SerializeField] private GameObject stabVFXPrefab;
        [SerializeField] private GameObject horizontalVFXPrefab;
        [SerializeField] private GameObject verticalVFXPrefab;
        [SerializeField] private GameObject damagePopupPrefab;

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

        // ✅ NEW: DamageCalculatorService instance
        private IDamageCalculatorService damageCalculator;

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

            // ✅ NEW: Initialize damage calculator
            damageCalculator = new DamageCalculatorService();

            GameObject playerObj = FindLocalPlayerEntity();
            if (playerObj == null)
            {
                Debug.LogError("[PlayerAttackPresenter] Local player not found!");
                enabled = false;
                return;
            }

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
                if (pv != null && pv.IsMine)
                    return go;
            }

            foreach (GameObject go in GameObject.FindGameObjectsWithTag("PlayerEntity"))
            {
                PhotonView pv = go.GetComponent<PhotonView>();
                if (pv != null && pv.IsMine)
                    return go;
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
            if (CombatModePresenter.Instance == null || !CombatModePresenter.Instance.IsCombatModeActive())
                return;

            // ✅ NEW: Block attack if no weapon equipped
            if (WeaponEquipPresenter.Instance == null || !WeaponEquipPresenter.Instance.IsWeaponEquipped())
            {
                return;
            }

            if (Input.GetMouseButtonDown(0) && service.CanAttack())
            {
                ExecuteAttack();
            }
        }

        #endregion

        #region Attack Execution

        private void ExecuteAttack()
        {
            if (service == null || !service.IsInitialized())
                return;

            // ✅ NEW: Get weapon - required for attack
            var currentWeapon = WeaponEquipPresenter.Instance?.GetCurrentWeapon();
            if (currentWeapon == null)
            {
                Debug.LogWarning("[PlayerAttackPresenter] No weapon equipped - attack blocked!");
                return;
            }

            int comboStep = service.GetCurrentComboStep();
            GameObject vfxPrefab = service.GetVFXPrefab(comboStep);
            float vfxDuration = service.GetVFXDuration(comboStep);

            if (vfxPrefab == null)
            {
                Debug.LogWarning($"[PlayerAttackPresenter] VFX prefab missing for combo step {comboStep}");
                return;
            }

            // ✅ NEW: Use DamageCalculatorService with weapon damage
            // strength + weaponDamage = base, then apply combo multiplier
            int strength = statsService.GetAttackDamage();
            int weaponDamage = currentWeapon.damage;
            int baseDamage = damageCalculator.CalculateBasicAttackDamage(strength, weaponDamage);

            // Apply combo multiplier on top
            int finalDamage = service.CalculateDamage(comboStep, baseDamage);

            float knockbackForce = statsService.GetKnockbackForce();

            SpawnSlashVFX(vfxPrefab, vfxDuration, finalDamage, knockbackForce, comboStep);

            if (WeaponAnimationPresenter.Instance != null && WeaponAnimationPresenter.Instance.IsWeaponActive())
            {
                WeaponAnimationPresenter.Instance.PlayAttackAnimation();
            }

            service.ExecuteAttack();

            float cooldown = statsService.GetCooldownTime();
            service.SetAttackCooldown(cooldown);

            Debug.Log($"[PlayerAttackPresenter] Attack: Step={comboStep}, " +
                      $"Strength={strength} + WeaponDmg={weaponDamage} " +
                      $"= Base={baseDamage} × ComboMult → Final={finalDamage} | " +
                      $"Weapon={currentWeapon.weaponName}");
        }

        #endregion

        #region VFX Spawning

        private void SpawnSlashVFX(GameObject vfxPrefab, float duration, int damage, float knockback, int comboStep)
        {
            Transform centerPoint = service.GetCenterPoint();
            Vector3 pointerDirection = pointerPresenter.GetPointerDirection();

            // Calculate spawn position with base offset
            float spawnOffset = service.GetVFXSpawnOffset();
            Vector3 spawnPosition = centerPoint.position + pointerDirection * spawnOffset;

            // Apply per-VFX position offset (inspector adjustable)
            Vector2 positionOffset = service.GetPositionOffset(comboStep);
            spawnPosition += (Vector3)positionOffset;

            spawnPosition.z = centerPoint.position.z;

            // Calculate rotation
            float angle = Mathf.Atan2(pointerDirection.y, pointerDirection.x) * Mathf.Rad2Deg;
            Quaternion rotation = Quaternion.Euler(0f, 0f, angle);

            // Spawn VFX
            GameObject vfxInstance = Instantiate(vfxPrefab, spawnPosition, rotation);

            // Fix upside-down flip when direction is to the left
            if (pointerDirection.x < 0)
            {
                Vector3 scale = vfxInstance.transform.localScale;
                scale.y *= -1; // Flip vertically
                vfxInstance.transform.localScale = scale;
            }

            // Initialize hitbox
            SlashHitboxPresenter hitboxPresenter = vfxInstance.GetComponent<SlashHitboxPresenter>();
            if (hitboxPresenter == null)
            {
                hitboxPresenter = vfxInstance.AddComponent<SlashHitboxPresenter>();
            }

            hitboxPresenter.Initialize(
                damage,
                knockback,
                service.GetEnemyLayers(),
                service.GetPlayerTransform(),
                service.GetDamagePopupPrefab(),
                duration
            );

            Debug.Log($"[PlayerAttackPresenter] Spawned VFX at {spawnPosition}, angle={angle}°, offset={positionOffset}");
        }

        #endregion

        #region Getters for View

        public bool IsInitialized() => service?.IsInitialized() ?? false;
        public bool CanAttack() => service?.CanAttack() ?? false;
        public int GetCurrentComboStep() => service?.GetCurrentComboStep() ?? 0;
        public float GetCooldownPercent() => service?.GetCooldownPercent(statsService?.GetCooldownTime() ?? 1f) ?? 0f;

        #endregion

        #region Public API for Other Systems

        public IPlayerAttackService GetService() => service;

        #endregion
    }
}