using UnityEngine;
using CombatManager.Model;
using CombatManager.Presenter;
using CombatManager.Service;

public class TestDiceSystem : MonoBehaviour
{
    private IDiceRollerService diceRoller;
    private IDamageCalculatorService damageCalc;

    private void Start()
    {
        // Initialize utility services
        diceRoller = new DiceRollerService();
        damageCalc = new DamageCalculatorService();
    }

    private void Update()
    {
        // Press 1-5 to test each dice tier
        if (Input.GetKeyDown(KeyCode.Alpha1)) TestRoll(CombatManager.Model.DiceTier.D6);
        if (Input.GetKeyDown(KeyCode.Alpha2)) TestRoll(CombatManager.Model.DiceTier.D8);
        if (Input.GetKeyDown(KeyCode.Alpha3)) TestRoll(CombatManager.Model.DiceTier.D10);
        if (Input.GetKeyDown(KeyCode.Alpha4)) TestRoll(CombatManager.Model.DiceTier.D12);
        if (Input.GetKeyDown(KeyCode.Alpha5)) TestRoll(CombatManager.Model.DiceTier.D20);
    }

    private void TestRoll(CombatManager.Model.DiceTier tier)
    {
        // Roll dice
        int roll = diceRoller.Roll(tier);

        // Calculate damage (example: strength=10, multiplier=1.5)
        int damage = damageCalc.CalculateSkillDamage(roll, 10, 1.5f);

        Debug.Log($"Rolled {tier}: {roll} → Damage: {damage}");

        // Show visual
        Vector3 pos = transform.position;
        DiceDisplayPresenter.Show(tier, roll, pos);
    }
}