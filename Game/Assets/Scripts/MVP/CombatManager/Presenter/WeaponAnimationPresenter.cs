using UnityEngine;
using Photon.Pun;
using System.Collections;
using CombatManager.Model;
using CombatManager.Service;
using CombatManager.View;

namespace CombatManager.Presenter
{
    /// <summary>
    /// Presenter for Weapon Animation system.
    /// Connects WeaponAnimationModel and WeaponAnimationService to WeaponAnimationView.
    /// Handles weapon spawning based on combat mode and coordinates with attack system.
    /// </summary>
    public class WeaponAnimationPresenter : MonoBehaviour
    {
        // Singleton for easy access by PlayerAttackPresenter
        public static WeaponAnimationPresenter Instance { get; private set; }

        [Header("Model")]
        [SerializeField] private WeaponAnimationModel model = new WeaponAnimationModel();

        [Header("Weapon Prefab")]
        [SerializeField] private GameObject weaponAnimationPrefab;

        [Header("Position Settings")]
        [SerializeField] private Vector3 anchorOffset = Vector3.zero;
        [SerializeField] private Vector3 gripLocalOffset = Vector3.zero;

        [Header("Rotation Settings")]
        [Tooltip("If sword sprite points RIGHT at 0°, keep 0. If points UP, set -90.")]
        [SerializeField] private float rotationOffsetDegrees = 0f;

        private IWeaponAnimationService service;

        #region Unity Lifecycle

        private void Awake()
        {
            // Singleton setup
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Debug.LogWarning("[WeaponAnimationPresenter] Duplicate instance found, destroying");
                Destroy(gameObject);
                return;
            }
        }

        private void Start()
        {
            SubscribeToCombatModeEvents();
        }

        private void OnDestroy()
        {
            UnsubscribeFromCombatModeEvents();

            if (Instance == this)
            {
                Instance = null;
            }
        }

        #endregion

        #region Combat Mode Events

        private void SubscribeToCombatModeEvents()
        {
            if (CombatModePresenter.Instance != null)
            {
                CombatModePresenter.Instance.RegisterCallback(OnCombatModeChanged);
            }
            else
            {
                Debug.LogWarning("[WeaponAnimationPresenter] CombatModePresenter.Instance not found");
            }
        }

        private void UnsubscribeFromCombatModeEvents()
        {
            if (CombatModePresenter.Instance != null)
            {
                CombatModePresenter.Instance.UnregisterCallback(OnCombatModeChanged);
            }
        }

        private void OnCombatModeChanged(bool isActive)
        {
            if (isActive)
            {
                StartCoroutine(SpawnWhenPlayerReady());
            }
            else
            {
                DespawnWeapon();
            }
        }

        #endregion

        #region Initialization

        private IEnumerator SpawnWhenPlayerReady()
        {
            float timeout = 5f;
            float elapsed = 0f;

            while (elapsed < timeout)
            {
                if (TryInitializeService())
                {
                    SpawnWeapon();
                    yield break;
                }

                elapsed += 0.1f;
                yield return new WaitForSeconds(0.1f);
            }

            Debug.LogError("[WeaponAnimationPresenter] Could not find local player/center point");
        }

        private bool TryInitializeService()
        {
            // Find main camera
            Camera mainCamera = Camera.main;
            if (mainCamera == null)
                mainCamera = FindObjectOfType<Camera>();

            // Find local player
            GameObject playerObj = FindLocalPlayerEntity();
            if (playerObj == null)
                return false;

            // Find center point
            Transform centerPoint = playerObj.transform.Find("CenterPoint");
            if (centerPoint == null)
                centerPoint = playerObj.transform;

            // Validate prefab
            GameObject prefabToUse = model.weaponAnimationPrefab != null ? model.weaponAnimationPrefab : weaponAnimationPrefab;
            if (prefabToUse == null)
            {
                Debug.LogError("[WeaponAnimationPresenter] Weapon prefab not assigned!");
                return false;
            }

            // Initialize service
            service = new WeaponAnimationService(model);
            service.Initialize(
                prefabToUse,
                centerPoint,
                mainCamera,
                anchorOffset,
                gripLocalOffset,
                rotationOffsetDegrees
            );

            Debug.Log("[WeaponAnimationPresenter] Initialized successfully");
            return true;
        }

        private GameObject FindLocalPlayerEntity()
        {
            foreach (GameObject go in GameObject.FindGameObjectsWithTag("Player"))
            {
                PhotonView pv = go.GetComponent<PhotonView>();
                if (pv != null && pv.IsMine)
                    return go;
            }

            foreach (GameObject go in GameObject.FindGameObjectsWithTag("PlayerEntity"))
            {
                PhotonView pv = go.GetComponent<PhotonView>();
                if (pv != null && pv.IsMine)
                    return go;
            }

            GameObject fallback = GameObject.Find("PlayerEntity");
            if (fallback != null)
            {
                Debug.LogWarning("[WeaponAnimationPresenter] Found PlayerEntity by name");
                return fallback;
            }

            return null;
        }

        #endregion

        #region Weapon Management

        public void SpawnWeapon()
        {
            if (service == null || !service.IsInitialized())
            {
                Debug.LogWarning("[WeaponAnimationPresenter] Cannot spawn - service not initialized");
                return;
            }

            service.SpawnWeapon();
        }

        public void DespawnWeapon()
        {
            if (service != null)
            {
                service.DespawnWeapon();
            }
        }

        #endregion

        #region Public API

        public void PlayAttackAnimation()
        {
            service?.PlayAttackAnimation();
        }

        public bool IsWeaponActive()
        {
            return service?.IsWeaponActive() ?? false;
        }

        #endregion

        #region Getters for View

        public bool IsInitialized() => service?.IsInitialized() ?? false;
        public Vector3 GetMouseDirection() => service?.CalculateMouseDirection() ?? Vector3.right;
        public float GetRotationAngle(Vector3 direction) => service?.CalculateRotationAngle(direction) ?? 0f;
        public GameObject GetPivotRoot() => service?.GetPivotRoot();
        public Transform GetCenterPoint() => service?.GetCenterPoint();

        #endregion

        #region Public API for Other Systems

        public IWeaponAnimationService GetService() => service;

        #endregion
    }
}