using System.Collections.Generic;

public class QuestLogPresenter
{
    private QuestLogView view;
    private IQuestService service;

    public QuestLogPresenter(QuestLogView view, IQuestService service)
    {
        this.view = view;
        this.service = service;
    }

    public void OpenQuestLog()
    {
        List<QuestModel> quests = service.GetActiveQuests();

        view.TogglePanel();
        view.ShowQuestList(quests);
    }
}