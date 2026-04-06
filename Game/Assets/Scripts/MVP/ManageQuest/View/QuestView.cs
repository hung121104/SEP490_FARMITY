using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// Render-only DTO passed from QuestPresenter to QuestView.
/// View never touches QuestModel directly.
/// </summary>
public class QuestDisplayData
{
    public string questName;
    public string description;
    public string npcName;
    public Sprite avatar;
    public Sprite rewardIcon;
    public int    rewardQuantity;
    public bool   hasReward;
}

/// <summary>
/// Shared UI surface for quest display.
/// Does NOT hold a presenter reference — each QuestPresenter subscribes/unsubscribes
/// its own handler to OnAccept per-session to avoid multi-NPC overwrite.
/// </summary>
public class QuestView : MonoBehaviour
{
    [SerializeField] private NPCDialogueView dialogueView;

    public event Action OnAccept;
    public event Action OnBack;

    // ── Called by Presenter — pure UI rendering, zero business logic ────────
    public void Render(QuestDisplayData data)
    {
        DialogueNode node = new DialogueNode
        {
            dialogueText = data.questName + "\n\n" + data.description,
            options = new List<DialogueOption>
            {
                new DialogueOption { optionText = "Accept", nextNodeIndex = -1 },
                new DialogueOption { optionText = "Back",   nextNodeIndex = -1 }
            }
        };

        dialogueView.ShowNode(data.npcName, node, data.avatar);

        if (data.hasReward && data.rewardIcon != null)
            dialogueView.ShowReward(data.rewardIcon, data.rewardQuantity);
    }

    // ── Unity button callbacks — wire in Inspector ───────────────────────
    public void Accept() => OnAccept?.Invoke();
    public void Back()   => OnBack?.Invoke();
}