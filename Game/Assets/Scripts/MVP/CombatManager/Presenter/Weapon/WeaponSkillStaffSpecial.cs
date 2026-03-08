using UnityEngine;
using System.Collections;
using CombatManager.Model;
using CombatManager.Service;
using CombatManager.SO;

namespace CombatManager.Presenter
{
    /// <summary>
    /// Weapon skill for Staff type.
    /// Projectile based - reuses AirSlashProjectilePresenter.
    /// Same confirm flow as SwordSpecial and SpearSpecial.
    /// Only usable when Staff type weapon is equipped.
    /// Triggered by R key via SkillHotbarPresenter.
    /// </summary>
    public class WeaponSkillStaffSpecial : MonoBehaviour
    {
        public static WeaponSkillStaffSpecial Instance { get; private set; }

        [Header("Skill Settings")]
        [SerializeField] private float cooldownDuration = 5f;
        [SerializeField] private float chargeDuration = 0.3f;
        [SerializeField] private float skillMultiplier = 2.0f;
        [SerializeField] private DiceTier diceTier = DiceTier.D6;

        [Header("Confirmation Keys")]
        [SerializeField] private KeyCode confirmKey = KeyCode.E;
        [SerializeField] private KeyCode cancelKey = KeyCode.Q;

        [Header("Projectile Settings")]
        [SerializeField] private GameObject projectilePrefab;
        [SerializeField] private float projectileSpeed = 10f;
        [SerializeField] private float projectileRange = 8f;
        [SerializeField] private float knockbackForce = 5f;

        [Header("Hitbox Settings")]
        [SerializeField] private LayerMask enemyLayers;

        [Header("Dependencies")]
        [SerializeField] private StatsPresenter statsPresenter;
        [SerializeField] private PlayerPointerPresenter pointerPresenter;

        private IDamageCalculatorService damageCalculator;
        private bool isExecuting = false;
        private float cooldownRemaining = 0f;
        private GameObject localPlayerObj;

        // Confirmation state
        private bool isWaitingConfirm = false;
        private bool confirmReceived = false;
        private bool cancelReceived = false;

        #region Unity Lifecycle

        private void Awake()
        {
            if (Instance == null)
                Instance = this;
            else
            {
                Destroy(gameObject);
                return;
            }
        }

        private void Start()
        {
            damageCalculator = new DamageCalculatorService();

            if (statsPresenter == null)
                statsPresenter = FindObjectOfType<StatsPresenter>();

            if (pointerPresenter == null)
                pointerPresenter = FindObjectOfType<PlayerPointerPresenter>();

            StartCoroutine(FindLocalPlayer());
        }

        private void Update()
        {
            if (cooldownRemaining > 0f)
                cooldownRemaining -= Time.deltaTime;

            if (isWaitingConfirm)
            {
                if (Input.GetKeyDown(confirmKey))
                {
                    confirmReceived = true;
                    Debug.Log("[WeaponSkillStaffSpecial] Skill CONFIRMED!");
                }
                else if (Input.GetKeyDown(cancelKey))
                {
                    cancelReceived = true;
                    Debug.Log("[WeaponSkillStaffSpecial] Skill CANCELLED!");
                }
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        #endregion

        #region Initialization

        private IEnumerator FindLocalPlayer()
        {
            float timeout = 5f;
            float elapsed = 0f;

            while (localPlayerObj == null && elapsed < timeout)
            {
                localPlayerObj = FindLocalPlayerEntity();
                elapsed += 0.1f;
                yield return new WaitForSeconds(0.1f);
            }

            if (localPlayerObj == null)
                Debug.LogError("[WeaponSkillStaffSpecial] Could not find local player!");
            else
                Debug.Log("[WeaponSkillStaffSpecial] Initialized successfully");
        }

        private GameObject FindLocalPlayerEntity()
        {
            foreach (GameObject go in GameObject.FindGameObjectsWithTag("Player"))
            {
                var pv = go.GetComponent<Photon.Pun.PhotonView>();
                if (pv != null && pv.IsMine) return go;
            }
            foreach (GameObject go in GameObject.FindGameObjectsWithTag("PlayerEntity"))
            {
                var pv = go.GetComponent<Photon.Pun.PhotonView>();
                if (pv != null && pv.IsMine) return go;
            }
            return GameObject.Find("PlayerEntity");
        }

        #endregion

        #region Skill Execution

        public void TryExecute()
        {
            var currentWeapon = WeaponEquipPresenter.Instance?.GetCurrentWeapon();

            // Guard: only staff type
            if (currentWeapon == null || currentWeapon.weaponType != WeaponType.Staff)
            {
                Debug.LogWarning("[WeaponSkillStaffSpecial] Staff not equipped!");
                return;
            }

            if (isExecuting)
            {
                Debug.Log("[WeaponSkillStaffSpecial] Already executing!");
                return;
            }

            if (cooldownRemaining > 0f)
            {
                Debug.Log($"[WeaponSkillStaffSpecial] On cooldown: {cooldownRemaining:F1}s remaining");
                return;
            }

            if (localPlayerObj == null)
            {
                Debug.LogError("[WeaponSkillStaffSpecial] Local player not found!");
                return;
            }

            StartCoroutine(ExecuteSkillSequence(currentWeapon));
        }

        private IEnumerator ExecuteSkillSequence(WeaponDataSO weaponData)
        {
            isExecuting = true;
            Debug.Log("[WeaponSkillStaffSpecial] Executing Staff Special - Charging...");

            // Phase 1: Charge
            yield return new WaitForSeconds(chargeDuration);

            // Phase 2: Roll dice
            var diceRollerService = new DiceRollerService();
            int diceRoll = diceRollerService.Roll(diceTier);
            DiceDisplayPresenter.Show(diceRoll, diceTier);

            // Phase 3: Wait for confirmation
            yield return StartCoroutine(WaitForConfirmation());

            // Phase 4: Cancelled → abort
            if (cancelReceived)
            {
                DiceDisplayPresenter.Hide();
                isExecuting = false;
                isWaitingConfirm = false;
                confirmReceived = false;
                cancelReceived = false;
                Debug.Log("[WeaponSkillStaffSpecial] Skill aborted!");
                yield break;
            }

            // Phase 5: Confirmed → calculate + fire
            DiceDisplayPresenter.Hide();

            int strength = statsPresenter?.GetService().GetTempStrength() ?? 0;
            int weaponDamage = weaponData.damage;

            int finalDamage = damageCalculator.CalculateSkillDamage(
                diceRoll,
                strength,
                skillMultiplier,
                weaponDamage
            );

            Debug.Log($"[WeaponSkillStaffSpecial] Damage: Roll={diceRoll}, " +
                      $"Str={strength}, WeaponDmg={weaponDamage}, " +
                      $"Mult={skillMultiplier} → Final={finalDamage}");

            FireProjectile(finalDamage);

            // Phase 6: Cooldown + reset
            cooldownRemaining = cooldownDuration;
            isExecuting = false;
            confirmReceived = false;
            cancelReceived = false;
        }

        private IEnumerator WaitForConfirmation()
        {
            isWaitingConfirm = true;
            confirmReceived = false;
            cancelReceived = false;

            Debug.Log($"[WeaponSkillStaffSpecial] Waiting for confirm [{confirmKey}] " +
                      $"or cancel [{cancelKey}]...");

            while (!confirmReceived && !cancelReceived)
                yield return null;

            isWaitingConfirm = false;
        }

        private void FireProjectile(int damage)
        {
            if (projectilePrefab == null)
            {
                Debug.LogWarning("[WeaponSkillStaffSpecial] Projectile prefab not assigned!");
                return;
            }

            if (localPlayerObj == null) return;

            Vector3 direction = Vector3.right;
            if (pointerPresenter != null)
                direction = pointerPresenter.GetCurrentDirection();

            GameObject projectileGO = Instantiate(
                projectilePrefab,
                localPlayerObj.transform.position,
                Quaternion.identity
            );

            // ✅ Renamed: AirSlashProjectileModel → ProjectileModel
            ProjectileModel projectileModel = new ProjectileModel
            {
                direction      = direction.normalized,
                speed          = projectileSpeed,
                maxRange       = projectileRange,
                damage         = damage,
                knockbackForce = knockbackForce,
                enemyLayers    = enemyLayers,
                playerTransform = localPlayerObj.transform
            };

            // ✅ Renamed: AirSlashProjectilePresenter → ProjectilePresenter
            ProjectilePresenter projectilePresenter =
                projectileGO.GetComponent<ProjectilePresenter>();

            if (projectilePresenter == null)
            {
                Debug.LogWarning("[WeaponSkillStaffSpecial] ProjectilePresenter " +
                                 "missing on projectile prefab!");
                Destroy(projectileGO);
                return;
            }

            projectilePresenter.Initialize(projectileModel);

            Debug.Log($"[WeaponSkillStaffSpecial] Projectile fired! " +
                      $"Damage={damage} | Dir={direction} | Range={projectileRange}");
        }

        #endregion

        #region Public API

        public bool IsOnCooldown() => cooldownRemaining > 0f;
        public float GetCooldownPercent() => Mathf.Clamp01(cooldownRemaining / cooldownDuration);
        public bool IsExecuting() => isExecuting;

        #endregion
    }
}