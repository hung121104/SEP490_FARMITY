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

        [Header("Pre-assigned Skills (Optional)")]
        [SerializeField] private SkillData[] initialSkills = new SkillData[4];

        #endregion

        #region Runtime - Skill Management (merged from SkillManagerPresenter)

        // ✅ Merged from SkillManagerPresenter
        private SkillData[] equippedSkillsData = new SkillData[4];
        private SkillPresenterBase[] equippedSkillsComponents = new SkillPresenterBase[4];
        private Dictionary<string, SkillPresenterBase> skillComponentsByName
            = new Dictionary<string, SkillPresenterBase>();

        #endregion

        #region Runtime - Hotbar

        private ISkillHotbarService service;
        private List<SkillHotbarSlotView> slots = new List<SkillHotbarSlotView>();
        private WeaponSkillSlotView weaponSkillSlotView;

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

            StartCoroutine(DelayedSkillSetup());

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

        #region Skill Setup (merged from SkillManagerPresenter)

        private IEnumerator DelayedSkillSetup()
        {
            // Wait for all SkillPresenters to Start()
            yield return new WaitForSeconds(0.6f);

            CacheAllSkillComponents();
            LinkInitialSkills();
            RefreshAllSlots();

            Debug.Log("[SkillHotbarPresenter] Skill setup complete!");
        }

        /// <summary>
        /// Find all SkillPresenter components in scene.
        /// Merged from SkillManagerPresenter.CacheAllSkillComponents()
        /// </summary>
        private void CacheAllSkillComponents()
        {
            skillComponentsByName.Clear();

            // Search under parent (CombatSystem) same as before
            Transform root = transform.parent != null ? transform.parent : transform;
            SkillPresenter[] allSkills = root.GetComponentsInChildren<SkillPresenter>(true);

            foreach (SkillPresenter skill in allSkills)
            {
                string componentName = skill.GetType().Name;
                if (!skillComponentsByName.ContainsKey(componentName))
                {
                    skillComponentsByName[componentName] = skill;
                    Debug.Log($"[SkillHotbarPresenter] Cached skill: {componentName}");
                }
                else
                {
                    Debug.LogWarning($"[SkillHotbarPresenter] Duplicate skill component: {componentName}");
                }
            }

            Debug.Log($"[SkillHotbarPresenter] Cached {skillComponentsByName.Count} skill components");
        }

        /// <summary>
        /// Link pre-assigned skills from Inspector.
        /// Merged from SkillManagerPresenter.LinkInitialSkills()
        /// </summary>
        private void LinkInitialSkills()
        {
            for (int i = 0; i < initialSkills.Length && i < equippedSkillsData.Length; i++)
            {
                if (initialSkills[i] != null)
                {
                    EquipSkill(i, initialSkills[i]);
                    Debug.Log($"[SkillHotbarPresenter] Linked initial skill slot {i}: {initialSkills[i].skillName}");
                }
            }
        }

        #endregion

        #region Slot Spawning

        private void SpawnSlots()
        {
            if (skillHotbarContainer == null)
            {
                Debug.LogError("[SkillHotbarPresenter] skillHotbarContainer not assigned!");
                return;
            }

            if (skillHotbarSlotPrefab == null)
            {
                Debug.LogError("[SkillHotbarPresenter] skillHotbarSlotPrefab not assigned!");
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
                Debug.Log($"[SkillHotbarPresenter] Slot {i} spawned");
            }
        }

        private void SpawnWeaponSkillSlot()
        {
            if (weaponSkillSection == null)
            {
                Debug.LogError("[SkillHotbarPresenter] weaponSkillSection not assigned!");
                return;
            }

            if (weaponSkillSlotPrefab == null)
            {
                Debug.LogError("[SkillHotbarPresenter] weaponSkillSlotPrefab not assigned!");
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

            SkillPresenterBase skill = equippedSkillsComponents[slotIndex];
            if (skill == null)
            {
                Debug.LogWarning($"[SkillHotbarPresenter] No component for slot {slotIndex}!");
                return;
            }

            skill.TriggerSkill();
            Debug.Log($"[SkillHotbarPresenter] Triggered slot {slotIndex}: " +
                      $"{equippedSkillsData[slotIndex]?.skillName}");
        }

        #endregion

        #region Skill Equipment (merged from SkillManagerPresenter)

        public void EquipSkill(int slotIndex, SkillData skillData)
        {
            if (!IsSlotIndexValid(slotIndex)) return;

            equippedSkillsData[slotIndex] = skillData;

            if (skillData != null)
            {
                string componentName = skillData.linkedComponentName;
                if (skillComponentsByName.TryGetValue(componentName, out SkillPresenterBase component))
                {
                    equippedSkillsComponents[slotIndex] = component;
                    Debug.Log($"[SkillHotbarPresenter] Equipped '{skillData.skillName}' → slot {slotIndex}");
                }
                else
                {
                    equippedSkillsComponents[slotIndex] = null;
                    Debug.LogWarning($"[SkillHotbarPresenter] Component '{componentName}' not found!");
                }
            }
            else
            {
                equippedSkillsComponents[slotIndex] = null;
            }
        }

        public void UnequipSkill(int slotIndex)
        {
            if (!IsSlotIndexValid(slotIndex)) return;
            equippedSkillsData[slotIndex] = null;
            equippedSkillsComponents[slotIndex] = null;
            Debug.Log($"[SkillHotbarPresenter] Unequipped slot {slotIndex}");
        }

        public void SwapSkills(int slotA, int slotB)
        {
            if (!IsSlotIndexValid(slotA) || !IsSlotIndexValid(slotB)) return;

            SkillData tempData = equippedSkillsData[slotA];
            equippedSkillsData[slotA] = equippedSkillsData[slotB];
            equippedSkillsData[slotB] = tempData;

            SkillPresenterBase tempComp = equippedSkillsComponents[slotA];
            equippedSkillsComponents[slotA] = equippedSkillsComponents[slotB];
            equippedSkillsComponents[slotB] = tempComp;

            Debug.Log($"[SkillHotbarPresenter] Swapped slot {slotA} ↔ slot {slotB}");
        }

        #endregion

        #region Slot Events

        private void OnSkillDroppedOnSlot(int slotIndex, SkillData skillData)
        {
            EquipSkill(slotIndex, skillData);
            RefreshSlot(slotIndex);
            Debug.Log($"[SkillHotbarPresenter] Dropped '{skillData.skillName}' → slot {slotIndex}");
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

        private void OnCombatModeChanged(bool isActive)
        {
            SetHotbarVisible(isActive);
        }

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
                SkillPresenterBase skill = equippedSkillsComponents[i];
                if (skill == null) { slots[i].UpdateCooldownFill(0f); continue; }

                SkillPresenter sp = skill as SkillPresenter;
                if (sp == null) { slots[i].UpdateCooldownFill(0f); continue; }

                float fill = sp.IsCoolingDown() ? 1f - sp.GetCooldownPercent() : 0f;
                slots[i].UpdateCooldownFill(fill);
            }
        }

        #endregion

        #region Weapon Skill Slot

        private void SubscribeToWeaponEvents()
        {
            WeaponEquipPresenter.OnWeaponEquipped += OnWeaponEquipped;
            WeaponEquipPresenter.OnWeaponUnequipped += OnWeaponUnequipped;
        }

        private void UnsubscribeFromWeaponEvents()
        {
            WeaponEquipPresenter.OnWeaponEquipped -= OnWeaponEquipped;
            WeaponEquipPresenter.OnWeaponUnequipped -= OnWeaponUnequipped;
        }

        private void OnWeaponEquipped(WeaponDataSO weaponData)
        {
            if (weaponSkillSlotView == null) return;

            if (weaponData.linkedSkill == null)
            {
                Debug.LogWarning($"[SkillHotbarPresenter] {weaponData.weaponName} has no linked skill!");
                weaponSkillSlotView.SetEmpty();
                return;
            }

            weaponSkillSlotView.SetSkill(weaponData.linkedSkill.skillIcon);
            weaponSkillSlotView.SetVisible(true);
        }

        private void OnWeaponUnequipped()
        {
            if (weaponSkillSlotView == null) return;
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

            var weaponType = WeaponEquipPresenter.Instance.GetCurrentWeaponType();

            switch (weaponType)
            {
                case CombatManager.Model.WeaponType.Sword:
                    WeaponSkillSwordSpecial.Instance?.TryExecute();
                    break;
                case CombatManager.Model.WeaponType.Spear:
                    WeaponSkillSpearSpecial.Instance?.TryExecute();
                    break;
                case CombatManager.Model.WeaponType.Staff:
                    WeaponSkillStaffSpecial.Instance?.TryExecute();
                    break;
                default:
                    Debug.LogWarning($"[SkillHotbarPresenter] No weapon skill for: {weaponType}");
                    break;
            }
        }

        private void UpdateWeaponSkillCooldown()
        {
            if (weaponSkillSlotView == null) return;

            var weaponType = WeaponEquipPresenter.Instance?.GetCurrentWeaponType()
                             ?? CombatManager.Model.WeaponType.None;

            float cooldownPercent = 0f;
            switch (weaponType)
            {
                case CombatManager.Model.WeaponType.Sword:
                    cooldownPercent = WeaponSkillSwordSpecial.Instance?.GetCooldownPercent() ?? 0f;
                    break;
                case CombatManager.Model.WeaponType.Spear:
                    cooldownPercent = WeaponSkillSpearSpecial.Instance?.GetCooldownPercent() ?? 0f;
                    break;
                case CombatManager.Model.WeaponType.Staff:
                    cooldownPercent = WeaponSkillStaffSpecial.Instance?.GetCooldownPercent() ?? 0f;
                    break;
            }

            weaponSkillSlotView.UpdateCooldown(cooldownPercent);
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

        public int GetHoveredSlotIndex() => service.GetHoveredSlotIndex();
        public bool IsInitialized() => service.IsInitialized();
        public bool IsSlotEmpty(int slotIndex) => equippedSkillsData[slotIndex] == null;
        public SkillData GetSkillData(int slotIndex) => equippedSkillsData[slotIndex];
        public SkillPresenterBase GetSkillComponent(int slotIndex) => equippedSkillsComponents[slotIndex];
        public int GetSlotCount() => equippedSkillsData.Length;

        #endregion
    }
}