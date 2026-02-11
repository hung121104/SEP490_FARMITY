using UnityEngine;

public interface INPCDialogueService
{
    string GetNPCName();
    Sprite GetAvatar();
    bool IsDialogueActive();
    string GetNextDialogue();
}
