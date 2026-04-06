namespace CombatManager.Model
{
    /// <summary>
    /// Defines WHO can use this skill and WHERE it can be equipped.
    /// PlayerSkill → hotbar slots 1-4, any weapon can use it.
    /// WeaponSkill → weapon slot only (R key), tied to specific weapon type.
    /// </summary>
    public enum SkillOwnership
    {
        PlayerSkill = 0,    // Equippable in hotbar slots 1-4
        WeaponSkill = 1,    // Only usable via weapon slot (R key)
    }
}