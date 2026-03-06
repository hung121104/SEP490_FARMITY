using UnityEngine;
using CombatManager.Model;

namespace CombatManager.Service
{
    /// <summary>
    /// Service layer for player pointer management.
    /// Handles mouse direction tracking, pointer positioning, and rotation calculations.
    /// </summary>
    public class PlayerPointerService : IPlayerPointerService
    {
        private PlayerPointerModel model;

        #region Constructor

        public PlayerPointerService(PlayerPointerModel model)
        {
            this.model = model;
        }

        #endregion

        #region Initialization

        public void Initialize(Transform playerTransform, GameObject pointerPrefab, Camera mainCamera)
        {
            model.playerTransform = playerTransform;
            model.pointerPrefab = pointerPrefab;
            model.mainCamera = mainCamera;

            // Find CenterPoint child or use player transform
            Transform found = playerTransform.Find("CenterPoint");
            model.centerPoint = found != null ? found : playerTransform;

            Debug.Log($"[PlayerPointerService] Initialized with player: {playerTransform.name}");
        }

        public bool IsInitialized()
        {
            return model.isInitialized;
        }

        #endregion

        #region Pointer Spawning (Called by Presenter)

        public void SpawnPointer()
        {
            if (model.pointerPrefab == null || model.centerPoint == null)
            {
                Debug.LogError("[PlayerPointerService] Cannot spawn pointer - missing prefab or center point");
                return;
            }

            GameObject pointerGO = Object.Instantiate(model.pointerPrefab, model.centerPoint.position, Quaternion.identity);
            model.pointerTransform = pointerGO.transform;
            model.pointerTransform.SetParent(model.centerPoint);
            model.pointerTransform.localPosition = Vector3.zero;

            // Get SpriteRenderer from child
            model.pointerSpriteRenderer = pointerGO.GetComponentInChildren<SpriteRenderer>();
            if (model.pointerSpriteRenderer != null)
            {
                model.pointerSpriteRenderer.enabled = true;
                model.pointerSpriteRenderer.sortingOrder = 100;
            }

            model.isInitialized = true;

            Debug.Log("[PlayerPointerService] Pointer spawned successfully");
        }

        #endregion

        #region Direction Updates

        public void UpdateMouseDirection()
        {
            if (model.mainCamera == null || model.centerPoint == null)
                return;

            Ray ray = model.mainCamera.ScreenPointToRay(Input.mousePosition);
            Plane plane = new Plane(Vector3.forward, model.centerPoint.position);

            if (plane.Raycast(ray, out float dist))
            {
                Vector3 mouseWorldPos = ray.GetPoint(dist);
                Vector3 direction = mouseWorldPos - model.centerPoint.position;
                direction.z = 0f;

                if (direction.magnitude > 0.01f)
                {
                    model.currentDirection = direction.normalized;
                }
            }
        }

        public Vector3 GetCurrentDirection()
        {
            return model.currentDirection;
        }

        #endregion

        #region Position Calculations

        public Vector3 GetPointerPosition()
        {
            return model.pointerTransform != null ? model.pointerTransform.position : Vector3.zero;
        }

        public Vector3 CalculatePointerLocalPosition()
        {
            return model.currentDirection * model.orbitRadius;
        }

        public Quaternion CalculatePointerRotation()
        {
            float angle = Mathf.Atan2(model.currentDirection.y, model.currentDirection.x) * Mathf.Rad2Deg;
            return Quaternion.Euler(0f, 0f, angle);
        }

        #endregion

        #region Settings

        public float GetOrbitRadius()
        {
            return model.orbitRadius;
        }

        public void SetOrbitRadius(float radius)
        {
            model.orbitRadius = radius;
        }

        #endregion

        #region References

        public Transform GetPlayerTransform() => model.playerTransform;
        public Transform GetCenterPoint() => model.centerPoint;
        public Transform GetPointerTransform() => model.pointerTransform;
        public SpriteRenderer GetPointerSpriteRenderer() => model.pointerSpriteRenderer;
        public Camera GetMainCamera() => model.mainCamera;

        #endregion
    }
}