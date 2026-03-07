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

        #endregion

        #region Runtime

        private ISkillHotbarService service;
        private List<SkillHotbarSlotView> slots = new List<SkillHotbarSlotView>();

        // ✅ Spawned at runtime, not assigned in Inspector
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
            SpawnWeaponSkillSlot(); // ✅ NEW
            service.Initialize();
            RefreshAllSlots();

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
                    Debug.LogError($"[SkillHotbarPresenter] SkillHotbarSlotView missing on prefab!");
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

        // ✅ NEW: Spawn weapon skill slot into WeaponSkillSection
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
            if (SkillManagerPresenter.Instance == null) return;
            if (SkillManagerPresenter.Instance.IsSlotEmpty(slotIndex))
            {
                Debug.Log($"[SkillHotbarPresenter] Slot {slotIndex} is empty!");
                return;
            }

            SkillManagerPresenter.Instance.TriggerSkill(slotIndex);
            Debug.Log($"[SkillHotbarPresenter] Triggered slot {slotIndex}");
        }

        #endregion

        #region Slot Events

        private void OnSkillDroppedOnSlot(int slotIndex, SkillData skillData)
        {
            if (SkillManagerPresenter.Instance == null) return;
            SkillManagerPresenter.Instance.EquipSkill(slotIndex, skillData);
            RefreshSlot(slotIndex);
            Debug.Log($"[SkillHotbarPresenter] Equipped '{skillData.skillName}' → slot {slotIndex}");
        }

        private void OnSlotSwapRequested(int slotA, int slotB)
        {
            if (SkillManagerPresenter.Instance == null) return;
            SkillManagerPresenter.Instance.SwapSkills(slotA, slotB);
            RefreshSlot(slotA);
            RefreshSlot(slotB);
            Debug.Log($"[SkillHotbarPresenter] Swapped slot {slotA} ↔ slot {slotB}");
        }

        private void OnSlotUnequipRequested(int slotIndex)
        {
            if (SkillManagerPresenter.Instance == null) return;
            SkillManagerPresenter.Instance.UnequipSkill(slotIndex);
            RefreshSlot(slotIndex);
            Debug.Log($"[SkillHotbarPresenter] Unequipped slot {slotIndex}");
        }

        private void OnSlotHoverEnter(int slotIndex) => service.SetHoveredSlot(slotIndex);
        private void OnSlotHoverExit(int slotIndex) => service.ClearHoveredSlot();

        #endregion

        #region Refresh

        public void RefreshSlot(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= slots.Count) return;
            SkillData skillData = SkillManagerPresenter.Instance?.GetSkillData(slotIndex);
            slots[slotIndex].RefreshDisplay(skillData);
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
            Debug.Log($"[SkillHotbarPresenter] Combat mode: {isActive} → Hotbar visible: {isActive}");
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
            if (SkillManagerPresenter.Instance == null) return;

            for (int i = 0; i < slots.Count; i++)
            {
                SkillPresenterBase baseSkill = SkillManagerPresenter.Instance.GetSkillComponent(i);
                if (baseSkill == null) { slots[i].UpdateCooldownFill(0f); continue; }

                SkillPresenter skill = baseSkill as SkillPresenter;
                if (skill == null) { slots[i].UpdateCooldownFill(0f); continue; }

                float fill = skill.IsCoolingDown() ? 1f - skill.GetCooldownPercent() : 0f;
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
            Debug.Log($"[SkillHotbarPresenter] Weapon skill slot loaded: {weaponData.linkedSkill.skillName}");
        }

        private void OnWeaponUnequipped()
        {
            if (weaponSkillSlotView == null) return;
            weaponSkillSlotView.SetEmpty();
            weaponSkillSlotView.SetVisible(false);
            Debug.Log("[SkillHotbarPresenter] Weapon skill slot cleared");
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

                // ✅ Uncommented - Staff skill ready!
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

                // ✅ Uncommented - Staff cooldown ready!
                case CombatManager.Model.WeaponType.Staff:
                    cooldownPercent = WeaponSkillStaffSpecial.Instance?.GetCooldownPercent() ?? 0f;
                    break;
            }

            weaponSkillSlotView.UpdateCooldown(cooldownPercent);
        }

        #endregion

        #region Public API

        public int GetHoveredSlotIndex() => service.GetHoveredSlotIndex();
        public bool IsInitialized() => service.IsInitialized();

        #endregion
    }
}