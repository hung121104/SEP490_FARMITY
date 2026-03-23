using UnityEngine;
using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;
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

        [Header("Base Weapon Prefabs")]
        [SerializeField] private GameObject swordBasePrefab;
        [SerializeField] private GameObject staffBasePrefab;
        [SerializeField] private GameObject spearBasePrefab;

        [Header("Position Settings")]
        [SerializeField] private Vector3 anchorOffset = Vector3.zero;
        [SerializeField] private Vector3 gripLocalOffset = Vector3.zero;

        [Header("Pivot Compensation (normalized pivot 0..1)")]
        [SerializeField] private Vector2 fallbackSourcePivot = new Vector2(0.5f, 0.5f);
        [SerializeField] private Vector2 swordDesiredPivot = new Vector2(0.81f, 0.19f);
        [SerializeField] private Vector2 staffDesiredPivot = new Vector2(0.83f, 0.14f);
        [SerializeField] private Vector2 spearDesiredPivot = new Vector2(0.71f, 0.28f);

        [Header("Rotation Settings")]
        [Tooltip("If sword sprite points RIGHT at 0°, keep 0. If points UP, set -90.")]
        [SerializeField] private float rotationOffsetDegrees = 0f;

        [Header("Debug")]
        [SerializeField] private bool enableWeaponVisualDebug = true;

        private IWeaponAnimationService service;

        private WeaponData currentWeaponData;
        private Coroutine applyVisualCoroutine;
        private readonly List<SpriteRenderer> activeWeaponRenderers = new List<SpriteRenderer>();
        private Sprite activeWeaponSprite;
        private int spriteOverrideLogCount;

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

            private void LateUpdate()
            {
                ForceWeaponSpriteOverride();
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
                prefabToUse = ResolveBaseWeaponPrefab(currentWeaponData.weaponType);

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

        private GameObject ResolveBaseWeaponPrefab(WeaponType weaponType)
        {
            return weaponType switch
            {
                WeaponType.Sword => swordBasePrefab,
                WeaponType.Staff => staffBasePrefab,
                WeaponType.Spear => spearBasePrefab,
                _ => null,
            };
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

            if (applyVisualCoroutine != null)
                StopCoroutine(applyVisualCoroutine);

            applyVisualCoroutine = StartCoroutine(ApplyWeaponVisualAndPivotWhenReady());
        }

        public void DespawnWeapon()
        {
            if (applyVisualCoroutine != null)
            {
                StopCoroutine(applyVisualCoroutine);
                applyVisualCoroutine = null;
            }

            activeWeaponRenderers.Clear();
            activeWeaponSprite = null;
            spriteOverrideLogCount = 0;

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

        private IEnumerator ApplyWeaponVisualAndPivotWhenReady()
        {
            if (currentWeaponData == null)
                yield break;

            GameObject weaponVisual = service?.GetWeaponVisual();
            if (weaponVisual == null)
                yield break;

            if (!TryGetWeaponSpriteRenderers(weaponVisual, out List<SpriteRenderer> targetRenderers))
                yield break;

            const int maxAttempts = 60;
            const float retryDelay = 0.1f;
            Sprite resolvedSprite = null;

            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                resolvedSprite = ItemCatalogService.Instance?.GetCachedSprite(currentWeaponData.itemID);
                if (resolvedSprite != null)
                    break;

                if (attempt == 1)
                {
                    Debug.LogWarning(
                        $"[WeaponAnimationPresenter] Icon sprite not ready for '{currentWeaponData.weaponName}' " +
                        $"(itemID='{currentWeaponData.itemID}'). Retrying...");
                }

                yield return new WaitForSeconds(retryDelay);
            }

            if (resolvedSprite == null && !string.IsNullOrWhiteSpace(currentWeaponData.iconUrl))
            {
                // Fallback path: direct icon download in case catalog cache is late or missing this key.
                yield return DownloadIconSpriteAt16Ppu(currentWeaponData.iconUrl, sprite => resolvedSprite = sprite);
            }

            if (resolvedSprite != null)
            {
                foreach (SpriteRenderer renderer in targetRenderers)
                {
                    renderer.sprite = resolvedSprite;
                    renderer.enabled = true;
                }

                activeWeaponRenderers.Clear();
                activeWeaponRenderers.AddRange(targetRenderers);
                activeWeaponSprite = resolvedSprite;
                spriteOverrideLogCount = 0;

                Debug.Log(
                    $"[WeaponAnimationPresenter] Applied item icon visual to '{weaponVisual.name}' " +
                    $"from itemID='{currentWeaponData.itemID}'.");

                LogWeaponVisualState("AppliedVisual", weaponVisual, targetRenderers, resolvedSprite);
            }
            else
            {
                Debug.LogWarning(
                    $"[WeaponAnimationPresenter] Missing icon sprite for weapon '{currentWeaponData.weaponName}' " +
                    $"(itemID='{currentWeaponData.itemID}'). Base prefab sprite will be used.");
            }

            ApplyWeaponPivotCompensation(targetRenderers[0].sprite);
            applyVisualCoroutine = null;
        }

        private bool TryGetWeaponSpriteRenderers(GameObject weaponVisual, out List<SpriteRenderer> targetRenderers)
        {
            targetRenderers = new List<SpriteRenderer>();
            HashSet<SpriteRenderer> uniqueRenderers = new HashSet<SpriteRenderer>();

            DynamicSpriteSwapper[] swappers = weaponVisual.GetComponentsInChildren<DynamicSpriteSwapper>(true);
            foreach (DynamicSpriteSwapper swapper in swappers)
            {
                // Weapon visuals now come directly from item icons, not from runtime sheet swapping.
                swapper.enabled = false;

                SpriteRenderer swapperRenderer = swapper.GetComponent<SpriteRenderer>();
                if (swapperRenderer != null)
                    uniqueRenderers.Add(swapperRenderer);
            }

            if (uniqueRenderers.Count == 0)
            {
                SpriteRenderer fallbackRenderer = weaponVisual.GetComponentInChildren<SpriteRenderer>(true);
                if (fallbackRenderer != null)
                    uniqueRenderers.Add(fallbackRenderer);
            }

            if (uniqueRenderers.Count == 0)
            {
                Debug.LogWarning(
                    $"[WeaponAnimationPresenter] Weapon prefab '{weaponVisual.name}' has no SpriteRenderer. " +
                    "Cannot apply item icon visual.");
                return false;
            }

            foreach (SpriteRenderer renderer in uniqueRenderers)
                targetRenderers.Add(renderer);

            return true;
        }

        private void ForceWeaponSpriteOverride()
        {
            if (activeWeaponSprite == null || activeWeaponRenderers.Count == 0)
                return;

            for (int i = activeWeaponRenderers.Count - 1; i >= 0; i--)
            {
                SpriteRenderer renderer = activeWeaponRenderers[i];
                if (renderer == null)
                {
                    activeWeaponRenderers.RemoveAt(i);
                    continue;
                }

                if (renderer.sprite != activeWeaponSprite)
                {
                    string previous = renderer.sprite != null ? renderer.sprite.name : "<null>";
                    renderer.sprite = activeWeaponSprite;
                    renderer.enabled = true;

                    if (enableWeaponVisualDebug && spriteOverrideLogCount < 10)
                    {
                        Debug.LogWarning(
                            $"[WeaponAnimationPresenter] Sprite override reapplied on '{renderer.name}'. " +
                            $"Previous='{previous}', Forced='{activeWeaponSprite.name}'.");
                        spriteOverrideLogCount++;
                    }
                }
            }
        }

        private void LogWeaponVisualState(string phase, GameObject weaponVisual, List<SpriteRenderer> targetRenderers, Sprite resolvedSprite)
        {
            if (!enableWeaponVisualDebug || weaponVisual == null)
                return;

            Animator[] animators = weaponVisual.GetComponentsInChildren<Animator>(true);
            DynamicSpriteSwapper[] swappers = weaponVisual.GetComponentsInChildren<DynamicSpriteSwapper>(true);

            Debug.Log(
                $"[WeaponAnimationPresenter][{phase}] weapon='{weaponVisual.name}', itemID='{currentWeaponData?.itemID}', " +
                $"iconUrl='{currentWeaponData?.iconUrl}', sprite='{resolvedSprite?.name}', " +
                $"size={resolvedSprite?.rect.width}x{resolvedSprite?.rect.height}, ppu={resolvedSprite?.pixelsPerUnit}, " +
                $"renderers={targetRenderers.Count}, animators={animators.Length}, swappers={swappers.Length}");

            foreach (SpriteRenderer renderer in targetRenderers)
            {
                if (renderer == null) continue;
                string spriteName = renderer.sprite != null ? renderer.sprite.name : "<null>";
                Debug.Log(
                    $"[WeaponAnimationPresenter][{phase}] renderer='{renderer.name}', enabled={renderer.enabled}, " +
                    $"currentSprite='{spriteName}'");
            }
        }

        private IEnumerator DownloadIconSpriteAt16Ppu(string iconUrl, System.Action<Sprite> onCompleted)
        {
            onCompleted?.Invoke(null);
            using UnityWebRequest req = UnityWebRequestTexture.GetTexture(iconUrl);
            req.timeout = 15;
            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning($"[WeaponAnimationPresenter] Direct icon download failed: {req.error}");
                yield break;
            }

            Texture2D tex = DownloadHandlerTexture.GetContent(req);
            if (tex == null)
                yield break;

            tex.filterMode = FilterMode.Point;
            Sprite sprite = Sprite.Create(
                tex,
                new Rect(0, 0, tex.width, tex.height),
                new Vector2(0.5f, 0.5f),
                16f);

            onCompleted?.Invoke(sprite);
        }

        private void ApplyWeaponPivotCompensation(Sprite sourceSprite)
        {
            GameObject weaponVisual = service?.GetWeaponVisual();
            if (weaponVisual == null)
                return;

            Vector3 compensatedLocalOffset = gripLocalOffset + GetPivotCompensationOffset(
                currentWeaponData?.weaponType ?? WeaponType.None,
                sourceSprite);
            weaponVisual.transform.localPosition = compensatedLocalOffset;

            Debug.Log(
                $"[WeaponAnimationPresenter] Applied pivot compensation {compensatedLocalOffset} " +
                $"for weapon type {currentWeaponData?.weaponType}");
        }

        private Vector3 GetPivotCompensationOffset(WeaponType weaponType, Sprite sourceSprite)
        {
            Vector2 desiredPivot = weaponType switch
            {
                WeaponType.Sword => swordDesiredPivot,
                WeaponType.Staff => staffDesiredPivot,
                WeaponType.Spear => spearDesiredPivot,
                _ => fallbackSourcePivot,
            };

            Vector2 sourcePivot = fallbackSourcePivot;
            if (sourceSprite != null && sourceSprite.rect.width > 0.01f && sourceSprite.rect.height > 0.01f)
            {
                sourcePivot = new Vector2(
                    sourceSprite.pivot.x / sourceSprite.rect.width,
                    sourceSprite.pivot.y / sourceSprite.rect.height);
            }

            // To mimic changing sprite pivot at runtime, offset by inverse delta between source and desired pivots.
            Vector2 delta = sourcePivot - desiredPivot;
            return new Vector3(delta.x, delta.y, 0f);
        }

        #endregion
    }
}