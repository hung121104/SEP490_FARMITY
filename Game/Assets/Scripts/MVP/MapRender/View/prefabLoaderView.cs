using UnityEngine;

public class prefabLoaderView : MonoBehaviour
{
    [Tooltip("Scene object to toggle.")]
    [SerializeField] private GameObject prefab;

    [SerializeField] private bool setActiveOnStart = true;

    void Start()
    {
        ApplyActiveState(setActiveOnStart);
    }

    [ContextMenu("Set Active")]
    public void ContextSetActive()
    {
        ApplyActiveState(true);
    }

    [ContextMenu("Set Inactive")]
    public void ContextSetInactive()
    {
        ApplyActiveState(false);
    }

    private void ApplyActiveState(bool isActive)
    {
        if (prefab == null)
        {
            Debug.LogWarning("prefabLoaderView: No target object assigned.");
            return;
        }

        prefab.SetActive(isActive);
    }
}
