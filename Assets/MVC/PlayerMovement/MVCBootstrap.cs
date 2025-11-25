using UnityEngine;
using MVC.Model;
using MVC.View;
using MVC.Controller;

namespace MVC
{
    /// <summary>
    /// MVCBootstrap - MonoBehaviour that wires together Model-View-Controller
    /// Attach this to your Player GameObject
    /// </summary>
    public class MVCBootstrap : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private float maxStamina = 100f;
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float staminaDrainRate = 10f;
        [SerializeField] private float staminaRegenRate = 12f;
        [SerializeField] private float staminaRegenDelay = 1f;

        [Header("References")]
        [SerializeField] private PlayerView playerView;

        private PlayerModel playerModel;
        private PlayerController playerController;

        private void Awake()
        {
            // Get or create View
            if (playerView == null)
                playerView = GetComponent<PlayerView>();

            // Create Model
            playerModel = new PlayerModel(
                maxStamina: maxStamina,
                moveSpeed: moveSpeed,
                drainRate: staminaDrainRate,
                regenRate: staminaRegenRate,
                regenDelay: staminaRegenDelay
            );

            // Initialize model position from current transform
            playerModel.Position = transform.position;

            // Create Controller and wire everything together
            playerController = new PlayerController(playerModel, playerView);

            Debug.Log("MVC Player Movement System initialized");
        }

        private void Update()
        {
            // Controller updates model based on view input
            playerController.Update(Time.deltaTime);
        }

        private void OnDestroy()
        {
            playerController?.OnDestroy();
        }

        // Public API for other systems (e.g., UI can access the model)
        public PlayerModel GetPlayerModel() => playerModel;
        public PlayerController GetPlayerController() => playerController;
    }
}
