using UnityEngine;
using MVC.Model;
using MVC.View;

namespace MVC.Controller
{
    /// <summary>
    /// PlayerController - orchestrates game logic, updates Model based on input from View
    /// </summary>
    public class PlayerController
    {
        private readonly PlayerModel model;
        private readonly PlayerView view;

        public PlayerController(PlayerModel model, PlayerView view)
        {
            this.model = model;
            this.view = view;

            // Subscribe to model events to update view
            model.OnPositionChanged += view.UpdatePosition;
            model.OnStaminaDepleted += view.OnStaminaDepleted;
        }

        public void Update(float deltaTime)
        {
            // 1. Get input from View
            Vector2 input = view.MoveInput;

            // 2. Calculate movement based on model rules
            float intensity = Mathf.Clamp01(input.magnitude);
            
            // Apply movement
            if (intensity > 0.01f)
            {
                // Drain stamina while moving
                float drain = model.StaminaDrainRate * intensity * deltaTime;
                model.DrainStamina(drain);
                model.NotifyStaminaChanged();

                // Calculate speed (reduced if stamina is empty)
                float speedMultiplier = model.IsStaminaEmpty ? 0.5f : 1f;
                Vector2 velocity = input.normalized * model.MoveSpeed * speedMultiplier;
                
                // Update model state
                model.Velocity = velocity;
                model.Position = view.GetCurrentPosition() + velocity * deltaTime;
                
                // Notify view to update visuals
                model.NotifyPositionChanged();
                view.UpdateVelocity(velocity);
            }
            else
            {
                // Not moving - regenerate stamina
                model.RegenerateStamina(deltaTime);
                model.NotifyStaminaChanged();
                
                // Stop movement
                model.Velocity = Vector2.zero;
                view.UpdateVelocity(Vector2.zero);
            }
        }

        public void OnDestroy()
        {
            // Unsubscribe from events to prevent memory leaks
            if (model != null)
            {
                model.OnPositionChanged -= view.UpdatePosition;
                model.OnStaminaDepleted -= view.OnStaminaDepleted;
            }
        }

        // Public API for external systems
        public PlayerModel GetModel() => model;
    }
}
