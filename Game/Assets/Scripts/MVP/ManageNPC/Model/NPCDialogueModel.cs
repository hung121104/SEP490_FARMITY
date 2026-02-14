using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "NPCDialogueData",
    menuName = "NPC/Dialogue Data"
)]
public class NPCDialogueModel : ScriptableObject
{
    public string npcName;
    public Sprite avatar;

    public List<DialogueNode> nodes;
}
