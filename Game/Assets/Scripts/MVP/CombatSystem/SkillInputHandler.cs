using UnityEngine;

/// <summary>
/// Handles player input during skill execution (confirm/cancel)
/// </summary>
public class SkillInputHandler : MonoBehaviour
{
    [SerializeField] private KeyCode confirmKey = KeyCode.Space;
    [SerializeField] private KeyCode cancelKey = KeyCode.Escape;

    private bool isWaitingForInput = false;
    private bool inputReceived = false;
    private bool inputConfirmed = false;

    private void Update()
    {
        if (!isWaitingForInput)
            return;

        if (Input.GetKeyDown(confirmKey))
        {
            inputReceived = true;
            inputConfirmed = true;
        }
        else if (Input.GetKeyDown(cancelKey))
        {
            inputReceived = true;
            inputConfirmed = false;
        }
    }

    /// <summary>
    /// Wait for player to confirm or cancel
    /// </summary>
    public void StartWaiting()
    {
        isWaitingForInput = true;
        inputReceived = false;
        inputConfirmed = false;
    }

    /// <summary>
    /// Check if player has made a decision
    /// </summary>
    public bool HasInput() => inputReceived;

    /// <summary>
    /// Get whether player confirmed (true) or cancelled (false)
    /// </summary>
    public bool IsConfirmed() => inputConfirmed;

    /// <summary>
    /// Stop waiting for input
    /// </summary>
    public void StopWaiting()
    {
        isWaitingForInput = false;
    }

    public KeyCode GetConfirmKey() => confirmKey;
    public KeyCode GetCancelKey() => cancelKey;
}