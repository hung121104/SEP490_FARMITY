public interface IStaminaService
{
    void Tick(StaminaModel model, float deltaTime, float gameMinutesDelta, bool canSprint, float sprintCostPerSecond);
    bool TryConsume(StaminaModel model, float rawCost, float now);
    void RestoreViableByConsumable(StaminaModel model, float amount);
    void RestoreBySleep(StaminaModel model);
    void ApplyRegenBoost(StaminaModel model, float multiplier, float durationSeconds);
    void ApplyToolEfficiency(StaminaModel model, float reductionPercent, float durationSeconds);
}
