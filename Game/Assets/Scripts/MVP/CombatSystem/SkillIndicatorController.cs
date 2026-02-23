using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Sits on Indicator_Arrow/Cone/Circle GameObject.
/// Handles visuals for ONE indicator type.
/// Gets told what to do by SkillIndicatorManager.
/// </summary>
public class SkillIndicatorController : MonoBehaviour
{
    [Header("References - Assign in Inspector")]
    [SerializeField] private Canvas indicatorCanvas;
    [SerializeField] private RectTransform imageRectTransform;

    [Header("Calibration")]
    [Tooltip("How many world units does the image cover at Scale Y = 1?")]
    [SerializeField] private float referenceWorldUnitsY = 1f;
    [Tooltip("Fixed visual width - never changes")]
    [SerializeField] private float fixedScaleX = 0.05f;

    // Runtime state
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

        // Find PlayerEntity
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
        UpdateRotation();
    }

    #endregion

    #region Setup - Called by SkillIndicatorManager

    /// <summary>
    /// Setup arrow with exact world unit range
    /// </summary>
    public void SetupArrow(float range)
    {
        currentRange = range;
        ApplyArrowScale(range);
    }

    #endregion

    #region Mouse & Rotation

    private void UpdateMouseWorldPosition()
    {
        if (mainCamera == null) return;

        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        Plane plane = new Plane(Vector3.forward, playerTransform.position);

        if (plane.Raycast(ray, out float dist))
            mouseWorldPosition = ray.GetPoint(dist);
    }

    private void UpdateRotation()
    {
        Vector3 direction = mouseWorldPosition - playerTransform.position;
        direction.z = 0f;

        if (direction.magnitude < 0.01f)
            return;

        currentDirection = direction.normalized;

        // Sprite points UP
        // Atan2 gives angle from right (0°)
        // -90° offset aligns it to up
        float angle = Mathf.Atan2(currentDirection.y, currentDirection.x) * Mathf.Rad2Deg - 90f;
        indicatorCanvas.transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    #endregion

    #region Scale

    /// <summary>
    /// Scale arrow Y to match exact world unit range.
    /// X stays fixed for visual width.
    /// </summary>
    private void ApplyArrowScale(float range)
    {
        if (imageRectTransform == null)
            return;

        // scaleY = range / referenceWorldUnitsY
        // This makes the arrow tip land exactly at 'range' world units from player
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