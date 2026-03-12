/// <summary>Weapon item data. Replaces WeaponDataSO.</summary>
[System.Serializable]
public class WeaponData : ItemData
{
    public int    damage          = 10;
    public int    critChance      = 5;
    public float  attackSpeed     = 1.0f;
    /// <summary>
    /// References a Material document by materialId (e.g. "mat_steel").
    /// Resolved at runtime via MaterialCatalogService.GetMaterial().
    /// </summary>
    public string weaponMaterialId = "";

    public WeaponData()
    {
        isStackable = false;
        maxStack    = 1;
    }
}

