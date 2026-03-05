using UnityEngine;
using System.Collections;

/// <summary>
/// Lightning Strike skill - Calls circle indicator at mouse position.
/// No damage logic yet - indicator test only.
/// Activation: Alpha 3
/// </summary>
public class LightningStrike : SkillBase
{
    #region Serialized Fields

    [Header("Lightning Strike Settings")]
    [SerializeField] private float strikeRadius = 3f;
    [SerializeField] private float strikeMaxRange = 10f;
    [SerializeField] private float attackAnimationDuration = 0.5f;

    #endregion

    #region SkillBase Implementation

    protected override SkillIndicatorData GetIndicatorData()
        => SkillIndicatorData.Circle(strikeRadius, strikeMaxRange);

    protected override IEnumerator OnExecute(int diceRoll)
    {
        // TODO: Add lightning strike damage logic here
        yield return new WaitForSeconds(attackAnimationDuration);
    }

    #endregion
}