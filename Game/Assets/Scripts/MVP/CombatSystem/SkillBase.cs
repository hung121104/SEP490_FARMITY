using UnityEngine;
using System.Collections;

/// <summary>
/// Base class for all skills.
/// Handles global flow: Charge -> Roll -> Confirm/Cancel -> Execute
/// Each skill only overrides GetIndicatorData() and OnExecute()
/// </summary>
public abstract class SkillBase : MonoBehaviour
{
    #region Enums

    public enum SkillState
    {
        Idle,
        Charging,
        WaitingConfirm,
        Executing
    }

    #endregion

    #region Serialized Fields

    [Header("Input - Base")]
    [SerializeField] protected KeyCode skillKey = KeyCode.Alpha1;
    [SerializeField] protected KeyCode confirmKey = KeyCode.E;
    [SerializeField] protected KeyCode cancelKey = KeyCode.Q;

    [Header("Skill Settings - Base")]
    [SerializeField] protected float skillCooldown = 3f;

    [Header("Dice - Base")]
    [SerializeField] protected DiceTier skillTier = DiceTier.D6;
    [SerializeField] protected float skillMultiplier = 1.5f;

    [Header("Timing - Base")]
    [SerializeField] protected float chargeDuration = 0.2f;
    [SerializeField] protected float rollDisplayDuration = 0.4f;

    #endregion

    #region Protected Fields - Components

    protected PlayerCombat playerCombat;
    protected PlayerMovement playerMovement;
    protected PlayerHealth playerHealth;
    protected SpriteRenderer spriteRenderer;
    protected StatsManager statsManager;
    protected Camera mainCamera;

    #endregion

    #region Private Fields

    private RollDisplayController rollDisplayInstance;
    private SkillState currentState = SkillState.Idle;
    private bool isExecuting = false;
    private float skillTimer = 0f;
    private int currentDiceRoll = 0;

    #endregion

    #region Protected Fields - Aiming

    protected Vector3 targetDirection = Vector3.right;

    #endregion

    #region Unity Lifecycle

    protected virtual void Start()
    {
        InitializeBaseComponents();
        OnStart();
    }

    protected virtual void Update()
    {
        UpdateSkillCooldown();
        CheckSkillInput();
        HandleStateInput();
        UpdateAiming();
    }

    #endregion

    #region Initialization

    private void InitializeBaseComponents()
    {
        playerCombat = GetComponent<PlayerCombat>();
        playerMovement = GetComponent<PlayerMovement>();
        playerHealth = GetComponent<PlayerHealth>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        mainCamera = Camera.main;

        statsManager = StatsManager.Instance;
        if (statsManager == null)
            statsManager = FindObjectOfType<StatsManager>();

        EnsureRollDisplay();
    }

    private void EnsureRollDisplay()
    {
        if (rollDisplayInstance != null)
            return;

        GameObject rollDisplayGO = new GameObject($"RollDisplay_{GetType().Name}");
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
            TriggerSkill();
    }

    private void HandleStateInput()
    {
        if (currentState != SkillState.WaitingConfirm)
            return;

        if (Input.GetKeyDown(confirmKey))
            ConfirmSkill();
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
            skillTimer -= Time.deltaTime;
    }

    private bool CanTriggerSkill()
    {
        return !isExecuting
            && currentState == SkillState.Idle
            && skillTimer <= 0;
    }

    #endregion

    #region Skill Flow

    private void TriggerSkill()
    {
        isExecuting = true;
        skillTimer = skillCooldown;

        DisablePlayerSystems();
        StartCoroutine(ExecuteSkillSequence());
    }

    private IEnumerator ExecuteSkillSequence()
    {
        // === CHARGE PHASE ===
        currentState = SkillState.Charging;
        PlayChargeAnimation();
        yield return new WaitForSeconds(chargeDuration);

        // === ROLL PHASE ===
        currentDiceRoll = DiceRoller.Roll(skillTier);
        ShowRollDisplay(currentDiceRoll);
        yield return new WaitForSeconds(rollDisplayDuration);

        // === FIRST CONFIRMATION ===
        yield return StartCoroutine(WaitForConfirmation());

        // Cancelled?
        if (!isExecuting)
            yield break;

        // === EXECUTE PHASE ===
        currentState = SkillState.Executing;
        PlayAttackAnimation();
        yield return new WaitForSeconds(0.1f);

        yield return StartCoroutine(OnExecute(currentDiceRoll));

        EndSkillExecution();
    }

    /// <summary>
    /// Waits for player to confirm or cancel.
    /// Can be called multiple times for multi-hit skills.
    /// </summary>
    protected IEnumerator WaitForConfirmation()
    {
        currentState = SkillState.WaitingConfirm;
        SkillIndicatorManager.Instance?.ShowIndicator(GetIndicatorData());

        while (currentState == SkillState.WaitingConfirm && isExecuting)
        {
            yield return null;
        }

        SkillIndicatorManager.Instance?.HideAll();
    }

    private void ConfirmSkill()
    {
        if (currentState == SkillState.WaitingConfirm)
            currentState = SkillState.Executing;
    }

    private void CancelSkill()
    {
        if (!isExecuting) return;

        isExecuting = false;
        currentState = SkillState.Idle;

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
        if (playerCombat != null)
        {
            playerCombat.enabled = false;
            playerCombat.blockAttackDamage = true; // Block animation event damage
        }

        if (playerMovement != null)
            playerMovement.enabled = false;
    }

    private void EnablePlayerSystems()
    {
        if (playerCombat != null)
        {
            playerCombat.enabled = true;
            playerCombat.blockAttackDamage = false; // Re-enable normal attack damage
        }

        if (playerMovement != null)
            playerMovement.enabled = true;
    }

    #endregion

    #region Abstract Methods

    protected abstract SkillIndicatorData GetIndicatorData();
    protected abstract IEnumerator OnExecute(int diceRoll);

    #endregion

    #region Virtual Methods

    protected virtual void OnStart() { }

    #endregion

    /// <summary>
    /// Roll dice and show display.
    /// Can be called by skills that need multiple rolls (e.g DoubleStrike).
    /// Returns the rolled value.
    /// </summary>
    protected int RollAndDisplay()
    {
        int roll = DiceRoller.Roll(skillTier);
        ShowRollDisplay(roll);
        return roll;
    }

    #region Public API

    public float GetSkillCooldownPercent() => Mathf.Clamp01(1f - (skillTimer / skillCooldown));
    public bool IsExecuting => isExecuting;
    public SkillState GetCurrentState => currentState;

    #endregion
}