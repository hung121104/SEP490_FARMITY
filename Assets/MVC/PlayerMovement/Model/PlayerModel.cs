using System;
using UnityEngine;

namespace MVC.Model
{
    /// <summary>
    /// PlayerModel - pure data/state for the player (no Unity dependencies preferred, but Vector2 used for convenience)
    /// </summary>
    public class PlayerModel
    {
        // Position and movement
        public Vector2 Position { get; set; }
        public Vector2 Velocity { get; set; }
        
        // Stamina
        public float CurrentStamina { get; private set; }
        public float MaxStamina { get; private set; }
        
        // Configuration
        public float MoveSpeed { get; set; }
        public float StaminaDrainRate { get; set; }
        public float StaminaRegenRate { get; set; }
        public float StaminaRegenDelay { get; set; }
        
        // Internal state
        private float timeSinceLastDrain;

        public PlayerModel(float maxStamina = 100f, float moveSpeed = 5f, float drainRate = 10f, float regenRate = 12f, float regenDelay = 1f)
        {
            MaxStamina = maxStamina;
            CurrentStamina = maxStamina;
            MoveSpeed = moveSpeed;
            StaminaDrainRate = drainRate;
            StaminaRegenRate = regenRate;
            StaminaRegenDelay = regenDelay;
            timeSinceLastDrain = 0f;
            Position = Vector2.zero;
            Velocity = Vector2.zero;
        }

        public void DrainStamina(float amount)
        {
            if (amount <= 0f) return;
            CurrentStamina = Mathf.Max(0f, CurrentStamina - amount);
            timeSinceLastDrain = 0f;
        }

        public void RegenerateStamina(float deltaTime)
        {
            timeSinceLastDrain += deltaTime;
            if (timeSinceLastDrain >= StaminaRegenDelay && CurrentStamina < MaxStamina)
            {
                CurrentStamina = Mathf.Min(MaxStamina, CurrentStamina + StaminaRegenRate * deltaTime);
            }
        }

        public void SetStamina(float value)
        {
            CurrentStamina = Mathf.Clamp(value, 0f, MaxStamina);
        }

        public float StaminaNormalized => MaxStamina > 0f ? CurrentStamina / MaxStamina : 0f;

        public bool IsStaminaEmpty => CurrentStamina <= 0f;

        // Events for observers (View can subscribe)
        public event Action<Vector2> OnPositionChanged;
        public event Action<float> OnStaminaChanged;
        public event Action OnStaminaDepleted;

        public void NotifyPositionChanged()
        {
            OnPositionChanged?.Invoke(Position);
        }

        public void NotifyStaminaChanged()
        {
            OnStaminaChanged?.Invoke(CurrentStamina);
            if (IsStaminaEmpty)
                OnStaminaDepleted?.Invoke();
        }
    }
}
