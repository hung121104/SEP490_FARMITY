using UnityEngine;

public class NPCDialogueService : INPCDialogueService
{
    private NPCDialogueModel model;
    private int currentNodeIndex = -1;
    private bool isDialogueActive;

    public NPCDialogueService(NPCDialogueModel model)
    {
        this.model = model;
    }

    public string GetNPCName() => model.npcName;
    public Sprite GetAvatar() => model.avatar;
    public bool IsDialogueActive() => isDialogueActive;

    public DialogueNode GetCurrentNode()
    {
        if (!isDialogueActive)
        {
            isDialogueActive = true;
            currentNodeIndex = 0;
        }

        if (model.nodes == null || model.nodes.Count == 0)
            return null;

        if (currentNodeIndex < 0 || currentNodeIndex >= model.nodes.Count)
            return null;

        return model.nodes[currentNodeIndex];
    }

    public bool MoveNext()
    {
        if (!isDialogueActive) return false;

        currentNodeIndex++;

        if (currentNodeIndex >= model.nodes.Count)
        {
            return false; 
        }

        return true;
    }

    public void ChooseOption(int index)
    {
        if (!isDialogueActive) return;

        var node = model.nodes[currentNodeIndex];

        if (node.options == null || node.options.Count == 0)
        {
            Reset();
            return;
        }

        if (index < 0 || index >= node.options.Count)
            return;

        currentNodeIndex = node.options[index].nextNodeIndex;
    }

    public void Reset()
    {
        isDialogueActive = false;
        currentNodeIndex = -1;
    }
}
