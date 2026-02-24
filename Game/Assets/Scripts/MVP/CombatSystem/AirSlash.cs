using UnityEngine;

/// <summary>
/// Air Slash skill - A ranged flying slash projectile.
/// Pattern: Charge -> Roll -> Confirm/Cancel -> Fire
/// Reuses basic attack animation, flies toward mouse direction.
/// </summary>
public class AirSlash : MonoBehaviour
{
    #region Enums

    public enum SkillState
    {
        Idle,
        Charging,
        WaitingConfirm,
        Firing
    }

    #endregion

    #region Serialized Fields

    [Header("Input")]
    [SerializeField] private KeyCode skillKey = KeyCode.Alpha2;
    [SerializeField] private KeyCode confirmKey = KeyCode.E;
    [SerializeField] private KeyCode cancelKey = KeyCode.Q;

    [Header("Skill Settings")]
    [SerializeField] private float skillCooldown = 4f;
    [SerializeField] private float projectileRange = 8f;
    [SerializeField] private float projectileSpeed = 10f;

    [Header("Projectile")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private Transform firePoint;

    [Header("Dice")]
    [SerializeField] private DiceTier skillTier = DiceTier.D8;
    [SerializeField] private float skillMultiplier = 1.2f;

    [Header("Timing")]
    [SerializeField] private float rollDisplayDuration = 0.4f;
    [SerializeField] private float attackAnimationDuration = 0.5f;

    #endregion

    #region Private Fields

    private PlayerCombat playerCombat;
    private PlayerMovement playerMovement;
    private PlayerHealth playerHealth;
    private SpriteRenderer spriteRenderer;
    private StatsManager statsManager;
    private RollDisplayController rollDisplayInstance;
    private Camera mainCamera;

    private SkillState currentState = SkillState.Idle;
    private bool isExecuting = false;
    private float skillTimer = 0f;
    private Vector3 targetDirection = Vector3.right;
    private int currentDiceRoll = 0;

    #endregion

    #region Unity Lifecycle

    private void Start()
    {
        InitializeComponents();
    }

    private void Update()
    {
        UpdateSkillCooldown();
        CheckSkillInput();
        HandleStateInput();
        UpdateAiming();
    }

    #endregion

    #region Initialization

    private void InitializeComponents()
    {
        playerCombat = GetComponent<PlayerCombat>();
        playerMovement = GetComponent<PlayerMovement>();
        playerHealth = GetComponent<PlayerHealth>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        mainCamera = Camera.main;

        statsManager = StatsManager.Instance;
        if (statsManager == null)
            statsManager = FindObjectOfType<StatsManager>();

        if (firePoint == null && playerCombat != null)
            firePoint = playerCombat.attackPoint;

        EnsureRollDisplay();
    }

    private void EnsureRollDisplay()
    {
        if (rollDisplayInstance != null)
            return;

        GameObject rollDisplayGO = new GameObject("RollDisplay_AirSlash");
        rollDisplayGO.transform.SetParent(transform);
        rollDisplayGO.transform.localPosition = Vector3.zero;

        rollDisplayInstance = rollDisplayGO.AddComponent<RollDisplayController>();
        rollDisplayInstance.AttachTo(transform, DiceDisplayManager.Instance.GetRollDisplayOffset());
    }

    #endregion

    #region Input Handling

    private void CheckSkillInput()
    {
        if (Input.GetKeyDown(skillKey) && CanTriggerSkill())
            TriggerAirSlash();
    }

    private void HandleStateInput()
    {
        if (currentState != SkillState.WaitingConfirm)
            return;

        if (Input.GetKeyDown(confirmKey))
            ConfirmFire();
        else if (Input.GetKeyDown(cancelKey))
            CancelSkill();
    }

    #endregion

    #region Aiming

    private void UpdateAiming()
    {
        if (currentState != SkillState.WaitingConfirm)
            return;

        if (mainCamera == null)
            mainCamera = Camera.main;

        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        Plane plane = new Plane(Vector3.forward, transform.position);

        if (plane.Raycast(ray, out float dist))
        {
            Vector3 mouseWorldPos = ray.GetPoint(dist);
            Vector3 direction = mouseWorldPos - transform.position;
            direction.z = 0f;

            if (direction.magnitude > 0.01f)
                targetDirection = direction.normalized;
        }

        // Flip sprite based on mouse direction
        if (spriteRenderer != null)
            spriteRenderer.flipX = targetDirection.x < 0;
    }

    #endregion

    #region Cooldown

    private void UpdateSkillCooldown()
    {
        if (skillTimer > 0)
            skillTimer -= Time.unscaledDeltaTime;
    }

    private bool CanTriggerSkill()
    {
        return !isExecuting
            && currentState == SkillState.Idle
            && skillTimer <= 0;
    }

    #endregion

    #region Skill Execution

    private void TriggerAirSlash()
    {
        isExecuting = true;
        skillTimer = skillCooldown;

        DisablePlayerSystems();
        StartCoroutine(ExecuteAirSlashSequence());
    }

    private System.Collections.IEnumerator ExecuteAirSlashSequence()
    {
        // === CHARGE PHASE ===
        currentState = SkillState.Charging;
        TimeManager.Instance.SetSlowMotion();
        PlayChargeAnimation();
        yield return new WaitForSecondsRealtime(0.2f);

        // === ROLL PHASE ===
        currentDiceRoll = DiceRoller.Roll(skillTier);
        ShowRollDisplay(currentDiceRoll);
        yield return new WaitForSecondsRealtime(rollDisplayDuration);

        // === WAIT FOR CONFIRMATION ===
        currentState = SkillState.WaitingConfirm;
        SkillIndicatorManager.Instance?.ShowIndicator(
            SkillIndicatorData.Arrow(projectileRange)
        );

        while (currentState == SkillState.WaitingConfirm && isExecuting)
        {
            yield return null;
        }

        SkillIndicatorManager.Instance?.HideAll();

        // Cancelled?
        if (!isExecuting)
        {
            TimeManager.Instance.SetNormalSpeed();
            yield break;
        }

        // === FIRE PHASE ===
        currentState = SkillState.Firing;
        TimeManager.Instance.SetNormalSpeed();

        PlayAttackAnimation();
        yield return new WaitForSeconds(0.1f);

        FireProjectile(targetDirection);

        yield return new WaitForSeconds(attackAnimationDuration);

        EndSkillExecution();
    }

    private void ConfirmFire()
    {
        if (currentState == SkillState.WaitingConfirm)
            currentState = SkillState.Firing;
    }

    private void CancelSkill()
    {
        if (!isExecuting) return;

        isExecuting = false;
        currentState = SkillState.Idle;

        TimeManager.Instance.SetNormalSpeed();
        StopSkillAnimation();
        SkillIndicatorManager.Instance?.HideAll();
        EnablePlayerSystems();
    }

    private void EndSkillExecution()
    {
        EnablePlayerSystems();
        StopSkillAnimation();
        SkillIndicatorManager.Instance?.HideAll();

        isExecuting = false;
        currentState = SkillState.Idle;
        TimeManager.Instance.SetNormalSpeed();
    }

    #endregion

    #region Projectile

    private void FireProjectile(Vector3 direction)
    {
        if (projectilePrefab == null)
        {
            Debug.LogWarning("AirSlash: Projectile prefab not assigned!");
            return;
        }

        Vector3 spawnPos = firePoint != null ? firePoint.position : transform.position;
        GameObject projectileGO = Instantiate(projectilePrefab, spawnPos, Quaternion.identity);

        // Rotate to face direction
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        projectileGO.transform.rotation = Quaternion.Euler(0f, 0f, angle);

        AirSlashProjectile projectile = projectileGO.GetComponent<AirSlashProjectile>();
        if (projectile != null)
        {
            int damage = DamageCalculator.CalculateSkillDamage(
                currentDiceRoll,
                statsManager.strength,
                skillMultiplier
            );

            projectile.Initialize(
                direction,
                projectileSpeed,
                projectileRange,
                damage,
                statsManager.knockbackForce,
                transform,
                playerCombat.enemyLayers,
                playerCombat.damagePopupPrefab
            );
        }
        else
        {
            Debug.LogWarning("AirSlash: AirSlashProjectile component missing on prefab!");
        }
    }

    #endregion

    #region Roll Display

    private void ShowRollDisplay(int rollValue)
    {
        EnsureRollDisplay();
        if (rollDisplayInstance == null) return;

        rollDisplayInstance.Show();
        rollDisplayInstance.PlayRoll(rollValue, skillTier, rollDisplayDuration);
    }

    #endregion

    #region Animation

    private void PlayChargeAnimation()
    {
        if (playerCombat?.anim == null) return;

        playerCombat.anim.SetBool("isWalking", false);
        playerCombat.anim.SetBool("isAttacking", false);
        playerCombat.anim.SetBool("isSkillCharging", true);
        playerCombat.anim.SetBool("isSkillAttacking", false);
    }

    private void PlayAttackAnimation()
    {
        if (playerCombat?.anim == null) return;

        playerCombat.anim.SetBool("isSkillCharging", false);
        playerCombat.anim.SetBool("isAttacking", true);
    }

    private void StopSkillAnimation()
    {
        if (playerCombat?.anim == null) return;

        playerCombat.anim.SetBool("isSkillCharging", false);
        playerCombat.anim.SetBool("isSkillAttacking", false);
        playerCombat.anim.SetBool("isAttacking", false);
    }

    #endregion

    #region Player Systems

    private void DisablePlayerSystems()
    {
        if (playerCombat != null) playerCombat.enabled = false;
        if (playerMovement != null) playerMovement.enabled = false;
    }

    private void EnablePlayerSystems()
    {
        if (playerCombat != null) playerCombat.enabled = true;
        if (playerMovement != null) playerMovement.enabled = true;
    }

    #endregion

    #region Public API

    public float GetSkillCooldownPercent() => Mathf.Clamp01(1f - (skillTimer / skillCooldown));
    public bool IsExecuting => isExecuting;
    public SkillState GetCurrentState => currentState;

    #endregion
}