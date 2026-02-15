using UnityEngine;

public class RainZoneController : MonoBehaviour
{
    private Transform target;

    [Header("Zone Settings")]
    [SerializeField] private float teleportRadius = 25f; // bán kính hình tròn
    [SerializeField] private float heightOffset = 20f;

    private Vector3 currentCenter;

    private void Start()
    {
        if (target == null && Camera.main != null)
        {
            target = Camera.main.transform;
        }

        MoveZone();
    }

    private void Update()
    {
        if (target == null) return;

        float distance = Vector2.Distance(
            new Vector2(target.position.x, target.position.y),
            new Vector2(currentCenter.x, currentCenter.y)
        );

        if (distance > teleportRadius)
        {
            MoveZone();
        }
    }

    private void MoveZone()
    {
        currentCenter = new Vector3(
            target.position.x,
            target.position.y,
            0f
        );

        transform.position = new Vector3(
            currentCenter.x,
            currentCenter.y + heightOffset,
            0f
        );
    }
}