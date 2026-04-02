using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
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
    public class WeaponAnimationPresenter : MonoBehaviourPunCallbacks, Photon.Realtime.IOnEventCallback
    {
        private const string KEY_WEAPON = "apWeapon";
        private const byte WEAPON_AIM_EVENT = 162;
        private const float AIM_SEND_INTERVAL = 0.05f;
        private const float REMOTE_ROTATION_SMOOTH_SPEED = 18f;
        private const float AUTO_ANCHOR_X = 0.3f;
        private const float AUTO_ANCHOR_Y = 0f;
        private const string PARAM_IS_WALKING = "isWalking";
        private const string PARAM_INPUT_X = "InputX";
        private const string PARAM_INPUT_Y = "InputY";
        private const string PARAM_LAST_INPUT_X = "LastInputX";
        private const string PARAM_LAST_INPUT_Y = "LastInputY";

        private sealed class RemoteWeaponState
        {
            public int actorNumber;
            public string weaponItemId = string.Empty;
            public WeaponData weaponData;
            public GameObject pivotRoot;
            public GameObject weaponVisual;
            public Animator animator;
            public SpriteRenderer renderer;
            public Sprite sprite;
            public Vector2 lastDirection = Vector2.right;
            public Coroutine spriteApplyCoroutine;
            public float targetAimAngle;
            public bool hasNetworkAim;
        }

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
        [SerializeField] private bool enableWeaponVisualDebug = false;

        private IWeaponAnimationService service;

        private WeaponData currentWeaponData;
        private Coroutine applyVisualCoroutine;
        private readonly List<SpriteRenderer> activeWeaponRenderers = new List<SpriteRenderer>();
        private Sprite activeWeaponSprite;
        private readonly Dictionary<int, RemoteWeaponState> remoteWeaponStates = new Dictionary<int, RemoteWeaponState>();
        private float lastAimSendTime;
        private Vector2 localLastFacingDirection = Vector2.right;

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
            SubscribeToWeaponEquipEvents();
            PlayerAttackPresenter.OnRemoteAttackVisual += HandleRemoteAttackVisual;
            InitializeRemoteWeaponStatesFromRoom();
        }

        private void OnEnable()
        {
            PhotonNetwork.AddCallbackTarget(this);
            PlayerRegistry.OnLocalPlayerSpawned += HandleLocalPlayerSpawned;
        }

        private void OnDisable()
        {
            PhotonNetwork.RemoveCallbackTarget(this);
            PlayerRegistry.OnLocalPlayerSpawned -= HandleLocalPlayerSpawned;
        }

        private void OnDestroy()
        {
            UnsubscribeFromCombatModeEvents();
            UnsubscribeFromWeaponEquipEvents();
            PlayerAttackPresenter.OnRemoteAttackVisual -= HandleRemoteAttackVisual;

            CleanupAllRemoteWeapons();

            if (Instance == this)
                Instance = null;
        }

        private void LateUpdate()
        {
            ForceWeaponSpriteOverride();
            UpdateLocalWeaponAnchorSide();
            BroadcastLocalAimAngleIfNeeded();
            UpdateRemoteWeaponTransforms();
        }

        public void OnEvent(ExitGames.Client.Photon.EventData photonEvent)
        {
            if (photonEvent.Code != WEAPON_AIM_EVENT)
                return;

            if (photonEvent.CustomData is not object[] payload || payload.Length < 2)
                return;

            if (!TryGetPayloadInt(payload, 0, out int actorNumber) ||
                !TryGetPayloadFloat(payload, 1, out float aimAngle))
            {
                return;
            }

            if (actorNumber == (PhotonNetwork.LocalPlayer?.ActorNumber ?? -1))
                return;

            if (remoteWeaponStates.TryGetValue(actorNumber, out RemoteWeaponState state) && state != null)
            {
                state.targetAimAngle = aimAngle + rotationOffsetDegrees;
                state.hasNetworkAim = true;
            }
        }

        public override void OnPlayerPropertiesUpdate(Player targetPlayer, ExitGames.Client.Photon.Hashtable changedProps)
        {
            if (targetPlayer == null || targetPlayer.IsLocal || changedProps == null)
                return;

            if (!changedProps.TryGetValue(KEY_WEAPON, out object value))
                return;

            string itemId = value as string ?? string.Empty;
            if (string.IsNullOrWhiteSpace(itemId))
            {
                RemoveRemoteWeapon(targetPlayer.ActorNumber);
                return;
            }

            TrySpawnRemoteWeapon(targetPlayer.ActorNumber, itemId);
        }

        public override void OnPlayerLeftRoom(Player otherPlayer)
        {
            if (otherPlayer == null)
                return;

            RemoveRemoteWeapon(otherPlayer.ActorNumber);
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

        private void HandleLocalPlayerSpawned(Transform playerTransform)
        {
            if (playerTransform == null)
                return;

            WeaponData equipped = WeaponEquipPresenter.Instance?.GetCurrentWeapon();
            if (equipped == null)
            {
                DespawnWeapon();
                return;
            }

            currentWeaponData = equipped;
            DespawnWeapon();
            service = null;
            model.isInitialized = false;
            StartCoroutine(SpawnWhenPlayerReady());
        }

        private void InitializeRemoteWeaponStatesFromRoom()
        {
            if (PhotonNetwork.CurrentRoom == null)
                return;

            foreach (Player roomPlayer in PhotonNetwork.PlayerList)
            {
                if (roomPlayer == null || roomPlayer.IsLocal)
                    continue;

                string itemId = roomPlayer.CustomProperties != null &&
                                roomPlayer.CustomProperties.TryGetValue(KEY_WEAPON, out object weaponValue)
                    ? weaponValue as string ?? string.Empty
                    : string.Empty;

                if (!string.IsNullOrWhiteSpace(itemId))
                    TrySpawnRemoteWeapon(roomPlayer.ActorNumber, itemId);
            }
        }

        private void CleanupAllRemoteWeapons()
        {
            foreach (RemoteWeaponState state in remoteWeaponStates.Values)
            {
                if (state?.spriteApplyCoroutine != null)
                    StopCoroutine(state.spriteApplyCoroutine);

                if (state?.weaponVisual != null)
                    Destroy(state.weaponVisual);

                if (state?.pivotRoot != null)
                    Destroy(state.pivotRoot);
            }

            remoteWeaponStates.Clear();
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

        private static GameObject FindRemotePlayerEntity(int actorNumber)
        {
            foreach (GameObject go in GameObject.FindGameObjectsWithTag("PlayerEntity"))
            {
                PhotonView pv = go.GetComponent<PhotonView>();
                if (pv != null && !pv.IsMine && pv.Owner != null && pv.Owner.ActorNumber == actorNumber)
                    return go;
            }

            foreach (GameObject go in GameObject.FindGameObjectsWithTag("Player"))
            {
                PhotonView pv = go.GetComponent<PhotonView>();
                if (pv != null && !pv.IsMine && pv.Owner != null && pv.Owner.ActorNumber == actorNumber)
                    return go;
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

            service?.DespawnWeapon();
        }

        private void TrySpawnRemoteWeapon(int actorNumber, string weaponItemId)
        {
            WeaponData weaponData = ItemCatalogService.Instance?.GetItemData<WeaponData>(weaponItemId);
            if (weaponData == null)
            {
                if (enableWeaponVisualDebug)
                {
                    Debug.LogWarning(
                        $"[WeaponAnimationPresenter] Cannot spawn remote weapon for actor {actorNumber}. " +
                        $"itemID='{weaponItemId}' not found in item catalog.");
                }

                return;
            }

            RemoveRemoteWeapon(actorNumber);

            GameObject ownerPlayer = FindRemotePlayerEntity(actorNumber);
            if (ownerPlayer == null)
                return;

            Transform centerPoint = ownerPlayer.transform.Find("CenterPoint");
            if (centerPoint == null)
                centerPoint = ownerPlayer.transform;

            GameObject basePrefab = ResolveBaseWeaponPrefab(weaponData.weaponType) ?? fallbackWeaponPrefab;
            if (basePrefab == null)
                return;

            RemoteWeaponState state = new RemoteWeaponState
            {
                actorNumber = actorNumber,
                weaponItemId = weaponItemId,
                weaponData = weaponData,
            };

            state.pivotRoot = new GameObject($"RemoteWeaponPivotRoot_{actorNumber}");
            state.pivotRoot.transform.SetParent(centerPoint, false);
            bool facingLeft = IsFacingLeft(ownerPlayer, ref state.lastDirection);
            state.pivotRoot.transform.localPosition = BuildSignedAnchorOffset(facingLeft);

            state.weaponVisual = Instantiate(basePrefab, state.pivotRoot.transform);
            state.weaponVisual.name = $"RemoteWeaponVisual_{actorNumber}";
            state.weaponVisual.transform.localPosition = gripLocalOffset;
            state.weaponVisual.transform.localRotation = Quaternion.identity;

            state.animator = state.weaponVisual.GetComponent<Animator>()
                             ?? state.weaponVisual.GetComponentInChildren<Animator>();

            DynamicSpriteSwapper[] swappers = state.weaponVisual.GetComponentsInChildren<DynamicSpriteSwapper>(true);
            foreach (DynamicSpriteSwapper swapper in swappers)
                swapper.enabled = false;

            state.renderer = state.weaponVisual.GetComponentInChildren<SpriteRenderer>(true);
            if (state.renderer != null)
            {
                state.spriteApplyCoroutine = StartCoroutine(ApplyRemoteWeaponSprite(state));
            }

            remoteWeaponStates[actorNumber] = state;
        }

        private void RemoveRemoteWeapon(int actorNumber)
        {
            if (!remoteWeaponStates.TryGetValue(actorNumber, out RemoteWeaponState state))
                return;

            if (state.spriteApplyCoroutine != null)
                StopCoroutine(state.spriteApplyCoroutine);

            if (state.weaponVisual != null)
                Destroy(state.weaponVisual);

            if (state.pivotRoot != null)
                Destroy(state.pivotRoot);

            remoteWeaponStates.Remove(actorNumber);
        }

        private IEnumerator ApplyRemoteWeaponSprite(RemoteWeaponState state)
        {
            if (state == null || state.renderer == null)
                yield break;

            const int maxAttempts = 60;
            const float retryDelay = 0.1f;
            Sprite resolvedSprite = null;

            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                resolvedSprite = ItemCatalogService.Instance?.GetCachedSprite(state.weaponItemId);
                if (resolvedSprite != null)
                    break;

                yield return new WaitForSeconds(retryDelay);
            }

            if (resolvedSprite == null && !string.IsNullOrWhiteSpace(state.weaponData?.iconUrl))
            {
                yield return DownloadIconSpriteAt16Ppu(state.weaponData.iconUrl, sprite => resolvedSprite = sprite);
            }

            if (resolvedSprite == null || state.renderer == null)
                yield break;

            state.renderer.sprite = resolvedSprite;
            state.renderer.enabled = true;
            state.sprite = resolvedSprite;

            Vector3 compensation = GetPivotCompensationOffset(state.weaponData.weaponType, resolvedSprite);
            if (state.weaponVisual != null)
                state.weaponVisual.transform.localPosition = gripLocalOffset + compensation;

            state.spriteApplyCoroutine = null;
        }

        private void UpdateRemoteWeaponTransforms()
        {
            if (remoteWeaponStates.Count == 0)
                return;

            List<int> staleActors = null;

            foreach (KeyValuePair<int, RemoteWeaponState> kvp in remoteWeaponStates)
            {
                int actor = kvp.Key;
                RemoteWeaponState state = kvp.Value;

                if (state == null || state.pivotRoot == null || state.weaponVisual == null)
                {
                    staleActors ??= new List<int>();
                    staleActors.Add(actor);
                    continue;
                }

                if (state.renderer != null && state.sprite != null && state.renderer.sprite != state.sprite)
                {
                    state.renderer.sprite = state.sprite;
                    state.renderer.enabled = true;
                }

                GameObject ownerPlayer = FindRemotePlayerEntity(actor);
                if (ownerPlayer == null)
                {
                    staleActors ??= new List<int>();
                    staleActors.Add(actor);
                    continue;
                }

                Transform centerPoint = ownerPlayer.transform.Find("CenterPoint") ?? ownerPlayer.transform;
                if (state.pivotRoot.transform.parent != centerPoint)
                    state.pivotRoot.transform.SetParent(centerPoint, false);

                bool facingLeft = IsFacingLeft(ownerPlayer, ref state.lastDirection);
                state.pivotRoot.transform.localPosition = BuildSignedAnchorOffset(facingLeft);

                float desiredAngle = state.hasNetworkAim
                    ? state.targetAimAngle
                    : GetRemoteAimAngle(ownerPlayer, state);

                float currentAngle = state.pivotRoot.transform.eulerAngles.z;
                float smoothedAngle = Mathf.LerpAngle(
                    currentAngle,
                    desiredAngle,
                    Mathf.Clamp01(Time.deltaTime * REMOTE_ROTATION_SMOOTH_SPEED));

                state.pivotRoot.transform.rotation = Quaternion.Euler(0f, 0f, smoothedAngle);
            }

            if (staleActors == null)
                return;

            foreach (int actor in staleActors)
                RemoveRemoteWeapon(actor);
        }

        private float GetRemoteAimAngle(GameObject ownerPlayer, RemoteWeaponState state)
        {
            if (ownerPlayer == null)
                return rotationOffsetDegrees;

            Vector2 facingDirection = ResolveFacingDirection(ownerPlayer, ref state.lastDirection);
            return Mathf.Atan2(facingDirection.y, facingDirection.x) * Mathf.Rad2Deg + rotationOffsetDegrees;
        }

        private void UpdateLocalWeaponAnchorSide()
        {
            if (!IsWeaponActive() || service == null)
                return;

            GameObject pivotRoot = service.GetPivotRoot();
            Transform centerPoint = service.GetCenterPoint();
            if (pivotRoot == null || centerPoint == null)
                return;

            GameObject ownerPlayer = centerPoint.GetComponentInParent<PlayerMovement>()?.gameObject ?? centerPoint.gameObject;
            bool facingLeft = IsFacingLeft(ownerPlayer, ref localLastFacingDirection);

            Vector3 desired = BuildSignedAnchorOffset(facingLeft);
            if ((pivotRoot.transform.localPosition - desired).sqrMagnitude > 0.000001f)
                pivotRoot.transform.localPosition = desired;
        }

        private static bool IsFacingLeft(GameObject ownerPlayer, ref Vector2 lastDirection)
        {
            Vector2 direction = ResolveFacingDirection(ownerPlayer, ref lastDirection);
            return direction.x < 0f;
        }

        private static Vector2 ResolveFacingDirection(GameObject ownerPlayer, ref Vector2 lastDirection)
        {
            if (ownerPlayer == null)
                return lastDirection.sqrMagnitude > 0.0001f ? lastDirection.normalized : Vector2.right;

            Animator ownerAnimator = ownerPlayer.GetComponentInChildren<Animator>();
            if (ownerAnimator == null)
                return lastDirection.sqrMagnitude > 0.0001f ? lastDirection.normalized : Vector2.right;

            bool isWalking = ownerAnimator.GetBool(PARAM_IS_WALKING);
            float x = isWalking ? ownerAnimator.GetFloat(PARAM_INPUT_X) : ownerAnimator.GetFloat(PARAM_LAST_INPUT_X);
            float y = isWalking ? ownerAnimator.GetFloat(PARAM_INPUT_Y) : ownerAnimator.GetFloat(PARAM_LAST_INPUT_Y);
            Vector2 dir = new Vector2(x, y);

            if (dir.sqrMagnitude > 0.0001f)
                lastDirection = dir.normalized;

            return lastDirection.sqrMagnitude > 0.0001f ? lastDirection.normalized : Vector2.right;
        }

        private static Vector3 BuildSignedAnchorOffset(bool facingLeft)
        {
            float signedX = facingLeft ? -Mathf.Abs(AUTO_ANCHOR_X) : Mathf.Abs(AUTO_ANCHOR_X);
            return new Vector3(signedX, AUTO_ANCHOR_Y, 0f);
        }

        private void HandleRemoteAttackVisual(int actorNumber, float angle)
        {
            if (!remoteWeaponStates.TryGetValue(actorNumber, out RemoteWeaponState state) || state == null)
                return;

            if (state.pivotRoot != null)
            {
                state.targetAimAngle = angle + rotationOffsetDegrees;
                state.hasNetworkAim = true;
            }

            if (state.animator != null)
                state.animator.SetTrigger("Attack");
        }

        private void BroadcastLocalAimAngleIfNeeded()
        {
            if (!PhotonNetwork.IsConnected)
                return;

            if (!IsWeaponActive() || service == null)
                return;

            if (Time.unscaledTime - lastAimSendTime < AIM_SEND_INTERVAL)
                return;

            int actorNumber = PhotonNetwork.LocalPlayer?.ActorNumber ?? -1;
            if (actorNumber <= 0)
                return;

            Vector3 direction = service.CalculateMouseDirection();
            if (direction.sqrMagnitude < 0.0001f)
                return;

            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            object[] payload = { actorNumber, angle };
            PhotonNetwork.RaiseEvent(
                WEAPON_AIM_EVENT,
                payload,
                new RaiseEventOptions { Receivers = ReceiverGroup.Others },
                ExitGames.Client.Photon.SendOptions.SendUnreliable);

            lastAimSendTime = Time.unscaledTime;
        }

        private static bool TryGetPayloadInt(object[] payload, int index, out int value)
        {
            value = 0;
            if (index < 0 || index >= payload.Length || payload[index] == null)
                return false;

            if (payload[index] is int i)
            {
                value = i;
                return true;
            }

            if (payload[index] is byte b)
            {
                value = b;
                return true;
            }

            return false;
        }

        private static bool TryGetPayloadFloat(object[] payload, int index, out float value)
        {
            value = 0f;
            if (index < 0 || index >= payload.Length || payload[index] == null)
                return false;

            if (payload[index] is float f)
            {
                value = f;
                return true;
            }

            if (payload[index] is int i)
            {
                value = i;
                return true;
            }

            return false;
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
                    if (enableWeaponVisualDebug)
                    {
                        Debug.Log(
                            $"[WeaponAnimationPresenter] Icon sprite not ready for '{currentWeaponData.weaponName}' " +
                            $"(itemID='{currentWeaponData.itemID}'). Retrying...");
                    }
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

                if (enableWeaponVisualDebug)
                {
                    Debug.Log(
                        $"[WeaponAnimationPresenter] Applied item icon visual to '{weaponVisual.name}' " +
                        $"from itemID='{currentWeaponData.itemID}'.");
                }
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
                    renderer.sprite = activeWeaponSprite;
                    renderer.enabled = true;
                }
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