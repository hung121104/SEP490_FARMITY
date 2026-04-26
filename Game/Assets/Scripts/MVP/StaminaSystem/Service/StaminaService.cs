using UnityEngine;

public class StaminaService : IStaminaService
{
    public void Tick(StaminaModel model, float deltaTime, float gameMinutesDelta, bool canSprint, float sprintCostPerSecond)
    {
        if (model == null || deltaTime <= 0f) return;

        if (gameMinutesDelta > 0f)
        {
            model.viableStamina = Mathf.Max(model.PassiveDecayFloor, model.viableStamina - gameMinutesDelta);
            model.currentStamina = Mathf.Min(model.currentStamina, model.viableStamina);
        }

        if (canSprint && sprintCostPerSecond > 0f)
            TryConsume(model, sprintCostPerSecond * deltaTime, Time.time);

        if (model.regenBoostRemaining > 0f)
        {
            model.regenBoostRemaining = Mathf.Max(0f, model.regenBoostRemaining - deltaTime);
            if (model.regenBoostRemaining <= 0f) model.regenBoostMultiplier = 1f;
        }

        if (model.toolEfficiencyRemaining > 0f)
        {
            model.toolEfficiencyRemaining = Mathf.Max(0f, model.toolEfficiencyRemaining - deltaTime);
            if (model.toolEfficiencyRemaining <= 0f) model.toolEfficiencyReduction = 0f;
        }

        bool canRegen = Time.time - model.lastConsumeTime >= model.regenDelaySeconds;
        if (!canRegen) return;

        float regenPerSecond = model.maxStamina * model.regenPercentPerSecond * Mathf.Max(1f, model.regenBoostMultiplier);
        if (regenPerSecond <= 0f) return;

        float regenAmount = regenPerSecond * deltaTime;
        float regenCap = Mathf.Min(model.viableStamina, model.maxStamina);
        model.currentStamina = Mathf.Min(regenCap, model.currentStamina + regenAmount);
    }

    public bool TryConsume(StaminaModel model, float rawCost, float now)
    {
        if (model == null) return false;
        float clampedCost = Mathf.Max(0f, rawCost);
        float finalCost = clampedCost * (1f - Mathf.Clamp01(model.toolEfficiencyReduction));

        if (finalCost <= 0f) return true;
        if (model.currentStamina + 0.0001f < finalCost) return false;

        model.currentStamina = Mathf.Max(0f, model.currentStamina - finalCost);
        model.lastConsumeTime = now;
        return true;
    }

    public void RestoreViableByConsumable(StaminaModel model, float amount)
    {
        if (model == null || amount <= 0f) return;
        // Never reduce viable below its current value — if already above the soft cap (e.g. after sleep), do nothing.
        float targetViable = Mathf.Max(model.viableStamina, Mathf.Min(model.ConsumableSoftCap, model.viableStamina + amount));
        float gained = Mathf.Max(0f, targetViable - model.viableStamina);
        model.viableStamina = targetViable;
        model.currentStamina = Mathf.Min(model.viableStamina, model.currentStamina + gained);
    }

    public void RestoreBySleep(StaminaModel model)
    {
        if (model == null) return;
        model.viableStamina = model.maxStamina;
        model.currentStamina = model.maxStamina;
    }

    public void ApplyRegenBoost(StaminaModel model, float multiplier, float durationSeconds)
    {
        if (model == null || multiplier <= 1f || durationSeconds <= 0f) return;
        if (multiplier > model.regenBoostMultiplier || model.regenBoostRemaining <= 0f)
            model.regenBoostMultiplier = multiplier;
        model.regenBoostRemaining = Mathf.Max(model.regenBoostRemaining, durationSeconds);
    }

    public void ApplyToolEfficiency(StaminaModel model, float reductionPercent, float durationSeconds)
    {
        if (model == null || reductionPercent <= 0f || durationSeconds <= 0f) return;
        float clampedReduction = Mathf.Clamp(reductionPercent, 0f, 0.95f);
        if (clampedReduction > model.toolEfficiencyReduction || model.toolEfficiencyRemaining <= 0f)
            model.toolEfficiencyReduction = clampedReduction;
        model.toolEfficiencyRemaining = Mathf.Max(model.toolEfficiencyRemaining, durationSeconds);
    }
}
