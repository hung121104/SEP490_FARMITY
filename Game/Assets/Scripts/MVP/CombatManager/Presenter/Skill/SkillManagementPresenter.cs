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
    /// No longer depends on SkillDatabasePresenter.
    /// Owns playerSkills list directly via Inspector.
    /// WeaponSkills are NOT shown here - they live in WeaponDataSO.linkedSkill.
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

        [Header("Buttons")]
        [SerializeField] private Button closeButton;

        [Header("Player Skills")]
        [Tooltip("Assign all PlayerSkill SO assets here. WeaponSkills stay in WeaponDataSO.")]
        [SerializeField] private List<SkillData> playerSkills = new List<SkillData>();

        #endregion

        #region Runtime

        private ISkillManagementService service;
        private List<SkillDisplayItemView> displayItems = new List<SkillDisplayItemView>();

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
            SetupCloseButton();

            // ✅ Debug: check list before initialize
            Debug.Log($"[SkillManagementPresenter] playerSkills count: {playerSkills.Count}");
            foreach (var s in playerSkills)
                Debug.Log($"[SkillManagementPresenter] → {s?.skillName} | " +
                          $"ownership={s?.skillOwnership} | IsPlayerSkill={s?.IsPlayerSkill}");

            service.Initialize(playerSkills);
            
            // ✅ Debug: check after filter
            Debug.Log($"[SkillManagementPresenter] After filter: " +
                      $"{service.GetAllSkills().Count} skills");

            PopulateGrid();

            CombatModePresenter.OnCombatModeChanged += OnCombatModeChanged;
            SetPanelVisible(false);

            Debug.Log("[SkillManagementPresenter] Initialized!");
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

            displayItems.Add(view);
        }

        #endregion

        #region Drag Handling

        private void OnSkillBeginDrag(SkillDisplayItemView item)
        {
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
            if (service.IsPanelOpen()) HidePanel();
        }

        #endregion

        #region Public API

        public bool IsPanelOpen()           => service.IsPanelOpen();
        public bool IsAnySkillDragging()    => service.IsAnySkillDragging();
        public SkillData GetDraggingSkill() => service.GetDraggingSkill();

        #endregion
    }
}