using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InRoomPlayerItemView : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI playerUsernameText;
    [SerializeField] private Button blacklistButton;

    private string playerId;
    private Action<string> onBlacklistClicked;

    public void Bind(string accountId, string displayName, bool isSelf, bool isAlreadyBlacklisted, Action<string> blacklistCallback)
    {
        playerId = accountId;
        onBlacklistClicked = blacklistCallback;

        if (playerUsernameText != null)
            playerUsernameText.text = string.IsNullOrEmpty(displayName) ? accountId : displayName;

        if (blacklistButton == null)
            return;

        TextMeshProUGUI buttonLabel = blacklistButton.GetComponentInChildren<TextMeshProUGUI>();
        if (buttonLabel != null)
        {
            if (isSelf)
                buttonLabel.text = "You";
            else if (isAlreadyBlacklisted)
                buttonLabel.text = "Blacklisted";
            else
                buttonLabel.text = "Blacklist";
        }

        bool canBlacklist = !isSelf && !isAlreadyBlacklisted;
        blacklistButton.interactable = canBlacklist;
        blacklistButton.onClick.RemoveAllListeners();
        blacklistButton.onClick.AddListener(() =>
        {
            if (canBlacklist)
                onBlacklistClicked?.Invoke(playerId);
        });
    }
}
