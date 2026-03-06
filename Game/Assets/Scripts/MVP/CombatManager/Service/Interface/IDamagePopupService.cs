using UnityEngine;
using CombatManager.Model;

namespace CombatManager.Service
{
    /// <summary>
    /// Interface for damage popup service.
    /// Defines operations for spawning damage popups.
    /// </summary>
    public interface IDamagePopupService
    {
        #region Initialization

        void Initialize(GameObject popupPrefab);
        bool IsInitialized();

        #endregion

        #region Spawn Popup

        void SpawnPopup(Vector3 position, int damage);
        void SpawnPopup(Vector3 position, int damage, PopupType type);
        void SpawnPopup(Vector3 position, string text, PopupType type);

        #endregion

        #region Settings

        void SetSpawnOffset(Vector3 offset);
        void SetRandomOffset(float x, float y);

        #endregion
    }
}