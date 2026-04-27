using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;
using CombatManager.Model;
using CombatManager.Service;
using CombatManager.View;
using CombatManager.Model;

namespace CombatManager.Presenter
{
    /// <summary>
    /// Presenter for SkillManagement panel.
    /// Player skills are loaded from CombatSkillCatalogService (DB-driven).
    /// Weapon skills are excluded from this panel.
    /// </summary>
    public class SkillManagementPresenter : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Model")]
        [SerializeField] private SkillManagementModel model = new SkillManagementModel();

        [Header("Canvas Reference")]
        [SerializeField] private GameObject skillManagementCanvas;
        [SerializeField] private CanvasGroup skillManagementCanvasGroup;

        [Header("Grid")]
        [SerializeField] private Transform skillGridContainer;

        [Header("Prefabs")]
        [SerializeField] private GameObject skillDisplayItemPrefab;

        [Header("Buttons")]
        [SerializeField] private Button closeButton;
        [Header("Detail Tooltip")]
        [SerializeField] private ItemDetailView skillDetailView;

        #endregion

        #region Runtime

        private ISkillManagementService service;
        private List<SkillDisplayItemView> displayItems = new List<SkillDisplayItemView>();
        private InputAction escapeCloseAction;
        private StatsPresenter cachedStatsPresenter;
        private SkillDisplayItemView currentHoverItem;
        private bool hasLoggedMissingDetailView;
        private Coroutine pendingHoverExitCoroutine;
        private Coroutine realtimeRefreshCoroutine;
        private const float HoverExitGraceSeconds = 0.06f;
        private Canvas skillTooltipCanvas;

        #endregion

        #region Singleton

        public static SkillManagementPresenter Instance { get; private set; }

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            service = new SkillManagementService(model);
            escapeCloseAction = new InputAction("CloseSkillManagementPanel", InputActionType.Button, "<Keyboard>/escape");
            escapeCloseAction.performed += OnEscapeClosePanel;

            if (skillManagementCanvasGroup == null && skillManagementCanvas != null)
                skillManagementCanvasGroup = skillManagementCanvas.GetComponent<CanvasGroup>();

            TryResolveSkillDetailView();
        }

        private void OnEnable()
        {
            escapeCloseAction?.Enable();
        }

        private void Start()
        {
            SetupCloseButton();

            StartCoroutine(LoadCatalogSkills());
            CombatSkillCatalogService.OnCatalogDefinitionChanged += OnCombatSkillCatalogDefinitionChanged;
            CatalogSyncManager.OnCatalogChanged += OnCatalogChanged;

            CombatModePresenter.OnCombatModeChanged += OnCombatModeChanged;
            GameEventBus.OnLevelReached += OnLevelReached;
            SetPanelVisible(false);

            Debug.Log("[SkillManagementPresenter] Initialized!");
        }

        private IEnumerator LoadCatalogSkills()
        {
            float elapsed = 0f;
            while ((CombatSkillCatalogService.Instance == null || !CombatSkillCatalogService.Instance.IsReady)
                   && elapsed < 10f)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            if (CombatSkillCatalogService.Instance == null || !CombatSkillCatalogService.Instance.IsReady)
            {
                Debug.LogWarning("[SkillManagementPresenter] CombatSkillCatalogService unavailable. Panel will be empty.");
                service.Initialize(new List<SkillData>(), GetCurrentPlayerLevel());
                PopulateGrid();
                yield break;
            }

            List<SkillData> allSkills = CombatSkillCatalogService.Instance.GetAllSkills();
            Debug.Log($"[SkillManagementPresenter] Catalog skills loaded: {allSkills.Count}");

            service.Initialize(allSkills, GetCurrentPlayerLevel());
            Debug.Log($"[SkillManagementPresenter] Player skills after filter: {service.GetAllSkills().Count}");

            PopulateGrid();
        }

        private void Update()
        {
            HandleInput();
        }

        private void OnDestroy()
        {
            CombatSkillCatalogService.OnCatalogDefinitionChanged -= OnCombatSkillCatalogDefinitionChanged;
            CatalogSyncManager.OnCatalogChanged -= OnCatalogChanged;
            CombatModePresenter.OnCombatModeChanged -= OnCombatModeChanged;
            GameEventBus.OnLevelReached -= OnLevelReached;

            if (escapeCloseAction != null)
            {
                escapeCloseAction.performed -= OnEscapeClosePanel;
                escapeCloseAction.Dispose();
                escapeCloseAction = null;
            }
        }

        private void OnCombatSkillCatalogDefinitionChanged(string changeType, string skillId)
        {
            RequestRealtimeRefresh($"catalog:{changeType}:{skillId}");
        }

        private void OnCatalogChanged(string changeType, string entityType, string entityName, string typeName)
        {
            if (!string.Equals(entityType, "combat-skill", System.StringComparison.OrdinalIgnoreCase))
                return;

            RequestRealtimeRefresh($"sync:{changeType}:{entityName}");
        }

        private void RequestRealtimeRefresh(string reason)
        {
            if (realtimeRefreshCoroutine != null)
                StopCoroutine(realtimeRefreshCoroutine);

            realtimeRefreshCoroutine = StartCoroutine(RealtimeRefreshRoutine(reason));
        }

        private IEnumerator RealtimeRefreshRoutine(string reason)
        {
            yield return null;

            if (CombatSkillCatalogService.Instance == null || !CombatSkillCatalogService.Instance.IsReady)
            {
                realtimeRefreshCoroutine = null;
                yield break;
            }

            List<SkillData> allSkills = CombatSkillCatalogService.Instance.GetAllSkills();
            service.Initialize(allSkills, GetCurrentPlayerLevel());

            if (service.IsPanelOpen())
                PopulateGrid();

            Debug.Log($"[SkillManagementPresenter] Realtime refresh applied ({reason}). skills={allSkills.Count}");
            realtimeRefreshCoroutine = null;
        }

        #endregion

        #region Initialization

        private void SetupCloseButton()
        {
            if (closeButton != null)
            {
                closeButton.onClick.RemoveAllListeners();
                closeButton.onClick.AddListener(HidePanel);
            }
        }

        #endregion

        #region Grid Population

        private void PopulateGrid()
        {
            if (skillGridContainer == null)
            {
                Debug.LogError("[SkillManagementPresenter] skillGridContainer not assigned!");
                return;
            }

            if (skillDisplayItemPrefab == null)
            {
                Debug.LogError("[SkillManagementPresenter] skillDisplayItemPrefab not assigned!");
                return;
            }

            displayItems.Clear();
            foreach (Transform child in skillGridContainer)
                Destroy(child.gameObject);

            var skills = service.GetAllSkills();
            Debug.Log($"[SkillManagementPresenter] PopulateGrid: {skills.Count} skills to show");

            foreach (SkillData skill in skills)
                CreateSkillItem(skill);

            Debug.Log($"[SkillManagementPresenter] Populated {displayItems.Count} skill items");
        }

        private void CreateSkillItem(SkillData skillData)
        {
            GameObject itemGO = Instantiate(skillDisplayItemPrefab, skillGridContainer);
            itemGO.name = $"Skill_{skillData.skillName}";

            SkillDisplayItemView view = itemGO.GetComponent<SkillDisplayItemView>();
            if (view == null)
            {
                Debug.LogError("[SkillManagementPresenter] SkillDisplayItemView missing on prefab!");
                Destroy(itemGO);
                return;
            }

            view.Initialize(skillData);
            view.OnBeginDragEvent += OnSkillBeginDrag;
            view.OnDragEvent      += OnSkillDrag;
            view.OnEndDragEvent   += OnSkillEndDrag;
            view.OnSelectEvent    += OnSkillSelected;
            view.OnHoverEnterEvent += OnSkillHoverEnter;
            view.OnHoverExitEvent += OnSkillHoverExit;

            Debug.Log($"[SkillManagementPresenter] Hover handlers bound for: {skillData.skillName}");

            displayItems.Add(view);
        }

        #endregion

        #region Drag Handling

        private void OnSkillBeginDrag(SkillDisplayItemView item)
        {
            HideSkillDetail();
            service.SetDraggingSkill(item.GetSkillData());
            Debug.Log($"[SkillManagementPresenter] Begin drag: {item.GetSkillData().skillName}");
        }

        private void OnSkillDrag(SkillDisplayItemView item) { }

        private void OnSkillEndDrag(SkillDisplayItemView item)
        {
            TryDropOnHotbar(item.GetSkillData());
            service.ClearDraggingSkill();
        }

        private void TryDropOnHotbar(SkillData skillData)
        {
            if (SkillHotbarPresenter.Instance == null) return;

            // ✅ Extra safety guard - service already filters but double check
            if (skillData != null && skillData.IsWeaponSkill)
            {
                Debug.LogWarning($"[SkillManagementPresenter] " +
                                 $"'{skillData.skillName}' is WeaponSkill - cannot drop here!");
                return;
            }

            int hoveredSlot = SkillHotbarPresenter.Instance.GetHoveredSlotIndex();
            if (hoveredSlot < 0) return;

            SkillHotbarPresenter.Instance.EquipSkill(hoveredSlot, skillData);
            SkillHotbarPresenter.Instance.RefreshSlot(hoveredSlot);

            Debug.Log($"[SkillManagementPresenter] " +
                      $"Dropped '{skillData?.skillName}' → slot {hoveredSlot}");
        }

        #endregion

        #region Select Handling

        private void OnSkillSelected(SkillDisplayItemView item)
        {
            HideSkillDetail();
            if (SkillHotbarPresenter.Instance == null) return;

            int slotCount = SkillHotbarPresenter.Instance.GetSlotCount();
            for (int i = 0; i < slotCount; i++)
            {
                if (SkillHotbarPresenter.Instance.IsSlotEmpty(i))
                {
                    SkillHotbarPresenter.Instance.EquipSkill(i, item.GetSkillData());
                    SkillHotbarPresenter.Instance.RefreshSlot(i);
                    Debug.Log($"[SkillManagementPresenter] " +
                              $"Auto-equipped '{item.GetSkillData().skillName}' → slot {i}");
                    return;
                }
            }

            Debug.Log("[SkillManagementPresenter] No empty slot available!");
        }

        #endregion

        #region Panel Show/Hide

        public void ShowPanel()
        {
            TryResolveSkillDetailView();
            RefreshUnlockedSkills();
            PopulateGrid();
            service.OpenPanel();
            SetPanelVisible(true);
        }

        public void HidePanel()
        {
            CancelAllDrags();
            HideSkillDetail();
            service.ClosePanel();
            SetPanelVisible(false);
        }

        public void TogglePanel()
        {
            HideSkillDetailImmediate();
            if (service.IsPanelOpen()) HidePanel();
            else ShowPanel();
        }

        private void SetPanelVisible(bool visible)
        {
            if (visible)
            {
                if (skillManagementCanvasGroup != null)
                    skillManagementCanvasGroup.Show();

                if (skillManagementCanvas != null)
                    skillManagementCanvas.SetActive(true);

                return;
            }

            if (skillManagementCanvasGroup != null)
                skillManagementCanvasGroup.Hide();

            if (skillManagementCanvasGroup == null && skillManagementCanvas != null)
                skillManagementCanvas.SetActive(false);
        }

        private void CancelAllDrags()
        {
            foreach (var item in displayItems)
                item?.ForceResetState();
            service.ClearDraggingSkill();
        }

        #endregion

        #region Input

        private void HandleInput()
        {
            InputManager inputManager = InputManager.Instance;
            if (inputManager == null)
                return;

            if (inputManager.OpenCharacterProgression.WasPressedThisFrame())
                TogglePanel();

            if (inputManager.SkillCancel.WasPressedThisFrame() && service.IsAnySkillDragging())
                CancelAllDrags();
        }

        private void OnEscapeClosePanel(InputAction.CallbackContext _)
        {
            if (!service.IsPanelOpen())
                return;

            HidePanel();
        }

        #endregion

        #region Combat Mode

        private void OnCombatModeChanged(bool isActive)
        {
            if (service.IsPanelOpen()) HidePanel();
        }

        private void OnLevelReached(int level, int count)
        {
            RefreshUnlockedSkills(level);

            if (service.IsPanelOpen())
                PopulateGrid();
        }

        private int GetCurrentPlayerLevel()
        {
            if (cachedStatsPresenter == null)
                cachedStatsPresenter = FindObjectOfType<StatsPresenter>();

            return cachedStatsPresenter != null ? Mathf.Max(1, cachedStatsPresenter.GetLevel()) : 1;
        }

        private void RefreshUnlockedSkills(int? explicitLevel = null)
        {
            if (service == null || !service.IsInitialized())
                return;

            int level = explicitLevel.HasValue ? Mathf.Max(1, explicitLevel.Value) : GetCurrentPlayerLevel();
            service.RefreshForLevel(level);
        }

        #endregion

        #region Public API

        public bool IsPanelOpen()           => service.IsPanelOpen();
        public bool IsAnySkillDragging()    => service.IsAnySkillDragging();
        public SkillData GetDraggingSkill() => service.GetDraggingSkill();

        #endregion

        #region Hover Tooltip

        private void OnSkillHoverEnter(SkillDisplayItemView item, Vector2 screenPosition)
        {
            Debug.Log($"[SkillManagementPresenter] OnSkillHoverEnter received. item={(item != null ? item.name : "null")}, detailView={(skillDetailView != null ? skillDetailView.name : "null")}");

            if (item == null)
                return;

            if (skillDetailView == null)
            {
                TryResolveSkillDetailView();
                if (skillDetailView == null)
                {
                    if (!hasLoggedMissingDetailView)
                    {
                        Debug.LogWarning("[SkillManagementPresenter] skillDetailView is not assigned/found. Assign ItemDetailView in inspector.");
                        hasLoggedMissingDetailView = true;
                    }
                    Debug.LogWarning("[SkillManagementPresenter] Hover enter aborted: no ItemDetailView available.");
                    return;
                }
            }

            SkillData skill = item.GetSkillData();
            if (skill == null)
                return;

            currentHoverItem = item;

            EnsureSkillTooltipRenderOrder();

            if (pendingHoverExitCoroutine != null)
            {
                StopCoroutine(pendingHoverExitCoroutine);
                pendingHoverExitCoroutine = null;
            }

            if (skillDetailView != null)
                skillDetailView.transform.SetAsLastSibling();

            skillDetailView.SetItemDetail(new ItemDetailData
            {
                Icon = skill.skillIcon,
                Name = string.IsNullOrWhiteSpace(skill.skillName) ? "Skill" : skill.skillName,
                NameColor = skill.skillColor,
                Description = string.IsNullOrWhiteSpace(skill.skillDescription) ? "No description." : skill.skillDescription,
                Stats = BuildSkillStatsText(skill),
            });
            skillDetailView.Show();
            skillDetailView.SetPosition(screenPosition);
            Debug.Log($"[SkillManagementPresenter] Tooltip shown for skill: {skill.skillName}");
        }

        private void EnsureSkillTooltipRenderOrder()
        {
            if (skillDetailView == null)
                return;

            Canvas hostCanvas = null;
            if (skillManagementCanvas != null)
                hostCanvas = skillManagementCanvas.GetComponentInParent<Canvas>();

            if (hostCanvas != null && !skillDetailView.transform.IsChildOf(hostCanvas.transform))
            {
                skillDetailView.transform.SetParent(hostCanvas.transform, true);
                Debug.Log($"[SkillManagementPresenter] Reparented tooltip under host canvas: {hostCanvas.name}");
            }

            if (hostCanvas != null)
            {
                if (skillTooltipCanvas == null)
                    skillTooltipCanvas = skillDetailView.GetComponent<Canvas>();
                if (skillTooltipCanvas == null)
                    skillTooltipCanvas = skillDetailView.gameObject.AddComponent<Canvas>();

                skillTooltipCanvas.overrideSorting = true;
                skillTooltipCanvas.sortingLayerID = hostCanvas.sortingLayerID;
                skillTooltipCanvas.sortingOrder = hostCanvas.sortingOrder + 200;

                GraphicRaycaster tooltipRaycaster = skillDetailView.GetComponent<GraphicRaycaster>();
                if (tooltipRaycaster == null)
                    tooltipRaycaster = skillDetailView.gameObject.AddComponent<GraphicRaycaster>();
                tooltipRaycaster.enabled = false;
            }

            skillDetailView.transform.SetAsLastSibling();
        }

        private void TryResolveSkillDetailView()
        {
            if (skillDetailView != null)
                return;

            if (skillManagementCanvas != null)
                skillDetailView = skillManagementCanvas.GetComponentInChildren<ItemDetailView>(true);

            if (skillDetailView != null)
            {
                Debug.Log($"[SkillManagementPresenter] Resolved ItemDetailView from skillManagementCanvas: {skillDetailView.name}");
                return;
            }

            if (skillDetailView == null)
                skillDetailView = GetComponentInChildren<ItemDetailView>(true);

            if (skillDetailView != null)
            {
                Debug.Log($"[SkillManagementPresenter] Resolved ItemDetailView from presenter children: {skillDetailView.name}");
                return;
            }

            skillDetailView = ResolveBestDetailViewFromScene();
            if (skillDetailView != null)
            {
                Debug.Log($"[SkillManagementPresenter] Resolved ItemDetailView via ranked scene search: {GetTransformPath(skillDetailView.transform)}");
                return;
            }

            Debug.LogWarning("[SkillManagementPresenter] Could not resolve ItemDetailView in scene.");
        }

        private ItemDetailView ResolveBestDetailViewFromScene()
        {
            ItemDetailView[] candidates = FindObjectsOfType<ItemDetailView>(true);
            if (candidates == null || candidates.Length == 0)
                return null;

            Transform skillRoot = skillManagementCanvas != null ? skillManagementCanvas.transform : null;
            Canvas skillCanvas = skillManagementCanvas != null ? skillManagementCanvas.GetComponentInParent<Canvas>() : null;

            ItemDetailView best = null;
            int bestScore = int.MinValue;

            foreach (ItemDetailView candidate in candidates)
            {
                if (candidate == null)
                    continue;

                int score = 0;

                if (skillRoot != null && candidate.transform.IsChildOf(skillRoot))
                    score += 100;

                Canvas candidateCanvas = candidate.GetComponentInParent<Canvas>();
                if (skillCanvas != null && candidateCanvas == skillCanvas)
                    score += 50;

                if (candidate.gameObject.activeInHierarchy)
                    score += 20;

                string name = candidate.name;
                if (!string.IsNullOrEmpty(name))
                {
                    if (name.IndexOf("skill", System.StringComparison.OrdinalIgnoreCase) >= 0)
                        score += 15;
                    if (name.IndexOf("inventory", System.StringComparison.OrdinalIgnoreCase) >= 0)
                        score -= 15;
                }

                if (score > bestScore)
                {
                    bestScore = score;
                    best = candidate;
                }
            }

            return best;
        }

        private static string GetTransformPath(Transform t)
        {
            if (t == null)
                return "<null>";

            System.Text.StringBuilder sb = new System.Text.StringBuilder(t.name);
            Transform current = t.parent;
            while (current != null)
            {
                sb.Insert(0, current.name + "/");
                current = current.parent;
            }
            return sb.ToString();
        }

        private void OnSkillHoverExit(SkillDisplayItemView item)
        {
            Debug.Log($"[SkillManagementPresenter] OnSkillHoverExit received. item={(item != null ? item.name : "null")}, current={(currentHoverItem != null ? currentHoverItem.name : "null")}");
            if (currentHoverItem != null && item != currentHoverItem)
                return;

            if (pendingHoverExitCoroutine != null)
                StopCoroutine(pendingHoverExitCoroutine);

            pendingHoverExitCoroutine = StartCoroutine(DeferredHoverExit(item));
        }

        private IEnumerator DeferredHoverExit(SkillDisplayItemView item)
        {
            yield return new WaitForSecondsRealtime(HoverExitGraceSeconds);
            pendingHoverExitCoroutine = null;

            if (item == null)
            {
                HideSkillDetail();
                yield break;
            }

            // Ignore false exit events caused by transient UI overlap around the cursor.
            if (IsPointerInsideItem(item))
                yield break;

            HideSkillDetail();
        }

        private bool IsPointerInsideItem(SkillDisplayItemView item)
        {
            RectTransform rt = item != null ? item.GetComponent<RectTransform>() : null;
            if (rt == null)
                return false;

            Canvas canvas = item.GetComponentInParent<Canvas>();
            Camera cam = null;
            if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
                cam = canvas.worldCamera;

            return RectTransformUtility.RectangleContainsScreenPoint(rt, Input.mousePosition, cam);
        }

        private void HideSkillDetail()
        {
            if (pendingHoverExitCoroutine != null)
            {
                StopCoroutine(pendingHoverExitCoroutine);
                pendingHoverExitCoroutine = null;
            }

            currentHoverItem = null;
            if (skillDetailView != null)
            {
                skillDetailView.Hide();
                Debug.Log("[SkillManagementPresenter] Tooltip hide requested.");
            }
        }

        private void HideSkillDetailImmediate()
        {
            currentHoverItem = null;
            if (skillDetailView != null)
            {
                skillDetailView.HideImmediate();
                Debug.Log("[SkillManagementPresenter] Tooltip hide immediate requested.");
            }
        }

        private static string BuildSkillStatsText(SkillData skill)
        {
            if (skill == null)
                return string.Empty;

            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.AppendLine($"Category: {skill.skillCategory}");
            sb.AppendLine($"Cooldown: {skill.cooldown:0.##}s");
            sb.AppendLine($"Dice: {skill.diceTier}");
            sb.AppendLine($"Multiplier: {skill.skillMultiplier:0.##}x");

            if (skill.IsProjectile)
            {
                sb.AppendLine($"Projectile Speed: {skill.projectileSpeed:0.##}");
                sb.AppendLine($"Projectile Range: {skill.projectileRange:0.##}");
                sb.AppendLine($"Projectile Knockback: {skill.projectileKnockback:0.##}");
            }
            else if (skill.IsSlash)
            {
                sb.AppendLine($"Slash Knockback: {skill.slashKnockbackForce:0.##}");
            }
            else if (skill.IsAoE)
            {
                sb.AppendLine($"Cast Range: {skill.aoeCastRange:0.##}");
                sb.AppendLine($"Radius: {skill.aoeRadius:0.##}");
            }
            else if (skill.IsBuff)
            {
                sb.AppendLine($"Buff Type: {skill.buffSubCategory}");
                sb.AppendLine($"Buff Value: {skill.buffValue:0.##}");
                sb.AppendLine($"Buff Duration: {skill.buffDuration:0.##}s");
            }

            return sb.ToString().TrimEnd();
        }

        #endregion
    }
}
