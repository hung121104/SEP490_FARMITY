using UnityEngine;
using System.Collections;
using Photon.Pun;
using CombatManager.Model;
using CombatManager.Service;
using CombatManager.View;

namespace CombatManager.Presenter
{
    /// <summary>
    /// Presenter for Player Pointer system.
    /// Connects PlayerPointerModel and PlayerPointerService to PlayerPointerView.
    /// Handles initialization and coordinates with the View for updates.
    /// </summary>
    public class PlayerPointerPresenter : MonoBehaviour
    {
        [Header("Model")]
        [SerializeField] private PlayerPointerModel model = new PlayerPointerModel();

        [Header("References")]
        [SerializeField] private GameObject pointerPrefab;

        private IPlayerPointerService service;

        #region Unity Lifecycle

        private void Start()
        {
            StartCoroutine(DelayedInitialize());
        }

        #endregion

        #region Initialization

        private IEnumerator DelayedInitialize()
        {
            yield return new WaitForSeconds(model.initializationDelay);
            InitializeComponents();
        }

        private void InitializeComponents()
        {
            // Find main camera
            Camera mainCamera = Camera.main;
            if (mainCamera == null)
                mainCamera = FindObjectOfType<Camera>();

            // Find local player entity
            GameObject playerObj = FindLocalPlayerEntity();
            if (playerObj == null)
            {
                Debug.LogError("[PlayerPointerPresenter] Local player not found!");
                enabled = false;
                return;
            }

            // Use pointerPrefab from model if set, otherwise from inspector
            GameObject prefabToUse = model.pointerPrefab != null ? model.pointerPrefab : pointerPrefab;
            if (prefabToUse == null)
            {
                Debug.LogError("[PlayerPointerPresenter] Pointer prefab not assigned!");
                enabled = false;
                return;
            }

            // Initialize service
            service = new PlayerPointerService(model);
            service.Initialize(playerObj.transform, prefabToUse, mainCamera);

            // Spawn pointer
            ((PlayerPointerService)service).SpawnPointer();

            Debug.Log("[PlayerPointerPresenter] Initialized successfully");
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
                Debug.LogWarning("[PlayerPointerPresenter] Found PlayerEntity by name (not recommended for production)");
                return fallback;
            }

            return null;
        }

        #endregion

        #region Public API for View

        public void UpdateDirection()
        {
            if (service == null || !service.IsInitialized())
                return;

            service.UpdateMouseDirection();
        }

        #endregion

        #region Getters for View

        public bool IsInitialized() => service?.IsInitialized() ?? false;
        public Vector3 GetCurrentDirection() => service?.GetCurrentDirection() ?? Vector3.right;
        public Vector3 GetPointerPosition() => service?.GetPointerPosition() ?? Vector3.zero;
        public Vector3 GetPointerLocalPosition() => service?.CalculatePointerLocalPosition() ?? Vector3.zero;
        public Quaternion GetPointerRotation() => service?.CalculatePointerRotation() ?? Quaternion.identity;
        public float GetOrbitRadius() => service?.GetOrbitRadius() ?? 1.5f;
        public Transform GetPlayerTransform() => service?.GetPlayerTransform();
        public Transform GetPointerTransform() => service?.GetPointerTransform();

        #endregion

        #region Public API for Other Systems (Attack, Skills)

        public IPlayerPointerService GetService() => service;

        // Legacy support for old code
        public Vector3 GetPointerDirection() => GetCurrentDirection();

        #endregion
    }
}