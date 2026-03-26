using UnityEngine;

public class LoadMapScript : MonoBehaviour
{
    [SerializeField] private PolygonCollider2D loadTriggerCollider;

    [Tooltip("Scene object to toggle when a player enters this trigger.")]
    [SerializeField] private GameObject prefab;

    [Tooltip("Active state to apply when a player enters this trigger.")]
    [SerializeField] protected bool activeStateOnTrigger = true;

    protected virtual bool TargetActiveState => activeStateOnTrigger;

    private void Reset()
    {
        loadTriggerCollider = GetComponent<PolygonCollider2D>();
    }

    private void Awake()
    {
        if (loadTriggerCollider == null)
        {
            loadTriggerCollider = GetComponent<PolygonCollider2D>();
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision == null || !collision.gameObject.CompareTag("PlayerEntity") || prefab == null)
        {
            return;
        }

        prefab.SetActive(TargetActiveState);
    }
}
