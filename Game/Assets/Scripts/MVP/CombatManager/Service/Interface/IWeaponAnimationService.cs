using UnityEngine;

namespace CombatManager.Service
{
    /// <summary>
    /// Interface for weapon animation management service.
    /// Defines operations for weapon spawning, rotation, and animation control.
    /// </summary>
    public interface IWeaponAnimationService
    {
        #region Initialization

        void Initialize(
            GameObject weaponPrefab,
            Transform centerPoint,
            Camera mainCamera,
            Vector3 anchorOffset,
            Vector3 gripOffset,
            float rotationOffset);

        bool IsInitialized();

        #endregion

        #region Weapon Lifecycle

        void SpawnWeapon();
        void DespawnWeapon();
        bool IsWeaponActive();

        #endregion

        #region Rotation

        Vector3 CalculateMouseDirection();
        float CalculateRotationAngle(Vector3 direction);

        #endregion

        #region Animation

        void PlayAttackAnimation();

        #endregion

        #region Getters

        Transform GetCenterPoint();
        GameObject GetPivotRoot();
        GameObject GetWeaponVisual();
        Animator GetWeaponAnimator();
        float GetRotationOffset();

        #endregion
    }
}