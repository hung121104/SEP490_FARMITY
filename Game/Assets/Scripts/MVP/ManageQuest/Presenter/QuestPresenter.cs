using UnityEngine;

public class QuestPresenter
{
    private QuestView view;
    private IQuestService service;
    private QuestModel quest;
    private IInventoryService inventory;
    private string npcName;
    private Sprite avatar;

    public QuestPresenter(
     QuestView view,
     IQuestService service,
     IInventoryService inventory,
     QuestModel quest,
     string npcName,
     Sprite avatar)
    {
        this.view = view;
        this.service = service;
        this.quest = quest;
        this.npcName = npcName;
        this.avatar = avatar;
        this.inventory = inventory;
        view.OnAccept += AcceptQuest;
    }

    public void ShowQuest()
    {
        view.ShowQuest(quest, npcName, avatar);
    }

    public void AcceptQuest()
    {
        service.AcceptQuest(quest, inventory);
    }
}