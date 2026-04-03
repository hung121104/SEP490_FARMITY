using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Auto-generates one rebind row per binding in the Player action map.
///
/// Inspector setup:
///   1. Assign rowPrefab  – a prefab with RebindUIController on it.
///   2. Assign container  – the scroll view's Content transform.
///   3. (Optional) Add action names to skipActionNames to exclude them.
///   4. Enable showCompositeParts if you want WASD / Arrow Keys individually rebindable.
///
/// The panel rebuilds itself automatically on Start.
/// Call Build() manually if you need to rebuild after options change at runtime.
/// </summary>
public class KeybindPanelController : MonoBehaviour
{
    [Header("Row Prefab")]
    [Tooltip("Prefab that has RebindUIController on it. Should include an actionLabel TMP_Text, bindingLabel TMP_Text, and rebindButton.")]
    [SerializeField] private GameObject rowPrefab;

    [Tooltip("Parent transform where rows are spawned (e.g., the ScrollView's Content object).")]
    [SerializeField] private Transform container;

    [Header("Filtering")]
    [Tooltip("Action names to skip (e.g. HotbarSlot1, ScrollItem). Leave empty to include all.")]
    [SerializeField] private string[] skipActionNames = new string[0];

    [Tooltip("Show composite-part bindings (WASD up/down/left/right). If false they are skipped.")]
    [SerializeField] private bool showCompositeParts = true;

    // ───── Runtime ─────
    private readonly List<RebindUIController> _rows = new List<RebindUIController>();

    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // Lifecycle
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

    private void Start()
    {
        Build();
    }

    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // Public API
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

    /// <summary>
    /// Destroys all existing rows and rebuilds them from the current Player action map.
    /// </summary>
    public void Build()
    {
        // Clean up old rows
        foreach (var row in _rows)
            if (row != null) Destroy(row.gameObject);
        _rows.Clear();

        if (rowPrefab == null || container == null)
        {
            Debug.LogError("[KeybindPanel] rowPrefab or container is not assigned.");
            return;
        }

        if (InputManager.Instance == null)
        {
            Debug.LogError("[KeybindPanel] InputManager.Instance is null.");
            return;
        }

        var skipSet   = new HashSet<string>(skipActionNames);
        var playerMap = InputManager.Instance.Actions.Player.Get();

        foreach (var action in playerMap.actions)
        {
            if (skipSet.Contains(action.name)) continue;

            for (int i = 0; i < action.bindings.Count; i++)
            {
                var binding = action.bindings[i];

                // Skip composite parent entries (the container e.g. "2DVector")
                if (binding.isComposite) continue;

                // Optionally skip individual keys that are parts of a composite (e.g. WASD)
                if (binding.isPartOfComposite && !showCompositeParts) continue;

                // Build a human-readable label: "Move (up)" or just "Interact"
                string displayLabel = binding.isPartOfComposite
                    ? $"{FormatActionName(action.name)} ({binding.name})"
                    : FormatActionName(action.name);

                var go = Instantiate(rowPrefab, container);
                var row = go.GetComponent<RebindUIController>();
                if (row == null)
                {
                    Debug.LogError("[KeybindPanel] rowPrefab is missing a RebindUIController component.");
                    Destroy(go);
                    continue;
                }

                row.Initialize(action.name, i, displayLabel);
                _rows.Add(row);
            }
        }

        Debug.Log($"[KeybindPanel] Built {_rows.Count} keybind rows.");
    }

    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // Helpers
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

    /// <summary>
    /// Inserts a space before each upper-case letter so "OpenInventory" → "Open Inventory".
    /// </summary>
    private static string FormatActionName(string name)
    {
        if (string.IsNullOrEmpty(name)) return name;
        var sb = new System.Text.StringBuilder();
        sb.Append(name[0]);
        for (int i = 1; i < name.Length; i++)
        {
            if (char.IsUpper(name[i])) sb.Append(' ');
            sb.Append(name[i]);
        }
        return sb.ToString();
    }
}
