using System;
using UnityEngine;

public class PlayerModel
{
    #region Core Stats

    // Health
    public float CurrentHealth { get; private set; }
    public float MaxHealth { get; private set; }

    // Stamina
    public float CurrentStamina { get; private set; }
    public float MaxStamina { get; private set; }

    #endregion

    #region Currency & Resources

    public int Gold { get; private set; }

    #endregion

    #region Movement Stats

    public float MoveSpeed { get; private set; }
    public Vector2 Position { get; private set; }

    #endregion

    #region Events

    // Health events
    public event Action<float, float> OnHealthChanged;
    public event Action OnHealthDepleted;

    // Stamina events
    public event Action<float, float> OnStaminaChanged;
    public event Action OnStaminaDepleted;

    // Currency events
    public event Action<int> OnGoldChanged;

    // Movement events
    public event Action<Vector2> OnPositionChanged;

    #endregion

    #region Constructor

    /// <summary>
    /// Initialize with default values
    /// </summary>
    public PlayerModel()
    {
        MaxHealth = 100f;
        CurrentHealth = MaxHealth;

        MaxStamina = 100f;
        CurrentStamina = MaxStamina;

        Gold = 0;

        MoveSpeed = 5f;
        Position = Vector2.zero;
    }

    /// <summary>
    /// Initialize with custom values
    /// </summary>
    public PlayerModel(float health, float stamina, int gold)
    {
        MaxHealth = health;
        CurrentHealth = health;

        MaxStamina = stamina;
        CurrentStamina = stamina;

        Gold = gold;

        MoveSpeed = 5f;
        Position = Vector2.zero;
    }

    #endregion

    #region Health Methods

    /// <summary>
    /// Take damage - reduce health
    /// </summary>
    public void TakeDamage(float damage)
    {
        if (damage <= 0) return;

        CurrentHealth = Mathf.Max(0, CurrentHealth - damage);
        CurrentHealth = Mathf.Min(CurrentHealth, MaxHealth);

        OnHealthChanged?.Invoke(CurrentHealth, MaxHealth);

        if (CurrentHealth <= 0)
        {
            OnHealthDepleted?.Invoke();
        }
    }

    /// <summary>
    /// Heal player
    /// </summary>
    public void Heal(float amount)
    {
        if (amount <= 0) return;

        CurrentHealth = Mathf.Min(MaxHealth, CurrentHealth + amount);
        OnHealthChanged?.Invoke(CurrentHealth, MaxHealth);
    }

    /// <summary>
    /// Set max health (for upgrades)
    /// </summary>
    public void SetMaxHealth(float value)
    {
        MaxHealth = Mathf.Max(0, value);
        CurrentHealth = Mathf.Min(CurrentHealth, MaxHealth);
        OnHealthChanged?.Invoke(CurrentHealth, MaxHealth);
    }

    /// <summary>
    /// Restore health to full
    /// </summary>
    public void RestoreHealthFull()
    {
        CurrentHealth = MaxHealth;
        OnHealthChanged?.Invoke(CurrentHealth, MaxHealth);
    }

    #endregion

    #region Stamina Methods

    /// <summary>
    /// Drain stamina (for tool usage, running, etc.)
    /// </summary>
    public void DrainStamina(float amount)
    {
        if (amount <= 0) return;

        CurrentStamina = Mathf.Max(0, CurrentStamina - amount);
        OnStaminaChanged?.Invoke(CurrentStamina, MaxStamina);

        if (CurrentStamina <= 0)
        {
            OnStaminaDepleted?.Invoke();
        }
    }

    /// <summary>
    /// Regenerate stamina
    /// </summary>
    public void RegenerateStamina(float amount)
    {
        if (amount <= 0) return;

        CurrentStamina = Mathf.Min(MaxStamina, CurrentStamina + amount);
        OnStaminaChanged?.Invoke(CurrentStamina, MaxStamina);
    }

    /// <summary>
    /// Set max stamina (for upgrades)
    /// </summary>
    public void SetMaxStamina(float value)
    {
        MaxStamina = Mathf.Max(0, value);
        CurrentStamina = Mathf.Min(CurrentStamina, MaxStamina);
        OnStaminaChanged?.Invoke(CurrentStamina, MaxStamina);
    }

    /// <summary>
    /// Restore stamina to full
    /// </summary>
    public void RestoreStaminaFull()
    {
        CurrentStamina = MaxStamina;
        OnStaminaChanged?.Invoke(CurrentStamina, MaxStamina);
    }

    /// <summary>
    /// Notify stamina changed (for external updates)
    /// </summary>
    public void NotifyStaminaChanged()
    {
        OnStaminaChanged?.Invoke(CurrentStamina, MaxStamina);
    }

    #endregion

    #region Currency Methods

    /// <summary>
    /// Add gold
    /// </summary>
    public void AddGold(int amount)
    {
        if (amount <= 0) return;

        Gold += amount;
        OnGoldChanged?.Invoke(Gold);
    }

    /// <summary>
    /// Deduct gold (returns true if successful)
    /// </summary>
    public bool DeductGold(int amount)
    {
        if (amount <= 0) return false;
        if (Gold < amount) return false;

        Gold -= amount;
        OnGoldChanged?.Invoke(Gold);
        return true;
    }

    /// <summary>
    /// Check if player has enough gold
    /// </summary>
    public bool HasGold(int amount)
    {
        return Gold >= amount;
    }

    #endregion

    #region Movement Methods

    /// <summary>
    /// Update player position
    /// </summary>
    public void SetPosition(Vector2 position)
    {
        Position = position;
        OnPositionChanged?.Invoke(Position);
    }

    /// <summary>
    /// Set move speed (for upgrades, buffs, etc.)
    /// </summary>
    public void SetMoveSpeed(float speed)
    {
        MoveSpeed = Mathf.Max(0, speed);
    }

    #endregion

    #region Utility Methods

    /// <summary>
    /// Check if player is alive
    /// </summary>
    public bool IsAlive()
    {
        return CurrentHealth > 0;
    }

    /// <summary>
    /// Get health percentage (0-1)
    /// </summary>
    public float GetHealthPercent()
    {
        return MaxHealth > 0 ? CurrentHealth / MaxHealth : 0;
    }

    /// <summary>
    /// Get stamina percentage (0-1)
    /// </summary>
    public float GetStaminaPercent()
    {
        return MaxStamina > 0 ? CurrentStamina / MaxStamina : 0;
    }

    #endregion
}
