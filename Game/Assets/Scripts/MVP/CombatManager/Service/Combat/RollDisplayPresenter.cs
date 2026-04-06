using UnityEngine;
using System.Collections;
using CombatManager.Model;
using CombatManager.View;

namespace CombatManager.Presenter
{
    public class RollDisplayPresenter : MonoBehaviour
    {
        private Transform followTarget;
        private Vector3 followOffset;
        private int finalValue;
        private CombatManager.Model.DiceTier diceTier;
        private float duration;
        private bool isRolling = false;

        private Coroutine rollCoroutine;
        private RollDisplayView view;

        #region Unity Lifecycle

        private void Awake()
        {
            view = GetComponent<RollDisplayView>();
            if (view == null)
                view = gameObject.AddComponent<RollDisplayView>();
        }

        private void LateUpdate()
        {
            if (followTarget == null) return;
            transform.position = followTarget.position + followOffset;
        }

        #endregion

        #region Initialization

        public void Initialize(Transform target, Vector3 offset)
        {
            followTarget = target;
            followOffset = offset;

            if (followTarget != null)
                transform.position = followTarget.position + followOffset;

            // ✅ Trigger popup animation on spawn
            view?.Show();

            Debug.Log($"[RollDisplayPresenter] Initialized - following {target?.name}");
        }

        #endregion

        #region Roll Animation

        public void PlayRoll(int finalValue, CombatManager.Model.DiceTier tier, float duration)
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

            // Animate random numbers cycling during roll duration
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

        public void HideWithAnimation()
        {
            // ✅ Trigger disappear animation (fades out then destroys itself)
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