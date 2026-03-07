using UnityEngine;

namespace CombatManager.Model
{
    /// <summary>
    /// Data model for weapon animation state and settings.
    /// Tracks weapon visual references, rotation settings, and spawn state.
    /// </summary>
    [System.Serializable]
    public class WeaponAnimationModel
    {
        #region Prefab & References

        [Header("Prefab & References")]
        public GameObject weaponAnimationPrefab = null;
        public Transform centerPoint = null;
        public Camera mainCamera = null;

        #endregion

        #region Position Settings

        [Header("Position Settings")]
        public Vector3 anchorOffset = Vector3.zero;
        public Vector3 gripLocalOffset = Vector3.zero;

        #endregion

        #region Rotation Settings

        [Header("Rotation Settings")]
        [Tooltip("If sword sprite points RIGHT at 0°, keep 0. If points UP, set -90.")]
        public float rotationOffsetDegrees = 0f;

        #endregion

        #region Runtime References

        [Header("Runtime References")]
        public GameObject pivotRoot = null;      // Fixed point on player
        public GameObject weaponVisual = null;   // Animated sword child
        public Animator weaponAnimator = null;

        #endregion

        #region Animation Trigger

        [Header("Animation Trigger")]
        public string attackTrigger = "Attack";

        #endregion

        #region State

        [Header("State")]
        public bool isWeaponActive = false;
        public bool isInitialized = false;

        #endregion

        #region Constructor

        public WeaponAnimationModel()
        {
            weaponAnimationPrefab = null;
            centerPoint = null;
            mainCamera = null;
            anchorOffset = Vector3.zero;
            gripLocalOffset = Vector3.zero;
            rotationOffsetDegrees = 0f;
            pivotRoot = null;
            weaponVisual = null;
            weaponAnimator = null;
            attackTrigger = "Attack";
            isWeaponActive = false;
            isInitialized = false;
        }

        #endregion
    }
}