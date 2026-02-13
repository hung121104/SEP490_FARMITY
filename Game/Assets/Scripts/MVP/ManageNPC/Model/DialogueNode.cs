using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class DialogueOption
{
    public string optionText;
    public int nextNodeIndex;
}

[System.Serializable]
public class DialogueNode
{
    [TextArea]
    public string dialogueText;

    public List<DialogueOption> options;
}
