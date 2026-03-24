using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;
using System.Collections;
using CombatManager.Model;
using CombatManager.Service;
using CombatManager.View;
using UnityEngine.EventSystems;

namespace CombatManager.Presenter
{
    public class PlayerAttackPresenter : MonoBehaviour, IOnEventCallback
    {
        private const byte ATTACK_VFX_EVENT = 161;
        private const byte ATTACK_KIND_MELEE = 1;
        private const byte ATTACK_KIND_STAFF = 2;
        private const string KEY_WEAPON = "apWeapon";

        public static event System.Action<int, float> OnRemoteAttackVisual;

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
        private WeaponData currentWeaponCache;
                    // Projectile stats are fully item-driven from WeaponData
        #region Unity Lifecycle

        private void Start()
        {
            StartCoroutine(DelayedInitialize());
        }

        private void OnEnable()
        {
            PhotonNetwork.AddCallbackTarget(this);
        }

        private void OnDisable()
        {
            PhotonNetwork.RemoveCallbackTarget(this);
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
            const float timeout = 8f;
            float elapsed = 0f;

            yield return new WaitForSeconds(0.2f);

            while (elapsed < timeout)
            {
                if (InitializeComponents())
                    yield break;

                elapsed += 0.25f;
                yield return new WaitForSeconds(0.25f);
            }

            Debug.LogError("[PlayerAttackPresenter] Initialization timeout: local player not found.");
            enabled = false;
        }

        private bool InitializeComponents()
        {
            if (statsPresenter == null)
                statsPresenter = FindObjectOfType<StatsPresenter>();

            if (pointerPresenter == null)
                pointerPresenter = FindObjectOfType<PlayerPointerPresenter>();

            if (statsPresenter == null)
            {
                Debug.LogError("[PlayerAttackPresenter] StatsPresenter not found!");
                return false;
            }

            if (pointerPresenter == null)
            {
                Debug.LogError("[PlayerAttackPresenter] PlayerPointerPresenter not found!");
                return false;
            }

            statsService = statsPresenter.GetService();
            damageCalculator = new DamageCalculatorService();

            GameObject playerObj = FindLocalPlayerEntity();
            if (playerObj == null)
            {
                return false;
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
            return true;
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

            return null;
        }

        #endregion

        #region Input Handling

        private void CheckAttackInput()
        {
            if (SkillManagementPresenter.Instance != null && SkillManagementPresenter.Instance.IsPanelOpen())
                return;

            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;

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
            service.SetAttackCooldown(currentWeapon.GetAttackCooldownSafe());
        }

        #endregion

        #region Melee Attack

        private void ExecuteMeleeAttack(int baseDamage, WeaponData currentWeapon)
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
            BroadcastMeleeAttackVfx(comboStep, vfxDuration);

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

        private void ExecuteStaffAttack(int baseDamage, WeaponData currentWeapon)
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

            // Projectile stats are fully item-driven from WeaponData
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
            BroadcastStaffProjectileVfx(currentWeapon, direction);

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
                ? currentWeaponCache.GetAttackCooldownSafe()
                : statsService?.GetCooldownTime() ?? 1f;

            return service.GetCooldownPercent(cooldownRef);
        }

        public IPlayerAttackService GetService() => service;

        public void OnEvent(EventData photonEvent)
        {
            if (photonEvent.Code != ATTACK_VFX_EVENT)
                return;

            if (photonEvent.CustomData is not object[] payload)
                return;

            if (payload.Length == 0)
                return;

            if (!TryGetPayloadByte(payload, 0, out byte attackKind))
                return;

            switch (attackKind)
            {
                case ATTACK_KIND_MELEE:
                    HandleRemoteMeleeAttack(payload);
                    break;
                case ATTACK_KIND_STAFF:
                    HandleRemoteStaffAttack(payload);
                    break;
            }
        }

        private void BroadcastMeleeAttackVfx(int comboStep, float duration)
        {
            if (!PhotonNetwork.IsConnected)
                return;

            int actorNumber = PhotonNetwork.LocalPlayer?.ActorNumber ?? -1;
            if (actorNumber <= 0)
                return;

            Transform centerPoint = service?.GetCenterPoint();
            if (centerPoint == null)
                return;

            Vector3 pointerDirection = pointerPresenter.GetPointerDirection();
            float spawnOffset = service.GetVFXSpawnOffset();
            Vector3 spawnPosition = centerPoint.position + pointerDirection * spawnOffset;

            Vector2 positionOffset = service.GetPositionOffset(comboStep);
            spawnPosition += (Vector3)positionOffset;
            spawnPosition.z = centerPoint.position.z;

            float angle = Mathf.Atan2(pointerDirection.y, pointerDirection.x) * Mathf.Rad2Deg;
            bool flipY = pointerDirection.x < 0f;

            object[] payload =
            {
                ATTACK_KIND_MELEE,
                actorNumber,
                comboStep,
                spawnPosition.x,
                spawnPosition.y,
                spawnPosition.z,
                angle,
                flipY,
                duration
            };

            RaiseEventOptions options = new RaiseEventOptions { Receivers = ReceiverGroup.Others };
            PhotonNetwork.RaiseEvent(ATTACK_VFX_EVENT, payload, options, SendOptions.SendUnreliable);
        }

        private void BroadcastStaffProjectileVfx(WeaponData weapon, Vector3 direction)
        {
            if (!PhotonNetwork.IsConnected)
                return;

            int actorNumber = PhotonNetwork.LocalPlayer?.ActorNumber ?? -1;
            if (actorNumber <= 0)
                return;

            Vector3 spawnPosition = localPlayerTransform != null ? localPlayerTransform.position : Vector3.zero;
            object[] payload =
            {
                ATTACK_KIND_STAFF,
                actorNumber,
                spawnPosition.x,
                spawnPosition.y,
                spawnPosition.z,
                direction.x,
                direction.y,
                direction.z,
                weapon.projectileSpeed,
                weapon.projectileRange
            };

            RaiseEventOptions options = new RaiseEventOptions { Receivers = ReceiverGroup.Others };
            PhotonNetwork.RaiseEvent(ATTACK_VFX_EVENT, payload, options, SendOptions.SendUnreliable);
        }

        private void HandleRemoteMeleeAttack(object[] payload)
        {
            if (payload.Length < 9)
                return;

            if (!TryGetPayloadInt(payload, 1, out int sourceActor) ||
                !TryGetPayloadInt(payload, 2, out int comboStep) ||
                !TryGetPayloadFloat(payload, 3, out float posX) ||
                !TryGetPayloadFloat(payload, 4, out float posY) ||
                !TryGetPayloadFloat(payload, 5, out float posZ) ||
                !TryGetPayloadFloat(payload, 6, out float angle) ||
                !TryGetPayloadBool(payload, 7, out bool flipY) ||
                !TryGetPayloadFloat(payload, 8, out float duration))
            {
                return;
            }

            if (sourceActor == (PhotonNetwork.LocalPlayer?.ActorNumber ?? -1))
                return;

            string weaponItemId = ResolveWeaponItemId(sourceActor);
            if (string.IsNullOrWhiteSpace(weaponItemId))
                return;

            if (!IsStaffWeapon(weaponItemId))
            {
                SpawnRemoteMeleeVfx(comboStep, new Vector3(posX, posY, posZ), angle, flipY, duration);
                OnRemoteAttackVisual?.Invoke(sourceActor, angle);
            }
        }

        private void HandleRemoteStaffAttack(object[] payload)
        {
            if (payload.Length < 10)
                return;

            if (!TryGetPayloadInt(payload, 1, out int sourceActor) ||
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

            string weaponItemId = ResolveWeaponItemId(sourceActor);
            if (string.IsNullOrWhiteSpace(weaponItemId) || !IsStaffWeapon(weaponItemId))
                return;

            SpawnRemoteStaffProjectile(
                new Vector3(posX, posY, posZ),
                new Vector3(dirX, dirY, dirZ).normalized,
                speed,
                range);

            float angle = Mathf.Atan2(dirY, dirX) * Mathf.Rad2Deg;
            OnRemoteAttackVisual?.Invoke(sourceActor, angle);
        }

        private void SpawnRemoteMeleeVfx(int comboStep, Vector3 spawnPosition, float angle, bool flipY, float duration)
        {
            GameObject vfxPrefab = service?.GetVFXPrefab(comboStep);
            if (vfxPrefab == null)
                return;

            Quaternion rotation = Quaternion.Euler(0f, 0f, angle);
            GameObject vfxInstance = Instantiate(vfxPrefab, spawnPosition, rotation);

            if (flipY)
            {
                Vector3 scale = vfxInstance.transform.localScale;
                scale.y *= -1f;
                vfxInstance.transform.localScale = scale;
            }

            Destroy(vfxInstance, Mathf.Max(0.05f, duration));
        }

        private void SpawnRemoteStaffProjectile(Vector3 spawnPosition, Vector3 direction, float speed, float range)
        {
            if (staffProjectilePrefab == null)
                return;

            GameObject projectileGO = Instantiate(staffProjectilePrefab, spawnPosition, Quaternion.identity);
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
        }

        private static bool TryGetPayloadByte(object[] payload, int index, out byte value)
        {
            value = 0;
            if (index < 0 || index >= payload.Length || payload[index] == null)
                return false;

            if (payload[index] is byte b)
            {
                value = b;
                return true;
            }

            if (payload[index] is int i)
            {
                value = (byte)i;
                return true;
            }

            return false;
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

        private static bool TryGetPayloadBool(object[] payload, int index, out bool value)
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

        private static string ResolveWeaponItemId(int actorNumber)
        {
            if (PhotonNetwork.CurrentRoom == null)
                return string.Empty;

            if (!PhotonNetwork.CurrentRoom.Players.TryGetValue(actorNumber, out Player sourcePlayer))
                return string.Empty;

            if (sourcePlayer?.CustomProperties == null)
                return string.Empty;

            return sourcePlayer.CustomProperties.TryGetValue(KEY_WEAPON, out object value)
                ? value as string ?? string.Empty
                : string.Empty;
        }

        private static bool IsStaffWeapon(string itemId)
        {
            WeaponData weapon = ItemCatalogService.Instance?.GetItemData<WeaponData>(itemId);
            return weapon != null && weapon.weaponType == WeaponType.Staff;
        }

        #endregion
    }
}