using UnityEngine;
using System.Collections;
using CombatManager.Model;
using CombatManager.View;

namespace CombatManager.Presenter
{
    /// <summary>
    /// Presenter for individual dice roll display.
    /// Attached to each spawned dice prefab.
    /// Handles roll animation lifecycle and follow behavior.
    /// </summary>
    public class RollDisplayPresenter : MonoBehaviour
    {
        [Header("Follow Settings")]
        private Transform followTarget;
        private Vector3 followOffset;

        [Header("Roll State")]
        private int finalValue;
        private CombatManager.Model.DiceTier diceTier; // ✅ Explicit namespace
        private float duration;
        private bool isRolling = false;

        private Coroutine rollCoroutine;
        private RollDisplayView view;

        #region Unity Lifecycle

        private void Awake()
        {
            view = GetComponent<RollDisplayView>();
            if (view == null)
            {
                view = gameObject.AddComponent<RollDisplayView>();
            }
        }

        private void LateUpdate()
        {
            FollowTarget();
        }

        #endregion

        #region Initialization

        public void Initialize(Transform target, Vector3 offset)
        {
            followTarget = target;
            followOffset = offset;

            if (followTarget != null)
            {
                transform.position = followTarget.position + followOffset;
            }

            Debug.Log($"[RollDisplayPresenter] Initialized - following {target?.name}");
        }

        #endregion

        #region Follow Logic

        private void FollowTarget()
        {
            if (followTarget == null)
                return;

            Vector3 targetPosition = followTarget.position + followOffset;
            transform.position = targetPosition;
        }

        #endregion

        #region Roll Animation

        public void PlayRoll(int finalValue, CombatManager.Model.DiceTier tier, float duration) // ✅ Explicit namespace
        {
            this.finalValue = finalValue;
            this.diceTier = tier;
            this.duration = duration;

            if (rollCoroutine != null)
                StopCoroutine(rollCoroutine);

            rollCoroutine = StartCoroutine(RollRoutine());
        }

        private IEnumerator RollRoutine()
        {
            isRolling = true;
            float elapsed = 0f;
            int sides = (int)diceTier;

            // Animate random rolls during duration
            while (elapsed < duration)
            {
                int tempValue = Random.Range(1, sides + 1);
                view?.UpdateDisplay(tempValue);

                elapsed += Time.deltaTime;
                yield return null;
            }

            // Show final value
            view?.UpdateDisplay(finalValue);
            isRolling = false;
            rollCoroutine = null;

            Debug.Log($"[RollDisplayPresenter] Roll complete: {finalValue}");
        }

        #endregion

        #region Visibility

        public void Show()
        {
            gameObject.SetActive(true);
            view?.Show();
        }

        public void Hide()
        {
            view?.Hide();
        }

        #endregion

        #region Getters

        public bool IsRolling() => isRolling;
        public int GetFinalValue() => finalValue;
        public Transform GetFollowTarget() => followTarget;

        #endregion
    }
}