using UnityEngine;
using UnityEngine.UI;
using CombatManager.Model;

namespace CombatManager.View
{
    /// <summary>
    /// View for a single skill indicator (Arrow / Cone / Circle).
    /// Sits on Indicator_Arrow / Indicator_Cone / Indicator_Circle prefab.
    /// Mirrors SkillIndicatorController from CombatSystem (kept for legacy).
    /// Tracks mouse, updates rotation/position/scale each frame.
    /// </summary>
    public class IndicatorView : MonoBehaviour
    {
        [Header("References - Assign in Inspector")]
        [SerializeField] private Canvas indicatorCanvas;
        [SerializeField] private RectTransform imageRectTransform;

        [Header("Calibration - Arrow & Cone")]
        [Tooltip("How many world units does the image cover at Scale Y = 1?")]
        [SerializeField] private float referenceWorldUnitsY = 100f;
        [Tooltip("Fixed visual width - never changes")]
        [SerializeField] private float fixedScaleX = 0.01f;

        [Header("Calibration - Cone")]
        [Tooltip("How wide is the cone sprite at Scale X = 1?")]
        [SerializeField] private float referenceConeWidthUnits = 100f;
        [Tooltip("How far forward to offset cone from center point")]
        [SerializeField] private float coneForwardOffset = 0.5f;

        [Header("Calibration - Circle")]
        [Tooltip("How many world units does the circle image cover at Scale = 1?")]
        [SerializeField] private float referenceCircleUnits = 100f;

        private IndicatorType indicatorType;
        private Transform centerPoint;
        private Camera mainCamera;
        private Vector3 mouseWorldPosition;
        private Vector3 currentDirection = Vector3.up;
        private float currentRange = 3f;
        private float currentRadius = 1f;
        private bool isVisible = false;

        #region Unity Lifecycle

        private void Awake()
        {
            Hide();
        }

        private void Update()
        {
            if (!isVisible || centerPoint == null)
                return;

            UpdateMouseWorldPosition();

            switch (indicatorType)
            {
                case IndicatorType.Arrow:
                    transform.position = centerPoint.position;
                    UpdateRotationIndicator();
                    break;

                case IndicatorType.Cone:
                    UpdateConePosition();
                    UpdateRotationIndicator();
                    break;

                case IndicatorType.Circle:
                    UpdateCircleIndicator();
                    break;
            }
        }

        #endregion

        #region Setup - Called by SkillIndicatorPresenter

        /// <summary>
        /// Called once after spawn to provide player references.
        /// </summary>
        public void SetReferences(Transform center, Camera cam)
        {
            centerPoint = center;
            mainCamera = cam;
        }

        public void SetupArrow(float range)
        {
            indicatorType = IndicatorType.Arrow;
            currentRange = range;
            ApplyArrowScale(range);
        }

        public void SetupCone(float range, float angle)
        {
            indicatorType = IndicatorType.Cone;
            currentRange = range;
            ApplyConeScale(range, angle);
        }

        public void SetupCircle(float radius, float maxRange)
        {
            indicatorType = IndicatorType.Circle;
            currentRadius = radius;
            currentRange = maxRange;
            ApplyCircleScale(radius);
        }

        #endregion

        #region Mouse Tracking

        private void UpdateMouseWorldPosition()
        {
            if (mainCamera == null)
                mainCamera = Camera.main;
            if (mainCamera == null || centerPoint == null)
                return;

            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            Plane plane = new Plane(Vector3.forward, centerPoint.position);

            if (plane.Raycast(ray, out float dist))
                mouseWorldPosition = ray.GetPoint(dist);
        }

        #endregion

        #region Indicator Updates

        private void UpdateRotationIndicator()
        {
            Vector3 direction = mouseWorldPosition - centerPoint.position;
            direction.z = 0f;

            if (direction.magnitude < 0.01f)
                return;

            currentDirection = direction.normalized;

            float angle = Mathf.Atan2(currentDirection.y, currentDirection.x)
                          * Mathf.Rad2Deg - 90f;
            indicatorCanvas.transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }

        private void UpdateCircleIndicator()
        {
            Vector3 direction = mouseWorldPosition - centerPoint.position;
            direction.z = 0f;

            float distance = Mathf.Clamp(direction.magnitude, 0f, currentRange);
            currentDirection = direction.magnitude > 0.01f
                ? direction.normalized
                : Vector3.right;

            transform.position = centerPoint.position + currentDirection * distance;
        }

        private void UpdateConePosition()
        {
            Vector3 direction = mouseWorldPosition - centerPoint.position;
            direction.z = 0f;

            if (direction.magnitude > 0.01f)
                currentDirection = direction.normalized;

            transform.position = centerPoint.position
                + currentDirection * coneForwardOffset;
        }

        #endregion

        #region Scale

        private void ApplyArrowScale(float range)
        {
            if (imageRectTransform == null) return;
            float scaleY = range / referenceWorldUnitsY;
            imageRectTransform.localScale = new Vector3(fixedScaleX, scaleY, 1f);
        }

        private void ApplyConeScale(float range, float angle)
        {
            if (imageRectTransform == null) return;
            float scaleY = range / referenceWorldUnitsY;
            float halfAngleRad = angle * 0.5f * Mathf.Deg2Rad;
            float coneWidth = 2f * range * Mathf.Tan(halfAngleRad);
            float scaleX = coneWidth / referenceConeWidthUnits;
            imageRectTransform.localScale = new Vector3(scaleX, scaleY, 1f);
        }

        private void ApplyCircleScale(float radius)
        {
            if (imageRectTransform == null) return;
            float scale = (radius * 2f) / referenceCircleUnits;
            imageRectTransform.localScale = new Vector3(scale, scale, 1f);
        }

        #endregion

        #region Show / Hide

        public void Show()
        {
            isVisible = true;
            if (indicatorCanvas != null)
                indicatorCanvas.gameObject.SetActive(true);
        }

        public void Hide()
        {
            isVisible = false;
            if (indicatorCanvas != null)
                indicatorCanvas.gameObject.SetActive(false);
        }

        #endregion

        #region Public Getters

        public Vector3 GetAimedDirection() => currentDirection;
        public Vector3 GetAimedPosition() => transform.position;
        public float GetAimedDistance() => currentRange;

        #endregion
    }
}