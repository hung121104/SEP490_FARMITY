using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using CombatManager.Model;
using CombatManager.Service;
using CombatManager.SO;

namespace CombatManager.Presenter
{
    /// <summary>
    /// Presenter for SkillManager system.
    /// Mirrors SkillManager from CombatSystem (kept for legacy).
    /// Links SkillData (from SkillDatabase) to SkillPresenter components in scene.
    /// Provides equipped skill info to SkillHotbar (future).
    /// </summary>
    public class SkillManagerPresenter : MonoBehaviour
    {
        [Header("Model")]
        [SerializeField] private SkillManagerModel model = new SkillManagerModel();

        private ISkillManagerService service;

        #region Singleton

        public static SkillManagerPresenter Instance { get; private set; }

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

            service = new SkillManagerService(model);
        }

        private void Start()
        {
            StartCoroutine(DelayedInitialization());
        }

        #endregion

        #region Initialization

        private IEnumerator DelayedInitialization()
        {
            // Wait for SkillDatabasePresenter to be ready
            for (int attempts = 0; attempts < 50; attempts++)
            {
                if (SkillDatabasePresenter.Instance != null
                    && SkillDatabasePresenter.Instance.IsInitialized())
                {
                    CacheAllSkillComponents();
                    LinkInitialSkills();
                    service.Initialize();
                    Debug.Log("[SkillManagerPresenter] Initialized!");
                    yield break;
                }
                yield return null;
            }

            Debug.LogError("[SkillManagerPresenter] Failed - SkillDatabasePresenter not found!");
        }

        /// <summary>
        /// Cache all SkillPresenter components found under CombatSystem parent.
        /// Mirrors CacheAllSkillComponents() from old SkillManager.
        /// </summary>
        private void CacheAllSkillComponents()
        {
            model.skillComponentsByName.Clear();

            // Search under parent (CombatSystem)
            Transform root = transform.parent != null ? transform.parent : transform;
            SkillPresenter[] allSkills = root.GetComponentsInChildren<SkillPresenter>(true);

            foreach (SkillPresenter skill in allSkills)
            {
                string componentName = skill.GetType().Name;

                if (!model.skillComponentsByName.ContainsKey(componentName))
                {
                    // ✅ SkillPresenter extends SkillPresenterBase
                    model.skillComponentsByName[componentName] = skill;
                    Debug.Log($"[SkillManagerPresenter] Cached: {componentName}");
                }
                else
                {
                    Debug.LogWarning($"[SkillManagerPresenter] Duplicate component: {componentName}");
                }
            }

            Debug.Log($"[SkillManagerPresenter] Cached {model.skillComponentsByName.Count} skill components");
        }

        /// <summary>
        /// Link skills that were pre-assigned in Inspector.
        /// Same as LinkInitialSkills() from old SkillManager.
        /// </summary>
        private void LinkInitialSkills()
        {
            for (int i = 0; i < model.equippedSkillsData.Length; i++)
            {
                if (model.equippedSkillsData[i] != null)
                {
                    service.EquipSkill(i, model.equippedSkillsData[i]);
                    Debug.Log($"[SkillManagerPresenter] Linked initial skill slot {i}: {model.equippedSkillsData[i].skillName}");
                }
            }
        }

        #endregion

        #region Public API - Equipment

        /// <summary>
        /// Equip a skill to a slot.
        /// Called by SkillHotbar on drag-drop.
        /// </summary>
        public void EquipSkill(int slotIndex, SkillData skillData)
        {
            service.EquipSkill(slotIndex, skillData);
        }

        /// <summary>
        /// Unequip skill from slot.
        /// Called by SkillHotbar on drag-out.
        /// </summary>
        public void UnequipSkill(int slotIndex)
        {
            service.UnequipSkill(slotIndex);
        }

        /// <summary>
        /// Swap two skill slots.
        /// Called by SkillHotbar on drag between slots.
        /// </summary>
        public void SwapSkills(int slotA, int slotB)
        {
            service.SwapSkills(slotA, slotB);
        }

        #endregion

        #region Public API - Trigger

        /// <summary>
        /// Trigger skill at slot index.
        /// Called by SkillHotbar on hotkey press (1/2/3/4).
        /// </summary>
        public void TriggerSkill(int slotIndex)
        {
            if (!service.IsInitialized())
            {
                Debug.LogWarning("[SkillManagerPresenter] Not initialized yet!");
                return;
            }

            SkillPresenterBase skill = service.GetSkillComponent(slotIndex);
            if (skill == null)
            {
                Debug.LogWarning($"[SkillManagerPresenter] No skill in slot {slotIndex}!");
                return;
            }

            skill.TriggerSkill();
            Debug.Log($"[SkillManagerPresenter] Triggered slot {slotIndex}: {service.GetSkillData(slotIndex)?.skillName}");
        }

        #endregion

        #region Public API - Queries

        public SkillData GetSkillData(int slotIndex)
            => service.GetSkillData(slotIndex);

        public SkillPresenterBase GetSkillComponent(int slotIndex)
            => service.GetSkillComponent(slotIndex);

        public int GetSlotCount()
            => service.GetSlotCount();

        public bool IsSlotEmpty(int slotIndex)
            => service.IsSlotEmpty(slotIndex);

        public bool IsInitialized()
            => service?.IsInitialized() ?? false;

        #endregion
    }
}