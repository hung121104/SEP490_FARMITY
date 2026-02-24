/// <summary>
/// Data container passed from any skill to SkillIndicatorManager.
/// Tells the manager what to display and how big.
/// </summary>
public struct SkillIndicatorData
{
    public SkillIndicatorManager.IndicatorType type;

    // Arrow
    public float arrowRange; // World units - exact skill range/dash distance

    // Cone (for later)
    public float coneRange;
    public float coneAngle;

    // Circle (for later)
    public float circleRadius;
    public float circleMaxRange;

    // Quick constructor for Arrow
    public static SkillIndicatorData Arrow(float range)
    {
        return new SkillIndicatorData
        {
            type = SkillIndicatorManager.IndicatorType.Arrow,
            arrowRange = range
        };
    }

    // Quick constructor for Cone (for later)
    public static SkillIndicatorData Cone(float range, float angle)
    {
        return new SkillIndicatorData
        {
            type = SkillIndicatorManager.IndicatorType.Cone,
            coneRange = range,
            coneAngle = angle
        };
    }

    // Quick constructor for Circle (for later)
    public static SkillIndicatorData Circle(float radius, float maxRange)
    {
        return new SkillIndicatorData
        {
            type = SkillIndicatorManager.IndicatorType.Circle,
            circleRadius = radius,
            circleMaxRange = maxRange
        };
    }
}