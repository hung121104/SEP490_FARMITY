using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using CombatManager.Model;
using CombatManager.Service;
using CombatManager.View;
using CombatManager.SO;

namespace CombatManager.Presenter
{
    /// <summary>
    /// Presenter for SkillManagement panel.
    /// Mirrors SkillManagementPanel from CombatSystem (kept for legacy).
    /// Manages: panel open/close, skill grid population,
    /// drag preview, drag-to-hotbar equip.
    /// NO script on Canvas - only on manager GameObject.
    /// </summary>
    public class SkillManagementPresenter : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Model")]
        [SerializeField] private SkillManagementModel model = new SkillManagementModel();

        [Header("Canvas Reference")]
        [SerializeField] private GameObject skillManagementCanvas;

        [Header("Grid")]
        [SerializeField] private Transform skillGridContainer;

        [Header("Prefabs")]
        [SerializeField] private GameObject skillDisplayItemPrefab;
        [SerializeField] private GameObject skillDragPreviewPrefab;

        [Header("Buttons")]
        [SerializeField] private Button closeButton;

        #endregion

        #region Runtime

        private ISkillManagementService service;
        private List<SkillDisplayItemView> displayItems = new List<SkillDisplayItemView>();

        // Drag preview image that follows mouse
        private RectTransform dragPreviewRect;
        private Image dragPreviewImage;
        private Canvas rootCanvas;

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
        }

        private void Start()
        {
            rootCanvas = GetComponentInParent<Canvas>();
            SetupCloseButton();
            SpawnDragPreview();
            StartCoroutine(DelayedInitialize());

            // Subscribe to CombatMode
            CombatModePresenter.OnCombatModeChanged += OnCombatModeChanged;

            // Start hidden
            SetPanelVisible(false);
        }

        private void Update()
        {
            HandleInput();
        }

        private void OnDestroy()
        {
            CombatModePresenter.OnCombatModeChanged -= OnCombatModeChanged;
        }

        #endregion

        #region Initialization

        private IEnumerator DelayedInitialize()
        {
            // Wait for SkillDatabasePresenter
            for (int i = 0; i < 50; i++)
            {
                if (SkillDatabasePresenter.Instance != null
                    && SkillDatabasePresenter.Instance.IsInitialized())
                {
                    List<SkillData> allSkills = SkillDatabasePresenter.Instance.GetAllSkills();
                    service.Initialize(allSkills);
                    PopulateGrid();
                    Debug.Log("[SkillManagementPresenter] Initialized!");
                    yield break;
                }
                yield return null;
            }

            Debug.LogError("[SkillManagementPresenter] SkillDatabasePresenter not found!");
        }

        private void SetupCloseButton()
        {
            if (closeButton != null)
            {
                closeButton.onClick.RemoveAllListeners();
                closeButton.onClick.AddListener(HidePanel);
                Debug.Log("[SkillManagementPresenter] Close button setup");
            }
        }

        private void SpawnDragPreview()
        {
            // ✅ No longer needed - item itself moves during drag
            // Preview prefab kept in Inspector but not used
            Debug.Log("[SkillManagementPresenter] Drag handled by item itself (no preview needed)");
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

            // Clear old items
            displayItems.Clear();
            foreach (Transform child in skillGridContainer)
                Destroy(child.gameObject);

            // Spawn one item per skill
            foreach (SkillData skill in service.GetAllSkills())
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
                Debug.LogError($"[SkillManagementPresenter] SkillDisplayItemView missing on prefab!");
                Destroy(itemGO);
                return;
            }

            view.Initialize(skillData);

            // Hook up drag events
            view.OnBeginDragEvent += OnSkillBeginDrag;
            view.OnDragEvent      += OnSkillDrag;
            view.OnEndDragEvent   += OnSkillEndDrag;
            view.OnSelectEvent    += OnSkillSelected;

            displayItems.Add(view);
        }

        #endregion

        #region Drag Handling

        private void OnSkillBeginDrag(SkillDisplayItemView item)
        {
            service.SetDraggingSkill(item.GetSkillData());
            Debug.Log($"[SkillManagementPresenter] Begin drag: {item.GetSkillData().skillName}");
        }

        private void OnSkillDrag(SkillDisplayItemView item)
        {
            // Item moves itself - nothing needed here
        }

        private void OnSkillEndDrag(SkillDisplayItemView item)
        {
            TryDropOnHotbar(item.GetSkillData());
            service.ClearDraggingSkill();
            Debug.Log($"[SkillManagementPresenter] End drag: {item.GetSkillData().skillName}");
        }

        private void TryDropOnHotbar(SkillData skillData)
        {
            if (SkillHotbarPresenter.Instance == null) return;

            // ✅ Guard: weapon skills cannot be dragged to hotbar
            if (skillData != null && skillData.IsWeaponSkill)
            {
                Debug.LogWarning($"[SkillManagementPresenter] " +
                                 $"'{skillData.skillName}' is a WeaponSkill - " +
                                 $"cannot drag to hotbar!");
                return;
            }

            int hoveredSlot = SkillHotbarPresenter.Instance.GetHoveredSlotIndex();
            if (hoveredSlot < 0) return;

            SkillHotbarPresenter.Instance.EquipSkill(hoveredSlot, skillData);
            SkillHotbarPresenter.Instance.RefreshSlot(hoveredSlot);

            Debug.Log($"[SkillManagementPresenter] " +
                      $"Dropped '{skillData?.skillName}' → Hotbar slot {hoveredSlot}");
        }

        #endregion

        #region Select Handling

        private void OnSkillSelected(SkillDisplayItemView item)
        {
            // ✅ Guard: weapon skills cannot be auto-equipped to hotbar
            if (item.GetSkillData().IsWeaponSkill)
            {
                Debug.LogWarning($"[SkillManagementPresenter] " +
                                 $"'{item.GetSkillData().skillName}' is a WeaponSkill - " +
                                 $"cannot equip to hotbar!");
                return;
            }

            int slotCount = SkillHotbarPresenter.Instance?.GetSlotCount() ?? 0;
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
            service.OpenPanel();
            SetPanelVisible(true);
        }

        public void HidePanel()
        {
            CancelAllDrags();
            service.ClosePanel();
            SetPanelVisible(false);
        }

        public void TogglePanel()
        {
            if (service.IsPanelOpen()) HidePanel();
            else ShowPanel();
        }

        private void SetPanelVisible(bool visible)
        {
            if (skillManagementCanvas != null)
                skillManagementCanvas.SetActive(visible);
        }

        private void CancelAllDrags()
        {
            foreach (var item in displayItems)
                item?.ForceResetState();

            if (dragPreviewRect != null)
                dragPreviewRect.gameObject.SetActive(false);

            service.ClearDraggingSkill();
        }

        #endregion

        #region Input

        private void HandleInput()
        {
            if (Input.GetKeyDown(model.toggleKey))
                TogglePanel();

            if (Input.GetKeyDown(KeyCode.Escape) && service.IsAnySkillDragging())
                CancelAllDrags();
        }

        #endregion

        #region Combat Mode

        private void OnCombatModeChanged(bool isActive)
        {
            // Hide panel when combat mode changes
            if (service.IsPanelOpen())
                HidePanel();

            Debug.Log($"[SkillManagementPresenter] Combat mode changed: {isActive}");
        }

        #endregion

        #region Public API

        public bool IsPanelOpen() => service.IsPanelOpen();
        public bool IsAnySkillDragging() => service.IsAnySkillDragging();
        public SkillData GetDraggingSkill() => service.GetDraggingSkill();

        #endregion
    }
}