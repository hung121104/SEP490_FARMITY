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
    /// Uses item-catalog WeaponData and resolves one of three base prefabs by weaponType.
    /// </summary>
    public class WeaponAnimationPresenter : MonoBehaviour
    {
        public static WeaponAnimationPresenter Instance { get; private set; }

        [Header("Model")]
        [SerializeField] private WeaponAnimationModel model = new WeaponAnimationModel();

        [Header("Fallback Prefab (if weapon has no prefab assigned)")]
        [SerializeField] private GameObject fallbackWeaponPrefab;

        [Header("Position Settings")]
        [SerializeField] private Vector3 anchorOffset = Vector3.zero;
        [SerializeField] private Vector3 gripLocalOffset = Vector3.zero;

        [Header("Rotation Settings")]
        [Tooltip("If sword sprite points RIGHT at 0°, keep 0. If points UP, set -90.")]
        [SerializeField] private float rotationOffsetDegrees = 0f;

        private IWeaponAnimationService service;

        private WeaponData currentWeaponData;

        #region Unity Lifecycle

        private void Awake()
        {
            if (Instance == null)
                Instance = this;
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
            SubscribeToWeaponEquipEvents(); // ✅ NEW
        }

        private void OnDestroy()
        {
            UnsubscribeFromCombatModeEvents();
            UnsubscribeFromWeaponEquipEvents(); // ✅ NEW

            if (Instance == this)
                Instance = null;
        }

        #endregion

        #region Combat Mode Events

        private void SubscribeToCombatModeEvents()
        {
            if (CombatModePresenter.Instance != null)
                CombatModePresenter.Instance.RegisterCallback(OnCombatModeChanged);
            else
                Debug.LogWarning("[WeaponAnimationPresenter] CombatModePresenter.Instance not found");
        }

        private void UnsubscribeFromCombatModeEvents()
        {
            if (CombatModePresenter.Instance != null)
                CombatModePresenter.Instance.UnregisterCallback(OnCombatModeChanged);
        }

        private void OnCombatModeChanged(bool isActive)
        {
            if (!isActive)
                DespawnWeapon();

            // ✅ Don't spawn here anymore - weapon equip event handles spawning
            // OnCombatModeChanged only handles DESPAWN on combat OFF
        }

        #endregion

        #region Weapon Equip Events - NEW

        private void SubscribeToWeaponEquipEvents()
        {
            WeaponEquipPresenter.OnWeaponEquipped += OnWeaponEquipped;
            WeaponEquipPresenter.OnWeaponUnequipped += OnWeaponUnequipped;
        }

        private void UnsubscribeFromWeaponEquipEvents()
        {
            WeaponEquipPresenter.OnWeaponEquipped -= OnWeaponEquipped;
            WeaponEquipPresenter.OnWeaponUnequipped -= OnWeaponUnequipped;
        }

        private void OnWeaponEquipped(WeaponData weaponData)
        {
            if (weaponData == null)
            {
                Debug.LogWarning("[WeaponAnimationPresenter] Equipped weapon data is null!");
                return;
            }

            currentWeaponData = weaponData;

            Debug.Log($"[WeaponAnimationPresenter] Weapon equipped: {weaponData.weaponName} " +
                      $"({weaponData.weaponType}) → spawning prefab");

            // Despawn old weapon first
            DespawnWeapon();

            // Reset service so it reinitializes with new prefab
            service = null;
            model.isInitialized = false;

            // Spawn new weapon
            StartCoroutine(SpawnWhenPlayerReady());
        }

        private void OnWeaponUnequipped()
        {
            currentWeaponData = null;
            DespawnWeapon();
            Debug.Log("[WeaponAnimationPresenter] Weapon unequipped → despawned");
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
            Camera mainCamera = Camera.main;
            if (mainCamera == null)
                mainCamera = FindObjectOfType<Camera>();

            GameObject playerObj = FindLocalPlayerEntity();
            if (playerObj == null)
                return false;

            Transform centerPoint = playerObj.transform.Find("CenterPoint");
            if (centerPoint == null)
                centerPoint = playerObj.transform;

            GameObject prefabToUse = null;

            if (currentWeaponData != null)
            {
                WeaponPrefabCatalogService weaponCatalog = WeaponPrefabCatalogService.Instance;
                if (weaponCatalog == null)
                {
                    Debug.LogWarning("[WeaponAnimationPresenter] WeaponPrefabCatalogService not found. " +
                                     "Add it to the download/bootstrap scene.");
                }
                else
                {
                    prefabToUse = weaponCatalog.ResolvePrefab(currentWeaponData.weaponType);
                }

                if (prefabToUse != null)
                {
                    Debug.Log($"[WeaponAnimationPresenter] Using base prefab for {currentWeaponData.weaponType} -> {prefabToUse.name}");
                }
            }

            if (prefabToUse == null)
            {
                if (fallbackWeaponPrefab != null)
                {
                    prefabToUse = fallbackWeaponPrefab;
                    Debug.LogWarning($"[WeaponAnimationPresenter] Weapon prefab key unresolved, " +
                                     $"using fallback: {fallbackWeaponPrefab.name}");
                }
                else
                {
                    Debug.LogError("[WeaponAnimationPresenter] No weapon prefab available!");
                    return false;
                }
            }

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
            ApplyWeaponVisualConfig();
        }

        public void DespawnWeapon()
        {
            service?.DespawnWeapon();
        }

        #endregion

        #region Public API

        public void PlayAttackAnimation()
        {
            service?.PlayAttackAnimation();
        }

        public bool IsWeaponActive() => service?.IsWeaponActive() ?? false;
        public bool IsInitialized() => service?.IsInitialized() ?? false;
        public Vector3 GetMouseDirection() => service?.CalculateMouseDirection() ?? Vector3.right;
        public float GetRotationAngle(Vector3 direction) => service?.CalculateRotationAngle(direction) ?? 0f;
        public GameObject GetPivotRoot() => service?.GetPivotRoot();
        public Transform GetCenterPoint() => service?.GetCenterPoint();
        public IWeaponAnimationService GetService() => service;

        // ✅ NEW
        public WeaponData GetCurrentWeaponData() => currentWeaponData;

        private void ApplyWeaponVisualConfig()
        {
            if (currentWeaponData == null)
                return;

            string visualConfigId = currentWeaponData.weaponVisualConfigId;
            if (string.IsNullOrWhiteSpace(visualConfigId))
            {
                // Soft fallback for migration safety while old data is being updated.
                visualConfigId = currentWeaponData.weaponMaterialId;
            }

            if (string.IsNullOrWhiteSpace(visualConfigId))
            {
                Debug.LogWarning(
                    $"[WeaponAnimationPresenter] No weaponVisualConfigId on '{currentWeaponData.weaponName}'. " +
                    "Base prefab sprite will be used.");
                return;
            }

            GameObject weaponVisual = service?.GetWeaponVisual();
            if (weaponVisual == null)
                return;

            DynamicSpriteSwapper swapper = weaponVisual.GetComponentInChildren<DynamicSpriteSwapper>();
            if (swapper == null)
            {
                Debug.LogWarning(
                    $"[WeaponAnimationPresenter] Weapon prefab '{weaponVisual.name}' has no DynamicSpriteSwapper. " +
                    "Cannot apply weaponVisualConfigId.");
                return;
            }

            swapper.ConfigId = visualConfigId;
            Debug.Log(
                $"[WeaponAnimationPresenter] Applied weapon visual config '{visualConfigId}' to '{weaponVisual.name}'");
        }

        #endregion
    }
}