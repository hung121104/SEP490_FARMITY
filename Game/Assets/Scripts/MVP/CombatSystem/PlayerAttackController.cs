using UnityEngine;

/// <summary>
/// Handles normal attack input, combo chain and slash VFX spawning.
/// Sits on PlayerAttacker GameObject inside CombatSystem.
/// Replaces PlayerCombat attack logic completely.
/// </summary>
public class PlayerAttackController : MonoBehaviour
{
    #region Serialized Fields

    [Header("References")]
    [SerializeField] private GameObject stabVFXPrefab;
    [SerializeField] private GameObject horizontalVFXPrefab;
    [SerializeField] private GameObject verticalVFXPrefab;
    [SerializeField] private GameObject damagePopupPrefab;
    [SerializeField] private LayerMask enemyLayers;

    [Header("Combo Settings")]
    [SerializeField] private float comboResetTime = 2f;

    [Header("Damage Multipliers")]
    [SerializeField] private float stabMultiplier = 0.8f;
    [SerializeField] private float horizontalMultiplier = 1.0f;
    [SerializeField] private float verticalMultiplier = 1.5f;

    [Header("VFX Settings")]
    [SerializeField] private float vfxSpawnOffset = 1f;
    [SerializeField] private float stabDuration = 0.2f;
    [SerializeField] private float horizontalDuration = 0.3f;
    [SerializeField] private float verticalDuration = 0.4f;

    [Header("VFX Position Offset")]
    [SerializeField] private Vector2 stabPositionOffset = Vector2.zero;
    [SerializeField] private Vector2 horizontalPositionOffset = Vector2.zero;
    [SerializeField] private Vector2 verticalPositionOffset = Vector2.zero;

    #endregion

    #region Private Fields

    private Transform playerTransform;
    private Transform centerPoint;
    private Animator playerAnimator;
    private StatsManager statsManager;
    private PlayerPointerController pointerController;
    private SkillBase skillBase;

    private int currentComboStep = 0;
    private float comboResetTimer = 0f;
    private float attackCooldownTimer = 0f;

    private const int TOTAL_COMBO_STEPS = 3;

    #endregion

    #region Unity Lifecycle

    private void Start()
    {
        InitializeComponents();
    }

    private void Update()
    {
        UpdateTimers();
        CheckAttackInput();
    }

    #endregion

    #region Initialization

    private void InitializeComponents()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("PlayerEntity");
        if (playerObj != null)
        {
            playerTransform = playerObj.transform;
            playerAnimator = playerObj.GetComponent<Animator>();

            Transform found = playerTransform.Find("CenterPoint");
            centerPoint = found != null ? found : playerTransform;

            // Register shared references into SkillBase
            skillBase = playerObj.GetComponent<SkillBase>();
            if (skillBase != null)
            {
                skillBase.damagePopupPrefab = damagePopupPrefab;
                skillBase.enemyLayers = enemyLayers;
                skillBase.anim = playerAnimator;
            }
        }
        else
        {
            Debug.LogWarning("PlayerAttackController: PlayerEntity tag not found!");
            return;
        }

        pointerController = FindObjectOfType<PlayerPointerController>();
        if (pointerController == null)
            Debug.LogWarning("PlayerAttackController: PlayerPointerController not found!");

        statsManager = StatsManager.Instance;
        if (statsManager == null)
            statsManager = FindObjectOfType<StatsManager>();
    }

    #endregion

    #region Timers

    private void UpdateTimers()
    {
        if (attackCooldownTimer > 0)
            attackCooldownTimer -= Time.deltaTime;

        if (comboResetTimer > 0)
        {
            comboResetTimer -= Time.deltaTime;
            if (comboResetTimer <= 0)
                ResetCombo();
        }
    }

    #endregion

    #region Attack Input

    private void CheckAttackInput()
    {
        if (Input.GetMouseButtonDown(0) && CanAttack())
            ExecuteAttack();
    }

    private bool CanAttack()
    {
        return attackCooldownTimer <= 0
            && statsManager != null
            && pointerController != null;
    }

    #endregion

    #region Attack Execution

    private void ExecuteAttack()
    {
        attackCooldownTimer = statsManager.cooldownTime;
        comboResetTimer = comboResetTime;

        PlayAttackAnimation();
        SpawnSlashVFX(currentComboStep);

        currentComboStep = (currentComboStep + 1) % TOTAL_COMBO_STEPS;
    }

    private void PlayAttackAnimation()
    {
        if (playerAnimator == null) return;

        // Use existing isAttacking for now
        // Replace with proper triggers when animations are ready
        playerAnimator.SetBool("isAttacking", true);
    }

    private void SpawnSlashVFX(int comboStep)
    {
        if (pointerController == null) return;

        Vector3 direction = pointerController.GetPointerDirection();

        // Spawn from CenterPoint outward with offset
        Vector3 spawnPos = centerPoint.position + direction * (pointerController.GetOrbitRadius() + vfxSpawnOffset);
        spawnPos.z = centerPoint.position.z;

        // Apply per-VFX position offset
        spawnPos += (Vector3)GetPositionOffset(comboStep);

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        Quaternion rotation = Quaternion.Euler(0f, 0f, angle);

        GameObject vfxPrefab = GetVFXPrefab(comboStep);
        if (vfxPrefab == null)
        {
            Debug.LogWarning($"PlayerAttackController: VFX prefab for combo step {comboStep} not assigned!");
            return;
        }

        GameObject vfxGO = Instantiate(vfxPrefab, spawnPos, rotation);

        // Fix upside-down VFX when player is flipped (facing left)
        if (direction.x < 0)
        {
            Vector3 scale = vfxGO.transform.localScale;
            scale.y *= -1; // Flip Y scale
            vfxGO.transform.localScale = scale;
        }

        SlashHitbox hitbox = vfxGO.GetComponent<SlashHitbox>();

        if (hitbox != null)
        {
            int damage = CalculateDamage(comboStep);
            float duration = GetVFXDuration(comboStep);

            hitbox.Initialize(
                damage,
                statsManager.knockbackForce,
                enemyLayers,
                playerTransform,
                damagePopupPrefab,
                duration
            );
        }
        else
        {
            Debug.LogWarning("PlayerAttackController: SlashHitbox missing on VFX prefab!");
        }
    }

    #endregion

    #region Helpers

    private GameObject GetVFXPrefab(int comboStep)
    {
        switch (comboStep)
        {
            case 0: return stabVFXPrefab;
            case 1: return horizontalVFXPrefab;
            case 2: return verticalVFXPrefab;
            default: return stabVFXPrefab;
        }
    }

    private float GetVFXDuration(int comboStep)
    {
        switch (comboStep)
        {
            case 0: return stabDuration;
            case 1: return horizontalDuration;
            case 2: return verticalDuration;
            default: return stabDuration;
        }
    }

    private int CalculateDamage(int comboStep)
    {
        int baseDamage = statsManager.GetAttackDamage();

        switch (comboStep)
        {
            case 0: return Mathf.RoundToInt(baseDamage * stabMultiplier);
            case 1: return Mathf.RoundToInt(baseDamage * horizontalMultiplier);
            case 2: return Mathf.RoundToInt(baseDamage * verticalMultiplier);
            default: return baseDamage;
        }
    }

    private Vector2 GetPositionOffset(int comboStep)
    {
        switch (comboStep)
        {
            case 0: return stabPositionOffset;
            case 1: return horizontalPositionOffset;
            case 2: return verticalPositionOffset;
            default: return Vector2.zero;
        }
    }

    private void ResetCombo()
    {
        currentComboStep = 0;
    }

    #endregion

    #region Public API

    public int GetCurrentComboStep() => currentComboStep;
    public float GetCooldownPercent() => Mathf.Clamp01(1f - (attackCooldownTimer / statsManager.cooldownTime));

    #endregion
}