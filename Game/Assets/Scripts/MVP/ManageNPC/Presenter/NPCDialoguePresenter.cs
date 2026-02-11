public class NPCDialoguePresenter
{
    private INPCDialogueService service;
    private NPCDialogueView view;

    private string currentDialogue;

    public NPCDialoguePresenter(
        INPCDialogueService service,
        NPCDialogueView view)
    {
        this.service = service;
        this.view = view;
    }

    public void OnInteract()
    {
        currentDialogue = service.GetNextDialogue();

        if (currentDialogue == null)
        {
            view.Hide();
            return;
        }

        view.Show(
            service.GetNPCName(),
            currentDialogue,
            service.GetAvatar()
        );
    }

    public void ShowFullCurrentText()
    {
        view.ShowFullText(currentDialogue);
    }

    public bool IsDialogueActive()
    {
        return service.IsDialogueActive();
    }
}
