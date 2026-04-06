using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BookPanelController : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private List<PanelEntry> panels = new List<PanelEntry>();

    [Header("Panel Flow")]
    [SerializeField] private Animator pageAnimator;
    [SerializeField] private int initialPanelIndex = 0;
    [SerializeField] private bool showInitialPanelOnAwake = true;

    /// <summary>Fired when any panel switch starts, with the target panel's turn animation.</summary>
    public event Action<BookAnimation> OnPanelOpened;

    private Coroutine _switchPanelCoroutine;
    private int _currentPanelIndex = -1;

    private void Awake()
    {
        BindButtons();

        if (showInitialPanelOnAwake)
            ShowPanelImmediate(initialPanelIndex);
    }

    private void BindButtons()
    {
        for (int i = 0; i < panels.Count; i++)
        {
            int capturedIndex = i;
            PanelEntry entry = panels[capturedIndex];

            if (entry.openButton != null)
                entry.openButton.onClick.AddListener(() => ShowPanel(capturedIndex));

            if (entry.backButton != null)
                entry.backButton.onClick.AddListener(() => ShowPanel(entry.backTargetIndex));
        }
    }

    /// <summary>Show a panel by index programmatically (e.g. from a presenter).</summary>
    public void ShowPanel(int index)
    {
        if (!IsValidIndex(index)) return;
        if (_currentPanelIndex == index) return;

        if (_switchPanelCoroutine != null)
            StopCoroutine(_switchPanelCoroutine);

        _switchPanelCoroutine = StartCoroutine(SwitchPanelRoutine(index));
    }

    private IEnumerator SwitchPanelRoutine(int targetIndex)
    {
        int previousPanelIndex = _currentPanelIndex;
        BookAnimation panelTurnAnimation = panels[targetIndex].turningPageAnimation;

        // Keep the current panel visible and show target panel before animation starts.
        ShowPanelsForTransition(previousPanelIndex, targetIndex);
        OnPanelOpened?.Invoke(panelTurnAnimation);

        // Let subscribers set animation triggers before reading animator clip data.
        yield return null;

        float waitTime = GetPageAnimationWaitTime();
        if (waitTime > 0f)
            yield return new WaitForSeconds(waitTime);

        // After animation finishes, keep only the target panel visible.
        ShowPanelImmediate(targetIndex);
        _switchPanelCoroutine = null;
    }

    private void ShowPanelsForTransition(int previousIndex, int targetIndex)
    {
        for (int i = 0; i < panels.Count; i++)
        {
            bool isPrevious = i == previousIndex;
            bool isTarget = i == targetIndex;
            CanvasGroup canvasGroup = panels[i].canvasGroup;

            if (canvasGroup == null)
                continue;

            if (isPrevious || isTarget)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }
            else
            {
                canvasGroup.Hide();
            }
        }
    }

    private float GetPageAnimationWaitTime()
    {
        if (pageAnimator == null) return 0f;
        if (pageAnimator.runtimeAnimatorController == null) return 0f;

        if (pageAnimator.IsInTransition(0))
        {
            AnimatorClipInfo[] nextClips = pageAnimator.GetNextAnimatorClipInfo(0);
            if (nextClips.Length > 0 && nextClips[0].clip != null)
                return nextClips[0].clip.length;
        }

        AnimatorClipInfo[] currentClips = pageAnimator.GetCurrentAnimatorClipInfo(0);
        if (currentClips.Length > 0 && currentClips[0].clip != null)
            return currentClips[0].clip.length;

        float stateLength = pageAnimator.GetCurrentAnimatorStateInfo(0).length;
        return stateLength > 0f ? stateLength : 0f;
    }

    private void ShowPanelImmediate(int index)
    {
        if (!IsValidIndex(index)) return;

        foreach (PanelEntry panel in panels)
            panel.canvasGroup?.Hide();

        panels[index].canvasGroup?.Show();
        _currentPanelIndex = index;
    }

    private bool IsValidIndex(int index)
    {
        return index >= 0 && index < panels.Count;
    }
    public void HideAllPanels()
    {
        foreach (PanelEntry panel in panels)
            panel.canvasGroup?.Hide();

        _currentPanelIndex = -1;
    }

     private void OnEnable()
     {
         if (panels.Count == 0)
         {
             Debug.LogWarning("[BookPanelController] No panels assigned. Please add panel entries in the Inspector.");
         }
     }

    [Serializable]
    public class PanelEntry
    {
        public CanvasGroup canvasGroup;
        public Button openButton;
        public Button backButton;
        public int backTargetIndex = -1;
        public BookAnimation turningPageAnimation = BookAnimation.TurnRToL;
    }
}
