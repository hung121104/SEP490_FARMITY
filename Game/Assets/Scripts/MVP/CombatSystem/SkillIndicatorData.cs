/// <summary>
/// Data container for skill indicators.
/// Skills pass this to SkillIndicatorManager.ShowIndicator().
/// </summary>
public class SkillIndicatorData
{
    public SkillIndicatorManager.IndicatorType type;

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
            type = SkillIndicatorManager.IndicatorType.Arrow,
            arrowRange = range
        };
    }

    public static SkillIndicatorData Cone(float range, float angle)
    {
        return new SkillIndicatorData
        {
            type = SkillIndicatorManager.IndicatorType.Cone,
            coneRange = range,
            coneAngle = angle
        };
    }

    public static SkillIndicatorData Circle(float radius, float maxRange)
    {
        return new SkillIndicatorData
        {
            type = SkillIndicatorManager.IndicatorType.Circle,
            circleRadius = radius,
            circleMaxRange = maxRange
        };
    }

    #endregion
}