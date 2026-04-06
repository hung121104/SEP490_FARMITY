using UnityEngine;

public interface INPCDialogueService
{
    string GetNPCName();
    Sprite GetAvatar();

    bool IsDialogueActive();

    DialogueNode GetCurrentNode();
    bool MoveNext();

    void ChooseOption(int index);

    void Reset();
}
