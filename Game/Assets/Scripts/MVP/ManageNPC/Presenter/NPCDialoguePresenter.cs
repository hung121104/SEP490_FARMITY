public class NPCDialoguePresenter
{
    private INPCDialogueService service;
    private NPCDialogueView view;
    private QuestPresenter questPresenter;

    public NPCDialoguePresenter(
      INPCDialogueService service,
      NPCDialogueView view,
      QuestPresenter questPresenter)
    {
        this.service = service;
        this.view = view;
        this.questPresenter = questPresenter;
    }

    public void StartDialogue()
    {
        ShowCurrentNode();
    }

    public void Continue()
    {
        var node = service.GetCurrentNode();

        if (node == null)
        {
            view.Hide();
            service.Reset();
            return;
        }

        //
        if (node.options != null && node.options.Count > 0)
            return;

        bool hasNext = service.MoveNext();

        if (!hasNext)
        {
            view.Hide();
            service.Reset();
            return;
        }

        ShowCurrentNode();
    }


    public void SelectOption(int index)
    {
        var node = service.GetCurrentNode();

        if (node == null) return;

        if (node.options == null || index >= node.options.Count)
            return;

        var option = node.options[index];

        // QUEST OPTION
        if (option.optionText == "Quest")
        {
            questPresenter.ShowQuest();
            return;
        }

        service.ChooseOption(index);
        ShowCurrentNode();
    }


    private void ShowCurrentNode()
    {
        var node = service.GetCurrentNode();

        if (node == null)
        {
            view.Hide();
            service.Reset();
            return;
        }

        view.ShowNode(
            service.GetNPCName(),
            node,
            service.GetAvatar()
        );
    }
    public DialogueNode GetCurrentNode()
    {
        return service.GetCurrentNode();
    }


    public bool IsDialogueActive()
    {
        return service.IsDialogueActive();
    }
}
