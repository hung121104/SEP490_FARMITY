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
        Cone,
        Circle
    }

    [Header("Indicator Prefabs - Assign in Inspector")]
    [SerializeField] private GameObject arrowIndicatorPrefab;
    [SerializeField] private GameObject coneIndicatorPrefab;
    [SerializeField] private GameObject circleIndicatorPrefab;

    private SkillIndicatorController arrowIndicator;
    private SkillIndicatorController coneIndicator;
    private SkillIndicatorController circleIndicator;
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
        // Arrow
        if (arrowIndicatorPrefab != null)
        {
            GameObject arrowGO = Instantiate(arrowIndicatorPrefab, transform);
            arrowIndicator = arrowGO.GetComponent<SkillIndicatorController>();
            if (arrowIndicator == null)
                Debug.LogWarning("SkillIndicatorManager: SkillIndicatorController missing on Arrow prefab!");
        }
        else
            Debug.LogWarning("SkillIndicatorManager: Arrow indicator prefab not assigned!");

        // Cone
        if (coneIndicatorPrefab != null)
        {
            GameObject coneGO = Instantiate(coneIndicatorPrefab, transform);
            coneIndicator = coneGO.GetComponent<SkillIndicatorController>();
            if (coneIndicator == null)
                Debug.LogWarning("SkillIndicatorManager: SkillIndicatorController missing on Cone prefab!");
        }
        else
            Debug.LogWarning("SkillIndicatorManager: Cone indicator prefab not assigned!");

        // Circle
        if (circleIndicatorPrefab != null)
        {
            GameObject circleGO = Instantiate(circleIndicatorPrefab, transform);
            circleIndicator = circleGO.GetComponent<SkillIndicatorController>();
            if (circleIndicator == null)
                Debug.LogWarning("SkillIndicatorManager: SkillIndicatorController missing on Circle prefab!");
        }
        else
            Debug.LogWarning("SkillIndicatorManager: Circle indicator prefab not assigned!");
    }

    #endregion

    #region Public API

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

            case IndicatorType.Cone:
                if (coneIndicator != null)
                {
                    coneIndicator.SetupCone(data.coneRange, data.coneAngle);
                    coneIndicator.Show();
                    activeIndicator = coneIndicator;
                }
                else
                    Debug.LogWarning("SkillIndicatorManager: Cone indicator not found!");
                break;

            case IndicatorType.Circle:
                if (circleIndicator != null)
                {
                    circleIndicator.SetupCircle(data.circleRadius, data.circleMaxRange);
                    circleIndicator.Show();
                    activeIndicator = circleIndicator;
                }
                else
                    Debug.LogWarning("SkillIndicatorManager: Circle indicator not found!");
                break;
        }
    }

    public void HideAll()
    {
        arrowIndicator?.Hide();
        coneIndicator?.Hide();
        circleIndicator?.Hide();

        activeIndicator = null;
        currentType = IndicatorType.None;
    }

    public Vector3 GetAimedDirection()
    {
        if (activeIndicator == null) return Vector3.right;
        return activeIndicator.GetAimedDirection();
    }

    public Vector3 GetAimedPosition()
    {
        if (activeIndicator == null) return Vector3.zero;
        return activeIndicator.GetAimedPosition();
    }

    public float GetAimedDistance()
    {
        if (activeIndicator == null) return 0f;
        return activeIndicator.GetAimedDistance();
    }

    public bool IsActive => currentType != IndicatorType.None;

    #endregion
}