using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// MVP View — Audio Settings panel.
/// Binds four volume sliders to AudioManager channels.
/// Values persist across sessions via PlayerPrefs.
/// The matching PlayerPrefs keys are also read by AudioManager.Awake()
/// to restore volumes before this panel is ever opened.
/// </summary>
public class AudioSettingsView : MonoBehaviour
{
    // ── PlayerPrefs keys (shared with AudioManager's startup load) ───────────
    public const string KEY_MASTER  = "Audio_MasterVol";
    public const string KEY_SFX     = "Audio_SFXVol";
    public const string KEY_UI      = "Audio_UIVol";
    public const string KEY_AMBIENT = "Audio_AmbientVol";

    private const float DEFAULT_VOLUME = 1f;

    [Header("Sliders")]
    [SerializeField] private Slider masterSlider;
    [SerializeField] private Slider sfxSlider;
    [SerializeField] private Slider uiSlider;
    [SerializeField] private Slider ambientSlider;

    private bool _listenersAttached;

    // ── Lifecycle ────────────────────────────────────────────────────────────

    private void OnEnable()
    {
        LoadAndApply();
        AttachListeners();
    }

    private void OnDisable()
    {
        DetachListeners();
    }

    // ── Initialise ───────────────────────────────────────────────────────────

    private void LoadAndApply()
    {
        float master  = PlayerPrefs.GetFloat(KEY_MASTER,  DEFAULT_VOLUME);
        float sfx     = PlayerPrefs.GetFloat(KEY_SFX,     DEFAULT_VOLUME);
        float ui      = PlayerPrefs.GetFloat(KEY_UI,      DEFAULT_VOLUME);
        float ambient = PlayerPrefs.GetFloat(KEY_AMBIENT, DEFAULT_VOLUME);

        SetSliderSilent(masterSlider,  master);
        SetSliderSilent(sfxSlider,     sfx);
        SetSliderSilent(uiSlider,      ui);
        SetSliderSilent(ambientSlider, ambient);

        if (AudioManager.Instance == null) return;
        AudioManager.Instance.SetMasterVolume(master);
        AudioManager.Instance.SetSFXVolume(sfx);
        AudioManager.Instance.SetUIVolume(ui);
        AudioManager.Instance.SetAmbientVolume(ambient);
    }

    // ── Listener wiring ──────────────────────────────────────────────────────

    private void AttachListeners()
    {
        if (_listenersAttached) return;
        masterSlider?.onValueChanged.AddListener(OnMasterChanged);
        sfxSlider?.onValueChanged.AddListener(OnSFXChanged);
        uiSlider?.onValueChanged.AddListener(OnUIChanged);
        ambientSlider?.onValueChanged.AddListener(OnAmbientChanged);
        _listenersAttached = true;
    }

    private void DetachListeners()
    {
        masterSlider?.onValueChanged.RemoveListener(OnMasterChanged);
        sfxSlider?.onValueChanged.RemoveListener(OnSFXChanged);
        uiSlider?.onValueChanged.RemoveListener(OnUIChanged);
        ambientSlider?.onValueChanged.RemoveListener(OnAmbientChanged);
        _listenersAttached = false;
    }

    // ── Slider callbacks ─────────────────────────────────────────────────────

    private void OnMasterChanged(float value)
    {
        AudioManager.Instance?.SetMasterVolume(value);
        PlayerPrefs.SetFloat(KEY_MASTER, value);
        PlayerPrefs.Save();
    }

    private void OnSFXChanged(float value)
    {
        AudioManager.Instance?.SetSFXVolume(value);
        PlayerPrefs.SetFloat(KEY_SFX, value);
        PlayerPrefs.Save();
    }

    private void OnUIChanged(float value)
    {
        AudioManager.Instance?.SetUIVolume(value);
        PlayerPrefs.SetFloat(KEY_UI, value);
        PlayerPrefs.Save();
    }

    private void OnAmbientChanged(float value)
    {
        AudioManager.Instance?.SetAmbientVolume(value);
        PlayerPrefs.SetFloat(KEY_AMBIENT, value);
        PlayerPrefs.Save();
    }

    // ── Public API ───────────────────────────────────────────────────────────

    /// <summary>Resets all volumes to 1 and re-applies immediately.</summary>
    public void ResetToDefaults()
    {
        PlayerPrefs.DeleteKey(KEY_MASTER);
        PlayerPrefs.DeleteKey(KEY_SFX);
        PlayerPrefs.DeleteKey(KEY_UI);
        PlayerPrefs.DeleteKey(KEY_AMBIENT);
        PlayerPrefs.Save();
        LoadAndApply();
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    /// <summary>Assigns slider value without firing onValueChanged.</summary>
    private static void SetSliderSilent(Slider slider, float value)
    {
        if (slider == null) return;
        slider.SetValueWithoutNotify(Mathf.Clamp(value, slider.minValue, slider.maxValue));
    }
}
