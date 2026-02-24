using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Controls a single indicator canvas.
/// Sits on Indicator_Arrow/Cone/Circle GameObject.
/// Gets told what to do by SkillIndicatorManager.
/// </summary>
public class SkillIndicatorController : MonoBehaviour
{
    public enum IndicatorType
    {
        Arrow,
        Cone,
        Circle
    }

    [Header("References - Assign in Inspector")]
    [SerializeField] private Canvas indicatorCanvas;
    [SerializeField] private RectTransform imageRectTransform;

    [Header("Calibration")]
    [Tooltip("How many world units does the image cover at Scale Y = 1?")]
    [SerializeField] private float referenceWorldUnitsY = 100f;
    [Tooltip("Fixed visual width - never changes")]
    [SerializeField] private float fixedScaleX = 0.01f;

    // Runtime state
    private IndicatorType indicatorType;
    private Transform playerTransform;
    private Camera mainCamera;
    private Vector3 mouseWorldPosition;
    private Vector3 currentDirection = Vector3.up;
    private float currentRange = 3f;
    private bool isVisible = false;

    #region Unity Lifecycle

    private void Awake()
    {
        mainCamera = Camera.main;

        GameObject playerObj = GameObject.FindGameObjectWithTag("PlayerEntity");
        if (playerObj != null)
            playerTransform = playerObj.transform;
        else
            Debug.LogWarning("SkillIndicatorController: PlayerEntity tag not found!");

        Hide();
    }

    private void Update()
    {
        if (!isVisible || playerTransform == null)
            return;

        // Always stay at player position
        transform.position = playerTransform.position;

        UpdateMouseWorldPosition();

        switch (indicatorType)
        {
            case IndicatorType.Arrow:
            case IndicatorType.Cone:
                UpdateRotationIndicator();
                break;

            case IndicatorType.Circle:
                UpdatePositionIndicator();
                break;
        }
    }

    #endregion

    #region Setup

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
        // TODO: Apply cone scale
    }

    public void SetupCircle(float radius, float maxRange)
    {
        indicatorType = IndicatorType.Circle;
        currentRange = maxRange;
        // TODO: Apply circle scale
    }

    #endregion

    #region Mouse Tracking

    private void UpdateMouseWorldPosition()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            return;
        }

        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        Plane plane = new Plane(Vector3.forward, playerTransform.position);

        if (plane.Raycast(ray, out float dist))
            mouseWorldPosition = ray.GetPoint(dist);
    }

    #endregion

    #region Indicator Updates

    private void UpdateRotationIndicator()
    {
        Vector3 direction = mouseWorldPosition - playerTransform.position;
        direction.z = 0f;

        if (direction.magnitude < 0.01f)
            return;

        currentDirection = direction.normalized;

        // Sprite points UP so -90 degree offset
        float angle = Mathf.Atan2(currentDirection.y, currentDirection.x) * Mathf.Rad2Deg - 90f;
        indicatorCanvas.transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    private void UpdatePositionIndicator()
    {
        Vector3 direction = mouseWorldPosition - playerTransform.position;
        direction.z = 0f;

        float clampedDistance = Mathf.Clamp(direction.magnitude, 0f, currentRange);
        currentDirection = direction.normalized;

        transform.position = playerTransform.position + currentDirection * clampedDistance;
    }

    #endregion

    #region Scale

    private void ApplyArrowScale(float range)
    {
        if (imageRectTransform == null)
            return;

        float scaleY = range / referenceWorldUnitsY;
        imageRectTransform.localScale = new Vector3(fixedScaleX, scaleY, 1f);
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

    #region Public API

    public Vector3 GetAimedDirection() => currentDirection;
    public Vector3 GetAimedPosition() => playerTransform.position + currentDirection * currentRange;
    public float GetAimedDistance() => currentRange;

    #endregion
}