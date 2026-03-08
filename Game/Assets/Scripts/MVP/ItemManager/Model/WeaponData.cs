/// <summary>Weapon item data. Replaces WeaponDataSO.</summary>
[System.Serializable]
public class WeaponData : ItemData
{
    public int          damage        = 10;
    public int          critChance    = 5;
    public float        attackSpeed   = 1.0f;
    public ToolMaterial weaponMaterial = ToolMaterial.Basic;

    public WeaponData()
    {
        isStackable = false;
        maxStack    = 1;
    }
}

