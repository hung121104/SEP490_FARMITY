using UnityEngine;

public class MyworldListRouter : MonoBehaviour
{
    [SerializeField]
    private GameObject editPanel;
    public void toggleEditing()
    {
        editPanel.SetActive(!editPanel.activeInHierarchy);
    }
}
