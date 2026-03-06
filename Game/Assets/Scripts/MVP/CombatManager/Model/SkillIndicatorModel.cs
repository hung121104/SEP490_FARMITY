using UnityEngine;

namespace CombatManager.Model
{
    /// <summary>
    /// Data model for skill indicator system.
    /// Mirrors SkillIndicatorData from CombatSystem (kept for legacy).
    /// Contains indicator type + all shape parameters.
    /// </summary>
    public enum IndicatorType
    {
        None,
        Arrow,
        Cone,
        Circle
    }

    [System.Serializable]
    public class SkillIndicatorData
    {
        public IndicatorType type;

        // Arrow
        public float arrowRange;

        // Cone
        public float coneRange;
        public float coneAngle;

        // Circle
        public float circleRadius;
        public float circleMaxRange;

        #region Static Factories

        public static SkillIndicatorData Arrow(float range)
        {
            return new SkillIndicatorData
            {
                type = IndicatorType.Arrow,
                arrowRange = range
            };
        }

        public static SkillIndicatorData Cone(float range, float angle)
        {
            return new SkillIndicatorData
            {
                type = IndicatorType.Cone,
                coneRange = range,
                coneAngle = angle
            };
        }

        public static SkillIndicatorData Circle(float radius, float maxRange)
        {
            return new SkillIndicatorData
            {
                type = IndicatorType.Circle,
                circleRadius = radius,
                circleMaxRange = maxRange
            };
        }

        #endregion
    }

    [System.Serializable]
    public class SkillIndicatorModel
    {
        [Header("Prefabs")]
        public GameObject arrowPrefab;
        public GameObject conePrefab;
        public GameObject circlePrefab;

        [Header("Runtime State")]
        public IndicatorType currentType = IndicatorType.None;
        public bool isInitialized = false;

        public bool IsActive => currentType != IndicatorType.None;
    }
}