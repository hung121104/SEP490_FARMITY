using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class StaminaView : MonoBehaviourPun
{
    [Header("Core")]
    [SerializeField] private float maxStamina = 200f;
    [SerializeField] private float passiveDecayFloorPercent = 0.5f;
    [SerializeField] private float consumableSoftCapPercent = 0.8f;

    [Header("Rates")]
    [SerializeField] private float regenDelaySeconds = 0.5f;
    [SerializeField] private float regenPercentPerSecond = 0.1f;
    [SerializeField] private float sprintCostPerSecond = 8f;

    [Header("Decay")]
    [Tooltip("How much viable stamina is lost per in-game minute.")]
    [SerializeField] private float viableDecayPerGameMinute = 0.05f;

    [Header("Sync")]
    [SerializeField] private float syncIntervalSeconds = 0.2f;

    private StaminaModel model;
    private StaminaPresenter presenter;
    private TimeManagerView timeManager;

    private float syncTimer;
    private bool sprintIntentSent;
    private bool masterSprintIntent;

    public float CurrentStamina => presenter?.CurrentStamina ?? maxStamina;
    public float ViableStamina => presenter?.ViableStamina ?? maxStamina;
    public float MaxStamina => presenter?.MaxStamina ?? maxStamina;

    public bool CanSprintLocally => CurrentStamina > 0.01f;

    private void Awake()
    {
        model = new StaminaModel
        {
            maxStamina = maxStamina,
            passiveDecayFloorPercent = passiveDecayFloorPercent,
            consumableSoftCapPercent = consumableSoftCapPercent,
            regenDelaySeconds = regenDelaySeconds,
            regenPercentPerSecond = regenPercentPerSecond,
            currentStamina = maxStamina,
            viableStamina = maxStamina,
            lastConsumeTime = -999f
        };

        presenter = new StaminaPresenter(model, new StaminaService());
        timeManager = FindFirstObjectByType<TimeManagerView>();
    }

    // Start() as IEnumerator is automatically treated as a coroutine by Unity.
    private System.Collections.IEnumerator Start()
    {
        // Only the MasterClient restores stamina from PlayerDataManager.
        // Non-master clients receive authoritative state via RPC_ApplyAuthoritativeState
        // which the master broadcasts every syncIntervalSeconds.
        if (!PhotonNetwork.IsMasterClient) yield break;

        // Wait until WorldDataBootstrapper has finished fetching world data.
        yield return new WaitUntil(() =>
            WorldDataBootstrapper.Instance != null && WorldDataBootstrapper.Instance.IsReady);

        // Wait for the owner's "accountId" custom property to arrive.
        // SpawnPlayer.cs calls SetCustomProperties("accountId") on Start(), but that
        // event can race with PhotonNetwork.Instantiate — mirror the same guard used
        // by LoadPlayerData.WaitAndApplyPositionForPlayer.
        float timeout = 10f;
        float elapsed = 0f;
        while (photonView?.Owner != null
               && !photonView.Owner.CustomProperties.ContainsKey("accountId")
               && elapsed < timeout)
        {
            yield return new WaitForSeconds(0.2f);
            elapsed += 0.2f;
        }

        TryRestoreFromSavedCharacterData();

        // Immediately push the restored values to all clients.
        BroadcastState();
        Debug.Log($"[StaminaView] Restored stamina for {GetOwnerAccountId()}: " +
                  $"current={model.currentStamina:F1}, viable={model.viableStamina:F1}");
    }

    private void OnDisable()
    {
        if (photonView != null && photonView.IsMine)
            SetLocalSprintIntent(false);
    }

    private void Update()
    {
        if (!PhotonNetwork.IsMasterClient || presenter == null) return;

        float gameSpeed = timeManager != null ? timeManager.timeSpeed : 1f;
        float gameMinutesDelta = Time.deltaTime * gameSpeed * viableDecayPerGameMinute;
        bool canSprint = masterSprintIntent && model.currentStamina > 0f;
        presenter.Tick(Time.deltaTime, gameMinutesDelta, canSprint, sprintCostPerSecond);

        syncTimer += Time.deltaTime;
        if (syncTimer >= syncIntervalSeconds)
        {
            syncTimer = 0f;
            BroadcastState();
        }
    }

    public void SetLocalSprintIntent(bool intent)
    {
        if (!photonView.IsMine) return;
        if (intent == sprintIntentSent) return;

        sprintIntentSent = intent;

        if (PhotonNetwork.IsMasterClient)
        {
            masterSprintIntent = intent;
            return;
        }

        photonView.RPC(nameof(RPC_SetSprintIntent), RpcTarget.MasterClient, intent, PhotonNetwork.LocalPlayer.ActorNumber);
    }

    public bool TryConsumeToolStamina(float rawCost)
    {
        if (presenter == null || !photonView.IsMine) return false;

        bool canConsume = presenter.TryConsume(rawCost);
        if (!canConsume) return false;

        if (PhotonNetwork.IsMasterClient)
        {
            BroadcastState();
        }
        else
        {
            photonView.RPC(nameof(RPC_RequestToolConsume), RpcTarget.MasterClient, rawCost, PhotonNetwork.LocalPlayer.ActorNumber);
        }

        return true;
    }

    public void ApplyConsumableEffects(float viableRestore, float regenMultiplier, float efficiencyReduction, float durationSeconds)
    {
        if (presenter == null || !photonView.IsMine) return;

        if (viableRestore > 0f) presenter.RestoreViableByConsumable(viableRestore);
        if (regenMultiplier > 1f && durationSeconds > 0f) presenter.ApplyRegenBoost(regenMultiplier, durationSeconds);
        if (efficiencyReduction > 0f && durationSeconds > 0f) presenter.ApplyToolEfficiency(efficiencyReduction, durationSeconds);

        if (PhotonNetwork.IsMasterClient)
        {
            BroadcastState();
        }
        else
        {
            photonView.RPC(nameof(RPC_RequestConsumable), RpcTarget.MasterClient,
                viableRestore, regenMultiplier, efficiencyReduction, durationSeconds, PhotonNetwork.LocalPlayer.ActorNumber);
        }
    }

    public void RestoreBySleep()
    {
        if (presenter == null || !photonView.IsMine) return;

        presenter.RestoreBySleep();

        if (PhotonNetwork.IsMasterClient)
        {
            BroadcastState();
        }
        else
        {
            photonView.RPC(nameof(RPC_RequestSleepRestore), RpcTarget.MasterClient, PhotonNetwork.LocalPlayer.ActorNumber);
        }
    }

    private string GetOwnerAccountId()
    {
        if (photonView?.Owner == null) return null;
        // Primary: app-level accountId set by SpawnPlayer via SetCustomProperties.
        if (photonView.Owner.CustomProperties.TryGetValue("accountId", out object id) && id is string s && !string.IsNullOrEmpty(s))
            return s;
        // Fallback: Photon UserId (only populated when Photon auth is configured).
        return string.IsNullOrEmpty(photonView.Owner.UserId) ? null : photonView.Owner.UserId;
    }

    private void TryRestoreFromSavedCharacterData()
    {
        if (PlayerDataManager.Instance == null || photonView?.Owner == null) return;

        string accountId = GetOwnerAccountId();
        if (string.IsNullOrEmpty(accountId)) return;

        var list = PlayerDataManager.Instance.players;
        int idx = list.FindIndex(p => p.accountId == accountId);
        if (idx < 0) return;

        var data = list[idx];
        if (data.currentStamina <= 0f && data.viableStamina <= 0f) return;

        float viable = data.viableStamina > 0f ? data.viableStamina : maxStamina;
        float current = data.currentStamina > 0f ? data.currentStamina : viable;
        presenter.SetState(current, viable);
    }

    private void BroadcastState()
    {
        photonView.RPC(nameof(RPC_ApplyAuthoritativeState), RpcTarget.All, model.currentStamina, model.viableStamina);
    }

    [PunRPC]
    private void RPC_SetSprintIntent(bool intent, int senderActorNumber, PhotonMessageInfo info)
    {
        if (!PhotonNetwork.IsMasterClient) return;
        if (!IsOwnerSender(senderActorNumber, info.Sender)) return;

        masterSprintIntent = intent;
    }

    [PunRPC]
    private void RPC_RequestToolConsume(float rawCost, int senderActorNumber, PhotonMessageInfo info)
    {
        if (!PhotonNetwork.IsMasterClient) return;
        if (!IsOwnerSender(senderActorNumber, info.Sender)) return;

        presenter.TryConsume(rawCost);
        BroadcastState();
    }

    [PunRPC]
    private void RPC_RequestConsumable(
        float viableRestore,
        float regenMultiplier,
        float efficiencyReduction,
        float durationSeconds,
        int senderActorNumber,
        PhotonMessageInfo info)
    {
        if (!PhotonNetwork.IsMasterClient) return;
        if (!IsOwnerSender(senderActorNumber, info.Sender)) return;

        if (viableRestore > 0f) presenter.RestoreViableByConsumable(viableRestore);
        if (regenMultiplier > 1f && durationSeconds > 0f) presenter.ApplyRegenBoost(regenMultiplier, durationSeconds);
        if (efficiencyReduction > 0f && durationSeconds > 0f) presenter.ApplyToolEfficiency(efficiencyReduction, durationSeconds);

        BroadcastState();
    }

    [PunRPC]
    private void RPC_RequestSleepRestore(int senderActorNumber, PhotonMessageInfo info)
    {
        if (!PhotonNetwork.IsMasterClient) return;
        if (!IsOwnerSender(senderActorNumber, info.Sender)) return;

        presenter.RestoreBySleep();
        BroadcastState();
    }

    [PunRPC]
    private void RPC_ApplyAuthoritativeState(float current, float viable)
    {
        presenter.SetState(current, viable);
    }

    private bool IsOwnerSender(int senderActorNumber, Player sender)
    {
        return photonView != null
            && photonView.Owner != null
            && sender != null
            && senderActorNumber == sender.ActorNumber
            && senderActorNumber == photonView.Owner.ActorNumber;
    }

    public static StaminaView FindLocal()
    {
        foreach (var go in GameObject.FindGameObjectsWithTag("PlayerEntity"))
        {
            var pv = go.GetComponent<PhotonView>();
            if (pv != null && pv.IsMine)
            {
                return go.GetComponent<StaminaView>();
            }
        }
        return null;
    }
}
