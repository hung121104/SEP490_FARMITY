/// <summary>Weapon item data used by combat runtime.</summary>
[System.Serializable]
public class WeaponData : ItemData
{
    public int    damage          = 10;
    public int    critChance      = 5;
    /// <summary>
    /// References a Material document by materialId (e.g. "mat_steel").
    /// Resolved at runtime via MaterialCatalogService.GetMaterial().
    /// </summary>
    public string weaponMaterialId = "";

    // Combat runtime fields (database driven)
    public CombatManager.Model.WeaponType weaponType = CombatManager.Model.WeaponType.None;
    public int tier = 1;
    public float attackCooldown = 0.5f;
    public float knockbackForce = 5f;
    public float projectileSpeed = 10f;
    public float projectileRange = 8f;
    public float projectileKnockback = 4f;
    public string linkedSkillId = "";
    public string weaponVisualConfigId = "";
    [System.Obsolete("Deprecated: prefab now resolves by weaponType via WeaponPrefabCatalogService")]
    public string weaponPrefabKey = "";

    public WeaponData()
    {
        isStackable = false;
        maxStack    = 1;
    }

    public string weaponName => itemName;

    public bool IsValid()
    {
        if (weaponType == CombatManager.Model.WeaponType.None)
            return false;

        if (string.IsNullOrWhiteSpace(itemName))
            return false;

        return true;
    }

    public float GetAttackCooldownSafe()
    {
        if (attackCooldown > 0f)
            return attackCooldown;

        return 0.5f;
    }
}

