using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Attach to a UI Canvas root (or AudioManager GO).
/// Listens for common UI interactions and plays corresponding sounds.
/// 
/// For buttons: call UISoundPlayer.Instance.PlayClick() from onClick,
/// or use the auto-hook on Awake to patch all tagged buttons.
/// </summary>
public class UISoundPlayer : MonoBehaviour
{
    public static UISoundPlayer Instance { get; private set; }

    [Header("Sound Mappings")]
    [SerializeField] private SoundId buttonClickSound = SoundId.UIButtonClick;
    [SerializeField] private SoundId panelOpenSound = SoundId.UIPanelOpen;
    [SerializeField] private SoundId panelCloseSound = SoundId.UIPanelClose;
    [SerializeField] private SoundId errorSound = SoundId.UIError;
    [SerializeField] private SoundId confirmSound = SoundId.UIConfirm;
    [SerializeField] private SoundId cancelSound = SoundId.UICancel;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;
    }

    /// <summary>Call from Button.onClick or any click handler.</summary>
    public void PlayClick()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayUI(buttonClickSound);
    }

    public void PlayPanelOpen()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayUI(panelOpenSound);
    }

    public void PlayPanelClose()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayUI(panelCloseSound);
    }

    public void PlayError()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayUI(errorSound);
    }

    public void PlayConfirm()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayUI(confirmSound);
    }

    public void PlayCancel()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayUI(cancelSound);
    }
}
