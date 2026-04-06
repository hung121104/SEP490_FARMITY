using UnityEngine;
using Photon.Pun;
using CombatManager.Presenter;

namespace CombatManager.Test
{
    /// <summary>
    /// Debug helper to grant EXP quickly while testing level progression.
    /// Attach to any scene object and use ContextMenu actions in the inspector.
    /// </summary>
    public class ProgressionExpTest : MonoBehaviour
    {
        [Header("Optional Target")]
        [SerializeField] private StatsPresenter targetStatsPresenter;

        [Header("Grant Settings")]
        [SerializeField] private int customExpAmount = 100;

        [Header("Debug")]
        [SerializeField] private bool showLogs = true;

        [ContextMenu("Grant 10 EXP")]
        public void Grant10Exp() => GrantExp(10);

        [ContextMenu("Grant 50 EXP")]
        public void Grant50Exp() => GrantExp(50);

        [ContextMenu("Grant 100 EXP")]
        public void Grant100Exp() => GrantExp(100);

        [ContextMenu("Grant 1000 EXP")]
        public void Grant1000Exp() => GrantExp(1000);

        [ContextMenu("Grant Custom EXP")]
        public void GrantCustomExp() => GrantExp(customExpAmount);

        [ContextMenu("Log Current Progression")]
        public void LogCurrentProgression()
        {
            StatsPresenter presenter = ResolveStatsPresenter();
            if (presenter == null)
            {
                Debug.LogWarning("[ProgressionExpTest] StatsPresenter not found.");
                return;
            }

            Debug.Log($"[ProgressionExpTest] Current progression: lv={presenter.GetLevel()} exp={presenter.GetCurrentExp()}/{presenter.GetExpToNextLevel()} str={presenter.GetStrength()} vit={presenter.GetVitality()} end={presenter.GetEndurance()}");
        }

        private void GrantExp(int amount)
        {
            if (amount <= 0)
            {
                Debug.LogWarning($"[ProgressionExpTest] Ignored invalid EXP amount: {amount}");
                return;
            }

            if (!CanMutateProgressionHere())
            {
                Debug.LogWarning("[ProgressionExpTest] In multiplayer, only Room Host (MasterClient) can grant EXP.");
                return;
            }

            StatsPresenter presenter = ResolveStatsPresenter();
            if (presenter == null)
            {
                Debug.LogWarning("[ProgressionExpTest] StatsPresenter not found.");
                return;
            }

            int beforeLevel = presenter.GetLevel();
            int beforeExp = presenter.GetCurrentExp();
            int beforeCap = presenter.GetExpToNextLevel();

            int levelsGained = presenter.AddExperienceFromHost(amount);

            if (showLogs)
            {
                Debug.Log($"[ProgressionExpTest] GrantExp amount={amount} levelsGained={levelsGained} before=lv{beforeLevel} {beforeExp}/{beforeCap} after=lv{presenter.GetLevel()} {presenter.GetCurrentExp()}/{presenter.GetExpToNextLevel()}");
            }
        }

        private StatsPresenter ResolveStatsPresenter()
        {
            if (targetStatsPresenter != null)
                return targetStatsPresenter;

            GameObject[] playerEntities = GameObject.FindGameObjectsWithTag("PlayerEntity");
            for (int i = 0; i < playerEntities.Length; i++)
            {
                PhotonView pv = playerEntities[i].GetComponent<PhotonView>();
                if (pv != null && pv.IsMine)
                {
                    StatsPresenter presenter = playerEntities[i].GetComponentInChildren<StatsPresenter>(true);
                    if (presenter != null)
                        return presenter;
                }
            }

            GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
            for (int i = 0; i < players.Length; i++)
            {
                PhotonView pv = players[i].GetComponent<PhotonView>();
                if (pv != null && pv.IsMine)
                {
                    StatsPresenter presenter = players[i].GetComponentInChildren<StatsPresenter>(true);
                    if (presenter != null)
                        return presenter;
                }
            }

            return FindFirstObjectByType<StatsPresenter>();
        }

        private static bool CanMutateProgressionHere()
        {
            if (!PhotonNetwork.IsConnected)
                return true;

            return PhotonNetwork.IsMasterClient;
        }
    }
}
