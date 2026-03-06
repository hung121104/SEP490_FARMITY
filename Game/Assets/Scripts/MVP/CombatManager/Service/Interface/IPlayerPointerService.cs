using UnityEngine;

namespace CombatManager.Service
{
    /// <summary>
    /// Interface for player pointer management service.
    /// Defines operations for pointer direction tracking and position updates.
    /// </summary>
    public interface IPlayerPointerService
    {
        #region Initialization

        void Initialize(Transform playerTransform, GameObject pointerPrefab, Camera mainCamera);
        bool IsInitialized();

        #endregion

        #region Direction Updates

        void UpdateMouseDirection();
        Vector3 GetCurrentDirection();

        #endregion

        #region Position Calculations

        Vector3 GetPointerPosition();
        Vector3 CalculatePointerLocalPosition();
        Quaternion CalculatePointerRotation();

        #endregion

        #region Settings

        float GetOrbitRadius();
        void SetOrbitRadius(float radius);

        #endregion

        #region References

        Transform GetPlayerTransform();
        Transform GetCenterPoint();
        Transform GetPointerTransform();
        SpriteRenderer GetPointerSpriteRenderer();
        Camera GetMainCamera();

        #endregion
    }
}