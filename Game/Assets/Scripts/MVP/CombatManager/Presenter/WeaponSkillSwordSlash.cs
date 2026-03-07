using UnityEngine;
using System.Collections;
using CombatManager.Model;
using CombatManager.Service;
using CombatManager.SO;

namespace CombatManager.Presenter
{
    public class WeaponSkillSwordSlash : MonoBehaviour
    {
        public static WeaponSkillSwordSlash Instance { get; private set; }

        [Header("Skill Settings")]
        [SerializeField] private float cooldownDuration = 3f;
        [SerializeField] private float chargeDuration = 0.3f;
        [SerializeField] private float skillMultiplier = 2.0f;

        [Header("VFX")]
        [SerializeField] private GameObject swordSlashVFXPrefab;
        [SerializeField] private float vfxDuration = 0.5f;
        [SerializeField] private float vfxSpawnOffset = 1.2f;
        [SerializeField] private Vector2 vfxPositionOffset = Vector2.zero;

        [Header("Hitbox Settings")]
        [SerializeField] private LayerMask enemyLayers;
        [SerializeField] private float knockbackForce = 5f;
        [SerializeField] private GameObject damagePopupPrefab;

        [Header("Dependencies")]
        [SerializeField] private StatsPresenter statsPresenter;
        [SerializeField] private PlayerPointerPresenter pointerPresenter;

        private IDamageCalculatorService damageCalculator;
        private bool isExecuting = false;
        private float cooldownRemaining = 0f;
        private GameObject localPlayerObj;

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
                Debug.LogError("[WeaponSkillSwordSlash] Could not find local player!");
            else
                Debug.Log("[WeaponSkillSwordSlash] Initialized successfully");
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
            if (currentWeapon == null || currentWeapon.weaponType != WeaponType.Sword)
            {
                Debug.LogWarning("[WeaponSkillSwordSlash] Sword not equipped!");
                return;
            }

            if (isExecuting)
            {
                Debug.Log("[WeaponSkillSwordSlash] Already executing!");
                return;
            }

            if (cooldownRemaining > 0f)
            {
                Debug.Log($"[WeaponSkillSwordSlash] On cooldown: {cooldownRemaining:F1}s remaining");
                return;
            }

            if (localPlayerObj == null)
            {
                Debug.LogError("[WeaponSkillSwordSlash] Local player not found!");
                return;
            }

            StartCoroutine(ExecuteSkillSequence(currentWeapon));
        }

        private IEnumerator ExecuteSkillSequence(WeaponDataSO weaponData)
        {
            isExecuting = true;
            Debug.Log("[WeaponSkillSwordSlash] Executing SwordSlash!");

            yield return new WaitForSeconds(chargeDuration);

            // ✅ Fix 1: Use DiceTier not SkillTier
            var diceRollerService = new DiceRollerService();
            int diceRoll = diceRollerService.Roll(DiceTier.D6);

            DiceDisplayPresenter.Show(diceRoll, DiceTier.D6);

            yield return new WaitForSeconds(0.8f);

            int strength = statsPresenter?.GetService().GetTempStrength() ?? 0;
            int weaponDamage = weaponData.damage;

            int finalDamage = damageCalculator.CalculateSkillDamage(
                diceRoll,
                strength,
                skillMultiplier,
                weaponDamage
            );

            Debug.Log($"[WeaponSkillSwordSlash] Damage: Roll={diceRoll}, " +
                      $"Str={strength}, WeaponDmg={weaponDamage}, " +
                      $"Mult={skillMultiplier} → Final={finalDamage}");

            SpawnSlashVFX(finalDamage);

            cooldownRemaining = cooldownDuration;
            isExecuting = false;
        }

        private void SpawnSlashVFX(int damage)
        {
            if (swordSlashVFXPrefab == null)
            {
                Debug.LogWarning("[WeaponSkillSwordSlash] VFX prefab not assigned!");
                return;
            }

            if (localPlayerObj == null) return;

            // ✅ Fix 2: Use GetCurrentDirection() not GetAimDirection()
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
                swordSlashVFXPrefab,
                spawnPos,
                Quaternion.Euler(0f, 0f, angle)
            );

            // ✅ Fix 3: Pass all required params to Initialize()
            SlashHitboxPresenter hitbox = vfxObj.GetComponent<SlashHitboxPresenter>();
            if (hitbox != null)
            {
                hitbox.Initialize(
                    damage,
                    knockbackForce,
                    enemyLayers,
                    localPlayerObj.transform,   // ownerTransform
                    damagePopupPrefab,          // damagePopupPrefab
                    vfxDuration                 // animationDuration
                );
                Debug.Log($"[WeaponSkillSwordSlash] VFX spawned with damage={damage}");
            }
            else
            {
                Debug.LogWarning("[WeaponSkillSwordSlash] SlashHitboxPresenter missing on VFX prefab!");
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