using UnityEngine;
using System.Collections;

/// <summary>
/// Heavy Swing skill - Calls cone indicator in mouse direction.
/// No damage logic yet - indicator test only.
/// Activation: Alpha 4
/// </summary>
public class HeavySwing : SkillBase
{
    #region Serialized Fields

    [Header("Heavy Swing Settings")]
    [SerializeField] private float swingRange = 5f;
    [SerializeField] private float swingAngle = 90f;
    [SerializeField] private float attackAnimationDuration = 0.6f;

    #endregion

    #region SkillBase Implementation

    protected override SkillIndicatorData GetIndicatorData()
        => SkillIndicatorData.Cone(swingRange, swingAngle);

    protected override IEnumerator OnExecute(int diceRoll)
    {
        // TODO: Add heavy swing damage logic here
        yield return new WaitForSeconds(attackAnimationDuration);
    }

    #endregion
}