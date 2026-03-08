using UnityEngine;

namespace CombatManager.Model
{
    /// <summary>
    /// Base class for all Skill Presenters.
    /// Provides common interface for SkillHotbar to trigger/query skills.
    /// Extracted from SkillManagerModel - lives in its own file.
    /// </summary>
    public abstract class SkillPresenterBase : MonoBehaviour
    {
        public abstract void TriggerSkill();
        public abstract bool IsExecuting { get; }
        public abstract bool IsCoolingDown();
        public abstract float GetCooldownPercent();
    }
}