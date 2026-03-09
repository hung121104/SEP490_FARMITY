using System;
using UnityEngine;
using UnityEngine.UI;

public class BookPanelController : MonoBehaviour
{
    [Header("Canvas Groups")]
    [SerializeField] private CanvasGroup loginCanvasGroup;
    [SerializeField] private CanvasGroup registerCanvasGroup;
    [SerializeField] private CanvasGroup titleCanvasGroup;

    [Header("Buttons")]
    [SerializeField] private Button openLoginBtn;
    [SerializeField] private Button closeLoginBtn;
    [SerializeField] private Button openRegisterBtn;
    [SerializeField] private Button closeRegisterBtn;
    [SerializeField] private Button openTitleBtn;

    public event Action OnShowLogin;
    public event Action OnShowRegister;
    public event Action OnShowTitle;

    private void Awake()
    {
        BindButtons();
    }

    private void BindButtons()
    {
        if (openLoginBtn    != null) openLoginBtn.onClick.AddListener(ShowLogin);
        if (closeLoginBtn   != null) closeLoginBtn.onClick.AddListener(ShowTitle);
        if (openRegisterBtn != null) openRegisterBtn.onClick.AddListener(ShowRegister);
        if (closeRegisterBtn != null) closeRegisterBtn.onClick.AddListener(ShowTitle);
        if (openTitleBtn    != null) openTitleBtn.onClick.AddListener(ShowTitle);
    }

    public void ShowLogin()
    {
        registerCanvasGroup.Hide();
        loginCanvasGroup.Show();
        OnShowLogin?.Invoke();
    }

    public void ShowRegister()
    {
        loginCanvasGroup.Hide();
        registerCanvasGroup.Show();
        OnShowRegister?.Invoke();
    }

    public void ShowTitle()
    {
        loginCanvasGroup.Hide();
        registerCanvasGroup.Hide();
        OnShowTitle?.Invoke();
    }
}
