using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using CombatManager.Model;
using CombatManager.Service;
using Photon.Pun;

namespace CombatManager.Presenter
{
    public class DiceDisplayPresenter : MonoBehaviour
    {
        [Header("Model")]
        [SerializeField] private DiceDisplayModel model = new DiceDisplayModel();

        [Header("Dice Prefabs")]
        [SerializeField] private GameObject d6Prefab;
        [SerializeField] private GameObject d8Prefab;
        [SerializeField] private GameObject d10Prefab;
        [SerializeField] private GameObject d12Prefab;
        [SerializeField] private GameObject d20Prefab;

        [Header("Spawn Settings")]
        [SerializeField] private Vector3 rollDisplayOffset = new Vector3(0f, 1.8f, 0f);

        [Header("Animation Settings")]
        [SerializeField] private float rollAnimationDuration = 0.4f;
        [SerializeField] private float wobbleScale = 1.15f;
        [SerializeField] private float wobbleSpeed = 10f;

        private IDiceDisplayService service;

        // Active dice instance
        private GameObject currentDiceInstance;
        private RollDisplayPresenter currentRollPresenter;

        // ✅ FIX 1: Found at runtime, not assigned in Inspector
        private Transform playerTransform;

        #region Singleton

        private static DiceDisplayPresenter instance;
        public static DiceDisplayPresenter Instance => instance;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;

            InitializeService();
        }

        private void Start()
        {
            // ✅ FIX 1: Find spawned player at Start (after Photon spawns)
            StartCoroutine(FindPlayerDelayed());
        }

        #endregion

        #region Find Player

        // ✅ FIX 1: Find local player entity like PlayerHealthManager does
        private IEnumerator FindPlayerDelayed()
        {
            yield return new WaitForSeconds(0.5f);
            FindLocalPlayer();
        }

        private void FindLocalPlayer()
        {
            // Try "Player" tag first (multiplayer spawn)
            foreach (GameObject go in GameObject.FindGameObjectsWithTag("Player"))
            {
                PhotonView pv = go.GetComponent<PhotonView>();
                if (pv != null && pv.IsMine)
                {
                    playerTransform = go.transform;
                    Debug.Log($"[DiceDisplayPresenter] Found local player: {go.name}");
                    return;
                }
            }

            // Fallback: "PlayerEntity" tag (test scenes)
            foreach (GameObject go in GameObject.FindGameObjectsWithTag("PlayerEntity"))
            {
                PhotonView pv = go.GetComponent<PhotonView>();
                if (pv != null && pv.IsMine)
                {
                    playerTransform = go.transform;
                    Debug.Log($"[DiceDisplayPresenter] Found local player (PlayerEntity): {go.name}");
                    return;
                }
            }

            // Final fallback: no PhotonView (solo test scene)
            GameObject fallback = GameObject.FindGameObjectWithTag("Player");
            if (fallback == null)
                fallback = GameObject.FindGameObjectWithTag("PlayerEntity");

            if (fallback != null)
            {
                playerTransform = fallback.transform;
                Debug.Log($"[DiceDisplayPresenter] Found player (fallback): {fallback.name}");
                return;
            }

            Debug.LogWarning("[DiceDisplayPresenter] Local player not found! Will retry on ShowRoll.");
        }

        #endregion

        #region Initialization

        private void InitializeService()
        {
            model.d6Prefab = d6Prefab;
            model.d8Prefab = d8Prefab;
            model.d10Prefab = d10Prefab;
            model.d12Prefab = d12Prefab;
            model.d20Prefab = d20Prefab;
            model.rollDisplayOffset = rollDisplayOffset;
            model.rollAnimationDuration = rollAnimationDuration;
            model.wobbleScale = wobbleScale;
            model.wobbleSpeed = wobbleSpeed;

            service = new DiceDisplayService(model);
            service.Initialize(model);

            Debug.Log("[DiceDisplayPresenter] Initialized successfully");
        }

        #endregion

        #region Public API - Roll Display

        public void ShowRoll(int finalValue, CombatManager.Model.DiceTier tier)
        {
            // ✅ FIX 1: Retry finding player if not found yet
            if (playerTransform == null)
            {
                FindLocalPlayer();
                if (playerTransform == null)
                {
                    Debug.LogWarning("[DiceDisplayPresenter] Cannot show roll - player not found!");
                    return;
                }
            }

            if (service == null || !service.IsInitialized())
            {
                Debug.LogWarning("[DiceDisplayPresenter] Service not initialized!");
                return;
            }

            // Destroy previous dice if exists
            HideRoll();

            // ✅ FIX 2: Spawn dice at player position + offset immediately
            Vector3 spawnPos = playerTransform.position + rollDisplayOffset;
            currentDiceInstance = service.SpawnDice(tier, playerTransform);
            if (currentDiceInstance == null)
            {
                Debug.LogError("[DiceDisplayPresenter] Failed to spawn dice!");
                return;
            }

            // ✅ FIX 2: Force position immediately at spawn
            currentDiceInstance.transform.position = spawnPos;

            // Get or add RollDisplayPresenter on the dice prefab
            currentRollPresenter = currentDiceInstance.GetComponent<RollDisplayPresenter>();
            if (currentRollPresenter == null)
            {
                currentRollPresenter = currentDiceInstance.AddComponent<RollDisplayPresenter>();
            }

            // ✅ FIX 2: Initialize with player transform + offset so it FOLLOWS
            currentRollPresenter.Initialize(playerTransform, rollDisplayOffset);

            // Play roll animation
            currentRollPresenter.PlayRoll(
                finalValue,
                tier,
                service.GetRollAnimationDuration()
            );

            Debug.Log($"[DiceDisplayPresenter] Showing roll: {finalValue} ({tier}) above {playerTransform.name}");
        }

        public void HideRoll()
        {
            if (currentDiceInstance != null)
            {
                // ✅ Play disappear animation first, THEN destroy
                if (currentRollPresenter != null)
                {
                    currentRollPresenter.HideWithAnimation();
                    // Destroy after animation finishes (0.3s = fadeOutDuration)
                    Destroy(currentDiceInstance, 0.35f);
                }
                else
                {
                    service?.DespawnDice(currentDiceInstance);
                }

                currentDiceInstance = null;
                currentRollPresenter = null;
            }
        }

        #endregion

        #region Static Helpers

        // ✅ No playerTransform parameter needed - found automatically!
        public static void Show(int finalValue, CombatManager.Model.DiceTier tier)
        {
            if (Instance != null)
            {
                Instance.ShowRoll(finalValue, tier);
            }
            else
            {
                Debug.LogWarning("[DiceDisplayPresenter] Instance not found!");
            }
        }

        public static void Hide()
        {
            Instance?.HideRoll();
        }

        #endregion

        #region Public API - Settings

        public Vector3 GetRollDisplayOffset()
        {
            return service?.GetRollDisplayOffset() ?? new Vector3(0f, 1.8f, 0f);
        }

        public float GetRollAnimationDuration()
        {
            return service?.GetRollAnimationDuration() ?? 0.4f;
        }

        public GameObject GetDicePrefab(CombatManager.Model.DiceTier tier)
        {
            return service?.GetDicePrefab(tier);
        }

        #endregion

        #region Getters

        public bool IsInitialized() => service?.IsInitialized() ?? false;
        public IDiceDisplayService GetService() => service;
        public bool IsRolling() => currentRollPresenter?.IsRolling() ?? false;
        public int GetLastRollValue() => currentRollPresenter?.GetFinalValue() ?? 0;
        public Transform GetPlayerTransform() => playerTransform;

        #endregion
    }
}