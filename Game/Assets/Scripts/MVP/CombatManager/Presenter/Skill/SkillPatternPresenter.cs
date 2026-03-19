using UnityEngine;
using System.Collections;
using Photon.Pun;
using CombatManager.Model;
using CombatManager.Service;
using CombatManager.View;

namespace CombatManager.Presenter
{
    /// <summary>
    /// Abstract base presenter for all skill types.
    /// Defines and owns the skill execution PATTERN:
    /// TriggerSkill() → Charge → DiceRoll → WaitConfirm/Cancel → Execute → Cooldown
    /// 
    /// Concrete skill presenters extend this:
    /// → ProjectileSkillPresenter (SkillCategory.Projectile)
    /// → SlashSkillPresenter      (SkillCategory.Slash)
    /// → Future: AoESkillPresenter, BuffSkillPresenter etc.
    /// 
    /// Data is driven by SkillData SO via SetSkillData() in subclasses.
    /// </summary>
    public abstract class SkillPatternPresenter : SkillPatternBase
    {
        #region Serialized Fields

        [Header("Pattern Model")]
        [SerializeField] protected SkillPatternModel model = new SkillPatternModel();

        [Header("Skill Settings")]
        [SerializeField] protected float skillCooldown    = 3f;
        [SerializeField] protected float chargeDuration   = 0.2f;
        [SerializeField] protected float rollDisplayDuration = 0.4f;

        [Header("Dice Settings")]
        [SerializeField] protected CombatManager.Model.DiceTier skillTier = 
            CombatManager.Model.DiceTier.D6;
        [SerializeField] protected float skillMultiplier = 1.5f;

        [Header("Combat Settings")]
        [SerializeField] public LayerMask enemyLayers;

        [Header("Input Settings")]
        [SerializeField] protected KeyCode confirmKey = KeyCode.E;
        [SerializeField] protected KeyCode cancelKey  = KeyCode.Q;

        #endregion

        #region Services

        protected ISkillPatternService skillService;
        protected IDiceRollerService diceRollerService;
        protected IDamageCalculatorService damageCalculatorService;

        #endregion

        #region Runtime References (Auto-Found)

        protected Transform playerTransform;
        protected PlayerMovement playerMovement;
        protected StatsPresenter statsPresenter;
        protected PlayerPointerPresenter pointerPresenter;
        protected CombatModePresenter combatModePresenter;
        protected Camera mainCamera;
        protected Transform attackPoint;
        protected Transform centerPoint;

        #endregion

        #region Unity Lifecycle

        protected virtual void Awake()
        {
            InitializeModel();
            InitializeServices();
        }

        protected virtual void Start()
        {
            StartCoroutine(FindPlayerDelayed());
            OnStart();
        }

        protected virtual void Update()
        {
            skillService?.UpdateCooldown(Time.deltaTime);
            HandleConfirmCancelInput();
            UpdateAiming();
        }

        #endregion

        #region Initialization

        private void InitializeModel()
        {
            model.skillCooldown      = skillCooldown;
            model.chargeDuration     = chargeDuration;
            model.rollDisplayDuration = rollDisplayDuration;
            model.skillTier          = skillTier;
            model.skillMultiplier    = skillMultiplier;
            model.enemyLayers        = enemyLayers;
            model.confirmKey         = confirmKey;
            model.cancelKey          = cancelKey;
        }

        private void InitializeServices()
        {
            skillService            = new SkillPatternService(model);
            diceRollerService       = new DiceRollerService();
            damageCalculatorService = new DamageCalculatorService();

            Debug.Log($"[{GetType().Name}] Services initialized");
        }

        #endregion

        #region Find Player (Spawned at Runtime)

        private IEnumerator FindPlayerDelayed()
        {
            yield return new WaitForSeconds(0.5f);
            FindLocalPlayer();
        }

        private void FindLocalPlayer()
        {
            foreach (GameObject go in GameObject.FindGameObjectsWithTag("PlayerEntity"))
            {
                PhotonView pv = go.GetComponent<PhotonView>();
                if (pv != null && pv.IsMine)
                {
                    SetupPlayerReferences(go);
                    return;
                }
            }

            GameObject fallback = GameObject.FindGameObjectWithTag("PlayerEntity");
            if (fallback != null)
            {
                SetupPlayerReferences(fallback);
                return;
            }

            Debug.LogError($"[{GetType().Name}] Local player not found!");
            enabled = false;
        }

        private void SetupPlayerReferences(GameObject playerGO)
        {
            playerTransform = playerGO.transform;

            playerMovement = playerGO.GetComponent<PlayerMovement>();
            if (playerMovement == null)
                Debug.LogWarning($"[{GetType().Name}] PlayerMovement not found on player!");

            Transform found = playerGO.transform.Find("CenterPoint");
            centerPoint = found != null ? found : playerGO.transform;

            statsPresenter = playerGO.GetComponent<StatsPresenter>();
            if (statsPresenter == null)
                statsPresenter = FindObjectOfType<StatsPresenter>();
            if (statsPresenter == null)
                Debug.LogWarning($"[{GetType().Name}] StatsPresenter not found!");

            pointerPresenter = FindObjectOfType<PlayerPointerPresenter>();
            if (pointerPresenter != null)
                attackPoint = pointerPresenter.transform;
            else
                Debug.LogWarning($"[{GetType().Name}] PlayerPointerPresenter not found!");

            combatModePresenter = CombatModePresenter.Instance;
            if (combatModePresenter == null)
                Debug.LogWarning($"[{GetType().Name}] CombatModePresenter not found!");

            mainCamera = Camera.main;

            Debug.Log($"[{GetType().Name}] Player references set from: {playerGO.name}");
        }

        #endregion

        #region Input Handling

        private void HandleConfirmCancelInput()
        {
            if (!model.IsWaitingConfirm) return;
            if (!IsCombatModeActive()) return;

            if (Input.GetKeyDown(model.confirmKey))
                ConfirmSkill();
            else if (Input.GetKeyDown(model.cancelKey))
                CancelSkill();
        }

        #endregion

        #region Aiming

        private void UpdateAiming()
        {
            if (!model.IsWaitingConfirm) return;
            if (mainCamera == null) mainCamera = Camera.main;
            if (playerTransform == null) return;

            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            Plane plane = new Plane(Vector3.forward, playerTransform.position);

            if (plane.Raycast(ray, out float dist))
            {
                Vector3 mouseWorldPos = ray.GetPoint(dist);
                Vector3 direction = mouseWorldPos - playerTransform.position;
                direction.z = 0f;

                if (direction.magnitude > 0.01f)
                    model.targetDirection = direction.normalized;
            }
        }

        #endregion

        #region Skill Pattern Coroutine

        private IEnumerator ExecuteSkillSequence()
        {
            skillService.SetState(SkillPatternState.Charging);
            OnChargeStart();

            yield return new WaitForSeconds(model.chargeDuration);

            model.currentDiceRoll = diceRollerService.Roll(model.skillTier);

            CombatManager.Presenter.DiceDisplayPresenter.Show(
                model.currentDiceRoll,
                (CombatManager.Model.DiceTier)model.skillTier
            );
            EnablePlayerSystems();

            yield return new WaitForSeconds(model.rollDisplayDuration);

            yield return StartCoroutine(WaitForConfirmationRoutine());

            if (!model.isExecuting) yield break;

            skillService.SetState(SkillPatternState.Executing);
            DisablePlayerSystems();

            OnAttackStart();

            // ✅ Play weapon attack animation when skill executes
            if (WeaponAnimationPresenter.Instance != null &&
                WeaponAnimationPresenter.Instance.IsWeaponActive())
            {
                WeaponAnimationPresenter.Instance.PlayAttackAnimation();
                Debug.Log($"[{GetType().Name}] Weapon attack animation triggered!");
            }

            yield return new WaitForSeconds(0.1f);

            int strength = 0;
            if (statsPresenter != null)
                strength = statsPresenter.GetService().GetTempStrength();

            int weaponDamage = 0;
            var currentWeapon = WeaponEquipPresenter.Instance?.GetCurrentWeapon();
            if (currentWeapon != null)
                weaponDamage = currentWeapon.damage;

            int finalDamage = damageCalculatorService.CalculateSkillDamage(
                model.currentDiceRoll,
                strength,
                model.skillMultiplier,
                weaponDamage
            );

            yield return StartCoroutine(OnExecute(finalDamage, model.targetDirection));

            EndSkillExecution();
        }

        private IEnumerator WaitForConfirmationRoutine()
        {
            skillService.SetState(SkillPatternState.WaitingConfirm);
            ShowIndicator();

            while (model.IsWaitingConfirm && model.isExecuting)
                yield return null;

            DisablePlayerSystems();
            HideIndicator();
            DiceDisplayPresenter.Hide();
        }

        private void ConfirmSkill()
        {
            if (!model.IsWaitingConfirm) return;
            skillService.SetState(SkillPatternState.Executing);
            Debug.Log($"[{GetType().Name}] Skill CONFIRMED");
        }

        private void CancelSkill()
        {
            if (!model.isExecuting) return;

            model.isExecuting = false;
            skillService.SetState(SkillPatternState.Idle);
            OnSkillCancelled();
            HideIndicator();
            DiceDisplayPresenter.Hide();
            EnablePlayerSystems();

            Debug.Log($"[{GetType().Name}] Skill CANCELLED");
        }

        private void EndSkillExecution()
        {
            OnAttackEnd();
            HideIndicator();
            EnablePlayerSystems();

            model.isExecuting = false;
            skillService.SetState(SkillPatternState.Idle);

            Debug.Log($"[{GetType().Name}] Skill execution END");
        }

        #endregion

        #region Indicator

        private void ShowIndicator()
        {
            CombatManager.Model.SkillIndicatorData data = GetIndicatorData();
            if (data == null) return;
            SkillIndicatorPresenter.Instance?.ShowIndicator(data);
        }

        private void HideIndicator()
        {
            SkillIndicatorPresenter.Instance?.HideAll();
        }

        #endregion

        #region Player Systems

        private void DisablePlayerSystems()
        {
            model.blockAttackDamage = true;
            if (playerMovement != null)
                playerMovement.enabled = false;
        }

        private void EnablePlayerSystems()
        {
            model.blockAttackDamage = false;
            if (playerMovement != null)
                playerMovement.enabled = true;
        }

        #endregion

        #region Combat Mode Check

        private bool IsCombatModeActive()
        {
            if (combatModePresenter == null) return true;
            return combatModePresenter.IsCombatModeActive();
        }

        #endregion

        #region Abstract Methods

        protected abstract CombatManager.Model.SkillIndicatorData GetIndicatorData();
        protected abstract IEnumerator OnExecute(int finalDamage, Vector3 direction);

        #endregion

        #region Virtual Methods

        protected virtual void OnStart() { }
        protected virtual void OnChargeStart() { }
        protected virtual void OnAttackStart() { }
        protected virtual void OnAttackEnd() { }
        protected virtual void OnSkillCancelled() { }

        #endregion

        #region SkillPatternBase Implementation

        public override bool IsExecuting => model.isExecuting;

        public override bool IsCoolingDown()
            => skillService?.IsCoolingDown() ?? false;

        public override float GetCooldownPercent()
            => skillService?.GetCooldownPercent() ?? 0f;

        public override void TriggerSkill()
        {
            if (!skillService.CanTrigger()) return;
            if (!IsCombatModeActive()) return;
            if (playerTransform == null)
            {
                Debug.LogWarning($"[{GetType().Name}] Player not found yet!");
                return;
            }

            model.isExecuting = true;
            skillService.StartCooldown();
            DisablePlayerSystems();
            StartCoroutine(ExecuteSkillSequence());
        }

        #endregion

        #region Public Getters

        public SkillPatternState GetCurrentState => model.currentState;
        public CombatManager.Model.DiceTier GetSkillTier() => model.skillTier;
        public LayerMask GetEnemyLayers() => model.enemyLayers;

        #endregion
    }
}