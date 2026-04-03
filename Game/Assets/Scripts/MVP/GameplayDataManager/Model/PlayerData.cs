using System;


[Serializable]
public struct PlayerData
{
    public string _id;
    public string worldId;
    public string accountId;
    public float positionX;
    public float positionY;
    public int sectionIndex;

    // Appearance config IDs (paper-doll layers)
    public string hairConfigId;
    public string outfitConfigId;
    public string hatConfigId;
    public string toolConfigId;

    public float currentStamina;
    public float viableStamina;
    public float currentHealth;

    public float regenBoostMultiplier;
    public float regenBoostRemaining;
    public float toolEfficiencyReduction;
    public float toolEfficiencyRemaining;

    public int level;
    public int currentExp;
    public int expToNextLevel;
    public int baseStrength;
    public int baseVitality;
}