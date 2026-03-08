using UnityEngine;
using System;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;

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

        if (quest.reward != null)
        {
            Sprite icon = ItemCatalogService.Instance.GetCachedSprite(quest.reward.itemId);

            if (icon != null)
            {
                dialogueView.ShowReward(icon, quest.reward.quantity);
            }
        }
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