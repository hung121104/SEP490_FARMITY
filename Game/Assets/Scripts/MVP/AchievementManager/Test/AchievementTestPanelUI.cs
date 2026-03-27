using UnityEngine;

/// <summary>
/// Lightweight runtime panel to trigger AchievementTestTrigger scenarios quickly.
/// Attach to any object in development/testing scenes.
/// Toggle panel with F8.
/// </summary>
public class AchievementTestPanelUI : MonoBehaviour
{
    [Header("Panel Settings")]
    [SerializeField] private bool showOnStart = true;
    [SerializeField] private KeyCode toggleKey = KeyCode.F8;
    [SerializeField] private float panelWidth = 430f;
    [SerializeField] private float panelHeight = 600f;

    [Header("References")]
    [SerializeField] private AchievementTestTrigger testTrigger;

    private bool isVisible;
    private Rect panelRect = new Rect(20f, 20f, 430f, 600f);
    private Vector2 scroll;

    private void Awake()
    {
        isVisible = showOnStart;
        panelRect.width = panelWidth;
        panelRect.height = panelHeight;

        if (testTrigger == null)
            testTrigger = FindObjectOfType<AchievementTestTrigger>();
    }

    private void Update()
    {
        if (Input.GetKeyDown(toggleKey))
            isVisible = !isVisible;
    }

    private void OnGUI()
    {
        if (!isVisible) return;

        panelRect = GUI.Window(19901, panelRect, DrawWindow, "Achievement Test Panel");
    }

    private void DrawWindow(int windowId)
    {
        GUILayout.Label("Toggle key: " + toggleKey);

        if (testTrigger == null)
        {
            GUILayout.Space(6f);
            GUILayout.Label("AchievementTestTrigger not found.");
            if (GUILayout.Button("Find Trigger"))
                testTrigger = FindObjectOfType<AchievementTestTrigger>();

            GUI.DragWindow(new Rect(0f, 0f, panelRect.width, 24f));
            return;
        }

        scroll = GUILayout.BeginScrollView(scroll, false, true);

        DrawSection("Utilities");
        DrawButton("Print Checklist (JSON)", testTrigger.FlowPrintChecklist);
        DrawButton("Simulate Login", testTrigger.SimulateLoginSuccess);
        DrawButton("Open Achievement Panel", testTrigger.OpenAchievementPanel);
        DrawButton("Print Achievement State", testTrigger.PrintAchievementState);

        GUILayout.Space(8f);
        DrawSection("Recommended Flow");
        DrawButton("Flow 1: Login + Fetch", testTrigger.FlowLoginAndFetch);
        DrawButton("Flow 2: Kill Validation", testTrigger.FlowKillValidation);
        DrawButton("Flow 3: Exact Pair Completions", testTrigger.FlowExactPairCompletions);
        DrawButton("Flow 4: Multi Requirement + Mixed Batch", testTrigger.FlowMultiRequirementAndMixedBatch);
        DrawButton("Flow 5: No-Match + OverTarget + Stress", testTrigger.FlowRobustness);
        DrawButton("Flow 6: Full Regression Smoke", testTrigger.FlowFullRegressionSmoke);

        GUILayout.Space(8f);
        DrawSection("Quick Actions");
        if (GUILayout.Button("Run Flow 1 -> 2 -> 3"))
        {
            testTrigger.FlowLoginAndFetch();
            testTrigger.FlowKillValidation();
            testTrigger.FlowExactPairCompletions();
        }

        if (GUILayout.Button("Run Full Regression + Print State"))
        {
            testTrigger.FlowFullRegressionSmoke();
            testTrigger.PrintAchievementState();
        }

        GUILayout.EndScrollView();

        GUILayout.Space(6f);
        if (GUILayout.Button("Close Panel"))
            isVisible = false;

        GUI.DragWindow(new Rect(0f, 0f, panelRect.width, 24f));
    }

    private static void DrawSection(string title)
    {
        GUILayout.Label("---------------------------------------------");
        GUILayout.Label(title);
    }

    private static void DrawButton(string label, System.Action action)
    {
        if (GUILayout.Button(label))
            action?.Invoke();
    }
}
