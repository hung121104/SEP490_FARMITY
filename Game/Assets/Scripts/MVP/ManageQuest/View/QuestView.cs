using UnityEngine;
using System;
using System.Collections.Generic;

public class QuestView : MonoBehaviour
{
    [SerializeField] private NPCDialogueView dialogueView;

    public Action OnAccept;
    public Action OnBack;

    public void ShowQuest(QuestModel quest, string npcName, Sprite avatar)
    {
        DialogueNode node = new DialogueNode();

        node.dialogueText =
            quest.questName + "\n\n" +
            quest.description;

        node.options = new List<DialogueOption>()
        {
            new DialogueOption { optionText = "Accept", nextNodeIndex = -1 },
            new DialogueOption { optionText = "Back", nextNodeIndex = -1 }
        };

        dialogueView.ShowNode(npcName, node, avatar);
    }

    public void Accept()
    {
        OnAccept?.Invoke();
    }

    public void Back()
    {
        OnBack?.Invoke();
    }
}