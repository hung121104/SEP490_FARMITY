using UnityEngine;
using CombatManager.Presenter;
using CombatManager.Model;

public class TestDiceDisplay : MonoBehaviour
{
    [SerializeField] private Transform playerTransform;

    private void Update()
    {
        // Press T → Show D6 roll
        if (Input.GetKeyDown(KeyCode.T))
        {
            int fakeRoll = Random.Range(1, 7);
            DiceDisplayPresenter.Show(fakeRoll, CombatManager.Model.DiceTier.D6, playerTransform);
            Debug.Log($"Test: Showing D6 roll = {fakeRoll}");
        }

        // Press H → Hide dice 
        if (Input.GetKeyDown(KeyCode.H))
        {
            DiceDisplayPresenter.Hide();
            Debug.Log("Test: Hiding dice");
        }

        // Press Y → Show D20 roll
        if (Input.GetKeyDown(KeyCode.Y))
        {
            int fakeRoll = Random.Range(1, 21);
            DiceDisplayPresenter.Show(fakeRoll, CombatManager.Model.DiceTier.D20, playerTransform);
            Debug.Log($"Test: Showing D20 roll = {fakeRoll}");
        }
    }
}