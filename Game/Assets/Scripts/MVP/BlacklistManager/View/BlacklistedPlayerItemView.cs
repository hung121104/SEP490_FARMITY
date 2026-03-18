using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BlacklistedPlayerItemView : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI playerUsernameText;
    [SerializeField] private Button removeButton;

    private string playerId;
    private Action<string> onRemoveClicked;

    public void Bind(string accountId, string displayName, Action<string> removeCallback)
    {
        playerId = accountId;
        onRemoveClicked = removeCallback;

        if (playerUsernameText != null)
            playerUsernameText.text = string.IsNullOrEmpty(displayName) ? accountId : displayName;

        if (removeButton == null)
            return;

        removeButton.onClick.RemoveAllListeners();
        removeButton.onClick.AddListener(() => onRemoveClicked?.Invoke(playerId));
    }
}
