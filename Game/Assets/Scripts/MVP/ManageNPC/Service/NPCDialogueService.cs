public class NPCDialogueService : INPCDialogueService

{
    private NPCDialogueModel model;
    private int currentIndex;
    private bool isDialogueActive;

    public NPCDialogueService(NPCDialogueModel model)
    {
        this.model = model;
        Reset();
    }

    public string GetNPCName()
    {
        return model.npcName;
    }

    public UnityEngine.Sprite GetAvatar()
    {
        return model.avatar;
    }

    public bool IsDialogueActive()
    {
        return isDialogueActive;
    }

    public string GetNextDialogue()
    {
        if (!isDialogueActive)
        {
            isDialogueActive = true;
            currentIndex = 0;
            return model.dialogues[currentIndex];
        }

        currentIndex++;

        if (currentIndex >= model.dialogues.Length)
        {
            Reset();
            return null;
        }

        return model.dialogues[currentIndex];
    }

    private void Reset()
    {
        isDialogueActive = false;
        currentIndex = -1;
    }
}
