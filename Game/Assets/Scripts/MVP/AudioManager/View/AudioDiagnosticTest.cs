using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// Attach to any GameObject in the scene to diagnose audio issues.
/// Right-click the component in Inspector to run each test.
/// </summary>
public class AudioDiagnosticTest : MonoBehaviour
{
    [Header("Test Clip — drag any AudioClip here")]
    [SerializeField] private AudioClip testClip;

    [Header("Optional — drag your AudioMixer here")]
    [SerializeField] private AudioMixer mixer;

    [Header("Optional — drag your SoundLibrary SO here")]
    [SerializeField] private SoundLibrary soundLibrary;

    // ── Test 1: Raw AudioSource.PlayOneShot — completely bypasses AudioManager ──
    [ContextMenu("Test 1 — Raw PlayOneShot (no mixer)")]
    private void Test_RawPlayOneShot()
    {
        if (testClip == null) { Debug.LogError("[AudioDiag] Assign a testClip first!"); return; }

        var src = gameObject.AddComponent<AudioSource>();
        src.spatialBlend = 0f;
        src.volume = 1f;
        src.PlayOneShot(testClip, 1f);
        Debug.Log($"[AudioDiag] Test1 fired — clip:{testClip.name} length:{testClip.length:F2}s. Did you hear it?");
        Invoke(nameof(CleanupTempSource), testClip.length + 0.5f);
    }

    // ── Test 1b: Play the SoundLibrary Chop clip DIRECTLY (no AudioManager) ──
    [ContextMenu("Test 1b — SoundLibrary Chop clip, direct PlayOneShot")]
    private void Test_LibraryClipDirect()
    {
        if (soundLibrary == null) { Debug.LogError("[AudioDiag] Assign soundLibrary first!"); return; }
        soundLibrary.Init();
        if (!soundLibrary.TryGet(SoundId.ToolSwing, out var entry)) { Debug.LogError("[AudioDiag] SoundId.ToolSwing not found in library!"); return; }

        var clip = entry.GetRandomClip();
        if (clip == null) { Debug.LogError("[AudioDiag] Chop entry has no clips assigned!"); return; }

        Debug.Log($"[AudioDiag] Test1b — clip:{clip.name}  loadState:{clip.loadState}  length:{clip.length:F2}s  loadType:{clip.loadType}  channels:{clip.channels}");

        var src = gameObject.AddComponent<AudioSource>();
        src.spatialBlend = 0f;
        src.volume = 1f;
        // No pitch change, no mixer group — identical to Test 1
        src.PlayOneShot(clip, 1f);
        Debug.Log("[AudioDiag] Test1b PlayOneShot called. Did you hear it?");
        Invoke(nameof(CleanupTempSource), clip.length + 0.5f);
    }

    // ── Test 2: Play via AudioManager.PlayOnSource ──
    [ContextMenu("Test 2 — Via AudioManager.PlayOnSource (SoundId.ToolSwing)")]
    private void Test_AudioManagerChop()
    {
        if (AudioManager.Instance == null) { Debug.LogError("[AudioDiag] AudioManager.Instance is null — is it in the scene?"); return; }

        var src = gameObject.AddComponent<AudioSource>(); // always fresh, same as Test 1
        src.spatialBlend = 0f;
        src.volume = 1f;

        AudioManager.Instance.PlayOnSource(SoundId.ToolSwing, src);
        Debug.Log("[AudioDiag] Test2 fired — PlayOnSource(Chop). Did you hear it?");
    }

    // ── Test 2b: Same as Test 2 but strips mixer group first ──
    [ContextMenu("Test 2b — PlayOnSource with mixer group REMOVED")]
    private void Test_AudioManagerChopNoMixer()
    {
        if (AudioManager.Instance == null) { Debug.LogError("[AudioDiag] AudioManager.Instance is null!"); return; }

        var src = gameObject.AddComponent<AudioSource>();
        src.spatialBlend = 0f;
        src.volume = 1f;
        src.outputAudioMixerGroup = null; // force bypass any mixer

        AudioManager.Instance.PlayOnSource(SoundId.ToolSwing, src);
        Debug.Log("[AudioDiag] Test2b — mixer stripped. Did you hear it?");
    }

    // ── Test 3: Print all AudioMixer group volumes ──
    [ContextMenu("Test 3 — Print Mixer Group Volumes")]
    private void Test_PrintMixerVolumes()
    {
        if (mixer == null) { Debug.LogWarning("[AudioDiag] No mixer assigned — skipping."); return; }

        string[] paramNames = { "MasterVolume", "SFXVolume", "UIVolume", "AmbientVolume" };
        foreach (var param in paramNames)
        {
            if (mixer.GetFloat(param, out float val))
                Debug.Log($"[AudioDiag] Mixer param '{param}' = {val:F1} dB  ({DecibelToLinear(val):F2} linear)");
            else
                Debug.LogWarning($"[AudioDiag] Mixer param '{param}' NOT EXPOSED — expose it in the AudioMixer asset!");
        }
    }

    // ── Test 4: Force reset all mixer volumes to 0 dB ──
    [ContextMenu("Test 4 — Reset All Mixer Volumes to 0 dB")]
    private void Test_ResetMixerVolumes()
    {
        if (mixer == null) { Debug.LogWarning("[AudioDiag] No mixer assigned."); return; }

        string[] paramNames = { "MasterVolume", "SFXVolume", "UIVolume", "AmbientVolume" };
        foreach (var param in paramNames)
        {
            if (mixer.SetFloat(param, 0f))
                Debug.Log($"[AudioDiag] Reset '{param}' to 0 dB");
            else
                Debug.LogWarning($"[AudioDiag] '{param}' not found — expose it in the AudioMixer!");
        }
    }

    // ── Test 5: Check scene for AudioListener ──
    [ContextMenu("Test 5 — Check AudioListener")]
    private void Test_CheckListener()
    {
        var listener = FindAnyObjectByType<AudioListener>();
        if (listener == null)
        {
            Debug.LogError("[AudioDiag] NO AudioListener found in scene! You cannot hear anything without one.");
            return;
        }
        Debug.Log($"[AudioDiag] AudioListener found on: '{listener.gameObject.name}'  " +
                  $"Volume:{AudioListener.volume}  Pause:{AudioListener.pause}  " +
                  $"Enabled:{listener.enabled}");
    }

    private void CleanupTempSource()
    {
        // Remove the temporary AudioSource added for Test1
        var srcs = GetComponents<AudioSource>();
        if (srcs.Length > 1)
            Destroy(srcs[srcs.Length - 1]);
    }

    private static float DecibelToLinear(float dB) => Mathf.Pow(10f, dB / 20f);
}
