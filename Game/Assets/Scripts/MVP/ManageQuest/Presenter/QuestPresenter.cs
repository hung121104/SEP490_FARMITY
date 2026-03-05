public class QuestPresenter
{
    private QuestView view;
    private IQuestService questService;
    private QuestModel quest;

    public QuestPresenter(
        QuestView view,
        IQuestService questService,
        QuestModel quest)
    {
        this.view = view;
        this.questService = questService;
        this.quest = quest;

        view.OnAcceptQuest += OnAcceptQuest;
    }

    public void ShowQuest()
    {
        view.ShowQuest(quest);
    }

    private void OnAcceptQuest()
    {
        questService.AcceptQuest(quest);
    }
}