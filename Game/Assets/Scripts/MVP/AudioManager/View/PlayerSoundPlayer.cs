using UnityEngine;
using Photon.Pun;

/// <summary>
/// Attach to each PlayerEntity prefab.
/// Subscribes to tool/action events and plays SFX on this player's AudioSource.
/// 
/// For the LOCAL player it subscribes to UseToolService + GameEventBus events.
/// For REMOTE players, sounds are triggered via the existing animation RPCs
/// — the remote PlayerAnimationView can call PlayRemoteAction() when
/// it receives an RPC trigger (chop, plow, water, etc.).
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class PlayerSoundPlayer : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PhotonView photonView;

    [Header("Footstep Settings")]
    [Tooltip("Minimum interval between footstep sounds (seconds)")]
    [SerializeField] private float footstepInterval = 0.35f;
    [Tooltip("Faster interval when sprinting")]
    [SerializeField] private float sprintFootstepInterval = 0.25f;

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = false;

    private AudioSource _source;
    private AudioSource _footstepSource;
    private float _nextFootstepTime;
    private bool _isLocal;
    private bool _localResolved;
    private PlayerAnimationView _animView;
    private Rigidbody2D _rb;
    private float _nextSkipDebugTime;

    private void Awake()
    {
        _source = GetComponent<AudioSource>();
        _source.playOnAwake = false;
        _source.spatialBlend = 1f; // 3D for remote players
        _source.maxDistance = 25f;
        _source.rolloffMode = AudioRolloffMode.Linear;

        _footstepSource = gameObject.AddComponent<AudioSource>();
        _footstepSource.playOnAwake = false;
        _footstepSource.spatialBlend = 1f;
        _footstepSource.maxDistance = 25f;
        _footstepSource.rolloffMode = AudioRolloffMode.Linear;

        if (photonView == null)
            photonView = GetComponentInParent<PhotonView>();

        _animView = GetComponent<PlayerAnimationView>();
        if (_animView == null)
            _animView = GetComponentInChildren<PlayerAnimationView>();
        if (_animView == null)
            _animView = GetComponentInParent<PlayerAnimationView>();

        _rb = GetComponent<Rigidbody2D>();
        if (_rb == null)
            _rb = GetComponentInChildren<Rigidbody2D>();
        if (_rb == null)
            _rb = GetComponentInParent<Rigidbody2D>();
    }

    private void OnEnable()
    {
        // Photon may not have assigned IsMine yet at OnEnable time,
        // so we attempt resolution here AND lazily in Update.
        TryResolveLocal();
    }

    private void TryResolveLocal()
    {
        if (_localResolved) return;

        if (PhotonNetwork.IsConnected)
        {
            if (photonView == null) return;           // not ready yet
            if (!photonView.IsMine) return;           // remote player — skip
        }

        _isLocal = true;
        _localResolved = true;
        _source.spatialBlend = 0f; // local player hears own sounds in 2D
        _footstepSource.spatialBlend = 0f;

        if (AudioManager.Instance != null)
        {
            _source.outputAudioMixerGroup = AudioManager.Instance.SfxGroup;
            _footstepSource.outputAudioMixerGroup = AudioManager.Instance.SfxGroup;
        }

        // Tool events
        UseToolService.OnHoeRequested += HandlePlow;
        UseToolService.OnWateringCanRequested += HandleWatering;
        UseToolService.OnAxeRequested += HandleChop;
        UseToolService.OnPickaxeRequested += HandlePickaxe;
        UseToolService.OnFishingRodRequested += HandleFishCast;

        // GameEventBus events
        GameEventBus.OnCropHarvested += HandleHarvest;
        GameEventBus.OnSeedPlanted += HandlePlant;
        GameEventBus.OnFishCaught += HandleFishCatch;
        GameEventBus.OnItemCollected += HandleItemPickup;
        GameEventBus.OnItemCrafted += HandleCraft;
        GameEventBus.OnFoodCooked += HandleCook;
        GameEventBus.OnEnemyKilled += HandleEnemyKill;
    }

    private void OnDisable()
    {
        if (!_localResolved) return;

        UseToolService.OnHoeRequested -= HandlePlow;
        UseToolService.OnWateringCanRequested -= HandleWatering;
        UseToolService.OnAxeRequested -= HandleChop;
        UseToolService.OnPickaxeRequested -= HandlePickaxe;
        UseToolService.OnFishingRodRequested -= HandleFishCast;

        GameEventBus.OnCropHarvested -= HandleHarvest;
        GameEventBus.OnSeedPlanted -= HandlePlant;
        GameEventBus.OnFishCaught -= HandleFishCatch;
        GameEventBus.OnItemCollected -= HandleItemPickup;
        GameEventBus.OnItemCrafted -= HandleCraft;
        GameEventBus.OnFoodCooked -= HandleCook;
        GameEventBus.OnEnemyKilled -= HandleEnemyKill;

        _localResolved = false;
        _isLocal = false;
    }

    #region Footsteps

    private void Update()
    {
        // Lazy resolve — Photon IsMine may not be ready at OnEnable time
        if (!_localResolved) { TryResolveLocal(); return; }
        if (!_isLocal) return;
        if (_animView != null && _animView.IsMovementLocked)
        {
            StopFootstepsImmediately();
            MaybeLogFootstepSkip("movement locked");
            return;
        }

        bool isMoving = IsLocomotionActive();
        if (!isMoving)
        {
            StopFootstepsImmediately();
            MaybeLogFootstepSkip("not moving");
            return;
        }

        if (Time.time < _nextFootstepTime)
        {
            MaybeLogFootstepSkip("footstep interval cooldown");
            return;
        }

        // Detect sprint: check InputManager sprint input
        bool sprinting = InputManager.Instance != null
            && InputManager.Instance.Sprint.ReadValue<float>() > 0.5f;

        _nextFootstepTime = Time.time + (sprinting ? sprintFootstepInterval : footstepInterval);

        PlayFootstep(SoundId.FootstepGrass);
        Log("FootstepGrass");
    }

    /// <summary>
    /// Alternative hook: add as an Animation Event on walk/run clips for frame-perfect timing.
    /// When Animation Events are set up, you can remove the Update-based approach above.
    /// </summary>
    public void OnFootstep()
    {
        if (Time.time < _nextFootstepTime) return;
        _nextFootstepTime = Time.time + footstepInterval;
        PlayFootstep(SoundId.FootstepGrass);
    }

    #endregion

    #region Remote Player Hooks

    /// <summary>
    /// Called by PlayerAnimationView on remote players when an action RPC arrives.
    /// This avoids needing separate sound RPCs — we piggyback on animation sync.
    /// </summary>
    public void PlayRemoteAction(SoundId id)
    {
        if (AudioManager.Instance == null) return;
        AudioManager.Instance.PlayOnSource(id, _source);
    }

    #endregion

    #region Tool Handlers

    private void HandlePlow(ToolData data, Vector3 pos)
    {
        Play(SoundId.Plow);
        Log("Plow");
    }

    private void HandleWatering(ToolData data, Vector3 pos)
    {
        Play(SoundId.Watering);
        Log("Watering");
    }

    private void HandleChop(ToolData data, Vector3 pos)
    {
        Play(SoundId.Chop);
        Log("Chop");
    }

    private void HandlePickaxe(ToolData data, Vector3 pos)
    {
        Play(SoundId.PickaxeHit);
        Log("Pickaxe");
    }

    private void HandleFishCast(ToolData data, Vector3 pos)
    {
        Play(SoundId.FishingCast);
        Log("FishCast");
    }

    #endregion

    #region GameEventBus Handlers

    private void HandleHarvest(string id, int count)
    {
        Play(SoundId.HarvestCrop);
    }

    private void HandlePlant(string id, int count)
    {
        Play(SoundId.PlantSeed);
    }

    private void HandleFishCatch(string id, int count)
    {
        Play(SoundId.FishingCatch);
    }

    private void HandleItemPickup(string id, int count)
    {
        Play(SoundId.ItemPickup);
    }

    private void HandleCraft(string id, int count)
    {
        Play(SoundId.CraftSuccess);
    }

    private void HandleCook(string id, int count)
    {
        Play(SoundId.CookSuccess);
    }

    private void HandleEnemyKill(string id, int count)
    {
        Play(SoundId.EnemyDeath);
    }

    #endregion

    private void Play(SoundId id)
    {
        if (AudioManager.Instance == null) return;
        AudioManager.Instance.PlayOnSource(id, _source);
    }

    private void PlayFootstep(SoundId id)
    {
        if (AudioManager.Instance == null) return;
        AudioManager.Instance.PlayOnSource(id, _footstepSource);
    }

    private void StopFootstepsImmediately()
    {
        if (_footstepSource == null) return;
        if (!_footstepSource.isPlaying) return;
        _footstepSource.Stop();
    }

    private bool IsLocomotionActive()
    {
        // Primary source: animation view movement vector.
        if (_animView != null && _animView.MoveDirection.sqrMagnitude > 0.0001f)
            return true;

        // Fallback #1: Rigidbody velocity.
        if (_rb != null && _rb.linearVelocity.sqrMagnitude > 0.0001f)
            return true;

        // Fallback #2: direct input vector from input system.
        if (InputManager.Instance != null)
        {
            Vector2 move = InputManager.Instance.Move.ReadValue<Vector2>();
            if (move.sqrMagnitude > 0.0001f)
                return true;
        }

        return false;
    }

    private void MaybeLogFootstepSkip(string reason)
    {
        if (!showDebugLogs) return;
        if (Time.time < _nextSkipDebugTime) return;
        _nextSkipDebugTime = Time.time + 1f;
        Debug.Log($"[PlayerSoundPlayer] Skip footstep: {reason}");
    }

    private void Log(string action)
    {
        if (showDebugLogs) Debug.Log($"[PlayerSoundPlayer] {action}");
    }
}
