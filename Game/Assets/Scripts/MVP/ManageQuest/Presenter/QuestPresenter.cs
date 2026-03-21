using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class QuestPresenter
{
    private QuestView view;
    private IQuestService service;
    private QuestModel quest;
    private IInventoryService inventory;
    private string npcName;
    private Sprite avatar;

    public QuestPresenter(QuestView view, IQuestService service, IInventoryService inventory, string npcName, Sprite avatar)
    {
        this.view = view;
        this.service = service;
        this.inventory = inventory;
        this.npcName = npcName;
        this.avatar = avatar;
        view.OnAccept += AcceptQuest;
    }

    public bool TryPickRandomQuest()
    {
        if (!QuestCatalogService.Instance.IsReady) return false;

      
        var availableQuests = QuestCatalogService.Instance.GetAllQuests()
            .Where(q => q.NPCName == npcName && !service.HasQuest(q.questId))
            .ToList();

        if (availableQuests.Count == 0) return false;

     
        float totalWeight = availableQuests.Sum(q => q.Weight);
        float randomValue = Random.Range(0, totalWeight);
        float cumulativeWeight = 0;

        QuestCatalogData selected = availableQuests[0];
        foreach (var q in availableQuests)
        {
            cumulativeWeight += q.Weight;
            if (randomValue <= cumulativeWeight)
            {
                selected = q;
                break;
            }
        }

      
        this.quest = new QuestModel
        {
            questId = selected.questId,
            questName = selected.questName,
            description = selected.description,
            npcName = selected.NPCName,
            reward = selected.reward,
            objectives = selected.objectives,
            status = QuestStatus.NotAccepted
        };

        return true;
    }

    public void ShowQuest()
    {
        if (quest != null) view.ShowQuest(quest, npcName, avatar);
    }

    public void AcceptQuest()
    {
        if (quest != null) service.AcceptQuest(quest, inventory);
    }
}