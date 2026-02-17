public class NPCDialoguePresenter
{
    private INPCDialogueService service;
    private NPCDialogueView view;

    public NPCDialoguePresenter(
        INPCDialogueService service,
        NPCDialogueView view)
    {
        this.service = service;
        this.view = view;
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
