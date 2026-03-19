using UnityEngine;
using System.Collections;
using Photon.Pun;
using CombatManager.Model;
using CombatManager.Service;
using CombatManager.View;

namespace CombatManager.Presenter
{
    /// <summary>
    /// Presenter for Skill Indicator system.
    /// Mirrors SkillIndicatorManager from CombatSystem (kept for legacy).
    /// Spawns indicator prefabs, finds local player, passes references to views.
    /// Called by SkillPresenter.ShowIndicator() / HideIndicator().
    /// </summary>
    public class SkillIndicatorPresenter : MonoBehaviour
    {
        [Header("Model")]
        [SerializeField] private SkillIndicatorModel model = new SkillIndicatorModel();

        [Header("Indicator Prefabs - Assign in Inspector")]
        [SerializeField] private GameObject arrowPrefab;
        [SerializeField] private GameObject conePrefab;
        [SerializeField] private GameObject circlePrefab;

        private ISkillIndicatorService service;

        // Spawned indicator view instances
        private IndicatorView arrowIndicator;
        private IndicatorView coneIndicator;
        private IndicatorView circleIndicator;
        private IndicatorView activeIndicator;

        // Player references (found at runtime)
        private Transform centerPoint;
        private Camera mainCamera;

        #region Singleton

        public static SkillIndicatorPresenter Instance { get; private set; }

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            InitializeService();
        }

        private void Start()
        {
            StartCoroutine(FindPlayerDelayed());
        }

        #endregion

        #region Initialization

        private void InitializeService()
        {
            model.arrowPrefab = arrowPrefab;
            model.conePrefab = conePrefab;
            model.circlePrefab = circlePrefab;

            service = new SkillIndicatorService(model);
            service.Initialize(model);

            Debug.Log("[SkillIndicatorPresenter] Service initialized");
        }

        private void SpawnIndicators()
        {
            // Arrow
            GameObject arrowGO = service.SpawnIndicator(arrowPrefab);
            if (arrowGO != null)
            {
                arrowGO.transform.SetParent(transform);
                arrowIndicator = arrowGO.GetComponent<IndicatorView>();
                if (arrowIndicator == null)
                    Debug.LogWarning("[SkillIndicatorPresenter] IndicatorView missing on Arrow prefab!");
            }

            // Cone
            GameObject coneGO = service.SpawnIndicator(conePrefab);
            if (coneGO != null)
            {
                coneGO.transform.SetParent(transform);
                coneIndicator = coneGO.GetComponent<IndicatorView>();
                if (coneIndicator == null)
                    Debug.LogWarning("[SkillIndicatorPresenter] IndicatorView missing on Cone prefab!");
            }

            // Circle
            GameObject circleGO = service.SpawnIndicator(circlePrefab);
            if (circleGO != null)
            {
                circleGO.transform.SetParent(transform);
                circleIndicator = circleGO.GetComponent<IndicatorView>();
                if (circleIndicator == null)
                    Debug.LogWarning("[SkillIndicatorPresenter] IndicatorView missing on Circle prefab!");
            }

            HideAll();
            Debug.Log("[SkillIndicatorPresenter] Indicators spawned");
        }

        #endregion

        #region Find Player (Spawned at Runtime)

        private IEnumerator FindPlayerDelayed()
        {
            yield return new WaitForSeconds(0.5f);
            FindLocalPlayer();
        }

        private void FindLocalPlayer()
        {
            // Multiplayer: find local PhotonView
            foreach (GameObject go in GameObject.FindGameObjectsWithTag("PlayerEntity"))
            {
                PhotonView pv = go.GetComponent<PhotonView>();
                if (pv != null && pv.IsMine)
                {
                    SetupReferences(go);
                    return;
                }
            }

            // Fallback: solo test scene
            GameObject fallback = GameObject.FindGameObjectWithTag("PlayerEntity");
            if (fallback != null)
            {
                SetupReferences(fallback);
                return;
            }

            Debug.LogError("[SkillIndicatorPresenter] Local player not found!");
        }

        private void SetupReferences(GameObject playerGO)
        {
            Transform found = playerGO.transform.Find("CenterPoint");
            centerPoint = found != null ? found : playerGO.transform;

            mainCamera = Camera.main;

            // Spawn indicators NOW that we have references
            SpawnIndicators();

            // Pass references to each view
            arrowIndicator?.SetReferences(centerPoint, mainCamera);
            coneIndicator?.SetReferences(centerPoint, mainCamera);
            circleIndicator?.SetReferences(centerPoint, mainCamera);

            Debug.Log($"[SkillIndicatorPresenter] References set from: {playerGO.name}");
        }

        #endregion

        #region Public API

        /// <summary>
        /// Called by SkillPresenter.ShowIndicator()
        /// </summary>
        public void ShowIndicator(CombatManager.Model.SkillIndicatorData data)
        {
            HideAll();
            service.SetCurrentType(data.type);

            switch (data.type)
            {
                case CombatManager.Model.IndicatorType.Arrow:
                    if (arrowIndicator != null)
                    {
                        arrowIndicator.SetupArrow(data.arrowRange);
                        arrowIndicator.Show();
                        activeIndicator = arrowIndicator;
                        Debug.Log($"[SkillIndicatorPresenter] Showing Arrow (range: {data.arrowRange})");
                    }
                    break;

                case CombatManager.Model.IndicatorType.Cone:
                    if (coneIndicator != null)
                    {
                        coneIndicator.SetupCone(data.coneRange, data.coneAngle);
                        coneIndicator.Show();
                        activeIndicator = coneIndicator;
                        Debug.Log($"[SkillIndicatorPresenter] Showing Cone (range: {data.coneRange}, angle: {data.coneAngle})");
                    }
                    break;

                case CombatManager.Model.IndicatorType.Circle:
                    if (circleIndicator != null)
                    {
                        circleIndicator.SetupCircle(data.circleRadius, data.circleMaxRange);
                        circleIndicator.Show();
                        activeIndicator = circleIndicator;
                        Debug.Log($"[SkillIndicatorPresenter] Showing Circle (radius: {data.circleRadius})");
                    }
                    break;
            }
        }

        public void HideAll()
        {
            arrowIndicator?.Hide();
            coneIndicator?.Hide();
            circleIndicator?.Hide();

            activeIndicator = null;
            service?.SetCurrentType(CombatManager.Model.IndicatorType.None);
        }

        #endregion

        #region Public Getters

        public Vector3 GetAimedDirection()
        {
            return activeIndicator != null
                ? activeIndicator.GetAimedDirection()
                : Vector3.right;
        }

        public Vector3 GetAimedPosition()
        {
            return activeIndicator != null
                ? activeIndicator.GetAimedPosition()
                : Vector3.zero;
        }

        public float GetAimedDistance()
        {
            return activeIndicator != null
                ? activeIndicator.GetAimedDistance()
                : 0f;
        }

        public bool IsActive => service?.IsActive() ?? false;

        #endregion
    }
}