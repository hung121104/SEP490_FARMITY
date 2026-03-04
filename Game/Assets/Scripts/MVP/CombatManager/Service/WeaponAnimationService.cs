using UnityEngine;
using CombatManager.Model;

namespace CombatManager.Service
{
    /// <summary>
    /// Service layer for weapon animation management.
    /// Handles weapon spawning, rotation calculations, and animation triggers.
    /// </summary>
    public class WeaponAnimationService : IWeaponAnimationService
    {
        private WeaponAnimationModel model;

        #region Constructor

        public WeaponAnimationService(WeaponAnimationModel model)
        {
            this.model = model;
        }

        #endregion

        #region Initialization

        public void Initialize(
            GameObject weaponPrefab,
            Transform centerPoint,
            Camera mainCamera,
            Vector3 anchorOffset,
            Vector3 gripOffset,
            float rotationOffset)
        {
            model.weaponAnimationPrefab = weaponPrefab;
            model.centerPoint = centerPoint;
            model.mainCamera = mainCamera;
            model.anchorOffset = anchorOffset;
            model.gripLocalOffset = gripOffset;
            model.rotationOffsetDegrees = rotationOffset;
            model.isInitialized = true;

            Debug.Log("[WeaponAnimationService] Initialized");
        }

        public bool IsInitialized()
        {
            return model.isInitialized;
        }

        #endregion

        #region Weapon Lifecycle

        public void SpawnWeapon()
        {
            if (model.weaponAnimationPrefab == null || model.centerPoint == null)
            {
                Debug.LogError("[WeaponAnimationService] Cannot spawn weapon - missing prefab or center point");
                return;
            }

            // Clean up existing weapon
            DespawnWeapon();

            // Create pivot root (fixed point on player)
            model.pivotRoot = new GameObject("WeaponPivotRoot");
            model.pivotRoot.transform.SetParent(model.centerPoint);
            model.pivotRoot.transform.position = model.centerPoint.position + model.anchorOffset;
            model.pivotRoot.transform.rotation = Quaternion.identity;

            // Spawn weapon visual
            model.weaponVisual = Object.Instantiate(model.weaponAnimationPrefab, model.pivotRoot.transform);
            model.weaponVisual.name = "WeaponVisual";
            model.weaponVisual.transform.localPosition = model.gripLocalOffset;
            model.weaponVisual.transform.localRotation = Quaternion.identity;

            // Get animator
            model.weaponAnimator = model.weaponVisual.GetComponent<Animator>();
            if (model.weaponAnimator == null)
            {
                model.weaponAnimator = model.weaponVisual.GetComponentInChildren<Animator>();
            }

            model.isWeaponActive = true;

            Debug.Log("[WeaponAnimationService] Weapon spawned");
        }

        public void DespawnWeapon()
        {
            if (model.weaponVisual != null)
            {
                Object.Destroy(model.weaponVisual);
                model.weaponVisual = null;
            }

            if (model.pivotRoot != null)
            {
                Object.Destroy(model.pivotRoot);
                model.pivotRoot = null;
            }

            model.weaponAnimator = null;
            model.isWeaponActive = false;

            Debug.Log("[WeaponAnimationService] Weapon despawned");
        }

        public bool IsWeaponActive()
        {
            return model.isWeaponActive && model.pivotRoot != null;
        }

        #endregion

        #region Rotation

        public Vector3 CalculateMouseDirection()
        {
            if (model.mainCamera == null || model.pivotRoot == null)
                return Vector3.right;

            Ray ray = model.mainCamera.ScreenPointToRay(Input.mousePosition);
            Plane plane = new Plane(Vector3.forward, model.centerPoint.position);

            if (!plane.Raycast(ray, out float dist))
                return Vector3.right;

            Vector3 mouseWorld = ray.GetPoint(dist);
            Vector3 dir = mouseWorld - model.pivotRoot.transform.position;
            dir.z = 0f;

            if (dir.sqrMagnitude < 0.0001f)
                return Vector3.right;

            return dir.normalized;
        }

        public float CalculateRotationAngle(Vector3 direction)
        {
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            return angle + model.rotationOffsetDegrees;
        }

        #endregion

        #region Animation

        public void PlayAttackAnimation()
        {
            if (model.weaponAnimator == null)
            {
                Debug.LogWarning("[WeaponAnimationService] Cannot play animation - animator not found");
                return;
            }

            model.weaponAnimator.SetTrigger(model.attackTrigger);
            Debug.Log("[WeaponAnimationService] Attack animation triggered");
        }

        #endregion

        #region Getters

        public Transform GetCenterPoint() => model.centerPoint;
        public GameObject GetPivotRoot() => model.pivotRoot;
        public GameObject GetWeaponVisual() => model.weaponVisual;
        public Animator GetWeaponAnimator() => model.weaponAnimator;
        public float GetRotationOffset() => model.rotationOffsetDegrees;

        #endregion
    }
}