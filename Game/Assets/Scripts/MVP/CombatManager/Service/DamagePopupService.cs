using UnityEngine;
using TMPro;
using CombatManager.Model;

namespace CombatManager.Service
{
    /// <summary>
    /// Service layer for damage popup management.
    /// Handles popup spawning with positioning and type variations.
    /// </summary>
    public class DamagePopupService : IDamagePopupService
    {
        private DamagePopupModel model;

        #region Constructor

        public DamagePopupService(DamagePopupModel model)
        {
            this.model = model;
        }

        #endregion

        #region Initialization

        public void Initialize(GameObject popupPrefab)
        {
            model.popupPrefab = popupPrefab;
            model.isInitialized = true;

            Debug.Log("[DamagePopupService] Initialized");
        }

        public bool IsInitialized()
        {
            return model.isInitialized && model.popupPrefab != null;
        }

        #endregion

        #region Spawn Popup

        public void SpawnPopup(Vector3 position, int damage)
        {
            SpawnPopup(position, damage.ToString(), PopupType.Damage);
        }

        public void SpawnPopup(Vector3 position, int damage, PopupType type)
        {
            SpawnPopup(position, damage.ToString(), type);
        }

        public void SpawnPopup(Vector3 position, string text, PopupType type)
        {
            if (!IsInitialized())
            {
                Debug.LogWarning("[DamagePopupService] Cannot spawn popup - not initialized");
                return;
            }

            // Calculate spawn position with offset and randomness
            Vector3 spawnPos = CalculateSpawnPosition(position);

            // Instantiate popup
            GameObject popup = Object.Instantiate(model.popupPrefab, spawnPos, Quaternion.identity);

            // Set text
            TMP_Text textComponent = popup.GetComponentInChildren<TMP_Text>();
            if (textComponent != null)
            {
                textComponent.text = text;
                ApplyPopupStyle(textComponent, type);
            }

            Debug.Log($"[DamagePopupService] Spawned popup: {text} at {spawnPos}");
        }

        #endregion

        #region Position Calculation

        private Vector3 CalculateSpawnPosition(Vector3 basePosition)
        {
            // Add base offset
            Vector3 finalPos = basePosition + model.spawnOffset;

            // Add random spread
            float randomX = Random.Range(-model.randomOffsetX, model.randomOffsetX);
            float randomY = Random.Range(-model.randomOffsetY, model.randomOffsetY);
            finalPos += new Vector3(randomX, randomY, 0f);

            return finalPos;
        }

        #endregion

        #region Styling

        private void ApplyPopupStyle(TMP_Text text, PopupType type)
        {
            switch (type)
            {
                case PopupType.Damage:
                    text.color = Color.white;
                    break;

                case PopupType.CritDamage:
                    text.color = new Color(1f, 0.8f, 0f); // Yellow/Orange
                    text.fontSize *= 1.2f; // Bigger for crits
                    break;

                case PopupType.Heal:
                    text.color = new Color(0f, 1f, 0.5f); // Green
                    break;

                case PopupType.Miss:
                    text.color = new Color(0.6f, 0.6f, 0.6f); // Gray
                    text.text = "MISS";
                    break;
            }
        }

        #endregion

        #region Settings

        public void SetSpawnOffset(Vector3 offset)
        {
            model.spawnOffset = offset;
        }

        public void SetRandomOffset(float x, float y)
        {
            model.randomOffsetX = x;
            model.randomOffsetY = y;
        }

        #endregion
    }
}