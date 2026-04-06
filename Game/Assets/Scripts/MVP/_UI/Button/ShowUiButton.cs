using UnityEngine;

public class ShowUiButton : MonoBehaviour
{
    [SerializeField] private CanvasGroup uiCanvasGroup;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        uiCanvasGroup.Hide();
        GetComponent<UnityEngine.UI.Button>().onClick.AddListener(ShowUI);
    }

    public void ShowUI()
    {
        Debug.Log("ShowUI called");
        uiCanvasGroup.Show();
        ToggleInGameSettingMenu.SetGlobalAllowToggleState(false);
    }
}
