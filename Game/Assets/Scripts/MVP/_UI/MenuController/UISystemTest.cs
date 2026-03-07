using UnityEngine;

/// <summary>
/// Manual test helper for UIPanelManager system.
/// Attach to any GameObject in the scene and use the buttons in the Inspector
/// (via Context Menu) or press the test hotkeys at runtime.
///
/// HOTKEYS (only active when this component is enabled):
///   T + 1  →  Toggle "Inventory"
///   T + 2  →  Toggle "Crafting"
///   T + 0  →  Close all panels
///   T + 9  →  Close topmost panel
///   T + I  →  Print registered panels to Console
/// </summary>
public class UISystemTest : MonoBehaviour
{
    [Header("Test Panel IDs")]
    [SerializeField] private string testPanelA = "Inventory";
    [SerializeField] private string testPanelB = "Crafting";
    [SerializeField] private string testPanelC = "Cooking";
    [SerializeField] private KeyCode toggleKey = KeyCode.O;


    private bool tHeld = false;

    private void Update()
    {
        tHeld = Input.GetKey(toggleKey);
        if (!tHeld) return;

        if (Input.GetKeyDown(KeyCode.Alpha1)) TogglePanel(testPanelA);
        if (Input.GetKeyDown(KeyCode.Alpha2)) TogglePanel(testPanelB);
        if (Input.GetKeyDown(KeyCode.Alpha3)) TogglePanel(testPanelC);
        if (Input.GetKeyDown(KeyCode.Alpha0)) CloseAll();
        if (Input.GetKeyDown(KeyCode.Alpha9)) CloseTopmost();
        if (Input.GetKeyDown(KeyCode.I))      PrintStatus();
    }

    // ── Context Menu (right-click component in Inspector) ─────────────────────

    [ContextMenu("Test / Toggle Inventory")]
    private void ToggleInventory() => TogglePanel(testPanelA);

    [ContextMenu("Test / Toggle Crafting")]
    private void ToggleCrafting() => TogglePanel(testPanelB);

    [ContextMenu("Test / Toggle Cooking")]
    private void ToggleCooking() => TogglePanel(testPanelC);

    [ContextMenu("Test / Close All")]
    private void CloseAll() => UIPanelManager.Instance?.CloseAll();

    [ContextMenu("Test / Close Topmost")]
    private void CloseTopmost() => UIPanelManager.Instance?.CloseTopmost();

    [ContextMenu("Test / Print Status")]
    private void PrintStatus()
    {
        if (UIPanelManager.Instance == null)
        {
            Debug.LogError("[UISystemTest] UIPanelManager.Instance is NULL — is it in the scene?");
            return;
        }

        bool anyOpen = UIPanelManager.Instance.IsAnyPanelOpen();
        bool aOpen   = UIPanelManager.Instance.IsPanelOpen(testPanelA);
        bool bOpen   = UIPanelManager.Instance.IsPanelOpen(testPanelB);
        bool cOpen   = UIPanelManager.Instance.IsPanelOpen(testPanelC);
        Debug.Log($"[UISystemTest] ── Panel Status ──────────────────────\n" +
                  $"  Any panel open : {anyOpen}\n" +
                  $"  {testPanelA,-20}: {(aOpen  ? "OPEN" : "closed")}\n" +
                  $"  {testPanelB,-20}: {(bOpen  ? "OPEN" : "closed")}\n" +
                  $"  {testPanelC,-20}: {(cOpen  ? "OPEN" : "closed")}\n" +
                  $"─────────────────────────────────────────────────");
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    private void TogglePanel(string panelId)
    {
        if (UIPanelManager.Instance == null)
        {
            Debug.LogError("[UISystemTest] UIPanelManager.Instance is NULL — is it in the scene?");
            return;
        }
        UIPanelManager.Instance.Toggle(panelId);
    }

    // ── OnAnyPanelActiveChanged listener test ─────────────────────────────────

    private void OnEnable()
    {
        if (UIPanelManager.Instance != null)
            UIPanelManager.Instance.OnAnyPanelActiveChanged += OnUIStateChanged;
    }

    private void OnDisable()
    {
        if (UIPanelManager.Instance != null)
            UIPanelManager.Instance.OnAnyPanelActiveChanged -= OnUIStateChanged;
    }

    private void OnUIStateChanged(bool isOpen)
    {
        Debug.Log($"[UISystemTest] OnAnyPanelActiveChanged → {(isOpen ? "UI OPEN  (disable movement)" : "UI CLOSED (enable movement)")}");
    }
}
