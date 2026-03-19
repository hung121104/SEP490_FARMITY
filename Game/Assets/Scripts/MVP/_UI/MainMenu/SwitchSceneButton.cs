using UnityEngine;

public class SwitchSceneButton : MonoBehaviour
{
    [SerializeField] private string sceneName = "YourSceneName";
    void Awake()
    {
        var button = GetComponent<UnityEngine.UI.Button>();
        if (button != null)
            button.onClick.AddListener(OnButtonClicked);
    }

    private void OnButtonClicked()
    {
        LoadScene(sceneName);
    }

    public void LoadScene(string sceneName)
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
    }
}
