using UnityEngine;

public class AuthenticationUIManager : MonoBehaviour
{
    [SerializeField]
    private CanvasGroup loginCanvasGroup;
    [SerializeField]
    private CanvasGroup titleCanvasGroup;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public void ShowLogin()
    {


        loginCanvasGroup.alpha = 1f;
        loginCanvasGroup.interactable = true;
        loginCanvasGroup.blocksRaycasts = true;
        titleCanvasGroup.alpha = 0f;
        titleCanvasGroup.interactable = false;
        titleCanvasGroup.blocksRaycasts = false;
    }
    public void ShowTitle()
    {
        loginCanvasGroup.alpha = 0f;
        loginCanvasGroup.interactable = false;
        loginCanvasGroup.blocksRaycasts = false;
        titleCanvasGroup.alpha = 1f;
        titleCanvasGroup.interactable = true;
        titleCanvasGroup.blocksRaycasts = true;
    }
}
