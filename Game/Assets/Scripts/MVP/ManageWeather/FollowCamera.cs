using UnityEngine;

public class FollowCamera : MonoBehaviour
{
    private Transform camTransform;

    [SerializeField] private float heightOffset = 10f;

    private void Start()
    {
        if (Camera.main != null)
        {
            camTransform = Camera.main.transform;
        }
    }

    private void LateUpdate()
    {
        if (camTransform == null) return;

        transform.position = new Vector3(
            camTransform.position.x,
            camTransform.position.y + heightOffset,
            0f
        );
    }
}
