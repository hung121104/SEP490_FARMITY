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
        DrawButton("Simulate Login", testTrigger.SimulateLoginSuccess);
        DrawButton("Open Achievement Panel", testTrigger.OpenAchievementPanel);
        DrawButton("Print Achievement State", testTrigger.PrintAchievementState);

        GUILayout.Space(8f);
        DrawSection("Core Scenarios");
        DrawButton("Scenario A: Mixed Types Same Window", testTrigger.ScenarioMixedTypesSameWindow);
        DrawButton("Scenario B: Multi Requirement", testTrigger.ScenarioMultiRequirement);
        DrawButton("Scenario C: Generic + Specific Kill Pair", testTrigger.ScenarioGenericAndSpecificPair);
        DrawButton("Scenario D: Non Matching Specific ID", testTrigger.ScenarioNonMatchingSpecificId);
        DrawButton("Scenario E: Over Target + Noop Safety", testTrigger.ScenarioOverTargetProgress);
        DrawButton("Scenario F: Skeleton Specific", testTrigger.ScenarioSkeletonSpecific);
        DrawButton("Scenario G: Stress Single Event", testTrigger.ScenarioStressSingleEvent);
        DrawButton("Scenario H: Full Coverage Smoke", testTrigger.ScenarioFullCoverageSmoke);

        GUILayout.Space(8f);
        DrawSection("Quick Actions");
        if (GUILayout.Button("Run A -> B -> C"))
        {
            testTrigger.ScenarioMixedTypesSameWindow();
            testTrigger.ScenarioMultiRequirement();
            testTrigger.ScenarioGenericAndSpecificPair();
        }

        if (GUILayout.Button("Run Full Smoke + Print State"))
        {
            testTrigger.ScenarioFullCoverageSmoke();
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
