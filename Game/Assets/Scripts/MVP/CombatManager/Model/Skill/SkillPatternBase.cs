using UnityEngine;

namespace CombatManager.Model
{
    /// <summary>
    /// Base class for all Skill Pattern Presenters.
    /// Defines the common interface for SkillHotbar to trigger/query skills.
    /// Pattern = press → charge → roll → wait confirm/cancel → execute or cancel
    /// All skills (Projectile, Slash, AoE etc) extend SkillPatternPresenter
    /// which extends this base.
    /// </summary>
    public abstract class SkillPatternBase : MonoBehaviour
    {
        public abstract void TriggerSkill();
        public abstract bool IsExecuting { get; }
        public abstract bool IsCoolingDown();
        public abstract float GetCooldownPercent();
    }
}