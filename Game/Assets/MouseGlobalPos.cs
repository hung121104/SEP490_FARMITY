using UnityEngine;

[ExecuteAlways]
public class MouseGlobalPos : MonoBehaviour
{
    [SerializeField] private Camera targetCamera;
    [SerializeField] private float planeZ;
    [SerializeField] private Vector2 mouseWorldPosition;

    public Vector2 MouseWorldPosition => mouseWorldPosition;

    private void Reset()
    {
        targetCamera = Camera.main;
    }

    private void Awake()
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }
    }

    private void Update()
    {
        UpdateMouseWorldPosition();
    }

    private void UpdateMouseWorldPosition()
    {
        if (targetCamera == null)
        {
            return;
        }

        Ray mouseRay = targetCamera.ScreenPointToRay(Input.mousePosition);
        Plane worldPlane = new Plane(Vector3.forward, new Vector3(0f, 0f, planeZ));

        if (!worldPlane.Raycast(mouseRay, out float distanceToPlane))
        {
            return;
        }

        Vector3 worldPoint = mouseRay.GetPoint(distanceToPlane);
        mouseWorldPosition = new Vector2(worldPoint.x, worldPoint.y);
    }
}
