using UnityEngine;
using AchievementManager.Model;
using AchievementManager.Presenter;
using AchievementManager.Service;
using AchievementManager.View;

namespace AchievementManager
{
    public class AchievementBootstrap : MonoBehaviour
    {
        [Header("Views")]
        [SerializeField] private AchievementPanelView panelView;
        [SerializeField] private AchievementUnlockPopupView unlockPopupView;

        [Header("Settings")]
        [SerializeField] private float fetchDelay = 1f;
        [SerializeField] private float catalogWaitTimeout = 10f;

        private AchievementPresenter presenter;

        private void Awake()
        {
            if (AchievementPresenter.Instance != null)
            {
                Destroy(gameObject);
                return;
            }
            DontDestroyOnLoad(gameObject);

            var model   = new AchievementModel();
            var service = new AchievementService();

            presenter = new AchievementPresenter(
                model, service, panelView, unlockPopupView,
                this, fetchDelay, catalogWaitTimeout);

            AchievementPresenter.Instance = presenter;

            var tracker = GetComponent<AchievementTrackerPresenter>()
                          ?? gameObject.AddComponent<AchievementTrackerPresenter>();
            tracker.Initialize(model, service, presenter);
            presenter.SetTracker(tracker);

            Debug.Log("[AchievementBootstrap] Achievement system initialized");
        }

        private void OnDestroy()
        {
            presenter?.Dispose();
            if (AchievementPresenter.Instance == presenter)
                AchievementPresenter.Instance = null;
        }
    }
}
