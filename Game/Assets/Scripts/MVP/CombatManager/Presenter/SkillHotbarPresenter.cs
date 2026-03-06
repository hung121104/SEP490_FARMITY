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
    /// Presenter for SkillHotbar system.
    /// Manages: slot spawning, skill equip/unequip/swap,
    /// hotkey triggers, combat mode show/hide.
    /// NO script on Canvas - only on manager GameObject.
    /// </summary>
    public class SkillHotbarPresenter : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Model")]
        [SerializeField] private SkillHotbarModel model = new SkillHotbarModel();

        [Header("Canvas Reference")]
        [SerializeField] private GameObject skillHotbarCanvas;

        [Header("Slot Container")]
        [SerializeField] private Transform skillHotbarContainer;

        [Header("Prefab")]
        [SerializeField] private GameObject skillHotbarSlotPrefab;

        #endregion

        #region Runtime

        private ISkillHotbarService service;
        private List<SkillHotbarSlotView> slots = new List<SkillHotbarSlotView>();

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
            service.Initialize();
            RefreshAllSlots();

            // Subscribe to combat mode
            CombatModePresenter.OnCombatModeChanged += OnCombatModeChanged;

            // Start hidden (combat mode starts off)
            SetHotbarVisible(false);

            Debug.Log("[SkillHotbarPresenter] Initialized!");
        }

        private void Update()
        {
            HandleHotkeyInput();
        }

        private void OnDestroy()
        {
            CombatModePresenter.OnCombatModeChanged -= OnCombatModeChanged;
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

                // Hook up slot events
                slotView.OnDroppedOnSlot     += OnSkillDroppedOnSlot;
                slotView.OnSlotSwapRequested += OnSlotSwapRequested;
                slotView.OnSlotUnequipRequested += OnSlotUnequipRequested;
                slotView.OnSlotHoverEnter    += OnSlotHoverEnter;
                slotView.OnSlotHoverExit     += OnSlotHoverExit;

                slots.Add(slotView);
                Debug.Log($"[SkillHotbarPresenter] Slot {i} spawned");
            }
        }

        #endregion

        #region Hotkey Input

        private void HandleHotkeyInput()
        {
            if (!CombatModePresenter.Instance?.IsCombatModeActive() ?? true) return;

            for (int i = 0; i < model.activationKeys.Length && i < slots.Count; i++)
            {
                if (Input.GetKeyDown(model.activationKeys[i]))
                {
                    TriggerSlot(i);
                }
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

        private void OnSlotHoverEnter(int slotIndex)
        {
            service.SetHoveredSlot(slotIndex);
        }

        private void OnSlotHoverExit(int slotIndex)
        {
            service.ClearHoveredSlot();
        }

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

        #region Public API

        public int GetHoveredSlotIndex() => service.GetHoveredSlotIndex();

        public bool IsInitialized() => service.IsInitialized();

        #endregion
    }
}