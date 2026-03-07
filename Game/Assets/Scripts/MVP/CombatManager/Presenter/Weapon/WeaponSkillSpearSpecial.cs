using UnityEngine;
using System.Collections;
using CombatManager.Model;
using CombatManager.Service;
using CombatManager.SO;

namespace CombatManager.Presenter
{
    /// <summary>
    /// Weapon skill for Spear type.
    /// Melee VFX + hitbox, same pattern as WeaponSkillSwordSlash.
    /// Only usable when Spear type weapon is equipped.
    /// Triggered by R key via SkillHotbarPresenter.
    /// </summary>
    public class WeaponSkillSpearSpecial : MonoBehaviour
    {
        public static WeaponSkillSpearSpecial Instance { get; private set; }

        [Header("Skill Settings")]
        [SerializeField] private float cooldownDuration = 4f;
        [SerializeField] private float chargeDuration = 0.3f;
        [SerializeField] private float skillMultiplier = 2.5f;
        [SerializeField] private DiceTier diceTier = DiceTier.D6;

        [Header("Confirmation Keys")]
        [SerializeField] private KeyCode confirmKey = KeyCode.E;
        [SerializeField] private KeyCode cancelKey = KeyCode.Q;

        [Header("VFX")]
        [SerializeField] private GameObject spearSpecialVFXPrefab;
        [SerializeField] private float vfxDuration = 0.5f;
        [SerializeField] private float vfxSpawnOffset = 1.5f;
        [SerializeField] private Vector2 vfxPositionOffset = Vector2.zero;

        [Header("Hitbox Settings")]
        [SerializeField] private LayerMask enemyLayers;
        [SerializeField] private float knockbackForce = 7f;
        [SerializeField] private GameObject damagePopupPrefab;

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
                    Debug.Log("[WeaponSkillSpearSpecial] Skill CONFIRMED!");
                }
                else if (Input.GetKeyDown(cancelKey))
                {
                    cancelReceived = true;
                    Debug.Log("[WeaponSkillSpearSpecial] Skill CANCELLED!");
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
                Debug.LogError("[WeaponSkillSpearSpecial] Could not find local player!");
            else
                Debug.Log("[WeaponSkillSpearSpecial] Initialized successfully");
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

            // Guard: only spear type
            if (currentWeapon == null || currentWeapon.weaponType != WeaponType.Spear)
            {
                Debug.LogWarning("[WeaponSkillSpearSpecial] Spear not equipped!");
                return;
            }

            if (isExecuting)
            {
                Debug.Log("[WeaponSkillSpearSpecial] Already executing!");
                return;
            }

            if (cooldownRemaining > 0f)
            {
                Debug.Log($"[WeaponSkillSpearSpecial] On cooldown: {cooldownRemaining:F1}s remaining");
                return;
            }

            if (localPlayerObj == null)
            {
                Debug.LogError("[WeaponSkillSpearSpecial] Local player not found!");
                return;
            }

            StartCoroutine(ExecuteSkillSequence(currentWeapon));
        }

        private IEnumerator ExecuteSkillSequence(WeaponDataSO weaponData)
        {
            isExecuting = true;
            Debug.Log("[WeaponSkillSpearSpecial] Executing Spear Special - Charging...");

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
                Debug.Log("[WeaponSkillSpearSpecial] Skill aborted!");
                yield break;
            }

            // Phase 5: Confirmed → calculate + execute
            DiceDisplayPresenter.Hide();

            int strength = statsPresenter?.GetService().GetTempStrength() ?? 0;
            int weaponDamage = weaponData.damage;

            int finalDamage = damageCalculator.CalculateSkillDamage(
                diceRoll,
                strength,
                skillMultiplier,
                weaponDamage
            );

            Debug.Log($"[WeaponSkillSpearSpecial] Damage: Roll={diceRoll}, " +
                      $"Str={strength}, WeaponDmg={weaponDamage}, " +
                      $"Mult={skillMultiplier} → Final={finalDamage}");

            SpawnSpearVFX(finalDamage);

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

            Debug.Log($"[WeaponSkillSpearSpecial] Waiting for confirm [{confirmKey}] " +
                      $"or cancel [{cancelKey}]...");

            while (!confirmReceived && !cancelReceived)
                yield return null;

            isWaitingConfirm = false;
        }

        private void SpawnSpearVFX(int damage)
        {
            if (spearSpecialVFXPrefab == null)
            {
                Debug.LogWarning("[WeaponSkillSpearSpecial] VFX prefab not assigned!");
                return;
            }

            if (localPlayerObj == null) return;

            Vector3 direction = Vector3.right;
            if (pointerPresenter != null)
                direction = pointerPresenter.GetCurrentDirection();

            Transform centerPoint = localPlayerObj.transform.Find("CenterPoint")
                                    ?? localPlayerObj.transform;

            Vector3 spawnPos = centerPoint.position
                             + direction * vfxSpawnOffset
                             + (Vector3)vfxPositionOffset;

            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

            GameObject vfxObj = Instantiate(
                spearSpecialVFXPrefab,
                spawnPos,
                Quaternion.Euler(0f, 0f, angle)
            );

            SlashHitboxPresenter hitbox = vfxObj.GetComponent<SlashHitboxPresenter>();
            if (hitbox != null)
            {
                hitbox.Initialize(
                    damage,
                    knockbackForce,
                    enemyLayers,
                    localPlayerObj.transform,
                    damagePopupPrefab,
                    vfxDuration
                );
                Debug.Log($"[WeaponSkillSpearSpecial] VFX spawned with damage={damage}");
            }
            else
            {
                Debug.LogWarning("[WeaponSkillSpearSpecial] SlashHitboxPresenter missing on VFX prefab!");
            }

            Destroy(vfxObj, vfxDuration);
        }

        #endregion

        #region Public API

        public bool IsOnCooldown() => cooldownRemaining > 0f;
        public float GetCooldownPercent() => Mathf.Clamp01(cooldownRemaining / cooldownDuration);
        public bool IsExecuting() => isExecuting;

        #endregion
    }
}