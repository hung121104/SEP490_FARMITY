using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using CombatManager.Presenter;

namespace CombatManager.View
{
    /// <summary>
    /// View for Enemy system.
    /// Handles visual updates (animations, sprite flipping, visual feedback).
    /// </summary>
    public class EnemyView : MonoBehaviour
    {
        private EnemyPresenter presenter;
        [Header("Nameplate")]
        [SerializeField] private Text enemyNameText;

        [Header("Level Tier Colors")]
        [SerializeField] private Color lowerOrEqualLevelColor = Color.white;
        [SerializeField] private Color slightlyHigherLevelColor = new Color(1f, 0.92f, 0.25f);
        [SerializeField] private Color highLevelColor = new Color(1f, 0.58f, 0.15f);
        [SerializeField] private Color dangerousLevelColor = new Color(0.95f, 0.2f, 0.2f);
        [SerializeField] private int slightlyHigherMinDelta = 1;
        [SerializeField] private int highMinDelta = 3;
        [SerializeField] private int dangerousMinDelta = 6;

        private StatsPresenter cachedLocalStatsPresenter;
        private float nextLocalStatsResolveAt;

        #region Initialization

        public void Initialize(EnemyPresenter presenter)
        {
            this.presenter = presenter;

            if (enemyNameText == null)
                enemyNameText = GetComponentInChildren<Text>(true);

            Debug.Log($"[EnemyView] Initialized for {gameObject.name}");
        }

        #endregion

        #region Unity Lifecycle

        private void LateUpdate()
        {
            if (presenter == null || !presenter.IsInitialized())
                return;

            UpdateVisuals();
        }

        #endregion

        #region Visual Updates

        private void UpdateVisuals()
        {
            UpdateAnimation();
            UpdateSpriteFlip();
            UpdateNameplate();
        }

        private void UpdateNameplate()
        {
            if (enemyNameText == null || presenter == null)
                return;

            enemyNameText.text = $"{presenter.GetEnemyDisplayName()} Lv.{presenter.GetEnemyLevel()}";
            enemyNameText.color = ResolveTierColor(presenter.GetEnemyLevel());
        }

        private Color ResolveTierColor(int enemyLevel)
        {
            int localLevel = ResolveLocalPlayerLevel();
            if (localLevel <= 0)
                return lowerOrEqualLevelColor;

            int delta = enemyLevel - localLevel;
            if (delta >= Mathf.Max(highMinDelta + 1, dangerousMinDelta))
                return dangerousLevelColor;

            if (delta >= Mathf.Max(slightlyHigherMinDelta + 1, highMinDelta))
                return highLevelColor;

            if (delta >= Mathf.Max(1, slightlyHigherMinDelta))
                return slightlyHigherLevelColor;

            return lowerOrEqualLevelColor;
        }

        private int ResolveLocalPlayerLevel()
        {
            if (cachedLocalStatsPresenter == null && Time.time >= nextLocalStatsResolveAt)
            {
                nextLocalStatsResolveAt = Time.time + 1f;
                cachedLocalStatsPresenter = FindLocalStatsPresenter();
            }

            if (cachedLocalStatsPresenter == null)
            {
                StatsPresenter fallback = FindObjectOfType<StatsPresenter>();
                if (fallback != null)
                    cachedLocalStatsPresenter = fallback;
            }

            if (cachedLocalStatsPresenter == null)
                return -1;

            return Mathf.Max(1, cachedLocalStatsPresenter.GetLevel());
        }

        private static StatsPresenter FindLocalStatsPresenter()
        {
            StatsPresenter[] presenters = FindObjectsOfType<StatsPresenter>(true);
            for (int i = 0; i < presenters.Length; i++)
            {
                StatsPresenter stats = presenters[i];
                if (stats == null)
                    continue;

                PhotonView pv = stats.GetComponent<PhotonView>();
                if (pv == null)
                    pv = stats.GetComponentInParent<PhotonView>();

                if (pv == null)
                    continue;

                if (pv.IsMine)
                    return stats;
            }

            return null;
        }

        private void UpdateAnimation()
        {
            Animator animator = presenter.GetAnimator();
            if (animator == null)
                return;

            // Animation is already handled by AIService
            // This is a placeholder for additional visual updates if needed
        }

        private void UpdateSpriteFlip()
        {
            SpriteRenderer spriteRenderer = presenter.GetSpriteRenderer();
            if (spriteRenderer == null)
                return;

            // Sprite flipping is already handled by AIService
            // This is a placeholder for additional visual updates if needed
        }

        #endregion
    }
}