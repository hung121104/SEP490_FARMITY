using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using CombatManager.Model;
using CombatManager.Service;
using CombatManager.View;
using CombatManager.SO;

namespace CombatManager.Presenter
{
    public class SkillHotbarPresenter : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Model")]
        [SerializeField] private SkillHotbarModel model = new SkillHotbarModel();

        [Header("Canvas Reference")]
        [SerializeField] private GameObject skillHotbarCanvas;

        [Header("Player Skill Slots")]
        [SerializeField] private Transform skillHotbarContainer;
        [SerializeField] private GameObject skillHotbarSlotPrefab;

        [Header("Weapon Skill Slot")]
        [SerializeField] private Transform weaponSkillSection;
        [SerializeField] private GameObject weaponSkillSlotPrefab;
        [SerializeField] private KeyCode weaponSkillKey = KeyCode.R;

        [Header("Default Skill IDs (Optional)")]
        [SerializeField] private string[] defaultSkillIds = new string[4];

        #endregion

        #region Runtime - Skill Management

        private SkillData[] equippedSkillsData = new SkillData[4];

        // ✅ No longer store component references by name
        // Instead find presenter by SkillCategory at trigger time

        #endregion

        #region Runtime - Hotbar

        private ISkillHotbarService service;
        private List<SkillHotbarSlotView> slots = new List<SkillHotbarSlotView>();
        private WeaponSkillSlotView weaponSkillSlotView;

        // ✅ Current weapon skill data (set when weapon equipped)
        private SkillData currentWeaponSkillData;

        #endregion

        #region Singleton

        public static SkillHotbarPresenter Instance { get; private set; }

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
            service = new SkillHotbarService(model);
        }

        private void Start()
        {
            SpawnSlots();
            SpawnWeaponSkillSlot();
            service.Initialize();

            StartCoroutine(InitializeSkillsFromCatalog());

            CombatModePresenter.OnCombatModeChanged += OnCombatModeChanged;
            SetHotbarVisible(false);
            SubscribeToWeaponEvents();

            Debug.Log("[SkillHotbarPresenter] Initialized!");
        }

        private void Update()
        {
            HandleHotkeyInput();
            UpdateCooldownFills();
            HandleWeaponSkillInput();
            UpdateWeaponSkillCooldown();
        }

        private void OnDestroy()
        {
            CombatModePresenter.OnCombatModeChanged -= OnCombatModeChanged;
            UnsubscribeFromWeaponEvents();
        }

        #endregion

        #region Skill Setup

        private IEnumerator InitializeSkillsFromCatalog()
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
                Debug.LogWarning("[SkillHotbarPresenter] CombatSkillCatalogService unavailable. No skills loaded.");
                yield break;
            }

            LinkDefaultSkills();
            RefreshAllSlots();
            Debug.Log("[SkillHotbarPresenter] Skill setup complete!");
        }

        private void LinkDefaultSkills()
        {
            for (int i = 0; i < defaultSkillIds.Length && i < equippedSkillsData.Length; i++)
            {
                string skillId = defaultSkillIds[i];
                if (string.IsNullOrWhiteSpace(skillId))
                {
                    continue;
                }

                SkillData skill = CombatSkillCatalogService.Instance.GetSkillById(skillId);
                if (skill == null)
                {
                    Debug.LogWarning($"[SkillHotbarPresenter] Skill ID '{skillId}' not found in catalog.");
                    continue;
                }

                EquipSkill(i, skill);
                Debug.Log($"[SkillHotbarPresenter] Default skill slot {i}: {skill.skillName}");
            }
        }

        #endregion

        #region Slot Spawning

        private void SpawnSlots()
        {
            if (skillHotbarContainer == null || skillHotbarSlotPrefab == null)
            {
                Debug.LogError("[SkillHotbarPresenter] Container or prefab not assigned!");
                return;
            }

            slots.Clear();

            for (int i = 0; i < model.slotCount; i++)
            {
                GameObject slotGO = Instantiate(skillHotbarSlotPrefab, skillHotbarContainer);
                slotGO.name = $"SkillSlot_{i + 1}";

                SkillHotbarSlotView slotView = slotGO.GetComponent<SkillHotbarSlotView>();
                if (slotView == null)
                {
                    Debug.LogError("[SkillHotbarPresenter] SkillHotbarSlotView missing on prefab!");
                    Destroy(slotGO);
                    continue;
                }

                slotView.Initialize(i, model);
                slotView.OnDroppedOnSlot        += OnSkillDroppedOnSlot;
                slotView.OnSlotSwapRequested    += OnSlotSwapRequested;
                slotView.OnSlotUnequipRequested += OnSlotUnequipRequested;
                slotView.OnSlotHoverEnter       += OnSlotHoverEnter;
                slotView.OnSlotHoverExit        += OnSlotHoverExit;

                slots.Add(slotView);
            }

            Debug.Log($"[SkillHotbarPresenter] {slots.Count} slots spawned");
        }

        private void SpawnWeaponSkillSlot()
        {
            if (weaponSkillSection == null || weaponSkillSlotPrefab == null)
            {
                Debug.LogError("[SkillHotbarPresenter] Weapon skill section or prefab not assigned!");
                return;
            }

            GameObject slotGO = Instantiate(weaponSkillSlotPrefab, weaponSkillSection);
            slotGO.name = "WeaponSkillSlot";

            weaponSkillSlotView = slotGO.GetComponent<WeaponSkillSlotView>();
            if (weaponSkillSlotView == null)
            {
                Debug.LogError("[SkillHotbarPresenter] WeaponSkillSlotView missing on prefab!");
                Destroy(slotGO);
                return;
            }

            weaponSkillSlotView.SetEmpty();
            weaponSkillSlotView.SetVisible(false);
            Debug.Log("[SkillHotbarPresenter] Weapon skill slot spawned!");
        }

        #endregion

        #region Hotkey Input

        private void HandleHotkeyInput()
        {
            if (!CombatModePresenter.Instance?.IsCombatModeActive() ?? true) return;

            for (int i = 0; i < model.activationKeys.Length && i < slots.Count; i++)
            {
                if (Input.GetKeyDown(model.activationKeys[i]))
                    TriggerSlot(i);
            }
        }

        private void TriggerSlot(int slotIndex)
        {
            if (IsSlotEmpty(slotIndex))
            {
                Debug.Log($"[SkillHotbarPresenter] Slot {slotIndex} is empty!");
                return;
            }

            SkillData skillData = equippedSkillsData[slotIndex];

            // ✅ Guard: weapon skills cannot be triggered from hotbar slots
            if (skillData.IsWeaponSkill)
            {
                Debug.LogWarning($"[SkillHotbarPresenter] " +
                                 $"'{skillData.skillName}' is a WeaponSkill - use R key!");
                return;
            }

            SkillPatternPresenter presenter = GetPresenterByCategory(skillData.skillCategory);
            if (presenter == null)
            {
                Debug.LogWarning($"[SkillHotbarPresenter] " +
                                 $"No presenter found for category: {skillData.skillCategory}");
                return;
            }

            // ✅ Set data THEN trigger
            SetPresenterData(presenter, skillData);
            presenter.TriggerSkill();

            Debug.Log($"[SkillHotbarPresenter] Triggered slot {slotIndex}: {skillData.skillName}");
        }

        #endregion

        #region Skill Equipment

        public void EquipSkill(int slotIndex, SkillData skillData)
        {
            if (!IsSlotIndexValid(slotIndex)) return;

            // ✅ Guard: weapon skills cannot be equipped to hotbar slots
            if (skillData != null && skillData.IsWeaponSkill)
            {
                Debug.LogWarning($"[SkillHotbarPresenter] " +
                                 $"'{skillData.skillName}' is a WeaponSkill - " +
                                 $"cannot equip to hotbar slot!");
                return;
            }

            equippedSkillsData[slotIndex] = skillData;
            Debug.Log($"[SkillHotbarPresenter] Equipped '{skillData?.skillName}' → slot {slotIndex}");
        }

        public void UnequipSkill(int slotIndex)
        {
            if (!IsSlotIndexValid(slotIndex)) return;
            equippedSkillsData[slotIndex] = null;
            Debug.Log($"[SkillHotbarPresenter] Unequipped slot {slotIndex}");
        }

        public void SwapSkills(int slotA, int slotB)
        {
            if (!IsSlotIndexValid(slotA) || !IsSlotIndexValid(slotB)) return;

            SkillData temp = equippedSkillsData[slotA];
            equippedSkillsData[slotA] = equippedSkillsData[slotB];
            equippedSkillsData[slotB] = temp;

            Debug.Log($"[SkillHotbarPresenter] Swapped slot {slotA} ↔ slot {slotB}");
        }

        #endregion

        #region Presenter Resolution by Category

        /// <summary>
        /// Find the correct presenter for a SkillCategory.
        /// No string matching - enum based! ✅
        /// </summary>
        private SkillPatternPresenter GetPresenterByCategory(SkillCategory category)
        {
            switch (category)
            {
                case SkillCategory.Projectile:
                    return ProjectileSkillPresenter.Instance;
                case SkillCategory.Slash:
                    return SlashSkillPresenter.Instance;
                default:
                    Debug.LogWarning($"[SkillHotbarPresenter] " +
                                     $"No presenter registered for: {category}");
                    return null;
            }
        }

        /// <summary>
        /// Pass SkillData to the correct presenter before triggering.
        /// </summary>
        private void SetPresenterData(SkillPatternPresenter presenter, SkillData skillData)
        {
            switch (presenter)
            {
                case ProjectileSkillPresenter p:
                    p.SetSkillData(skillData);
                    break;
                case SlashSkillPresenter s:
                    s.SetSkillData(skillData);
                    break;
            }
        }

        #endregion

        #region Slot Events

        private void OnSkillDroppedOnSlot(int slotIndex, SkillData skillData)
        {
            // ✅ Guard: weapon skills cannot be dropped on hotbar slots
            if (skillData != null && skillData.IsWeaponSkill)
            {
                Debug.LogWarning($"[SkillHotbarPresenter] " +
                                 $"'{skillData.skillName}' is a WeaponSkill - cannot drop here!");
                return;
            }

            EquipSkill(slotIndex, skillData);
            RefreshSlot(slotIndex);
        }

        private void OnSlotSwapRequested(int slotA, int slotB)
        {
            SwapSkills(slotA, slotB);
            RefreshSlot(slotA);
            RefreshSlot(slotB);
        }

        private void OnSlotUnequipRequested(int slotIndex)
        {
            UnequipSkill(slotIndex);
            RefreshSlot(slotIndex);
        }

        private void OnSlotHoverEnter(int slotIndex) => service.SetHoveredSlot(slotIndex);
        private void OnSlotHoverExit(int slotIndex) => service.ClearHoveredSlot();

        #endregion

        #region Refresh

        public void RefreshSlot(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= slots.Count) return;
            slots[slotIndex].RefreshDisplay(equippedSkillsData[slotIndex]);
        }

        public void RefreshAllSlots()
        {
            for (int i = 0; i < slots.Count; i++)
                RefreshSlot(i);
            Debug.Log("[SkillHotbarPresenter] All slots refreshed");
        }

        #endregion

        #region Combat Mode

        private void OnCombatModeChanged(bool isActive) => SetHotbarVisible(isActive);

        private void SetHotbarVisible(bool visible)
        {
            if (skillHotbarCanvas != null)
                skillHotbarCanvas.SetActive(visible);
        }

        #endregion

        #region Cooldown Fill

        private void UpdateCooldownFills()
        {
            for (int i = 0; i < slots.Count; i++)
            {
                SkillData skillData = equippedSkillsData[i];
                if (skillData == null) { slots[i].UpdateCooldownFill(0f); continue; }

                SkillPatternPresenter presenter = 
                    GetPresenterByCategory(skillData.skillCategory);
                if (presenter == null) { slots[i].UpdateCooldownFill(0f); continue; }

                SkillData presenterData = GetPresenterCurrentData(presenter);
                bool isThisSkill = presenterData == skillData;

                float fill = (isThisSkill && presenter.IsCoolingDown())
                    ? 1f - presenter.GetCooldownPercent()
                    : 0f;

                slots[i].UpdateCooldownFill(fill);
            }
        }

        private SkillData GetPresenterCurrentData(SkillPatternPresenter presenter)
        {
            switch (presenter)
            {
                case ProjectileSkillPresenter p: return p.GetCurrentSkillData();
                case SlashSkillPresenter s:      return s.GetCurrentSkillData();
                default:                         return null;
            }
        }

        #endregion

        #region Weapon Skill Slot

        private void SubscribeToWeaponEvents()
        {
            WeaponEquipPresenter.OnWeaponEquipped   += OnWeaponEquipped;
            WeaponEquipPresenter.OnWeaponUnequipped += OnWeaponUnequipped;
        }

        private void UnsubscribeFromWeaponEvents()
        {
            WeaponEquipPresenter.OnWeaponEquipped   -= OnWeaponEquipped;
            WeaponEquipPresenter.OnWeaponUnequipped -= OnWeaponUnequipped;
        }

        private void OnWeaponEquipped(WeaponDataSO weaponData)
        {
            if (weaponSkillSlotView == null) return;

            if (CombatSkillCatalogService.Instance == null || !CombatSkillCatalogService.Instance.IsReady)
            {
                Debug.LogWarning("[SkillHotbarPresenter] CombatSkillCatalogService not ready for weapon skill resolution.");
                weaponSkillSlotView.SetEmpty();
                currentWeaponSkillData = null;
                return;
            }

            if (string.IsNullOrWhiteSpace(weaponData.linkedSkillId))
            {
                Debug.LogWarning($"[SkillHotbarPresenter] " +
                                 $"{weaponData.weaponName} has no linked skill!");
                weaponSkillSlotView.SetEmpty();
                currentWeaponSkillData = null;
                return;
            }

            currentWeaponSkillData = CombatSkillCatalogService.Instance.GetSkillById(weaponData.linkedSkillId);
            if (currentWeaponSkillData == null)
            {
                Debug.LogWarning($"[SkillHotbarPresenter] Linked skill '{weaponData.linkedSkillId}' not found for {weaponData.weaponName}.");
                weaponSkillSlotView.SetEmpty();
                return;
            }

            weaponSkillSlotView.SetSkill(currentWeaponSkillData.skillIcon);
            weaponSkillSlotView.SetVisible(true);

            Debug.Log($"[SkillHotbarPresenter] Weapon skill set: " +
                      $"{currentWeaponSkillData.skillName}");
        }

        private void OnWeaponUnequipped()
        {
            if (weaponSkillSlotView == null) return;
            currentWeaponSkillData = null;
            weaponSkillSlotView.SetEmpty();
            weaponSkillSlotView.SetVisible(false);
        }

        private void HandleWeaponSkillInput()
        {
            if (!Input.GetKeyDown(weaponSkillKey)) return;
            if (CombatModePresenter.Instance == null ||
                !CombatModePresenter.Instance.IsCombatModeActive()) return;
            if (WeaponEquipPresenter.Instance == null ||
                !WeaponEquipPresenter.Instance.IsWeaponEquipped()) return;
            if (currentWeaponSkillData == null)
            {
                Debug.LogWarning("[SkillHotbarPresenter] No weapon skill data!");
                return;
            }

            SkillPatternPresenter presenter =
                GetPresenterByCategory(currentWeaponSkillData.skillCategory);

            if (presenter == null)
            {
                Debug.LogWarning($"[SkillHotbarPresenter] No presenter for weapon skill: " +
                                 $"{currentWeaponSkillData.skillName}");
                return;
            }

            SetPresenterData(presenter, currentWeaponSkillData);
            presenter.TriggerSkill();

            Debug.Log($"[SkillHotbarPresenter] Weapon skill triggered: " +
                      $"{currentWeaponSkillData.skillName}");
        }

        private void UpdateWeaponSkillCooldown()
        {
            if (weaponSkillSlotView == null || currentWeaponSkillData == null) return;

            SkillPatternPresenter presenter =
                GetPresenterByCategory(currentWeaponSkillData.skillCategory);

            if (presenter == null)
            {
                weaponSkillSlotView.UpdateCooldown(0f);
                return;
            }

            // ✅ Only show cooldown if THIS weapon skill is the one cooling down
            SkillData presenterData = GetPresenterCurrentData(presenter);
            bool isThisSkill = presenterData == currentWeaponSkillData;

            float fill = (isThisSkill && presenter.IsCoolingDown())
                ? 1f - presenter.GetCooldownPercent()
                : 0f;

            weaponSkillSlotView.UpdateCooldown(fill);
        }

        #endregion

        #region Helpers

        private bool IsSlotIndexValid(int index)
        {
            if (index < 0 || index >= equippedSkillsData.Length)
            {
                Debug.LogWarning($"[SkillHotbarPresenter] Invalid slot index: {index}");
                return false;
            }
            return true;
        }

        #endregion

        #region Public API

        public int GetHoveredSlotIndex()    => service.GetHoveredSlotIndex();
        public bool IsInitialized()         => service.IsInitialized();
        public bool IsSlotEmpty(int index)  => equippedSkillsData[index] == null;
        public SkillData GetSkillData(int index) => equippedSkillsData[index];
        public int GetSlotCount()           => equippedSkillsData.Length;

        // ✅ Kept for SkillManagementPresenter compatibility
        public SkillPatternBase GetSkillComponent(int slotIndex)
        {
            SkillData skillData = equippedSkillsData[slotIndex];
            if (skillData == null) return null;
            return GetPresenterByCategory(skillData.skillCategory);
        }

        #endregion
    }
}