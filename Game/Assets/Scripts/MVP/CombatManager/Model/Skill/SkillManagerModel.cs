using UnityEngine;
using System.Collections.Generic;
using CombatManager.SO;

namespace CombatManager.Model
{
    /// <summary>
    /// Data model for SkillManager.
    /// Mirrors SkillManager from CombatSystem (kept for legacy).
    /// Holds equipped skill data + component references.
    /// </summary>
    [System.Serializable]
    public class SkillManagerModel
    {
        [Header("Equipped Skills (Start Empty)")]
        public SkillData[] equippedSkillsData = new SkillData[4];

        [Header("State")]
        public bool isInitialized = false;

        // Runtime - not serialized
        [System.NonSerialized]
        public SkillPresenterBase[] equippedSkillsComponents = new SkillPresenterBase[4];

        [System.NonSerialized]
        public Dictionary<string, SkillPresenterBase> skillComponentsByName
            = new Dictionary<string, SkillPresenterBase>();

        public int SlotCount => equippedSkillsData.Length;
    }

    /// <summary>
    /// Wrapper to avoid direct dependency on SkillPresenter in model layer.
    /// </summary>
    public abstract class SkillPresenterBase : UnityEngine.MonoBehaviour
    {
        public abstract void TriggerSkill();
        public abstract bool IsExecuting { get; }
        public abstract bool IsCoolingDown();
        public abstract float GetCooldownPercent();
    }
}