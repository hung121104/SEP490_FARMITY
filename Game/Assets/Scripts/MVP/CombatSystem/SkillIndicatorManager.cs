using UnityEngine;

/// <summary>
/// Global manager inside CombatSystem.
/// Any skill calls ShowIndicator() with SkillIndicatorData.
/// Only one indicator active at a time.
/// Spawns indicator prefabs at runtime - no dependency on PlayerEntity.
/// </summary>
public class SkillIndicatorManager : MonoBehaviour
{
    public static SkillIndicatorManager Instance { get; private set; }

    public enum IndicatorType
    {
        None,
        Arrow,
        Cone,    // For later
        Circle   // For later
    }

    [Header("Indicator Prefabs - Assign in Inspector")]
    [SerializeField] private GameObject arrowIndicatorPrefab;
    // [SerializeField] private GameObject coneIndicatorPrefab;   // For later
    // [SerializeField] private GameObject circleIndicatorPrefab; // For later

    private SkillIndicatorController arrowIndicator;
    private IndicatorType currentType = IndicatorType.None;
    private SkillIndicatorController activeIndicator = null;

    #region Unity Lifecycle

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        SpawnIndicators();
        HideAll();
    }

    #endregion

    #region Initialization

    private void SpawnIndicators()
    {
        // Spawn Arrow indicator
        if (arrowIndicatorPrefab != null)
        {
            GameObject arrowGO = Instantiate(arrowIndicatorPrefab, transform);
            arrowIndicator = arrowGO.GetComponent<SkillIndicatorController>();

            if (arrowIndicator == null)
                Debug.LogWarning("SkillIndicatorManager: SkillIndicatorController missing on Arrow prefab!");
        }
        else
            Debug.LogWarning("SkillIndicatorManager: Arrow indicator prefab not assigned!");
    }

    #endregion

    #region Public API

    /// <summary>
    /// Call this from any skill to show indicator.
    /// Pass SkillIndicatorData with all needed values.
    /// </summary>
    public void ShowIndicator(SkillIndicatorData data)
    {
        HideAll();
        currentType = data.type;

        switch (data.type)
        {
            case IndicatorType.Arrow:
                if (arrowIndicator != null)
                {
                    arrowIndicator.SetupArrow(data.arrowRange);
                    arrowIndicator.Show();
                    activeIndicator = arrowIndicator;
                }
                else
                    Debug.LogWarning("SkillIndicatorManager: Arrow indicator not found!");
                break;

            // Cone and Circle - coming later
        }
    }

    /// <summary>
    /// Hide all indicators.
    /// Call when skill confirms, cancels or ends.
    /// </summary>
    public void HideAll()
    {
        arrowIndicator?.Hide();
        // coneIndicator?.Hide();
        // circleIndicator?.Hide();

        activeIndicator = null;
        currentType = IndicatorType.None;
    }

    /// <summary>
    /// Get aimed direction from active indicator
    /// </summary>
    public Vector3 GetAimedDirection()
    {
        if (activeIndicator == null) return Vector3.right;
        return activeIndicator.GetAimedDirection();
    }

    /// <summary>
    /// Get exact aimed world position from active indicator
    /// </summary>
    public Vector3 GetAimedPosition()
    {
        if (activeIndicator == null) return Vector3.zero;
        return activeIndicator.GetAimedPosition();
    }

    /// <summary>
    /// Get aimed distance from active indicator
    /// </summary>
    public float GetAimedDistance()
    {
        if (activeIndicator == null) return 0f;
        return activeIndicator.GetAimedDistance();
    }

    public bool IsActive => currentType != IndicatorType.None;

    #endregion
}