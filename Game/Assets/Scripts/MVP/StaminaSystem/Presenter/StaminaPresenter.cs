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

    public float RegenBoostRemaining     => model.regenBoostRemaining;
    public float RegenBoostMultiplier    => model.regenBoostMultiplier;
    public float ToolEfficiencyRemaining => model.toolEfficiencyRemaining;
    public float ToolEfficiencyReduction => model.toolEfficiencyReduction;

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

    /// <summary>
    /// Directly overwrites boost model state from the authoritative server sync.
    /// Bypasses the "take the strongest" guards in ApplyRegenBoost/ApplyToolEfficiency
    /// so that expired boosts (remaining = 0) are correctly cleared on clients.
    /// </summary>
    public void SyncBoostState(float regenMultiplier, float regenRemaining, float effReduction, float effRemaining)
    {
        model.regenBoostMultiplier    = regenMultiplier;
        model.regenBoostRemaining     = UnityEngine.Mathf.Max(0f, regenRemaining);
        model.toolEfficiencyReduction = effReduction;
        model.toolEfficiencyRemaining = UnityEngine.Mathf.Max(0f, effRemaining);
        if (model.regenBoostRemaining <= 0f) model.regenBoostMultiplier = 1f;
        if (model.toolEfficiencyRemaining <= 0f) model.toolEfficiencyReduction = 0f;
    }

    public void SetState(float current, float viable)
    {
        model.currentStamina = UnityEngine.Mathf.Clamp(current, 0f, model.maxStamina);
        model.viableStamina = UnityEngine.Mathf.Clamp(viable, model.PassiveDecayFloor, model.maxStamina);
        model.currentStamina = UnityEngine.Mathf.Min(model.currentStamina, model.viableStamina);
    }
}
