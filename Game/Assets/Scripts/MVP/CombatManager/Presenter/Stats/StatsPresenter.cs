using UnityEngine;
using System.Collections;
using CombatManager.Model;
using CombatManager.Service;
using CombatManager.View;

namespace CombatManager.Presenter
{
    /// <summary>
    /// Presenter for Stats system.
    /// Connects StatsModel and StatsService to StatsView.
    /// Handles user input and updates the view.
    /// </summary>
    public class StatsPresenter : MonoBehaviour
    {
        [Header("Model")]
        [SerializeField] private StatsModel model = new StatsModel();

        private IStatsService service;
        private IPlayerProgressionSyncService progressionSyncService;
        private bool suppressDirtySync;

        #region Unity Lifecycle

        private void Awake()
        {
            // Initialize service with model
            service = new StatsService(model);

            PlayerProgressionSyncService syncComponent = GetComponent<PlayerProgressionSyncService>();
            if (syncComponent == null)
                syncComponent = gameObject.AddComponent<PlayerProgressionSyncService>();
            progressionSyncService = syncComponent;

            Debug.Log("[StatsPresenter] Initialized");
        }

        private void Start()
        {
            StartCoroutine(InitializeProgressionFromServer());
        }

        #endregion

        #region View Update Notification

        private void NotifyViewUpdate()
        {
            // Find and update the view
            StatsView view = GetComponent<StatsView>();
            if (view != null)
            {
                view.UpdateDisplay();
            }

            if (suppressDirtySync)
                return;

            MarkProgressionDirty();
        }

        #endregion

        #region Getters for View

        public int GetStrength() => service.GetStrength();
        public int GetVitality() => service.GetVitality();
        public int GetLevel() => service.GetLevel();
        public int GetCurrentExp() => service.GetCurrentExp();
        public int GetExpToNextLevel() => service.GetExpToNextLevel();
        public float GetExpProgress01() => service.GetExpProgress01();
        public int GetAttackDamage() => service.GetAttackDamage();
        public int GetMaxHealth() => service.GetMaxHealth();

        #endregion

        #region Progression API

        public int AddExperienceFromHost(int amount)
        {
            int levelsGained = service.AddExperience(amount);
            if (levelsGained > 0)
            {
                GameEventBus.FireLevelReached(service.GetLevel(), levelsGained);

                PlayerHealthPresenter healthPresenter = FindObjectOfType<PlayerHealthPresenter>();
                if (healthPresenter != null)
                {
                    healthPresenter.RefreshHealthBar();
                }
            }

            NotifyViewUpdate();
            return levelsGained;
        }

        public void SetProgressionFromSave(int level, int currentExp, int expToNextLevel, int baseStrength, int baseVitality)
        {
            suppressDirtySync = true;
            service.SetProgressionState(level, currentExp, expToNextLevel);
            service.SetBaseStats(baseStrength, baseVitality);
            NotifyViewUpdate();
            suppressDirtySync = false;
        }

        #endregion

        #region Public API for Other Systems

        public IStatsService GetService() => service;

        #endregion

        private IEnumerator InitializeProgressionFromServer()
        {
            if (progressionSyncService == null)
                yield break;

            yield return progressionSyncService.InitializeAndFetch(
                (snapshot) =>
                {
                    SetProgressionFromSave(
                        snapshot.level,
                        snapshot.currentExp,
                        snapshot.expToNextLevel,
                        snapshot.baseStrength,
                        snapshot.baseVitality);
                },
                (error) => Debug.LogWarning($"[StatsPresenter] Progression fetch failed: {error}")
            );
        }

        private void MarkProgressionDirty()
        {
            if (progressionSyncService == null || !progressionSyncService.IsInitialized)
                return;

            progressionSyncService.SetRuntimeSnapshot(new PlayerProgressionSnapshot
            {
                level = service.GetLevel(),
                currentExp = service.GetCurrentExp(),
                expToNextLevel = service.GetExpToNextLevel(),
                baseStrength = service.GetStrength(),
                baseVitality = service.GetVitality(),
            }, true);
        }
    }
}