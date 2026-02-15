using UnityEngine;

public class PointGiver : MonoBehaviour
{
    [SerializeField] private int pointsToAdd = 10;
    [SerializeField] private KeyCode givePointsKey = KeyCode.P;

    private StatsManager statsManager;

    private void Start()
    {
        statsManager = StatsManager.Instance;
    }

    private void Update()
    {
        if (Input.GetKeyDown(givePointsKey))
        {
            statsManager.AddPoints(pointsToAdd);
            Debug.Log($"Added {pointsToAdd} points!");
        }
    }
}