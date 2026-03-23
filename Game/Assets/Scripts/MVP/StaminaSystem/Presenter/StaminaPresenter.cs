public class StaminaPresenter
{
    private readonly IStaminaService service;
    private readonly StaminaModel model;

    public StaminaPresenter(StaminaModel model, IStaminaService service)
    {
        this.model = model;
        this.service = service;
    }

    public float CurrentStamina => model.currentStamina;
    public float ViableStamina => model.viableStamina;
    public float MaxStamina => model.maxStamina;

    public void Tick(float deltaTime, float gameMinutesDelta, bool canSprint, float sprintCostPerSecond)
        => service.Tick(model, deltaTime, gameMinutesDelta, canSprint, sprintCostPerSecond);

    public bool TryConsume(float cost)
        => service.TryConsume(model, cost, UnityEngine.Time.time);

    public void RestoreViableByConsumable(float amount)
        => service.RestoreViableByConsumable(model, amount);

    public void RestoreBySleep()
        => service.RestoreBySleep(model);

    public void ApplyRegenBoost(float multiplier, float durationSeconds)
        => service.ApplyRegenBoost(model, multiplier, durationSeconds);

    public void ApplyToolEfficiency(float reductionPercent, float durationSeconds)
        => service.ApplyToolEfficiency(model, reductionPercent, durationSeconds);

    public void SetState(float current, float viable)
    {
        model.currentStamina = UnityEngine.Mathf.Clamp(current, 0f, model.maxStamina);
        model.viableStamina = UnityEngine.Mathf.Clamp(viable, model.PassiveDecayFloor, model.maxStamina);
        model.currentStamina = UnityEngine.Mathf.Min(model.currentStamina, model.viableStamina);
    }
}
