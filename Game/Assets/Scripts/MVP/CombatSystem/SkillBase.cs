using UnityEngine;
using System.Collections;
using Photon.Pun;

/// <summary>
/// Base class for all skills.
/// Handles global flow: Charge -> Roll -> Confirm/Cancel -> Execute
/// Also holds shared combat references previously in PlayerCombat.
/// Each skill only overrides GetIndicatorData() and OnExecute()
/// 
/// NOTE: Animation is now handled via spawned VFX prefabs instead of animator parameters.
/// See comments marked with "TODO: SPAWN_VFX" for where to add sword swing animations.
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

    [Header("Combat References - Base")]
    [SerializeField] protected Transform attackPoint;
    [SerializeField] public LayerMask enemyLayers;
    [System.NonSerialized] public GameObject damagePopupPrefab;
    [SerializeField] public Animator anim;
    [SerializeField] protected bool blockAttackDamage = false;

    #endregion

    #region Protected Fields - Components

    protected PlayerMovement playerMovement;
    protected PlayerHealthManager playerHealth;
    protected SpriteRenderer spriteRenderer;
    protected StatsManager statsManager;
    protected Camera mainCamera;
    protected Transform centerPoint;
    protected PlayerPointerController pointerController;

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
        // Find local player using Photon
        PlayerMovement pm = FindLocalPlayerMovement();
        if (pm != null)
        {
            playerMovement = pm;
        }
        else
        {
            Debug.LogError($"[{GetType().Name}] Local PlayerMovement not found!");
            enabled = false;
            return;
        }

        // Find PlayerHealthManager
        playerHealth = FindObjectOfType<PlayerHealthManager>();
        if (playerHealth == null)
            Debug.LogWarning($"[{GetType().Name}] PlayerHealthManager not found!");

        // Find StatsManager
        if (statsManager == null)
            statsManager = StatsManager.Instance;
        if (statsManager == null)
            statsManager = FindObjectOfType<StatsManager>();
        if (statsManager == null)
        {
            Debug.LogError($"[{GetType().Name}] StatsManager not found!");
            enabled = false;
            return;
        }

        // Find Animator on Player
        anim = pm.GetComponent<Animator>();
        if (anim == null)
            anim = FindObjectOfType<Animator>();

        // Find PlayerPointerController
        pointerController = FindObjectOfType<PlayerPointerController>();
        if (pointerController == null)
            Debug.LogWarning($"[{GetType().Name}] PlayerPointerController not found!");

        // Set attackPoint to pointer position for compatibility
        attackPoint = pointerController?.gameObject.transform;

        // Set spriteRenderer from PlayerEntity
        spriteRenderer = pm.GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
            spriteRenderer = pm.GetComponentInChildren<SpriteRenderer>();

        // Find CenterPoint
        Transform found = pm.transform.Find("CenterPoint");
        centerPoint = found != null ? found : pm.transform;

        // Initialize main camera
        if (mainCamera == null)
            mainCamera = Camera.main;

        EnsureRollDisplay();
    }

    private PlayerMovement FindLocalPlayerMovement()
    {
        foreach (GameObject go in GameObject.FindGameObjectsWithTag("PlayerEntity"))
        {
            PhotonView pv = go.GetComponent<PhotonView>();
            if (pv != null && pv.IsMine)
            {
                return go.GetComponent<PlayerMovement>();
            }
        }
        return null;
    }

    private void EnsureRollDisplay()
    {
        if (rollDisplayInstance != null) return;

        GameObject rollDisplayGO = new GameObject($"RollDisplay_{GetType().Name}");
        rollDisplayGO.transform.SetParent(transform);
        rollDisplayGO.transform.localPosition = Vector3.zero;

        rollDisplayInstance = rollDisplayGO.AddComponent<RollDisplayController>();
        if (rollDisplayInstance != null && DiceDisplayManager.Instance != null)
        {
            rollDisplayInstance.AttachTo(
                playerMovement.transform, 
                DiceDisplayManager.Instance.GetRollDisplayOffset()
            );
        }
    }

    #endregion

    #region Input Handling

    private void CheckSkillInput()
    {
        if (!CombatModeManager.Instance.IsCombatModeActive) return;

        // Get slot index from SkillManager
        int slotIndex = GetSlotIndex();
        if (slotIndex == -1) return;

        KeyCode assignedKey = GetKeyForSlot(slotIndex);
        
        if (Input.GetKeyDown(assignedKey) && CanTriggerSkillLocal())
            TriggerSkillLocal();
    }

    private int GetSlotIndex()
    {
        if (SkillManager.Instance == null) return -1;

        for (int i = 0; i < SkillManager.Instance.GetSkillCount(); i++)
        {
            if (SkillManager.Instance.GetSkill(i) == this)
                return i;
        }
        return -1;
    }

    private KeyCode GetKeyForSlot(int slotIndex)
    {
        switch (slotIndex)
        {
            case 0: return KeyCode.Alpha1;
            case 1: return KeyCode.Alpha2;
            case 2: return KeyCode.Alpha3;
            case 3: return KeyCode.Alpha4;
            default: return KeyCode.None;
        }
    }

    private void HandleStateInput()
    {
        if (currentState != SkillState.WaitingConfirm) return;
        if (!CombatModeManager.Instance.IsCombatModeActive) return;

        if (Input.GetKeyDown(confirmKey))
            ConfirmSkill();
        else if (Input.GetKeyDown(cancelKey))
            CancelSkill();
    }

    #endregion

    #region Aiming

    private void UpdateAiming()
    {
        if (currentState != SkillState.WaitingConfirm) return;

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

        // TODO: SPAWN_VFX - Replace sprite flip with sword swing direction indicator VFX
        // if (spriteRenderer != null)
        //     spriteRenderer.flipX = targetDirection.x < 0;
    }

    #endregion

    #region Cooldown

    private void UpdateSkillCooldown()
    {
        if (skillTimer > 0)
            skillTimer -= Time.deltaTime;
    }

    private bool CanTriggerSkillLocal()
    {
        return !isExecuting
            && currentState == SkillState.Idle
            && skillTimer <= 0;
    }

    #endregion

    #region Skill Flow

    private void TriggerSkillLocal()
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
        EnablePlayerSystems();
        yield return new WaitForSeconds(rollDisplayDuration);

        // === FIRST CONFIRMATION ===
        yield return StartCoroutine(WaitForConfirmation());

        if (!isExecuting)
            yield break;

        // === EXECUTE PHASE ===
        currentState = SkillState.Executing;
        DisablePlayerSystems();
        PlayAttackAnimation();
        yield return new WaitForSeconds(0.1f);

        yield return StartCoroutine(OnExecute(currentDiceRoll));

        EndSkillExecution();
    }

    protected IEnumerator WaitForConfirmation()
    {
        currentState = SkillState.WaitingConfirm;
        SkillIndicatorManager.Instance?.ShowIndicator(GetIndicatorData());

        while (currentState == SkillState.WaitingConfirm && isExecuting)
            yield return null;

        DisablePlayerSystems();
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
        // TODO: SPAWN_VFX - Spawn charge VFX prefab instead
        // if (anim == null) return;
        // anim.SetBool("isWalking", false);
        // anim.SetBool("isAttacking", false);
        // anim.SetBool("isSkillCharging", true);
        // anim.SetBool("isSkillAttacking", false);
    }

    private void PlayAttackAnimation()
    {
        // TODO: SPAWN_VFX - Spawn skill attack sword swing VFX prefab instead
        // if (anim == null) return;
        // anim.SetBool("isSkillCharging", false);
        // anim.SetBool("isAttacking", false);
        // anim.SetBool("isSkillAttacking", true);
    }

    private void StopSkillAnimation()
    {
        // TODO: SPAWN_VFX - Destroy/hide spawned VFX when skill ends
        // if (anim == null) return;
        // anim.SetBool("isSkillCharging", false);
        // anim.SetBool("isSkillAttacking", false);
        // anim.SetBool("isAttacking", false);
    }

    #endregion

    #region Player Systems

    private void DisablePlayerSystems()
    {
        blockAttackDamage = true;

        if (playerMovement != null)
            playerMovement.enabled = false;
    }

    private void EnablePlayerSystems()
    {
        blockAttackDamage = false;

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

    #region Helper Methods

    protected int RollAndDisplay()
    {
        int roll = DiceRoller.Roll(skillTier);
        ShowRollDisplay(roll);
        return roll;
    }

    #endregion

    #region Public API

    public float GetSkillCooldownPercent() => Mathf.Clamp01(1f - (skillTimer / skillCooldown));
    public bool IsExecuting => isExecuting;
    public SkillState GetCurrentState => currentState;

    #endregion
}