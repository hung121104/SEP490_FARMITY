using UnityEngine;

public class GlobalPos : MonoBehaviour
{
    [SerializeField] private Vector3 globalPosition;

    void Update()
    {
        globalPosition = transform.position;
    }
}
