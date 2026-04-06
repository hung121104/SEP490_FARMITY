using UnityEngine;

[System.Serializable]
public class StaminaModel
{
    public float maxStamina = 200f;
    public float passiveDecayFloorPercent = 0.5f;
    public float consumableSoftCapPercent = 0.8f;
    public float regenDelaySeconds = 0.5f;
    public float regenPercentPerSecond = 0.1f;

    public float currentStamina;
    public float viableStamina;

    public float lastConsumeTime;
    public float regenBoostMultiplier = 1f;
    public float regenBoostRemaining;

    public float toolEfficiencyReduction;
    public float toolEfficiencyRemaining;

    public bool sprintIntent;

    public float PassiveDecayFloor => maxStamina * passiveDecayFloorPercent;
    public float ConsumableSoftCap => maxStamina * consumableSoftCapPercent;
}
