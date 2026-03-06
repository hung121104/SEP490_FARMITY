using UnityEngine;
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
    /// Mirrors SkillHotbarUI + SkillHotbarSlot from CombatSystem (kept for legacy).
    /// Manages: slot display, hotkey trigger, drag-drop equip/swap/unequip.
    /// NO script on Canvas - only on manager GameObject.
    /// </summary>
    public class SkillHotbarPresenter : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Model")]
        [SerializeField] private SkillHotbarModel model = new SkillHotbarModel();

        [Header("Canvas Reference")]
        [SerializeField] private GameObject skillHotbarCanvas;

        [Header("Slot Views - Assign in Inspector")]
        [SerializeField] private SkillHotbarSlotView[] slotViews = new SkillHotbarSlotView[4];

        #endregion

        #region Runtime

        private ISkillHotbarService service;
        private int hoveredSlotIndex = -1;

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
            StartCoroutine(DelayedInitialize());

            // Subscribe to combat mode
            CombatModePresenter.OnCombatModeChanged += OnCombatModeChanged;

            // Start hidden
            SetHotbarVisible(false);
        }

        private void Update()
        {
            if (!service.IsInitialized()) return;
            HandleHotkeyInput();
            UpdateCooldownVisuals();
        }

        private void OnDestroy()
        {
            CombatModePresenter.OnCombatModeChanged -= OnCombatModeChanged;
        }

        #endregion

        #region Initialization

        private IEnumerator DelayedInitialize()
        {
            for (int i = 0; i < 50; i++)
            {
                if (SkillManagerPresenter.Instance != null
                    && SkillManagerPresenter.Instance.IsInitialized())
                {
                    InitializeSlots();
                    service.Initialize();
                    Debug.Log("[SkillHotbarPresenter] Initialized!");
                    yield break;
                }
                yield return null;
            }

            Debug.LogError("[SkillHotbarPresenter] SkillManagerPresenter not found!");
        }

        private void InitializeSlots()
        {
            for (int i = 0; i < slotViews.Length; i++)
            {
                if (slotViews[i] == null)
                {
                    Debug.LogWarning($"[SkillHotbarPresenter] SlotView {i} not assigned!");
                    continue;
                }

                KeyCode key = i < model.activationKeys.Length
                    ? model.activationKeys[i]
                    : KeyCode.Alpha1;

                slotViews[i].Initialize(i, key, model);

                // Hook up events
                int capturedIndex = i;
                slotViews[i].OnDropFromPanelEvent += OnDropFromPanel;
                slotViews[i].OnDropFromSlotEvent  += OnDropFromSlot;
                slotViews[i].OnUnequipEvent        += OnSlotUnequip;
                slotViews[i].OnBeginDragEvent      += OnSlotBeginDrag;
                slotViews[i].OnPointerEnterEvent   += OnSlotPointerEnter;
                slotViews[i].OnPointerExitEvent    += OnSlotPointerExit;

                Debug.Log($"[SkillHotbarPresenter] Slot {i} initialized");
            }

            RefreshAllSlots();
            Debug.Log("[SkillHotbarPresenter] All slots refreshed");
        }

        #endregion

        #region Hotkey Input

        private void HandleHotkeyInput()
        {
            // Block skill trigger when management panel is open
            if (SkillManagementPresenter.Instance != null
                && SkillManagementPresenter.Instance.IsPanelOpen())
                return;

            for (int i = 0; i < model.activationKeys.Length; i++)
            {
                if (Input.GetKeyDown(model.activationKeys[i]))
                {
                    TriggerSlot(i);
                    break;
                }
            }
        }

        private void TriggerSlot(int slotIndex)
        {
            SkillManagerPresenter.Instance?.TriggerSkill(slotIndex);
        }

        #endregion

        #region Cooldown Visuals

        private void UpdateCooldownVisuals()
        {
            for (int i = 0; i < slotViews.Length; i++)
            {
                if (slotViews[i] == null) continue;

                SkillPresenterBase skillComp =
                    SkillManagerPresenter.Instance?.GetSkillComponent(i);

                if (skillComp != null)
                    slotViews[i].UpdateCooldown(skillComp.GetCooldownPercent());
                else
                    slotViews[i].UpdateCooldown(1f);
            }
        }

        #endregion

        #region Drop Handlers

        private void OnDropFromPanel(SkillHotbarSlotView targetSlot, SkillData skillData)
        {
            if (skillData == null) return;

            int idx = targetSlot.GetSlotIndex();
            SkillManagerPresenter.Instance?.EquipSkill(idx, skillData);
            RefreshSlot(idx);

            Debug.Log($"[SkillHotbarPresenter] Equipped '{skillData.skillName}' → slot {idx}");
        }

        private void OnDropFromSlot(SkillHotbarSlotView targetSlot, SkillHotbarSlotView sourceSlot)
        {
            int targetIdx = targetSlot.GetSlotIndex();
            int sourceIdx = sourceSlot.GetSlotIndex();

            SkillManagerPresenter.Instance?.SwapSkills(sourceIdx, targetIdx);

            // Return source slot to original position
            sourceSlot.ForceReturnToPosition();

            RefreshSlot(targetIdx);
            RefreshSlot(sourceIdx);

            Debug.Log($"[SkillHotbarPresenter] Swapped slot {sourceIdx} ↔ slot {targetIdx}");
        }

        private void OnSlotUnequip(SkillHotbarSlotView slot)
        {
            int idx = slot.GetSlotIndex();
            SkillManagerPresenter.Instance?.UnequipSkill(idx);

            // Return slot to original position
            slot.ForceReturnToPosition();
            RefreshSlot(idx);

            Debug.Log($"[SkillHotbarPresenter] Unequipped slot {idx}");
        }

        private void OnSlotBeginDrag(SkillHotbarSlotView slot)
        {
            Debug.Log($"[SkillHotbarPresenter] Begin drag slot {slot.GetSlotIndex()}");
        }

        #endregion

        #region Hover Tracking (for SkillManagementPresenter)

        private void OnSlotPointerEnter(SkillHotbarSlotView slot)
        {
            hoveredSlotIndex = slot.GetSlotIndex();
            slot.SetHighlight(true, model);
        }

        private void OnSlotPointerExit(SkillHotbarSlotView slot)
        {
            if (hoveredSlotIndex == slot.GetSlotIndex())
                hoveredSlotIndex = -1;

            slot.SetHighlight(false, model);
        }

        #endregion

        #region Refresh

        public void RefreshSlot(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= slotViews.Length) return;
            if (slotViews[slotIndex] == null) return;

            SkillData skillData = SkillManagerPresenter.Instance?.GetSkillData(slotIndex);
            slotViews[slotIndex].RefreshDisplay(skillData, model);
        }

        private void RefreshAllSlots()
        {
            for (int i = 0; i < slotViews.Length; i++)
                RefreshSlot(i);
        }

        #endregion

        #region Visibility

        private void SetHotbarVisible(bool visible)
        {
            service.SetVisible(visible);
            if (skillHotbarCanvas != null)
                skillHotbarCanvas.SetActive(visible);
        }

        private void OnCombatModeChanged(bool isActive)
        {
            SetHotbarVisible(isActive);
            Debug.Log($"[SkillHotbarPresenter] Combat mode: {isActive} → hotbar visible: {isActive}");
        }

        #endregion

        #region Public API

        /// <summary>
        /// Returns index of slot currently hovered by mouse.
        /// Called by SkillManagementPresenter on drag drop.
        /// </summary>
        public int GetHoveredSlotIndex() => hoveredSlotIndex;

        public bool IsInitialized() => service.IsInitialized();

        #endregion
    }
}