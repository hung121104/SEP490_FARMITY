using UnityEngine;
using System.Collections;
using Photon.Pun;
using CombatManager.Model;
using CombatManager.Service;
using CombatManager.View;

namespace CombatManager.Presenter
{
    /// <summary>
    /// Presenter for Player Knockback system.
    /// Connects PlayerKnockbackModel and PlayerKnockbackService to PlayerKnockbackView.
    /// Handles knockback coroutines and visual effects coordination.
    /// </summary>
    public class PlayerKnockbackPresenter : MonoBehaviour
    {
        [Header("Model")]
        [SerializeField] private PlayerKnockbackModel model = new PlayerKnockbackModel();

        private IPlayerKnockbackService service;
        private Coroutine knockbackRoutine;

        #region Unity Lifecycle

        private void Start()
        {
            StartCoroutine(DelayedInitialize());
        }

        #endregion

        #region Initialization

        private IEnumerator DelayedInitialize()
        {
            yield return new WaitForSeconds(0.5f);
            InitializeComponents();
        }

        private void InitializeComponents()
        {
            GameObject playerObj = FindLocalPlayerEntity();
            if (playerObj == null)
            {
                Debug.LogError("[PlayerKnockbackPresenter] Local player not found!");
                enabled = false;
                return;
            }

            // Initialize service
            service = new PlayerKnockbackService(model);
            service.Initialize(playerObj.transform);

            Debug.Log("[PlayerKnockbackPresenter] Initialized successfully");
        }

        private GameObject FindLocalPlayerEntity()
        {
            // Try "Player" tag first (multiplayer spawn)
            foreach (GameObject go in GameObject.FindGameObjectsWithTag("Player"))
            {
                PhotonView pv = go.GetComponent<PhotonView>();
                if (pv != null && pv.IsMine)
                    return go;
            }

            // Fallback to "PlayerEntity" tag (test scenes)
            foreach (GameObject go in GameObject.FindGameObjectsWithTag("PlayerEntity"))
            {
                PhotonView pv = go.GetComponent<PhotonView>();
                if (pv != null && pv.IsMine)
                    return go;
            }

            // Last resort: find any GameObject named "PlayerEntity"
            GameObject fallback = GameObject.Find("PlayerEntity");
            if (fallback != null)
            {
                Debug.LogWarning("[PlayerKnockbackPresenter] Found PlayerEntity by name (not recommended for production)");
                return fallback;
            }

            return null;
        }

        #endregion

        #region Public API for External Systems

        public void Knockback(Transform enemyTransform, float knockbackForce)
        {
            if (service == null || !service.IsInitialized())
            {
                Debug.LogWarning("[PlayerKnockbackPresenter] Cannot apply knockback - not initialized");
                return;
            }

            // Stop any existing knockback
            if (knockbackRoutine != null)
            {
                StopCoroutine(knockbackRoutine);
            }

            // Apply knockback through service
            service.ApplyKnockback(enemyTransform, knockbackForce);

            // Start visual effects via View
            PlayerKnockbackView view = GetComponent<PlayerKnockbackView>();
            if (view != null)
            {
                view.StartKnockbackEffects();
            }
            else
            {
                // Fallback: run coroutines directly in Presenter
                knockbackRoutine = StartCoroutine(ApplyKnockback());
                StartCoroutine(WaveEffect());
                StartCoroutine(FlashRed());
            }
        }

        #endregion

        #region Knockback Coroutines (Fallback if no View)

        private IEnumerator ApplyKnockback()
        {
            PlayerMovement playerMovement = service.GetPlayerMovement();

            if (playerMovement != null)
                playerMovement.enabled = false;

            yield return new WaitForSeconds(service.GetKnockbackDuration());

            if (playerMovement != null)
                playerMovement.enabled = true;

            service.SetKnockbackActive(false);
        }

        private IEnumerator WaveEffect()
        {
            Transform playerEntity = service.GetPlayerEntity();
            if (playerEntity == null) yield break;

            Vector3 originalScale = service.GetOriginalScale();
            float squashPixels = service.GetSquashPixels();
            float stretchPixels = service.GetStretchPixels();
            float waveDuration = service.GetWaveDuration();

            float elapsed = 0f;
            float targetStretch = originalScale.y + stretchPixels;
            float targetSquash = originalScale.y - squashPixels;

            // Stretch
            while (elapsed < waveDuration / 2)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / (waveDuration / 2);
                playerEntity.localScale = new Vector3(
                    originalScale.x,
                    Mathf.Lerp(originalScale.y, targetStretch, progress),
                    originalScale.z
                );
                yield return null;
            }

            elapsed = 0f;

            // Squash
            while (elapsed < waveDuration / 2)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / (waveDuration / 2);
                playerEntity.localScale = new Vector3(
                    originalScale.x,
                    Mathf.Lerp(targetStretch, targetSquash, progress),
                    originalScale.z
                );
                yield return null;
            }

            elapsed = 0f;

            // Return
            while (elapsed < waveDuration / 4)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / (waveDuration / 4);
                playerEntity.localScale = new Vector3(
                    originalScale.x,
                    Mathf.Lerp(targetSquash, originalScale.y, progress),
                    originalScale.z
                );
                yield return null;
            }

            playerEntity.localScale = originalScale;
        }

        private IEnumerator FlashRed()
        {
            SpriteRenderer spriteRenderer = service.GetSpriteRenderer();
            if (spriteRenderer == null) yield break;

            Color originalColor = service.GetOriginalColor();
            float flashDuration = service.GetFlashDuration();
            int flashCount = service.GetFlashCount();

            for (int i = 0; i < flashCount; i++)
            {
                spriteRenderer.color = Color.red;
                yield return new WaitForSeconds(flashDuration / 2);

                spriteRenderer.color = originalColor;
                yield return new WaitForSeconds(flashDuration / 2);
            }
        }

        #endregion

        #region Getters for View

        public bool IsInitialized() => service?.IsInitialized() ?? false;
        public Transform GetPlayerEntity() => service?.GetPlayerEntity();
        public Vector3 GetOriginalScale() => service?.GetOriginalScale() ?? Vector3.one;
        public Color GetOriginalColor() => service?.GetOriginalColor() ?? Color.white;
        public float GetKnockbackDuration() => service?.GetKnockbackDuration() ?? 0.15f;
        public float GetSquashPixels() => service?.GetSquashPixels() ?? 0.05f;
        public float GetStretchPixels() => service?.GetStretchPixels() ?? 0.05f;
        public float GetWaveDuration() => service?.GetWaveDuration() ?? 0.3f;
        public float GetFlashDuration() => service?.GetFlashDuration() ?? 0.2f;
        public int GetFlashCount() => service?.GetFlashCount() ?? 2;
        public SpriteRenderer GetSpriteRenderer() => service?.GetSpriteRenderer();
        public PlayerMovement GetPlayerMovement() => service?.GetPlayerMovement();

        #endregion

        #region Public API for Other Systems

        public IPlayerKnockbackService GetService() => service;

        #endregion
    }
}