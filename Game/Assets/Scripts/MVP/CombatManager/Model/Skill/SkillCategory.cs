namespace CombatManager.Model
{
    /// <summary>
    /// Defines what TYPE of execution logic a skill uses.
    /// ProjectileSkillPresenter handles Projectile.
    /// SlashSkillPresenter handles Slash.
    /// One presenter script per category - data drives behavior.
    /// </summary>
    public enum SkillCategory
    {
        None        = 0,
        Projectile  = 1,    // Fires a projectile (AirSlash, StaffSpecial)
        Slash       = 2,    // Spawns melee VFX hitbox (SwordSpecial, SpearSpecial)
        AoE         = 3,    // Future: area burst
        Buff        = 4,    // Future: self/team buff
        Summon      = 5,    // Future: summon entity
    }
}