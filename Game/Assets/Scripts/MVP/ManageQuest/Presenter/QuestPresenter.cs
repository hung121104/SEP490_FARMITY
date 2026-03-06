using UnityEngine;

public class QuestPresenter
{
    private QuestView view;
    private IQuestService service;
    private QuestModel quest;

    private string npcName;
    private Sprite avatar;

    public QuestPresenter(
        QuestView view,
        IQuestService service,
        QuestModel quest,
        string npcName,
        Sprite avatar)
    {
        this.view = view;
        this.service = service;
        this.quest = quest;
        this.npcName = npcName;
        this.avatar = avatar;

        view.OnAccept += AcceptQuest;
    }

    public void ShowQuest()
    {
        view.ShowQuest(quest, npcName, avatar);
    }

    public void AcceptQuest()
    {
        service.AcceptQuest(quest);
    }
}